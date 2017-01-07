using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace UlteriusServer.Api.Network.Messages
{
    class MessageResolver : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            if (objectType == typeof(Rectangle) || objectType == typeof(Rectangle?))
            {
                JsonContract contract = base.CreateObjectContract(objectType);
                contract.Converter = new RectangleConverter();
                return contract;
            }
            return base.CreateContract(objectType);
        }
    }
}
