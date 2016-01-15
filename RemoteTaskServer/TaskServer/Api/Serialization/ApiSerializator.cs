#region

using System.Threading;
using System.Web.Script.Serialization;
using vtortola.WebSockets;

#endregion

namespace UlteriusServer.TaskServer.Api.Serialization
{
    public class ApiSerializator
    {
        public void Serialize(WebSocket client, string endpoint, string syncKey, object data)
        {
            var serializer = new JavaScriptSerializer {MaxJsonLength = int.MaxValue};
            var json = serializer.Serialize(new
            {
                endpoint,
                syncKey,
                results = data
            });

            Push(client, json);
        }

        private void Push(WebSocket client, string data)
        {
            client.WriteStringAsync(data, CancellationToken.None);
        }
    }
}