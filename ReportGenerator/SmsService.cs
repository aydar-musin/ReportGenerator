using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading;

namespace ReportGenerator
{
    class SmsService
    {
        public static State GetNumber()
        {
            var state=Request("http://onlinesim.ru/api/getNum.php?service=konntur");
            if (state.response != "1") throw new Exception("Ошибка получения номера "+state.response);

            Thread.Sleep(1000);

            while(state.response!= "TZ_NUM_WAIT")
            {
                state = Request("http://onlinesim.ru/api/getState.php?tzid="+state.tzid);
                Thread.Sleep(2000);
            }

            return state;
        }
        public static State GetMessage(State state)
        {
            while (state.response != "TZ_NUM_ANSWER")
            {
                state = Request("http://onlinesim.ru/api/getState.php?tzid=" + state.tzid);

                if (state.response == "TZ_OVER_EMPTY")
                    throw new Exception("Не удалось дождаться смс");

                Thread.Sleep(2000);
            }

            return state;
        }
        public static void SetOperationOK(State state)
        {
            Request("http://onlinesim.ru/api/setOperationOK.php?tzid=" + state.tzid);
        }
        private static State Request(string url)
        {
            int tries = 3;

            get: try
            {
                string result = "";

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url + "&apikey=" + Global.SmsServiceKey);
                using (var response = request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = reader.ReadToEnd();
                    }
                }
                return JsonConvert.DeserializeObject<State>(result);
            }
            catch(Exception ex)
            {
                if(tries>0)
                {
                    tries--;
                    goto get;
                }
                throw new Exception("Ошибка при запросе sms service: "+ex.Message, ex);
            }
        }
    }
    public class State
    {
        public int tzid { get; set; }
        public string response { get; set; }
        public string service { get; set; }
        public string number { get; set; }
        public string msg { get; set; }
        public string time { get; set; }
        public string form { get; set; }
    }
}
