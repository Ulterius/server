namespace UlteriusServer.Utilities.Drive
{
    public class Smart
    {
        public Smart(string attributeName)
        {
            Attribute = attributeName;
        }

        public bool HasData => Current != 0 || Worst != 0 || Threshold != 0 || Data != 0;

        public string Attribute { get; set; }
        public int Current { get; set; }
        public int Worst { get; set; }
        public int Threshold { get; set; }
        public int Data { get; set; }
        public bool IsOk { get; set; }
    }
}