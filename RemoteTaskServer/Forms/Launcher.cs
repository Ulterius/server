using System;
using System.Windows.Forms;
using UlteriusServer.Forms.Utilities;

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
            var streamWriter = new ConsoleStreamWriter(consoleBox);
            Console.SetOut(streamWriter);
        }
    }
}