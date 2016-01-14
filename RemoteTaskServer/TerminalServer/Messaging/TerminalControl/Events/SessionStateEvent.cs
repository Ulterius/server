#region

using System;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Events
{
    [Serializable]
    public class SessionStateEvent : IConnectionEvent
    {
        public Guid UserId { get; set; }
        public TerminalDescriptor[] Terminals { get; set; }
        public Guid ConnectionId { get; set; }
    }

    [Serializable]
    public class TerminalDescriptor
    {
        public string TerminalType { get; set; }
        public Guid TerminalId { get; set; }
        public string CurrentPath { get; set; }
    }
}