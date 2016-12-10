namespace UlteriusServer.Utilities.Drive
{
    public class SmartModel
    {
        #region Fields

        public string Attribute { get; set; }
        public string Current { get; set; }
        public string Threshold { get; set; }
        public string RawData { get; set; }

        public string RealData { get; set; }

        #endregion

        #region Constructor

        public SmartModel(string attribute, string current, string threshold, string rawData, string realData)
        {
            Attribute = attribute;
            Current = current;
            Threshold = threshold;
            RawData = rawData;
            RealData = realData;
        }

        #endregion
    }
}
