#region

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
<<<<<<< HEAD
=======
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
>>>>>>> bfec803572a4753144bfeca511d085dc78326a6d
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace RemoteTaskServer.Utilities
{
    internal class Tools
    {
        public static string GenerateAPIKey()
        {
            var res = "";
            var rnd = new Random();
            while (res.Length < 35)
                res += new Func<Random, string>(r =>
                {
                    var c = (char) (r.Next(123)*DateTime.Now.Millisecond%123);
                    return char.IsLetterOrDigit(c) ? c.ToString() : "";
                })(rnd);
            return res;
        }

        public static string GetQueryString(string url, string key)
        {
            var query_string = string.Empty;

            var uri = new Uri(url);
            var newQueryString = HttpUtility.ParseQueryString(uri.Query);
            if (newQueryString[key] != null)
            {
                query_string = newQueryString[key];
            }


            return query_string;
        }

        /// <summary>
        ///     Gets the icon for a process by its path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetIconForProcess(string path)
        {
            var appIcon = Icon.ExtractAssociatedIcon(path);
            var ms = new MemoryStream();
            appIcon.ToBitmap().Save(ms, ImageFormat.Png);
            var byteImage = ms.ToArray();
            var SigBase64 = Convert.ToBase64String(byteImage); //Get Base64
            return SigBase64;
        }

        private static bool HasInternetConnection
        {
            // There is no way you can reliably check if there is an internet connection, but we can come close
            get
            {
                bool result = false;

                try
                {
                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        using (Ping p = new Ping())
                        {
                            result = 
                                (p.Send("8.8.8.8", 15000).Status == IPStatus.Success) ||
                                (p.Send("8.8.4.4", 15000).Status == IPStatus.Success) ||
                                (p.Send("4.2.2.1", 15000).Status == IPStatus.Success);
                        }
                    }
                }
                catch { }

                return result;
            }
        }

        public static string CheckForUpdates()
        {
            var isError = false;
            var errorData = "";
            if (Tools.HasInternetConnection)
            {
                try
                {
                    string _releasePageURL = "";
                    Version _newVersion = null;
                    const string _versionConfig = "https://raw.github.com/StrikeOrg/ulterius-server/master/version.xml";
                    XmlTextReader _reader = new XmlTextReader(_versionConfig);
                    _reader.MoveToContent();
                    string _elementName = "";
                    try
                    {
                        if ((_reader.NodeType == XmlNodeType.Element) && (_reader.Name == "ulteriusserver"))
                        {
                            while (_reader.Read())
                            {
                                switch (_reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        _elementName = _reader.Name;
                                        break;
                                    default:
                                        if ((_reader.NodeType == XmlNodeType.Text) && (_reader.HasValue))
                                        {
                                            switch (_elementName)
                                            {
                                                case "version":
                                                    _newVersion = new Version(_reader.Value);
                                                    break;
                                                case "url":
                                                    _releasePageURL = _reader.Value;
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

                        //MessageBox.Show(Resources.ErrorUpdates, Resources.ErrorHeader, MessageBoxButtons.OK,
                        //    MessageBoxIcon.Error);
                        isError = true;
                        errorData = e.Message.ToString();
                        
                    }
                    finally
                    {
                        _reader.Close();
                    }

                    Version applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    JObject response;
                    if (isError)
                    {
                        response = JObject.FromObject(new
                        {
                            update = false,
                            error = errorData,
                            message = "Error retrieving update information: " + errorData
                        });
                    }
                    else if (applicationVersion.CompareTo(_newVersion) < 0)
                    {
                        response = JObject.FromObject(new
                        {
                            update = true,
                            url = _releasePageURL,
                            newVersion = _newVersion.ToString(),
                            message = "New version available: " + _newVersion.ToString(),
                        });
                    }
                    else
                    {
                        response = JObject.FromObject(new
                        {
                            update = false,
                            message = "You have the latest version."
                        });
                    }

                    return response.ToString();

                    /*
                    if (applicationVersion.CompareTo(_newVersion) < 0)
                    {

                        if (MessageBox.Show(System.Resources.InfoUpdateAvailable, Resources.InfoUpdatesHeader,
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        {
                            Tools.GotoSite(_releasePageURL);
                        }
                    }
                    */
                }
                catch (Exception e) {
                    return JObject.FromObject(new
                    {
                        update = false,
                        error = e.Message.ToString(),
                        message = "General bad thing has happened: " + e.Message.ToString()
                    }).ToString();
                }
            }
            else
            {
                return JObject.FromObject(new
                {
                    update = false,
                    error = "No connection",
                    message = "Unable to connect to the internet to check for update."
                }).ToString();
            }
        }
    }
}