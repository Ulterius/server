using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RemoteTaskServer.WebServer;
using UlteriusServer.Forms.Utilities;
using UlteriusServer.Plugins;
using UlteriusServer.TaskServer;
using UlteriusServer.TaskServer.Services.System;
using UlteriusServer.TerminalServer;
using UlteriusServer.Utilities;
using UlteriusServer.WebCams;

namespace UlteriusServer.Forms
{
    public partial class Launcher : Form
    {
        public Launcher()
        {
            InitializeComponent();
        }

        private void Launcher_Load(object sender, EventArgs e)
        {
            var streamWriter = new ConsoleStreamWriter(this.consoleBox);
            Console.SetOut(streamWriter);
        
        }
    }
}
