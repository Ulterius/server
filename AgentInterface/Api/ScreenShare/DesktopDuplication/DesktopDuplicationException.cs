using System;

namespace AgentInterface.Api.ScreenShare.DesktopDuplication
{
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException(string message)
            : base(message) { }
    }
}
