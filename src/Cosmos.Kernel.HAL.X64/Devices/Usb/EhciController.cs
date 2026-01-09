// This code is licensed under MIT license (see LICENSE for details)

using System;
using Cosmos.Kernel.Core;
using Cosmos.Kernel.Core.IO;
using Cosmos.Kernel.HAL.X64.Pci;
using Cosmos.Kernel.HAL.X64.Pci.Enums;

namespace Cosmos.Kernel.HAL.X64.Devices.Usb;

/// <summary>
/// Smol EHCI USB2 host controller driver :3
/// </summary>
public class EhciController : PciDevice
{
    // capability registers
    private const uint REG_HCSPARAMS = 0x04;

    private const uint OPREG_USBCMD = 0x00;
    private const uint OPREG_USBSTS = 0x04;
    private const uint OPREG_USBINTR = 0x08;
    private const uint OPREG_CONFIGFLAG = 0x40;
    private const uint OPREG_PORTSC_BASE = 0x44;

    // Port status/control bits
    private const uint PORTSC_CURRENT_CONNECT = 1 << 0;
    private const uint PORTSC_RESET = 1 << 8;

    private readonly ulong _mmioBase;
    private ulong _operationalBase;
    private byte _capLength;
    private uint _hcsParams;
    private bool[] _portState;

    public string Name => "EHCI USB2 Controller";

    public byte PortCount { get; private set; }

    public bool Ready { get; private set; }

    public EhciController(uint bus, uint slot, uint function) : base(bus, slot, function)
    {
        _mmioBase = BaseAddressBar != null && BaseAddressBar.Length > 0
            ? BaseAddressBar[0].BaseAddress
            : 0;
        _portState = Array.Empty<bool>();
    }

    /// <summary>
    /// Locate EHCI controller on the PCI bus and create a driver instance
    /// </summary>
    public static EhciController? FindAndCreate()
    {
        var device = PciManager.GetDeviceClass(ClassId.SerialBusController, SubclassId.UsbController, ProgramIf.UsbEhci);
        if (device != null && !device.Claimed)
        {
            return new EhciController(device.Bus, device.Slot, device.Function);
        }

        return null;
    }

    public void Initialize()
    {
        if (Ready)
        {
            return;
        }

        if (_mmioBase == 0)
        {
            Serial.Write("[EHCI] MMIO base missing\n");
            return;
        }

        EnableMemory(true);
        EnableBusMaster(true);
        Claimed = true;

        _capLength = Native.MMIO.Read8(_mmioBase);
        _hcsParams = Native.MMIO.Read32(_mmioBase + REG_HCSPARAMS);
        _operationalBase = _mmioBase + _capLength;
        PortCount = (byte)(_hcsParams & 0x0F);
        _portState = PortCount > 0 ? new bool[PortCount] : Array.Empty<bool>();

        // Stop controller and mask interrupts while we configure it.
        WriteOpReg(OPREG_USBCMD, 0);
        WriteOpReg(OPREG_USBINTR, 0);

        // Route all ports to EHCI.
        WriteOpReg(OPREG_CONFIGFLAG, 1);

        ResetPorts();
        CapturePortStates();
        Ready = true;

        Serial.Write("[EHCI] Initialized with ");
        Serial.WriteNumber(PortCount);
        Serial.Write(" ports\n");
    }

    public void ResetPorts()
    {
        if (_operationalBase == 0 || PortCount == 0)
        {
            return;
        }

        for (byte port = 0; port < PortCount; port++)
        {
            uint status = ReadPort(port);
            WritePort(port, status | PORTSC_RESET);

            // Small delay loop to allow reset to assert; replaced later with timer-backed wait
            for (int i = 0; i < 100_000; i++)
            {
                // Intentional empty loop
            }

            status = ReadPort(port) & ~PORTSC_RESET;
            WritePort(port, status);
        }
    }

    public bool IsPortConnected(byte port)
    {
        if (port >= _portState.Length)
        {
            return false;
        }

        return _portState[port];
    }

    public void Poll()
    {
        if (!Ready || PortCount == 0)
        {
            return;
        }

        for (byte port = 0; port < PortCount; port++)
        {
            uint status = ReadPort(port);
            bool connected = (status & PORTSC_CURRENT_CONNECT) != 0;

            if (port < _portState.Length && connected != _portState[port])
            {
                _portState[port] = connected;
            }
        }
    }

    private void CapturePortStates()
    {
        for (byte port = 0; port < PortCount && port < _portState.Length; port++)
        {
            uint status = ReadPort(port);
            _portState[port] = (status & PORTSC_CURRENT_CONNECT) != 0;
        }
    }

    private uint ReadPort(byte port) => ReadOpReg(OPREG_PORTSC_BASE + (uint)port * 4);

    private void WritePort(byte port, uint value) => WriteOpReg(OPREG_PORTSC_BASE + (uint)port * 4, value);

    private uint ReadOpReg(uint offset) => Native.MMIO.Read32(_operationalBase + offset);

    private void WriteOpReg(uint offset, uint value) => Native.MMIO.Write32(_operationalBase + offset, value);
}
