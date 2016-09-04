using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UlteriusServer.Api.Services.ScreenShare
{
    public class ScreenModel
    {
        public Task ScreenTask { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }
}
