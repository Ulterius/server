using System;
using System.Windows.Forms;
using UlteriusServer.Properties;

namespace UlteriusServer.Forms.Utilities
{
    public class UlteriusTray
    {
        public static ContextMenu Menu;
        public static MenuItem ExitProgram;
        public static MenuItem OpenClient;
        public static MenuItem OpenSettings;
        public static MenuItem RestartProgram;
        public static NotifyIcon NotificationIcon;

        public UlteriusTray()
        {
            Menu = new ContextMenu();
            RestartProgram = new MenuItem("Restart Server");
            ExitProgram = new MenuItem("Exit");
            OpenClient = new MenuItem("Open in Browser");
            OpenSettings = new MenuItem("Open Settings");
            Menu.MenuItems.Add(0, ExitProgram);
            Menu.MenuItems.Add(1, RestartProgram);
            Menu.MenuItems.Add(2, OpenClient);
            Menu.MenuItems.Add(3, OpenSettings);

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
        }

        private static void RestartEvent(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        private static void OpenSettingsEvent(object sender, EventArgs e)
        {
        }

        private static void OpenClientEvent(object sender, EventArgs e)
        {
        }

        private static void ExitEvent(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        public void ShowTray()
        {
            NotificationIcon.Visible = true;
            NotificationIcon.ShowBalloonTip(5);
            Application.Run();
        }
    }
}