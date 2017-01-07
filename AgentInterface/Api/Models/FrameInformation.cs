using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentInterface.Api.Models
{
   public class FrameInformation
    {
        public Rectangle Bounds { get; set; }

        public Bitmap ScreenImage { get; set; }
    }
}
