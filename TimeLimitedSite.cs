using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineActivityMonitor
{
    abstract class Filter
    {
        abstract public void init();
        abstract public bool tickPerSecond(Uri uri);
        abstract public string GetName();
        abstract public string GetData();
        abstract public void SetData(string s);
    }

    class TimeLimit
    {
        const int blockAfterLimitReach = 10;
        readonly int limitInSeconds;
        readonly int contLimitInSeconds;
        object thelock = new object();
        DateTime lastContLimitReachedTime = DateTime.MinValue;
        DateTime lastContCheckedTime = DateTime.MinValue;

        public int usage{get;set;}
        public int contUsage { get; set; }

        public TimeLimit(int maxInSeconds, int contInSeconds)
        {
            limitInSeconds = maxInSeconds;
            contLimitInSeconds = contUsage;
        }

        public void init() {
            lock (thelock) {
                usage = 0;
                contUsage = 0;
            }
        }

        public void increase()
        {
            DateTime now = DateTime.Now;
            lock (thelock) {
                if (usage <= limitInSeconds)
                {
                    if (contLimitInSeconds == int.MaxValue || 
                        now < lastContLimitReachedTime.AddMinutes(blockAfterLimitReach))
                        return;

                    if (now > lastContCheckedTime.AddSeconds(20))
                    {
                        contUsage =0;
                    }
                    else
                    {
                        contUsage ++;
                        lastContCheckedTime = now;
                    }
                    usage++;
                }

            }
        }

        public bool limitReached
        {
            get
            {
                var now = DateTime.Now;
                if (contLimitInSeconds < int.MaxValue)
                {
                    if (now < lastContLimitReachedTime.AddMinutes(10))
                        return true;
                    if (contUsage > this.contLimitInSeconds)
                    {
                        lastContLimitReachedTime = now;
                        contUsage = 0;
                    }
                }
                return usage > limitInSeconds;
            }
        }
    }

    class SiteContinuesTimeLimit : Filter
    {
        String[] siteWhiteList = new String[] {
            "fuhsd.org",
            "google.com",
            "schoolloop.com",
            "wikipedia.org",
            "turnitin.com",
            "mail.yahoo.com",
            "fuhsd.com",
            "qq.com"
        };
        const int SiteContinuousTimeLimitInSeconds = 900;// 15 minutes
        const int SiteContinuousWeekendDailyLimitInSeconds = 3600;// 1 hour
        Dictionary<Uri, int> SiteUsage = new Dictionary<Uri, int>();
        String currentSiteHost;
        TimeLimit limit;
        int othesiteGameTime;
        public SiteContinuesTimeLimit()
        {
            this.limit = new TimeLimit(SiteContinuousTimeLimitInSeconds, int.MaxValue);
            this.othesiteGameTime = 0;
        }

        public override void init()
        {
            limit.init();
            this.othesiteGameTime = 0;
        }

        public override bool tickPerSecond(Uri uri)
        {
            String lastHost = this.currentSiteHost;
            this.currentSiteHost = uri.Host;
            if (lastHost != this.currentSiteHost)
            {
                if (isGaming())
                    this.othesiteGameTime += limit.usage; 
                limit.init(); // reset timer
                return false;
            }

            foreach (string alwaysAllowedSite in this.siteWhiteList)
            {
                if (uri.Host.Contains(alwaysAllowedSite))
                    return false;
            }
            limit.increase();
            DateTime now = DateTime.Now;
            switch (now.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    if (this.isGaming())
                    {
                        int totalGameTime = this.othesiteGameTime + limit.usage;
                        return limit.limitReached && (totalGameTime > SiteContinuousWeekendDailyLimitInSeconds);
                    }
                    return false;

                default:
                    return limit.limitReached;
            }
        }

        private bool isGaming()
        {
            if (limit.usage > 5 * 60)
                return true;
            return false;
        }

        public override string GetName()
        {
            return "ContinuousAccess";
        }

        public override string GetData()
        {
            return othesiteGameTime.ToString();
        }

        public override void SetData(string s)
        {
            try
            {
                othesiteGameTime = Int16.Parse(s);
                return;
            }
            catch (Exception) { }
        }
    }
    class WeekendOnlySite : Filter 
    {
        TimeLimit limit;
        string siteKey;
        public WeekendOnlySite(string siteKey, TimeLimit limit)
        {
            this.siteKey = siteKey;
            this.limit = limit;
        }

        public override void init()
        {
            limit.init();
        }

        public override bool tickPerSecond(Uri uri)
        {
            if (!uri.OriginalString.Contains(siteKey))
                return false;
            DateTime now = DateTime.Now;
            switch (now.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:
                    limit.increase();
                    return limit.limitReached;

                default:
                    return false;
            }
        }

        public override string GetName()
        {
            return "Weekend-" + siteKey;
        }

        public override string GetData()
        {
            return limit.usage.ToString();
        }

        public override void SetData(string s)
        {
            try
            {
                limit.usage = Int16.Parse(s);
            }
            catch (Exception) { }
        }
    }
    class TimeLimitedSite :Filter
    {
        string[] siteKeys;
        TimeLimit limit;
        public TimeLimitedSite(string[] siteKeys, int limitInSeconds, int contLimitInSeconds)
        {
            limit = new TimeLimit(limitInSeconds, contLimitInSeconds);
            this.siteKeys = siteKeys;
        }

        override public void init()
        {
            limit.init();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <returns>if it should diable access the url</returns>
        override public bool tickPerSecond(Uri uri)
        {
            bool found = false;
            foreach (var key in siteKeys)
            {
                if (uri.OriginalString.Contains(key))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
                return false;
            limit.increase();
            return limit.limitReached;
        }

        public override string GetName()
        {
            return "TimeLimit-" + String.Join(",",siteKeys);
        }

        public override string GetData()
        {
            return limit.usage.ToString();
        }

        public override void SetData(string s)
        {
            try
            {
                limit.usage = Int16.Parse(s);
            }
            catch (Exception) { }
        }
    }
}
