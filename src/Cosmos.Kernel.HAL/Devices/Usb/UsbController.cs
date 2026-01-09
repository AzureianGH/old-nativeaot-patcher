// This code is licensed under MIT license (see LICENSE for details)

using Cosmos.Kernel.HAL.Interfaces.Devices;

namespace Cosmos.Kernel.HAL.Devices.Usb;

/// <summary>
/// Base class for USB host controllers.
/// </summary>
public abstract class UsbController : Device, IUsbController
{
    public abstract string Name { get; }

    public abstract UsbControllerType ControllerType { get; }

    public abstract byte PortCount { get; }

    public abstract bool Ready { get; }

    public UsbPortChangedHandler? OnPortChanged { get; set; }

    public abstract void Initialize();

    public abstract void ResetPorts();

    public abstract bool IsPortConnected(byte port);

    public virtual void Poll()
    {
        // Optional for controllers that rely on IRQs
    }

    /// <summary>
    /// Raise the port change callback if one is registered.
    /// </summary>
    /// <param name="port">Port index.</param>
    /// <param name="connected">True when a device is connected.</param>
    protected void NotifyPortChanged(byte port, bool connected)
    {
        OnPortChanged?.Invoke(port, connected);
    }
}
