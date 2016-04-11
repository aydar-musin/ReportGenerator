using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace KonturGeneralInfoParser
{
    static class ProxyProvider
    {
        public static int BannedProxiesCount { get { return bannedProxies.Count; } }
        private static Dictionary<string, DateTime> bannedProxies = initializeBannedProxies();
        private static readonly string bannedProxiesFilePath = ConfigurationManager.AppSettings["bannedProxiesFile"];

        private static WebProxy getRandomProxy(string parser = "KonturGeneralInfoParser")
        {
            WebProxy proxy = null;

            if (string.IsNullOrWhiteSpace(parser))
                throw new Exception("parser cannot be null (white space)");

            string getUrl = "http://185.27.192.166:6523/Server/Client/GetProxy?parser=" + parser + "&key=ea33d5c9dcfc";
            HttpWebRequest req = HttpWebRequest.CreateHttp(getUrl);

            req.Method = "GET";
            req.Timeout = 720000; // 720 sec

            byte[] answer = null;
            HttpWebResponse resp = null;
            Stream rcv = null;
            MemoryStream ms = null;
            try
            {
                resp = (HttpWebResponse)req.GetResponse();
                rcv = resp.GetResponseStream();
                ms = new MemoryStream();

                rcv.CopyTo(ms);
                answer = ms.ToArray();
            }
            finally
            {
                if (resp != null)
                    resp.Close();
                if (rcv != null)
                    rcv.Close();
                if (ms != null)
                    ms.Close();
            }

            var ProxyAddress = Encoding.GetEncoding(1251).GetString(answer, 0, answer.Length);

            if (string.IsNullOrWhiteSpace(ProxyAddress))
                throw new Exception("(1) Error! No available proxy");
            else if (!ProxyAddress.Contains(":"))
                throw new Exception("(2) Error! Wrong format of proxy");

            string ProxyHost = null;
            string ProxyPort = null;
            string login = null;
            string password = null;
            string[] parts = null;
            NetworkCredential cred = null;

            if (ProxyAddress.Contains('\t'))
            {
                if (ProxyAddress.Count(c => c == '\t') != 2)
                    throw new Exception("(2) Error! Wrong format of proxy");

                parts = ProxyAddress.Split(new string[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                    throw new Exception("(2) Error! Wrong format of proxy");

                login = parts[1];
                password = parts[2];
                cred = new NetworkCredential(login, password);

                parts = parts[0].Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new Exception("(2) Error! Wrong format of proxy");

                ProxyHost = parts[0];
                ProxyPort = parts[1];
            }
            else
            {
                parts = ProxyAddress.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts != null && parts.Length == 2)
                {
                    ProxyHost = parts[0];
                    ProxyPort = parts[1];
                }
                else
                    throw new Exception("(2) Error! Wrong format of proxy");
            }

            proxy = new WebProxy(new UriBuilder
            {
                Scheme = Uri.UriSchemeHttp,
                Host = ProxyHost,
                Port = int.Parse(ProxyPort)
            }.Uri);
            proxy.Credentials = cred;

            return proxy;
        }

        private static readonly TimeSpan banTime = new TimeSpan(int.Parse(ConfigurationManager.AppSettings["banDays"]), 0, 0, 0);

        public static WebProxy GetProxy()
        {
            //while (true)
            //{
            //    var proxy = getRandomProxy();

            //    if (bannedProxies.ContainsKey(proxy.Address.AbsoluteUri))
            //    {
            //        var bannedDate = bannedProxies[proxy.Address.AbsoluteUri];
            //        var elapsedBanTime = DateTime.UtcNow - bannedDate;

            //        if (elapsedBanTime > banTime)
            //            markAsUnbanned(proxy);
            //        else
            //            continue;
            //    }

            //    return proxy;
            //}

            return getRandomProxy();
        }

        private static Dictionary<string, DateTime> initializeBannedProxies()
        {
            var bannedProxies = new Dictionary<string, DateTime>();

            if (File.Exists(bannedProxiesFilePath))
                using (var linesReader = new StreamReader(bannedProxiesFilePath))
                {
                    string line;

                    while (!string.IsNullOrEmpty(line = linesReader.ReadLine()))
                    {
                        string[] value = line.Split('\t');
                        bannedProxies.Add(value[0], DateTime.Parse(value[1]));
                    }
                }

            return bannedProxies;
        }

        private static object locker = new object();

        public static void MarkAsBanned(WebProxy proxy)
        {
            lock (locker)
            {
                if (!bannedProxies.ContainsKey(proxy.Address.AbsoluteUri))
                {
                    bannedProxies.Add(proxy.Address.AbsoluteUri, DateTime.UtcNow);
                    writeBannedProxiesListToFile();
                    Console.WriteLine("ProxyProvider: {0} забанен.", proxy.Address.AbsoluteUri);
                }
            }
        }

        private static void markAsUnbanned(WebProxy proxy)
        {
            lock (locker)
            {
                if (bannedProxies.ContainsKey(proxy.Address.AbsoluteUri))
                {
                    bannedProxies.Remove(proxy.Address.AbsoluteUri);
                    writeBannedProxiesListToFile();
                    Console.WriteLine("ProxyProvider: {0} разбанен.", proxy.Address.AbsoluteUri);
                }
            }
        }

        private static void writeBannedProxiesListToFile()
        {
            var lines = new List<string>();
            foreach (var value in bannedProxies)
                lines.Add(string.Format("{0}\t{1}", value.Key, value.Value));

            File.WriteAllLines(bannedProxiesFilePath, lines);
        }
    }
}
