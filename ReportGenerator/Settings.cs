﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace ReportGenerator
{
    public static class Settings
    {
        public static string ErrorsReportEmail { get; set; }
        public static string OrdersEmail { get; set; }
        public static string OrdersEmailPass { get; set; }
        public static int Interval { get; set; }
        public static string Cookie { get; set; }
        public static bool UseProxy { get; set; }

        public static List<string> ProxyList { get; private set; }
        public static void LoadConfig()
        {
            ErrorsReportEmail = ConfigurationSettings.AppSettings["ErrorsReportEmail"];
            OrdersEmail = ConfigurationSettings.AppSettings["OrdersEmail"];
            OrdersEmailPass = ConfigurationSettings.AppSettings["OrdersEmailPass"];
            Interval = int.Parse(ConfigurationSettings.AppSettings["Interval"]);
            Cookie = ConfigurationSettings.AppSettings["Cookie"];
            UseProxy = bool.Parse(ConfigurationSettings.AppSettings["UseProxy"]);

            ProxyList = new List<string>( System.IO.File.ReadAllLines("proxy.txt"));
        }

        public static void SaveConfig()
        {
            var conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            conf.AppSettings.Settings["ErrorsReportEmail"].Value = ErrorsReportEmail;
            conf.AppSettings.Settings["OrdersEmail"].Value = OrdersEmail;
            conf.AppSettings.Settings["OrdersEmailPass"].Value = OrdersEmailPass;
            conf.AppSettings.Settings["Interval"].Value = Interval.ToString();
            conf.AppSettings.Settings["Cookie"].Value = Cookie;
            conf.AppSettings.Settings["UseProxy"].Value = UseProxy.ToString();

            conf.Save(ConfigurationSaveMode.Modified);
        }
    }
}
