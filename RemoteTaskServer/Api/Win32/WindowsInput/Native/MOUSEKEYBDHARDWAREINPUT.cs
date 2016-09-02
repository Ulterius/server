#region

using System.Runtime.InteropServices;

#endregion

namespace UlteriusServer.Api.Win32.WindowsInput.Native
{
#pragma warning disable 649
    /// <summary>
    ///     The combined/overlayed structure that includes Mouse, Keyboard and Hardware Input message data (see:
    ///     http://msdn.microsoft.com/en-us/library/ms646270(VS.85).aspx)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Mousekeybdhardwareinput
    {
        /// <summary>
        ///     The <see cref="Mouseinput" /> definition.
        /// </summary>
        [FieldOffset(0)] public Mouseinput Mouse;

        /// <summary>
        ///     The <see cref="Keybdinput" /> definition.
        /// </summary>
        [FieldOffset(0)] public Keybdinput Keyboard;

        /// <summary>
        ///     The <see cref="Hardwareinput" /> definition.
        /// </summary>
        [FieldOffset(0)] public Hardwareinput Hardware;
    }
#pragma warning restore 649
}