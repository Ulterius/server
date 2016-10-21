using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusServer.Api.Network.Models
{
    public class FanInformation
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string DeviceId { get; set; }
        public string Speed { get; set; }
    }
}
