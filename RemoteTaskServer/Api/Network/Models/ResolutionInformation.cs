using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusServer.Api.Network.Models
{
    public class ResolutionInformation
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Frequency { get; set; }
        public string Orientation { get; set; }
    }
}
