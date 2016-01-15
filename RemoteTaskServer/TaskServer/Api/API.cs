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

        public static States processState;
        public static States systemState;
    }
}