using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusServer.Api.Network.Models
{
   public class LoginInformation
    {
        public bool IsAdmin { get; set; }

        public bool LoggedIn { get; set; }
       public string Message { get; set; }
    }
}
