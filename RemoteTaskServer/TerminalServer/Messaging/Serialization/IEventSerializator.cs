#region

using System;
using System.IO;
using UlteriusServer.TerminalServer.Messaging.Connection;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Serialization
{
    public interface IEventSerializator
    {
        void Serialize(IConnectionEvent eventObject, Stream output);
        IConnectionRequest Deserialize(Stream source, out Type type);
    }
}