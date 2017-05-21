using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SimShift
{
    using System.Runtime.ExceptionServices;
    using System.Threading;

    static class Program
    {
        private static FileStream log;

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Log("ThreadException", e.Exception);
        }

        private static void ApplicationOnApplicationExit(object sender, EventArgs eventArgs)
        {
            log.Close();
        }

        static void CurrentDomain_FirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            Log("FirstChanceException", e.Exception);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log("UnhandledException", (Exception) e.ExceptionObject);
        }

        private static void Log(string what, Exception e)
        {
            var h = "------------------ " + what + " ----------------\r\n" + e.Message + "\r\nSTACKTRACE: " + e.StackTrace + "\r\n" + e.ToString() + "\r\n";
            var h2 = Encoding.ASCII.GetBytes(h);
            log.Write(h2, 0, h2.Length);
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ApplicationExit += ApplicationOnApplicationExit;
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Debug.Listeners.Add(new TextWriterTraceListener("trace.txt"));
            log = File.OpenWrite("./exception.txt");
            log.Seek(0, SeekOrigin.End);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
    }
}