// This code is licensed under MIT license (see LICENSE for details)

namespace Cosmos.Kernel.HAL.X64.Pci.Enums;

public enum ProgramIf
{
    // MassStorageController:
    SataVendorSpecific = 0x00,
    SataAhci = 0x01,
    SataSerialStorageBus = 0x02,
    SasSerialStorageBus = 0x01,
    NvmNvmhci = 0x01,
    NvmNvmExpress = 0x02,

    // SerialBusController → USB controller programming interface
    UsbUhci = 0x00,
    UsbOhci = 0x10,
    UsbEhci = 0x20,
    UsbXhci = 0x30,
    UsbDevice = 0xFE
}
