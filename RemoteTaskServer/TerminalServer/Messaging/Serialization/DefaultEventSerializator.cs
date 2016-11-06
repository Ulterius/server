#region

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UlteriusServer.Api.Network.Messages;
using UlteriusServer.TerminalServer.Messaging.Connection;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Requests;
using UlteriusServer.TerminalServer.Session;
using UlteriusServer.Utilities.Extensions;
using UlteriusServer.Utilities.Security;

#endregion

namespace UlteriusServer.TerminalServer.Messaging.Serialization
{
    public class DefaultEventSerializator : IEventSerializator
    {
        public void Serialize(Guid connectionId, IConnectionEvent eventObject, Stream output)
        {
            var serializer = new JsonSerializer {ContractResolver = new CamelCasePropertyNamesContractResolver()};
            UserConnection user;
            ConnectionManager._connections.TryGetValue(connectionId, out user);
            if (user != null)
            {
                var json = JObject.FromObject(eventObject, serializer);
                json.Add("type", new JValue(eventObject.GetType().Name));
                json.Remove("connectionId");
                var jsonString = json.ToString().UnicodeUtf8();
                Console.WriteLine(jsonString);
                if (user.AesShook)
                {
                    var keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(user.AesKey));
                    var iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(user.AesIv));
                    var encrpytedJson = UlteriusAes.Encrypt(jsonString, keybytes, iv);

                    using (var memoryStream = new MemoryStream())
                    {
                        using (var binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8))
                        {
                            var Endpoint = "terminal";
                            binaryWriter.Write(Endpoint.Length);
                            binaryWriter.Write(Encoding.UTF8.GetBytes(Endpoint));
                            binaryWriter.Write(Encoding.UTF8.GetBytes(UlteriusAes.EncryptionType.CBC.ToString()));
                            binaryWriter.Write(encrpytedJson);
                        }
                        using (var writer = new BinaryWriter(output, Encoding.UTF8, true))
                        {
                            writer.Write(memoryStream.ToArray());
                        }
                    }
                }
                else
                {
                    using (var writer = new StreamWriter(output, Encoding.UTF8, 4096, true))
                    {
                        writer.Write(jsonString);
                    }
                }
            }
        }

        public IConnectionRequest Deserialize(Guid connectionId, Stream source, out Type type)
        {
            UserConnection user;
            var typeName = string.Empty;
            ConnectionManager._connections.TryGetValue(connectionId, out user);
            if (user != null)
            {
                var data = ReadFully(source);
                if (data != null && data.Length > 0)
                {
                    if (user.AesShook)
                    {
                        try
                        {
                            var keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(user.AesKey));
                            var iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(user.AesIv));
                            var packetJson = JObject.Parse(UlteriusAes.Decrypt(data, keybytes, iv));
                            typeName = packetJson.Property("type").Value.ToString();
                            return Build(typeName, packetJson, out type, user);
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception.Message);
                            return Build("error", null, out type);
                        }
                    }
                    var json = JObject.Parse(Encoding.UTF8.GetString(data));
                    typeName = json.Property("type").Value.ToString();
                    return Build(typeName, json, out type, user);
                }
            }
            return Build("error", null, out type);
        }

        private byte[] ReadFully(Stream input)
        {
            var buffer = new byte[16*1024];
            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private IConnectionRequest Build(string typeName, JObject json, out Type type, UserConnection user = null)
        {
            switch (typeName)
            {
                case "CreateTerminalRequest":
                    type = typeof(CreateTerminalRequest);
                    return new CreateTerminalRequest
                    {
                        TerminalType = json.Property("terminalType").Value.ToString(),
                        CorrelationId = (int) json.Property("correlationId")
                    };
                case "AesHandShakeRequest":
                    type = typeof(AesHandshakeRequest);
                    if (user != null)
                    {
                        if (user.AesShook)
                        {
                            return new AesHandshakeRequest
                            {
                                AesShook = user.AesShook
                            };
                        }
                        try
                        {
                            var encryptedKey = json.Property("encryptedKey").Value.ToString();
                            var encryptedIv = json.Property("encryptedIv").Value.ToString();
                            var privateKey = user.PrivateKey;
                            user.AesKey = Rsa.Decryption(privateKey, encryptedKey);
                            user.AesIv = Rsa.Decryption(privateKey, encryptedIv);
                            user.AesShook = true;

                            return new AesHandshakeRequest
                            {
                                AesShook = user.AesShook
                            };
                        }
                        catch (Exception)
                        {
                            return new AesHandshakeRequest
                            {
                                AesShook = false
                            };
                        }
                    }

                    return new AesHandshakeRequest
                    {
                        AesShook = false
                    };


                case "TerminalInputRequest":
                    type = typeof(TerminalInputRequest);
                    return new TerminalInputRequest
                    {
                        TerminalId = Guid.Parse(json.Property("terminalId").Value.ToString()),
                        Input = json.Property("input").Value.ToString(),
                        CorrelationId = (int) json.Property("correlationId")
                    };
                case "CloseTerminalRequest":
                    type = typeof(CloseTerminalRequest);
                    return new CloseTerminalRequest
                    {
                        TerminalId = Guid.Parse(json.Property("terminalId").Value.ToString())
                    };
            }
            type = null;

            return null;
        }
    }
}