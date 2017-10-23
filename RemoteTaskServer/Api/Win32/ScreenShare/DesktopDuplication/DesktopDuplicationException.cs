using System;

namespace UlteriusServer.Api.Win32.ScreenShare.DesktopDuplication
{
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException(string message)
            : base(message) { }
    }
}
