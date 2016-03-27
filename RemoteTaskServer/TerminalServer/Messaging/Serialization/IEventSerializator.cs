#region

using System;
using System.IO;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Serialization
{
    public interface IEventSerializator
    {
        void Serialize(Guid connectionId, IConnectionEvent eventObject, Stream output);
        IConnectionRequest Deserialize(Guid connectionId, Stream source, out Type type);
    }
}