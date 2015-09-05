﻿using System;
using System.Threading;
using System.Windows.Forms;
using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.InterProcessCommunication;
using GestureSign.Common.Localization;
using GestureSign.Common.Plugins;
using GestureSign.Daemon.Input;
using GestureSign.Daemon.Surface;

namespace GestureSign.Daemon
{
    static class Program
    {
        private static SurfaceForm _surfaceForm;
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "GestureSignDaemon", out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    //Application.SetCompatibleTextRenderingDefault(false);
                    try
                    {
                        Application.ThreadException += Application_ThreadException;
                        Logging.OpenLogFile();

                        if ("Built-in".Equals(AppConfig.CultureName) ||
                            !LocalizationProvider.Instance.LoadFromFile("Daemon", Properties.Resources.en))
                        {
                            LocalizationProvider.Instance.LoadFromResource(Properties.Resources.en);
                        }

                        TouchCapture.Instance.Load();
                        _surfaceForm = new SurfaceForm();
                        TouchCapture.Instance.EnableTouchCapture();

                        GestureManager.Instance.Load(TouchCapture.Instance);
                        ApplicationManager.Instance.Load(TouchCapture.Instance);
                        // Create host control class and pass to plugins
                        HostControl hostControl = new HostControl()
                        {
                            _ApplicationManager = ApplicationManager.Instance,
                            _GestureManager = GestureManager.Instance,
                            _TouchCapture = TouchCapture.Instance,
                            _PluginManager = PluginManager.Instance,
                            _TrayManager = TrayManager.Instance
                        };
                        PluginManager.Instance.Load(hostControl);
                        TrayManager.Instance.Load();

                        AppConfig.ToggleWatcher();

                        NamedPipe.Instance.RunNamedPipeServer("GestureSignDaemon", new MessageProcessor());

                        if (TouchCapture.Instance.MessageWindow.NumberOfTouchscreens == 0)
                        {
                            MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.TouchscreenNotFound"),
                                LocalizationProvider.Instance.GetTextValue("Messages.TouchscreenNotFoundTitle"),
                                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            //return;
                        }
                        Application.Run();
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(e);
                        MessageBox.Show(e.ToString(), LocalizationProvider.Instance.GetTextValue("Messages.Error"),
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        Application.Exit();
                    }
                }
                else
                {
                    Application.Exit();
                }
            }


        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            DialogResult result = DialogResult.Abort;
            try
            {
                Logging.LogException(e.Exception);
                string errorMsg = "An application error occurred. Please contact the author with the following information:\n\n";
                errorMsg = errorMsg + e.Exception;
                result = MessageBox.Show(errorMsg, "Error", MessageBoxButtons.AbortRetryIgnore,
                   MessageBoxIcon.Stop);
            }
            catch (Exception fe)
            {
                try
                {
                    MessageBox.Show(fe.ToString(), "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    Application.Exit();
                }
            }

            // Exits the program when the user clicks Abort.
            if (result == DialogResult.Abort)
                Application.Exit();
        }
    }
}