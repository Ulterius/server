#region

using Topshelf;
using Warden.Core;

#endregion

namespace UlteriusAgent
{

    internal class Program
    {
        private static void Main(string[] args)
        {
            WardenManager.Initialize(new WardenOptions
            {
                DeepKill = true,
                CleanOnExit = true,
                ReadFileHeaders = false
            });
            HostFactory.Run(x => //1
            {
                x.Service<UlteriusAgent>(s => //2
                {
                    s.ConstructUsing(name => new UlteriusAgent()); //3
                    s.WhenStarted(tc => tc.Start()); //4
                    s.WhenStopped(tc => tc.Stop());
                    s.WhenSessionChanged((se, e, id) => { se.HandleEvent(e, id); }); //5
                });
                x.OnException(ex =>
                {
                    //TODO Logging
                });
                x.RunAsLocalSystem(); //6
                x.EnableSessionChanged();
                x.EnableServiceRecovery(r => { r.RestartService(1); });
                x.SetDescription("The server that powers Ulterius"); //7
                x.SetDisplayName("Ulterius Server"); //8
                x.SetServiceName("UlteriusServer"); //9
                x.StartAutomaticallyDelayed();
            });
        }
    }
}