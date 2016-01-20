namespace UlteriusServer.TaskServer.Api
{
    internal class API
    {
        public enum States
        {
            StreamingProcessData,
            StreamingSystemData,
            Standard
        }

        public static States ProcessState { get; set; }
        public static States SystemState { get; }
    }
}