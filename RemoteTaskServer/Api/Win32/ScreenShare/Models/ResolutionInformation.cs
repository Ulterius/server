namespace UlteriusServer.Api.Win32.ScreenShare.Models
{
    public class ResolutionInformation
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Frequency { get; set; }
        public string Orientation { get; set; }
        public int X { get; set; }

        public int Y { get; set; }
    }
}
