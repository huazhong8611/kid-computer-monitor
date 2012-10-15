using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MachineActivityMonitor
{
    class ProcessUtils
    {
        public static IEnumerable<Process> GetAllProcess(Predicate<Process> pred)
        {
            var currentSessionID = 
                Process.GetCurrentProcess().SessionId;
            Predicate<Process> sameSession = 
                p2 => (p2.SessionId == currentSessionID);
            return Process.GetProcesses()
                .Where(p => (p.SessionId == currentSessionID) 
                    && pred (p));
        }


        public static void CloseProcess(Predicate<Process> pred)
        {

            foreach (Process p in GetAllProcess(pred))
            {
                p.CloseMainWindow();
            }
        }
    }
}
