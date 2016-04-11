using System.Collections.Generic;
using System.Net;

namespace KonturGeneralInfoParser
{
    class StatisticsSender
    {
        public static string ServerUrl = "http://185.27.192.166:6523/Server/";
        public static string Key = "ea33d5c9dcfc";

        public static void SendSnapshot(string instanceName, Dictionary<string, int> measurments)
        {
            string url = string.Format("{0}/Client/SendStatistics?key={1}&instanceName={2}&measurments=", ServerUrl, Key, instanceName);

            foreach (var item in measurments)
            {
                url += item.Key + "~" + item.Value + "*";
            }

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

            var response = request.GetResponse();
            response.Dispose();
        }

        public static void SendMessage(string instanceName, string msg)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(ServerUrl + "/Client/SendMessage?instanceName=" + instanceName + "&message=" + msg);

            var response = request.GetResponse();
            response.Dispose();
        }
    }
}
