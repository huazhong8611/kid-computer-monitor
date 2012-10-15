using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MachineActivityMonitor
{
    public abstract class Settings
    {
        public abstract string getSnapshotPath();
    }
    class W7Settings : Settings
    {
        public override string getSnapshotPath()
        {
            return "c:\\Shared\\photos\\bbb\\Images\\";
        }
    }

    class XPSetting : Settings
    {
        public override string getSnapshotPath()
        {
            return "C:\\Images\\";
        }
    }

    class Profiles
    {
        DateTime lastCheckedDate = DateTime.MinValue;
        List<Filter> filters = new List<Filter>();
        const long IELimit = 60 * 60 * 3;
        string[] disabledKeyword = { 
                                       "mangareader",
                                       "game", 
                                       "bloontower"
                                      };
        public Profiles()
        {

            filters.Add(new TimeLimitedSite( new string[] {
                "facebook", 
                "youtube", 
                "mangareader", 
                "game", 
                "bloontower"}, 
                2400, 720));
            ReadUsage();
        }
        public void WriteUsage()
        {
            using (StreamWriter writer = new StreamWriter("app.config"))
            {
                foreach (Filter f in filters)
                {
                    writer.WriteLine(f.GetName() +":" +f.GetData());
                }
            }
        }
        int count = 0;
        public void ReadUsage()
        {
            count++;
            if (count < 5)
                return;
            count = 0;
            try
            {
                using (StreamReader reader = new StreamReader("app.config"))
                {
                    while (reader.Peek() > 0)
                    {
                        string s = reader.ReadLine();
                        int index = s.IndexOf(':');
                        string name = s.Substring(0, index - 1);
                        string data = s.Substring(index + 1);
                        foreach (Filter f in filters)
                        {
                            string filterName = f.GetName();
                            if (filterName.Equals(name))
                            {
                                f.SetData(data);
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public bool onTickPerSecond(Uri uri)
        {
            if (lastCheckedDate != DateTime.Now.Date)
            {
                lastCheckedDate = DateTime.Now.Date;
                foreach (Filter f in filters)
                {
                    f.init();
                }
            }

            foreach (Filter f in filters)
            {
                bool b = f.tickPerSecond(uri);
                if (b)
                    return true;
            }
            ProcessUtils.CloseProcess(p => p.ProcessName.StartsWith("Safari"));
            return false;
        }
        public virtual long browserLimit()
        {
            return IELimit;
        }
    }
}
