#region

using MassTransit;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers
{
    public interface IRequestHandler<T> : Consumes<T>.Selected where T : class, IConnectionRequest
    {
    }
}