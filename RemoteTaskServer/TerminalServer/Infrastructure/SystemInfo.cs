#region

using System;
using UlteriusServer.TerminalServer.Infrastructure;

#endregion

namespace UlteriusServer.TerminalServer.Infrastructure
{
    public class SystemInfo : ISystemInfo
    {
        public DateTime Now()
        {
            return DateTime.Now;
        }

        public Guid Guid()
        {
            return System.Guid.NewGuid();
        }
    }
}