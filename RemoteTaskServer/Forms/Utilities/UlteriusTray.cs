#region

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using UlteriusServer.Api.Services.Network;
using UlteriusServer.Properties;
using UlteriusServer.Utilities;
using UlteriusServer.WebServer;

#endregion

namespace UlteriusServer.Forms.Utilities
{
    public class UlteriusTray
    {
        public static ContextMenu Menu;
        public static MenuItem ExitProgram;
        public static MenuItem OpenClient;
        public static MenuItem OpenDonate;
        public static MenuItem About;
        public static MenuItem RestartProgram;
        public static NotifyIcon NotificationIcon;

        public static MenuItem OpenSettings { get; set; }
        public static bool AboutOpen;

        private static void RestartEvent(object sender, EventArgs e)
        {
            // Starts a new instance of the program itself
            var fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Bootstrapper.exe");
            var startInfo = new ProcessStartInfo(fileName)
            {
                WindowStyle = ProcessWindowStyle.Minimized,
                Arguments = "restart"
            };
            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private static void OpenSettingsEvent(object sender, EventArgs e)
        {
            try
            {
                Process.Start(AppEnvironment.DataPath);
            }
            catch (Exception)
            {
                //rare but can fail
            }
        }

        private static void OpenClientEvent(object sender, EventArgs e)
        {
            var ip = NetworkService.GetDisplayAddress();
            var httpPort = HttpServer.GlobalPort;
            Process.Start($"http://{ip}:{httpPort}");
        }

        private static void ExitEvent(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        public static void ShowMessage(string message, string title = "")
        {
            if (NotificationIcon == null) return;
            NotificationIcon.BalloonTipText = message;
            NotificationIcon.BalloonTipTitle = title;
            NotificationIcon.ShowBalloonTip(5000);
            RefreshTrayArea();
        }

        public static void ShowTray()
        {
            RefreshTrayArea();
            Menu = new ContextMenu();
            RestartProgram = new MenuItem("Restart Server");
            OpenClient = new MenuItem("Open Client");
            About = new MenuItem("About");
            OpenSettings = new MenuItem("Open App Data");
            ExitProgram = new MenuItem("Exit");
            OpenDonate = new MenuItem("Donate");
            Menu.MenuItems.Add(0, OpenClient);
            Menu.MenuItems.Add(1, OpenSettings);
            Menu.MenuItems.Add(2, RestartProgram);
            Menu.MenuItems.Add(3, OpenDonate);
            Menu.MenuItems.Add(4, About);
            Menu.MenuItems.Add(5, ExitProgram);
            About.Click += AboutEvent;
            ExitProgram.Click += ExitEvent;
            RestartProgram.Click += RestartEvent;
            OpenClient.Click += OpenClientEvent;
            OpenSettings.Click += OpenSettingsEvent;
            OpenDonate.Click += OpenDonateEvent;
            NotificationIcon = new NotifyIcon
            {
                Icon = Resources.ApplicationIcon,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = "Ulterius Server Started -- Open the Client in the Tray Icon",
                ContextMenu = Menu,
                Text = "Ulterius Server",
                Visible = true
            };
            NotificationIcon.DoubleClick += OpenClientEvent;
            Console.WriteLine("Starting notify icon");
            NotificationIcon.ShowBalloonTip(5000);
            Application.Run();
            Console.WriteLine("Started");
        }

        private static void AboutEvent(object sender, EventArgs e)
        {
          
            Thread thread = new Thread(OpenAbout) {Name = "About"};
            thread.Start();
         
        }

        private static void OpenAbout()
        {
            Application.Run(new AboutBox()); // or whatever
            AboutOpen = true;
        }


        private static void OpenDonateEvent(object sender, EventArgs e)
        {
            Process.Start("https://cash.me/$ulterius");
        }

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
            string lpszWindow);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        public static void RefreshTrayArea()
        {
            var systemTrayContainerHandle = FindWindow("Shell_TrayWnd", null);
            var systemTrayHandle = FindWindowEx(systemTrayContainerHandle, IntPtr.Zero, "TrayNotifyWnd", null);
            var sysPagerHandle = FindWindowEx(systemTrayHandle, IntPtr.Zero, "SysPager", null);
            var notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32",
                "Notification Area");
            if (notificationAreaHandle == IntPtr.Zero)
            {
                notificationAreaHandle = FindWindowEx(sysPagerHandle, IntPtr.Zero, "ToolbarWindow32",
                    "User Promoted Notification Area");
                var notifyIconOverflowWindowHandle = FindWindow("NotifyIconOverflowWindow", null);
                var overflowNotificationAreaHandle = FindWindowEx(notifyIconOverflowWindowHandle, IntPtr.Zero,
                    "ToolbarWindow32", "Overflow Notification Area");
                RefreshTrayArea(overflowNotificationAreaHandle);
            }
            RefreshTrayArea(notificationAreaHandle);
        }

        private static void RefreshTrayArea(IntPtr windowHandle)
        {
            const uint wmMousemove = 0x0200;
            Rect rect;
            GetClientRect(windowHandle, out rect);
            for (var x = 0; x < rect.right; x += 5)
                for (var y = 0; y < rect.bottom; y += 5)
                    SendMessage(windowHandle, wmMousemove, 0, (y << 16) + x);
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}