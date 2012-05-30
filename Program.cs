using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MachineActivityMonitor;
using System.Diagnostics;

namespace MsnMon
{
    static class Program
    {
        static bool isStarted()
        {
            var c = ProcessMon.GetAllProcess(
                p => p.ProcessName == "TsyUpdate" &&
                    p.MainWindowTitle == "Form1").Count();
            return (c > 1);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (isStarted())
                return;
            Settings st;
            if (args[0].Equals("1"))
                st = new W7Settings();
            else
                st = new XPSetting();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(st));
        }
    }
}
