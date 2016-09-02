namespace UlteriusServer.Api.Win32.WindowsInput
{
    /// <summary>
    ///     The contract for a service that simulates Keyboard and Mouse input and Hardware Input Device state detection for
    ///     the Windows Platform.
    /// </summary>
    public interface IInputSimulator
    {
        /// <summary>
        ///     Gets the <see cref="IKeyboardSimulator" /> instance for simulating Keyboard input.
        /// </summary>
        /// <value>The <see cref="IKeyboardSimulator" /> instance.</value>
        IKeyboardSimulator Keyboard { get; }

        /// <summary>
        ///     Gets the <see cref="IMouseSimulator" /> instance for simulating Mouse input.
        /// </summary>
        /// <value>The <see cref="IMouseSimulator" /> instance.</value>
        IMouseSimulator Mouse { get; }

        /// <summary>
        ///     Gets the <see cref="UlteriusServer.Api.Win32.WindowsInput.IInputDeviceStateAdaptor" /> instance for determining the state of the various input devices.
        /// </summary>
        /// <value>The <see cref="UlteriusServer.Api.Win32.WindowsInput.IInputDeviceStateAdaptor" /> instance.</value>
        UlteriusServer.Api.Win32.WindowsInput.IInputDeviceStateAdaptor InputDeviceState { get; }
    }
}