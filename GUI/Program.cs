﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CKAN.CmdLine;

namespace CKAN
{
    public static class GUI
    {
        internal static GUIUser user;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
            user = new GUIUser();
            return CmdMain.RunCommandLine(args, StartGuiApplication);
        }

        public static int StartGuiApplication(string[] args, bool showConsole = false)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionEventHandler;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Contains(URLHandlers.UrlRegistrationArgument))
            {
                //Passing in null will cause a NullRefrenceException if it tries to show the dialog 
                //asking for elevation permission, but we want that to happen. Doing that keeps us 
                //from getting in to a infinite loop of trying to register.
                URLHandlers.RegisterURLHandler(null, null); 
            }
            else
            {
                new Main(args, user, showConsole);
            }

            return Exit.OK;
        }

        public static void UnhandledExceptionEventHandler(Object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject;

            // Provide a stack backtrace, so our users and non-debugging devs can
            // see what's gone wrong.
            user.RaiseError("Unhandled exception:\n{0} ", exception.ToString());
        }
    }
}
