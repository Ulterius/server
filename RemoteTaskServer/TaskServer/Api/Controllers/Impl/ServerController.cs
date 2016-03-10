#region

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;
using System.Xml;
using UlteriusServer.TaskServer.Api.Serialization;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Controllers.Impl
{
    public class ServerController : ApiController
    {
        private readonly WebSocket client;
        private readonly Packets packet;
        private readonly ApiSerializator serializator = new ApiSerializator();

        public ServerController(WebSocket client, Packets packet)
        {
            this.client = client;
            this.packet = packet;
        }

        public void AesHandshake()
        {
            try
            {
                var encryptedKey = packet.args.AsEnumerable().First().ToString();
                foreach (var connectedClient in TaskManagerServer.AllClients.Where(connectedClient => connectedClient.Value.Client == client))
                {
                   connectedClient.Value.AesKey = Rsa.Decryption(connectedClient.Value.PrivateKey, encryptedKey);

                    var endData = new
                    {
                        shook = true
                    };
                    serializator.Serialize(client, packet.endpoint, packet.syncKey, endData);  
                }
            }
            catch (Exception e)
            {
                var endData = new
                {
                    shook = false,
                    message = e.Message
                };
                serializator.Serialize(client, packet.endpoint, packet.syncKey, endData);
            }
        }

        public void CheckForUpdate()
        {
            var isError = false;
            var errorData = "";
            if (Tools.HasInternetConnection)
            {
                try
                {
                    var releasePageUrl = "";
                    var changeNotes = "";
                    Version newVersion = null;
                    const string versionConfig = "https://raw.github.com/StrikeOrg/ulterius-server/master/version.xml";
                    var reader = new XmlTextReader(versionConfig);
                    reader.MoveToContent();
                    var elementName = "";
                    try
                    {
                        if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "ulteriusserver"))
                        {
                            while (reader.Read())
                            {
                                switch (reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        elementName = reader.Name;
                                        break;
                                    default:
                                        if ((reader.NodeType == XmlNodeType.Text) && reader.HasValue)
                                        {
                                            switch (elementName)
                                            {
                                                case "version":
                                                    newVersion = new Version(reader.Value);
                                                    break;
                                                case "changeNotes":
                                                    changeNotes = reader.Value;
                                                    break;
                                                case "url":
                                                    releasePageUrl = reader.Value;
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        isError = true;
                        errorData = e.Message;
                    }
                    finally
                    {
                        reader.Close();
                    }

                    var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (isError)
                    {
                        var data = new
                        {
                            update = false,
                            error = errorData,
                            message = "Error retrieving update information: " + errorData
                        };
                        serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                    }
                    else if (applicationVersion.CompareTo(newVersion) < 0)
                    {
                        var data = new
                        {
                            update = true,
                            url = releasePageUrl,
                            newVersion = newVersion.ToString(),
                            changeNotes,
                            message = "New version available: " + newVersion
                        };
                        serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                    }
                    else
                    {
                        var data = new
                        {
                            update = false,
                            message = "You have the latest version."
                        };
                        serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                    }
                }
                catch (Exception e)
                {
                    var data = new
                    {
                        update = false,
                        error = e.Message,
                        message = "General bad thing has happened: " + e.Message
                    };
                    serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
                }
            }
            var endData = new
            {
                update = false,
                error = "No connection",
                message = "Unable to connect to the internet to check for update."
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, endData);
        }

        public void RestartServer()
        {
            var data = new
            {
                serverRestarting = true
            };
            serializator.Serialize(client, packet.endpoint, packet.syncKey, data);
            var fileName = Assembly.GetExecutingAssembly().Location;
            Process.Start(fileName);
            Environment.Exit(0);
        }
    }
}