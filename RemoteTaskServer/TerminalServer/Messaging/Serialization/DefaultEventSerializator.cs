#region

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Serialization
{
    public class DefaultEventSerializator : IEventSerializator
    {
        public void Serialize(IConnectionEvent eventObject, Stream output)
        {
            var serializer = new JsonSerializer();
            serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();

            var json = JObject.FromObject(eventObject, serializer);
            json.Add("type", new JValue(eventObject.GetType().Name));
            json.Remove("connectionId");

            using (var writer = new StreamWriter(output, Encoding.UTF8, 4096, true))
            using (var jwriter = new JsonTextWriter(writer))
            {
                json.WriteTo(jwriter);
            }
        }

        public IConnectionRequest Deserialize(Stream source, out Type type)
        {
            using (var reader = new StreamReader(source, Encoding.UTF8))
            {
                var json = JObject.Load(new JsonTextReader(reader));
                var typeName = json.Property("type").Value.ToString();
                return Build(typeName, json, out type);
            }
        }

        private IConnectionRequest Build(string typeName, JObject json, out Type type)
        {
            switch (typeName)
            {
                case "CreateTerminalRequest":
                    type = typeof (CreateTerminalRequest);
                    return new CreateTerminalRequest
                    {
                        TerminalType = json.Property("terminalType").Value.ToString(),
                        CorrelationId = json.Property("correlationId").Value.ToString()
                    };
                case "TerminalInputRequest":
                    type = typeof (TerminalInputRequest);
                    return new TerminalInputRequest
                    {
                        TerminalId = Guid.Parse(json.Property("terminalId").Value.ToString()),
                        Input = json.Property("input").Value.ToString(),
                        CorrelationId = int.Parse(json.Property("correlationId").Value.ToString())
                    };
                case "CloseTerminalRequest":
                    type = typeof (CloseTerminalRequest);
                    return new CloseTerminalRequest
                    {
                        TerminalId = Guid.Parse(json.Property("terminalId").Value.ToString())
                    };
            }
            type = null;
            throw new IOException("There is no suitable deserialization for this object");
        }
    }
}