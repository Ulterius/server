#region

using System;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests
{
    public interface ITerminalRequest : IConnectionRequest
    {
        Guid TerminalId { get; set; }
    }
}