#region

using System;

#endregion

namespace UlteriusServer.TerminalServer.Infrastructure
{
    public interface ISystemInfo
    {
        DateTime Now();
        Guid Guid();
    }
}