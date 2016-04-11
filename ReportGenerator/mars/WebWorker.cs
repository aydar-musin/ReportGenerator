using System;
using System.IO;
using System.Net;
using System.Text;

namespace KonturGeneralInfoParser
{
    class WebWorker
    {
        static private string search_KonturFocus(string ogrn, WebProxy proxy = null)
        {
            string url = "https://focus.kontur.ru/entity?query=" + ogrn;
            string Html_SearchResults = "";

            HttpWebRequest req = HttpWebRequest.CreateHttp(url);

            if (proxy != null)
                req.Proxy = proxy;

            req.Method = "GET";
            req.Timeout = 300000;
            req.Host = "focus.kontur.ru";
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.80 Safari/537.36";
            req.Referer = "https://focus.kontur.ru/";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            req.Headers.Add("Accept-Language:ru-RU,ru;q=0.8,en-US;q=0.6,en;q=0.4");
            req.Headers.Add("Accept-Encoding:gzip, deflate, sdch");

            byte[] answer = null;
            using (WebResponse res = req.GetResponse())
            using (Stream rcv = res.GetResponseStream())
            using (MemoryStream ms = new MemoryStream())
            {
                rcv.CopyTo(ms);
                answer = ms.ToArray();
            }

            answer = Utilities.DecompressGZip(answer);
            Html_SearchResults = Encoding.GetEncoding("UTF-8").GetString(answer, 0, answer.Length);
            Html_SearchResults = WebUtility.HtmlDecode(Html_SearchResults);

            return Html_SearchResults;
        }

        static public string GetPage(string ogrn, WebProxy proxy = null)
        {
            string page = search_KonturFocus(ogrn, proxy);

            if (page.Contains("Не найдена организация или индивидуальный предприниматель"))
                throw new Exception(string.Format("Не найдена организация или индивидуальный предприниматель \"{0}\".", ogrn));
            else if (page.Contains("Слишком много запросов к серверу.") ||
                page.Contains("<h1 class=\"mainPage_title\">Быстрая проверка контрагентов</h1>")) // TODO: проверить // ситуация бана
                throw new Exception("Слишком много запросов к серверу.");

            return page;
        }
    }
}