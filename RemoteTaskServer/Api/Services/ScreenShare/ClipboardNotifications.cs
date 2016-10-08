using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UlteriusServer.Api.Win32;

namespace UlteriusServer.Api.Services.ScreenShare
{
    /// <summary>
    /// Provides notifications when the contents of the clipboard is updated.
    /// </summary>
    public sealed class ClipboardNotifications
    {
        /// <summary>
        /// Occurs when the contents of the clipboard is updated.
        /// </summary>
        public static event EventHandler ClipboardUpdate = delegate { };

        private static NotificationForm _clipboardForm;

        static ClipboardNotifications()
        {
            var formThread = new Thread(InitializeForm)
            {
                Name = "ClipboardListener",
                IsBackground = true,
            };
            formThread.SetApartmentState(ApartmentState.STA);
            formThread.Start();
        }

        private static void InitializeForm()
        {
            if (_clipboardForm != null)
            {
                throw new InvalidOperationException("Already initialized");
            }
            _clipboardForm = new NotificationForm();
            Application.Run(_clipboardForm);
        }

        /// <summary>
        /// Raises the <see cref="ClipboardUpdate"/> event.
        /// </summary>
        /// <param name="e">Event arguments for the event.</param>
        private static void OnClipboardUpdate()
        {
            ClipboardUpdate(_clipboardForm, EventArgs.Empty);
        }

        /// <summary>
        /// Hidden form to recieve the WM_CLIPBOARDUPDATE message.
        /// </summary>
        private class NotificationForm : Form
        {
            public NotificationForm()
            {
                WinApi.SetParent(Handle, WinApi.HWND_MESSAGE);
                WinApi.AddClipboardFormatListener(Handle);
            }

        

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WinApi.WM_CLIPBOARDUPDATE)
                {
                    OnClipboardUpdate();
                }
            }
        }

     
    }
}
