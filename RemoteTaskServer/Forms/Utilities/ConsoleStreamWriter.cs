using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RemoteTaskServer.WebServer;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer;
using UlteriusServer.TaskServer.Services.System;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;

namespace UlteriusServer.Forms.Utilities
{
    public class ConsoleStreamWriter : TextWriter
    {
        TextBox _output = null;

        public ConsoleStreamWriter(TextBox output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            _output.AppendText(value.ToString()); // When character data is written, append it to the text box.
            
        
        }

        public override Encoding Encoding
        {
            get { return System.Text.Encoding.UTF8; }
        }
    }
}