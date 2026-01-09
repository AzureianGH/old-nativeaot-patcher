// This code is licensed under MIT license (see LICENSE for details)

namespace Cosmos.Kernel.HAL.Interfaces.Devices;

/// <summary>
/// Delegate for USB port state changes.
/// </summary>
/// <param name="port">Zero-based port index on the controller.</param>
/// <param name="connected">True when a device is present.</param>
public delegate void UsbPortChangedHandler(byte port, bool connected);

/// <summary>
/// Interface for USB host controllers.
/// </summary>
public interface IUsbController
{
    /// <summary>
    /// Gets a short name for the controller implementation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the controller type (UHCI, OHCI, EHCI, XHCI, etc.).
    /// </summary>
    UsbControllerType ControllerType { get; }

    /// <summary>
    /// Gets the number of downstream ports exposed by the controller.
    /// </summary>
    byte PortCount { get; }

    /// <summary>
    /// Gets whether the controller completed initialization and is ready for use.
    /// </summary>
    bool Ready { get; }

    /// <summary>
    /// Initialize the controller hardware and route ports.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Reset all available downstream ports.
    /// </summary>
    void ResetPorts();

    /// <summary>
    /// Poll for port state changes when interrupts are unavailable.
    /// </summary>
    void Poll();

    /// <summary>
    /// Gets whether a specific downstream port currently reports a connection.
    /// </summary>
    /// <param name="port">Zero-based port index.</param>
    bool IsPortConnected(byte port);

    /// <summary>
    /// Optional callback invoked when a port connection state changes.
    /// </summary>
    UsbPortChangedHandler? OnPortChanged { get; set; }
}
