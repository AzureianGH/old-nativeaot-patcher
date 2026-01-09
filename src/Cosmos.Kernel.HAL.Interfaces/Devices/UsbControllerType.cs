// This code is licensed under MIT license (see LICENSE for details)

namespace Cosmos.Kernel.HAL.Interfaces.Devices;

/// <summary>
/// USB host controller flavors supported by the HAL.
/// </summary>
public enum UsbControllerType : byte
{
    Unknown = 0,
    Uhci = 1,
    Ohci = 2,
    Ehci = 3,
    Xhci = 4
}
