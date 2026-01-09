// This code is licensed under MIT license (see LICENSE for details)

namespace Cosmos.Kernel.HAL.X64.Devices.Usb;

/// <summary>
/// Tracks EHCI USB host controllers (x64) without virtual/interface dispatch.
/// </summary>
public static class UsbManager
{
    private static EhciController?[]? _controllers;
    private static int _controllerCount;
    private static bool _initialized;

    public static bool IsInitialized => _initialized;

    public static int ControllerCount => _controllerCount;

    public static EhciController? PrimaryController =>
        _controllers != null && _controllerCount > 0 ? _controllers[0] : null;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _controllers = new EhciController[4];
        _controllerCount = 0;
        _initialized = true;
    }

    public static void RegisterController(EhciController controller)
    {
        if (!_initialized || _controllers == null || controller == null)
        {
            return;
        }

        if (_controllerCount >= _controllers.Length)
        {
            return;
        }

        _controllers[_controllerCount++] = controller;
    }

    public static EhciController? GetController(int index)
    {
        if (_controllers == null || index < 0 || index >= _controllerCount)
        {
            return null;
        }

        return _controllers[index];
    }

    /// <summary>
    /// Invoke Poll on all registered controllers for simple change detection.
    /// </summary>
    public static void PollControllers()
    {
        if (_controllers == null)
        {
            return;
        }

        for (int i = 0; i < _controllerCount; i++)
        {
            _controllers[i]?.Poll();
        }
    }
}
