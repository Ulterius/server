#region

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UlteriusServer.Authentication;
using UlteriusServer.Utilities;
using UlteriusServer.Utilities.Security;

#endregion

namespace UlteriusServer.TaskServer

{
    public class Packets
    {
        private static readonly Settings settings = new Settings();

        public List<object> args;
        public string endpoint;
        public PacketType packetType;
        public string syncKey;
        private bool encryptionOff = false;

        public Packets(AuthClient client, string packetJson)
        {
         
            
            //An entire base64 string is an aes encrypted packet
           if (StringUtilities.IsBase64String(packetJson))
            {
                try
                {
                    var keybytes = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(client.AesKey));
                    var iv = Encoding.UTF8.GetBytes(Rsa.SecureStringToString(client.AesIv));
                    var packet = Convert.FromBase64String(packetJson);
                    packetJson = Aes.Decrypt(packet, keybytes, iv);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);

                    packetType = PacketType.InvalidOrEmptyPacket;
                    return;
                }
            }
            else
            {
                //the only non encrypted packet allowed is the first handshake
                try
                {
                    var validHandshake = JsonConvert.DeserializeObject<JsPacket>(packetJson);
                    var endpoint = validHandshake?.endpoint?.Trim()?.ToLower();
                    if (!endpoint.Equals("aeshandshake"))
                    {
                        packetType = PacketType.InvalidOrEmptyPacket;
                        return;
                    }
                    //prevent sending a new aes key pair after a handshake has already taken place
                    if (client.AesShook)
                    {
                        packetType = PacketType.InvalidOrEmptyPacket;
                        return;
                    }
                }
                catch (Exception)
                {
                    packetType = PacketType.InvalidOrEmptyPacket;
                    return;
                }
            }
           
    
        JsPacket deserializedPacket = null;
            try
            {
                deserializedPacket = JsonConvert.DeserializeObject<JsPacket>(packetJson);
            }
            catch (Exception)
            {
                packetType = PacketType.InvalidOrEmptyPacket;
                return;
            }

            if (deserializedPacket != null)
            {
                try
                {
                    endpoint = deserializedPacket?.endpoint?.Trim()?.ToLower();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    packetType = PacketType.InvalidOrEmptyPacket;
                    return;
                }
                args = deserializedPacket?.args ?? null;
                syncKey = deserializedPacket?.syncKey?.Trim() ?? null;

                switch (endpoint)
                {
                    case "authenticate":
                        packetType = PacketType.Authenticate;
                        break;
                    case "requestgpuinformation":
                        packetType = PacketType.RequestGpuInformation;
                        break;
                    case "createfiletree":
                        packetType = PacketType.CreateFileTree;
                        break;
                    case "requestprocessinformation":
                        packetType = PacketType.RequestProcess;
                        break;
                    case "streamprocessinformation":
                        packetType = PacketType.StreamProcesses;
                        break;
                    case "stopprocessinformationstream":
                        packetType = PacketType.StopProcessStream;
                        break;
                    case "downloadfile":
                        packetType = PacketType.DownloadFile;
                        break;
                    case "requestcpuinformation":
                        packetType = PacketType.RequestCpuInformation;
                        break;
                    case "requestosinformation":
                        packetType = PacketType.RequestOsInformation;
                        break;
                    case "requestnetworkinformation":
                        packetType = PacketType.RequestNetworkInformation;
                        break;
                    case "requestsysteminformation":
                        packetType = PacketType.RequestSystemInformation;
                        break;
                    case "startprocess":
                        packetType = PacketType.StartProcess;
                        break;
                    case "killprocess":
                        packetType = PacketType.KillProcess;
                        break;
                    case "generatenewkey":
                        packetType = PacketType.GenerateNewKey;
                        break;
                    case "togglewebserver":
                        packetType = PacketType.UseWebServer;
                        break;
                    case "changewebserverport":
                        packetType = PacketType.ChangeWebServerPort;
                        break;
                    case "changewebfilepath":
                        packetType = PacketType.ChangeWebFilePath;
                        break;
                    case "startvncserver":
                        packetType = PacketType.StartVncServer;
                        break;
                    case "changevncpass":
                        packetType = PacketType.ChangeVncPass;
                        break;
                    case "changetaskserverport":
                        packetType = PacketType.ChangeTaskServerPort;
                        break;
                    case "changevncport":
                        packetType = PacketType.ChangeVncPort;
                        break;
                    case "changevncproxyport":
                        packetType = PacketType.ChangeVncProxyPort;
                        break;
                    case "changenetworkresolve":
                        packetType = PacketType.ChangeNetworkResolve;
                        break;
                    case "changeloadplugins":
                        packetType = PacketType.ChangeLoadPlugins;
                        break;
                    case "changeuseterminal":
                        packetType = PacketType.ChangeUseTerminal;
                        break;
                    case "getcurrentsettings":
                        packetType = PacketType.GetCurrentSettings;
                        break;
                    case "geteventlogs":
                        packetType = PacketType.GetEventLogs;
                        break;
                    case "checkforupdate":
                        packetType = PacketType.CheckUpdate;
                        break;
                    case "restartserver":
                        packetType = PacketType.RestartServer;
                        break;
                    case "getwindowsdata":
                        packetType = PacketType.RequestWindowsInformation;
                        break;
                    case "getactivewindowssnapshots":
                        packetType = PacketType.GetActiveWindowsSnapshots;
                        break;
                    case "plugin":
                        packetType = PacketType.Plugin;
                        break;
                    case "getplugins":
                        packetType = PacketType.GetPlugins;
                        break;
                    case "getbadplugins":
                        packetType = PacketType.GetBadPlugins;
                        break;
                    case "startcamera":
                        packetType = PacketType.StartCamera;
                        break;
                    case "stopcamera":
                        packetType = PacketType.StopCamera;
                        break;
                    case "pausecamera":
                        packetType = PacketType.PauseCamera;
                        break;
                    case "getcameras":
                        packetType = PacketType.GetCameras;
                        break;
                    case "getcameraframe":
                        packetType = PacketType.GetCameraFrame;
                        break;
                    case "startcamerastream":
                        packetType = PacketType.StartCameraStream;
                        break;
                    case "stopcamerastream":
                        packetType = PacketType.StopCameraStream;
                        break;
                    case "refreshcameras":
                        packetType = PacketType.RefreshCameras;
                        break;
                    case "uploadfile":
                        packetType = PacketType.UploadFile;
                        break;
                    case "aeshandshake":
                        packetType = PacketType.AesHandshake;
                        break;
                    case "approveplugin":
                        packetType = PacketType.ApprovePlugin;
                        break;
                    case "getpendingplugins":
                        packetType = PacketType.GetPendingPlugins;
                        break;
                    default:
                        packetType = PacketType.InvalidOrEmptyPacket;
                        break;
                }
            }
        }
    }

    public class JsPacket
    {
        public List<object> args;
        public string endpoint;
        public string syncKey;
    }
}

public enum PacketType
{
    RequestProcess,
    RequestCpuInformation,
    RequestOsInformation,
    RequestNetworkInformation,
    RequestSystemInformation,
    StartProcess,
    KillProcess,
    GenerateNewKey,
    EmptyApiKey,
    InvalidApiKey,
    InvalidOrEmptyPacket,
    UseWebServer,
    ChangeWebServerPort,
    ChangeWebFilePath,
    ChangeTaskServerPort,
    ChangeNetworkResolve,
    GetCurrentSettings,
    GetEventLogs,
    CheckUpdate,
    RequestWindowsInformation,
    RestartServer,
    GetActiveWindowsSnapshots,
    Authenticate,
    StreamProcesses,
    StopProcessStream,
    DownloadFile,
    CreateFileTree,
    RequestGpuInformation,
    Plugin,
    ChangeVncPort,
    ChangeVncProxyPort,
    StartVncServer,
    ChangeVncPass,
    GetPlugins,
    GetBadPlugins,
    PauseCamera,
    StopCamera,
    StartCamera,
    GetCameras,
    GetCameraFrame,
    RefreshCameras,
    StartCameraStream,
    StopCameraStream,
    UploadFile,
    ChangeLoadPlugins,
    ChangeUseTerminal,
    AesHandshake,
    ApprovePlugin,
    GetPendingPlugins
}