using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;

namespace ReportGenerator
{
    public static class Global
    {
        public static List<string> Proxy;
        public static string SmsServiceKey;
        public static bool ManualMode;
        public static string ManualModeCookie;
        
        public static Action<string> MessageAction;
        static void Init()
        {
            var lines = File.ReadAllLines("proxy.txt");
            Proxy = new List<string>();
            Proxy.AddRange(lines.Where(s => !string.IsNullOrEmpty(s)));
            SmsServiceKey = ConfigurationSettings.AppSettings["SmsServiceKey"];
            ManualMode = bool.Parse(ConfigurationSettings.AppSettings["ManualMode"]);
            ManualModeCookie = ConfigurationSettings.AppSettings["ManualModeCookie"];

        }
        public static void SaveSettings()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["SmsServiceKey"].Value = SmsServiceKey;
            config.AppSettings.Settings["ManualMode"].Value = ManualMode.ToString();
            config.AppSettings.Settings["ManualModeCookie"].Value = ManualModeCookie;
            config.Save();

            File.WriteAllLines("proxy.txt", Proxy.ToArray());
        }
        public static void Message(string msg)
        {
            if(MessageAction!=null)
            {
                MessageAction(msg);
            }
        }
    }
}
