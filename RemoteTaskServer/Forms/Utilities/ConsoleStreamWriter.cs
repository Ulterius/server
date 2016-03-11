using System.IO;
using System.Text;
using System.Windows.Forms;

namespace UlteriusServer.Forms.Utilities
{
    public class ConsoleStreamWriter : TextWriter
    {
        private readonly TextBox _output;

        public ConsoleStreamWriter(TextBox output)
        {
            _output = output;
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        public override void Write(char value)
        {
            base.Write(value);
            _output.AppendText(value.ToString()); // When character data is written, append it to the text box.
        }
    }
}