using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RemoteTaskServer.WebServer;
using UlteriusServer.Properties;
using UlteriusServer.TaskServer.Services.Network;

namespace UlteriusServer.Forms.Utilities
{
    public class UlteriusTray
    {
        public static ContextMenu Menu;
        public static MenuItem ExitProgram;
        public static MenuItem OpenClient;
        public static MenuItem OpenSettings;
        public static MenuItem OpenLogs;
        public static MenuItem RestartProgram;
        public static NotifyIcon NotificationIcon;

      
        private static void OpenLogsEvent(object sender, EventArgs e)
        {
            Process.Start("log.txt");
        }

        private static void RestartEvent(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        private static void OpenSettingsEvent(object sender, EventArgs e)
        {
            Process.Start("UlteriusServer.ini");
        }

        private static void OpenClientEvent(object sender, EventArgs e)
        {
            var ip = NetworkUtilities.GetIPv4Address();
            var httpPort = HttpServer.GlobalPort;
            Process.Start($"http://{ip}:{httpPort}");
        }

        private static void ExitEvent(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        public static void ShowMessage(string message, string title = "")
        {
            var notification = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipTitle = title,
                BalloonTipText = message
            };
            notification.ShowBalloonTip(5000);
            notification.Dispose();
            RefreshTrayArea();
        }

        public static void ShowTray()
        {
            RefreshTrayArea();
            Menu = new ContextMenu();
            RestartProgram = new MenuItem("Restart Server");
            ExitProgram = new MenuItem("Exit");
            OpenClient = new MenuItem("OpenPort AuthClient");
            OpenLogs = new MenuItem("OpenPort Logs");
            OpenSettings = new MenuItem("OpenPort Settings");
            Menu.MenuItems.Add(0, ExitProgram);
            Menu.MenuItems.Add(1, RestartProgram);
            Menu.MenuItems.Add(2, OpenClient);
            Menu.MenuItems.Add(3, OpenSettings);
            Menu.MenuItems.Add(4, OpenLogs);

            NotificationIcon = new NotifyIcon
            {
                Icon = Resources.icon,
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = "Ulterius Server Started ",
                ContextMenu = Menu,
                Text = "Main"
            };


            ExitProgram.Click += ExitEvent;
            RestartProgram.Click += RestartEvent;
            OpenClient.Click += OpenClientEvent;
            OpenSettings.Click += OpenSettingsEvent;
            OpenLogs.Click += OpenLogsEvent;
            NotificationIcon.Visible = true;
            NotificationIcon.ShowBalloonTip(5000);
            Application.Run();
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