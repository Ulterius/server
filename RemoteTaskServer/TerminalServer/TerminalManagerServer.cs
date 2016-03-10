#region

using System;
using System.Net;
using MassTransit;
using UlteriusServer.TaskServer.Services.Network;
using UlteriusServer.TerminalServer.Cli;
using UlteriusServer.TerminalServer.Infrastructure;
using UlteriusServer.TerminalServer.Messaging;
using UlteriusServer.TerminalServer.Messaging.TerminalControl.Handlers;
using UlteriusServer.TerminalServer.Session;

#endregion

namespace UlteriusServer.TerminalServer
{
    internal class TerminalManagerServer
    {
        public static void Start()
        {
            var logger = new Log4NetLogger();
            var sysinfo = new SystemInfo();
            var endpoint = new IPEndPoint(IPAddress.Parse(/*NetworkUtilities.GetIPv4Address()*/ "0.0.0.0"), 22008);


            var server = new WebSocketQueueServer(endpoint, sysinfo, logger);
            var manager = new ConnectionManager(server, logger, sysinfo);

            var cliFactories = new ICliSessionFactory[]
            {
                // creates cmd.exe sessions
                new CommandSessionFactory(logger),

                // creates powershell sessions
                new PowerShellFactory(logger)
            };

            server.Queue.SubscribeInstance(new CreateTerminalRequestHandler(manager, cliFactories, logger, sysinfo));
            server.Queue.SubscribeInstance(new CloseTerminalRequestHandler(manager, logger));
            server.Queue.SubscribeInstance(new InputTerminalRequestHandler(manager, logger));
     

            try
            {
                server.StartAsync();
                Console.WriteLine("Terminal Server bound to " + NetworkUtilities.GetIPv4Address() + ":" + 22008);
                Console.ReadKey(true);
                server.Dispose();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            Console.WriteLine("End.");
            Console.ReadKey(true);
        }
    }
}