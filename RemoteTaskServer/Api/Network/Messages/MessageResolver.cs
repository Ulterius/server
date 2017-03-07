using System;
using System.Drawing;
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
