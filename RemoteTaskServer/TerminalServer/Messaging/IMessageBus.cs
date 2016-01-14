#region

using System.Threading.Tasks;
using MassTransit;

#endregion

namespace UlteriusServer.TerminalServer.Messaging
{
    public interface IMessageBus
    {
        IServiceBus Queue { get; }
        Task StartAsync();
    }
}