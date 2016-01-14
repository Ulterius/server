namespace UlteriusServer.TerminalServer.Cli
{
    public interface ICliSessionFactory
    {
        string Type { get; }
        ICliSession Create();
    }
}