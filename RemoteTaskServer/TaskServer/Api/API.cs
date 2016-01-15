using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlteriusServer.TaskServer.Api
{
    class API
    {
        public enum States
        {
            StreamingProcessData,
            StreamingSystemData,
            Standard
        }

        public static States processState;
        public static States systemState;
    }
}
