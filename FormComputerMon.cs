using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using MachineActivityMonitor;

namespace MsnMon
{

    public partial class FormComputerMon : Form
    {
        Settings settings;
        Profiles pf = new Profiles();
        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        long[] usedSeconds = new long[2];

        const int INTERVAL = 60;  // in seconds

        // Unmanaged function from user32.dll
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        // Struct we'll need to pass to the function
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private int GetIdleTime()
        {

            // Get the system uptime
            var systemUptime = Environment.TickCount;

            // The tick at which the last input was recorded
            var LastInputTicks = 0;

            // The number of ticks that passed since last input
            var IdleTicks = 0;



            // Set the struct
            LASTINPUTINFO LastInputInfo = new LASTINPUTINFO();
            LastInputInfo.cbSize = (uint)Marshal.SizeOf(LastInputInfo);
            LastInputInfo.dwTime = 0;

            // If we have a value from the function
            if (GetLastInputInfo(ref LastInputInfo))
            {
                // Get the number of ticks at the point when the last activity was seen
                LastInputTicks = (int)LastInputInfo.dwTime;
                // Number of idle ticks = system uptime ticks - number of ticks at last input
                IdleTicks = systemUptime - LastInputTicks;
            }

            return IdleTicks / 1000;
        }

        public FormComputerMon(Settings st)
        {
            settings = st;
            InitializeComponent();
        }
        Bitmap memoryImage;
        int seq = 0;
        private String filePath()
        {
            seq++;
            DateTime time = DateTime.Now;
            return settings.getSnapshotPath() + time.Month.ToString("00") + time.Day.ToString("00") +
                "-" + seq + "-" + time.Hour + "_" + time.Minute +
                "-" + time.Millisecond.ToString("00000");
        }
        private void CaptureScreen()
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {

                int w = SystemInformation.VirtualScreen.Width;
                int h = SystemInformation.VirtualScreen.Height;
                memoryImage = new Bitmap(w, h, g);
                Graphics memoryGraphics = Graphics.FromImage(memoryImage);
                memoryGraphics.CopyFromScreen(0, 0, 0, 0, new Size(w, h));
                DateTime time = DateTime.Now;
                String s = filePath() + "-snapshot.jpg";
                memoryImage.Save(s, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }
        DateTime lastCheckedDate = DateTime.MinValue;
        TimeSpan frequency = TimeSpan.FromMinutes(5);
        private void timer1_Tick(object sender, EventArgs e)
        {

            DateTime lastWriteTime = DateTime.MinValue;
            try
            {
                if (GetIdleTime() < INTERVAL)
                {
                    DateTime now = DateTime.Now.Date;
                    if (lastCheckedDate != now)
                    {
                        usedSeconds.Initialize();
                        lastCheckedDate = now;
                    }
                    if (now - lastWriteTime > frequency)
                    {
                        pf.WriteUsage();
                        lastWriteTime = now;
                    }
                    CaptureScreen();
                }
            }
            catch (Exception)
            {
            }

        }

        private void FormComputerMon_Shown(object sender, EventArgs e)
        {
            this.Hide();
            this.timer1.Interval = INTERVAL * 1000;
            this.timer1.Start();
            this.timer2.Start();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        private string GetActiveWindowTitle(IntPtr handle)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
        [DllImport("User32")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);
        static long lastAppTickCount = 0;
        static IntPtr lastActiveWindow = IntPtr.Zero;
        static IntPtr lastHiddenWindow = IntPtr.Zero;
        static DateTime lastHiddenTime = DateTime.MaxValue; 
        private void List_Click(object sender, EventArgs e)
        {
            this.listViewUrls.Items.Clear();
            DateTime now = DateTime.Now;
            IntPtr activeHWND = GetForegroundWindow();

            string activeWindowTitle = GetActiveWindowTitle(activeHWND);
            if (!activeHWND.Equals(lastActiveWindow))
            {
                lastAppTickCount = 0;
                lastActiveWindow = activeHWND;
            }
            lastAppTickCount++;
            if (lastHiddenTime.AddMinutes(5) < now)
            {
                ShowWindow((IntPtr)lastHiddenWindow, 2);
                lastHiddenTime = DateTime.MaxValue;
                lastHiddenWindow = IntPtr.Zero;
            }
            if (lastAppTickCount > 1200)
            {
                ShowWindow((IntPtr)activeHWND, 0); // hide window
                lastHiddenWindow = activeHWND;
                lastHiddenTime = now;
                lastAppTickCount = 0;
                return;
            }

            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindowsClass();
            string filename;
            foreach (SHDocVw.InternetExplorer ie in shellWindows)
            {
                filename = Path.GetFileNameWithoutExtension(ie.FullName).ToLower();
                if ((IntPtr)ie.HWND == GetForegroundWindow())
                {
                    if (filename.Equals("iexplore"))
                    {
                        if (ie.Height > 100)
                        {
                            string url = ie.LocationURL;
                            Uri uri = new Uri(url);
                            if (pf.onTickPerSecond(uri))
                            {
                                IntPtr hwnd = (IntPtr)ie.HWND;
                                MoveWindow(hwnd, 0, 0, 0, 0, false);
                            }
                        }
                    }
                }
            }

        }
        /*
        long disbleSiteUsedSeconds =0;
        DateTime lastDisabledSiteCheckedDate = DateTime.MinValue;
        private void CheckDisableUrl(IntPtr hwnd, string url)
        {
            foreach (string s in pf.getDisabledKeywords())
            {
                if (url.Contains(s))
                {

                    DateTime n = DateTime.Now;
                    if ((n.DayOfWeek == DayOfWeek.Saturday) 
                        || (n.DayOfWeek == DayOfWeek.Sunday))
                    {
                        disbleSiteUsedSeconds++;
                        if (lastDisabledSiteCheckedDate != n.Date)
                        {
                            disbleSiteUsedSeconds = 0;
                            lastDisabledSiteCheckedDate = n.Date;
                        }
                        else if (disbleSiteUsedSeconds > 3600)
                            MoveWindow(hwnd, 0, 0, 0, 0, false);
                    }
                }
            }
        }
        private void CheckRestrictedUrl(SHDocVw.InternetExplorer ie)
        {
            int i = 0;
            foreach (string s in pf.getRestrictedKeyword())
            {
                if (ie.LocationURL.Contains(s) && )
                {

                    if (pf.getRestrictedSeconds()[i] < usedSeconds[i])
                    {
                        MoveWindow((IntPtr)ie.HWND, 0, 0, 0, 0, false);
                    }
                    //this.listViewUrls.Items.Add("find active:" + ie.LocationName + " used:" + usedSeconds[i]);
                    usedSeconds[i]++;
                }
                i++;
            }
        }
*/
        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                List_Click(sender, e);
            }
            catch (Exception) { }
        }
    }
}