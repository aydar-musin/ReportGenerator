using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace ReportGenerator
{
    class KonturWorker
    {
        public string Cookie { get; set; }
        private WebProxy proxy;

        private void ChangeProxy()
        {
            if (Global.Proxy.Count > 0)
            {
                Random rnd = new Random();

                int rndIndex = rnd.Next(0, Global.Proxy.Count);

                this.proxy = new WebProxy(Global.Proxy[rndIndex]);
            }
            else
                this.proxy = null;
        }
        private bool ValidatePage(string page)
        {
            return (!page.Contains("░</span>") && !page.Contains("Быстрый анализ связанных компаний"));
        }
        private string Request(string url) // Add headers!!!!!
        {
            int tries = 3;
            req: try
            {


                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                if (this.proxy != null)
                    request.Proxy = proxy;
                request.UserAgent="Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";

                request.Headers.Add("Cookie", this.Cookie);

                string result = "";
                using (var response = request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        result = reader.ReadToEnd();
                    }
                }
                if (!ValidatePage(result)) throw new Exception("page validation error");

                return result;
            }
            catch (Exception ex)
            {
                tries--;
                if (tries > 0)
                {
                    goto req;
                }


                throw new Exception("get request error: "+url +" "+ex.Message, ex);
            }

        }
        public CompanyInfo Process(Order order)
        {
            if (order.CompanyINNOGRN.Length == 10)
                order.CompanyId = GetCompanyId(order.CompanyINNOGRN);
            else
                order.CompanyId = order.CompanyINNOGRN;

            CompanyInfo info = Parser.GeneralInfo(Request("https://focus.kontur.ru/entity?query=" + order.CompanyId));

            if (info.ArbitrationExists)
            {
                info.ArbitrAsPlaintiff = GetArbitrAsP(order.CompanyId);
                info.ArbitrAsRespondent = GetArbitrAsR(order.CompanyId);
                info.ArbitrAsThird = GetArbitrAsT(order.CompanyId);
            }

            if(info.LicensiesExist)//!!!!!!!
            {
                info.Lics = Parser.Lics(Request("https://focus.kontur.ru/lics?query=" + order.CompanyId));
            }
            if(info.BailiffsExist)
            {
                info.BailiffsInfo = Parser.Bailiffs(Request("https://focus.kontur.ru/fssp?query=" + order.CompanyId));
            }
            if(info.ContractsExist)
            {
                info.WonContracts = Parser.WonContracts(Request("https://focus.kontur.ru/contracts?type=customers&query=" + order.CompanyId));
                info.PostedContracts= Parser.PostedContracts(Request("https://focus.kontur.ru/contracts?type=suppliers&query=" + order.CompanyId));
            }
            info.Founders = GetFounders(order.CompanyId);
            info.Activities = GetActivities(order.CompanyId);
            info.Predecessors = GetPredecessors(order.CompanyId);
            info.RelatedCompanies = GetRelatedCompanies(order.CompanyId);

            return info;
        }
        public string GetGeneralInfoPage(string id)
        {
            var page = Request("https://focus.kontur.ru/entity?query=" + id);
            return page;
        }
        public List<RelatedCompany> GetPredecessors(string id)
        {
            return Parser.RelatedCompanies(Request("https://focus.kontur.ru/graph?page=1&filterFlags=268435456&order=29&query="+id));
        }
        public List<RelatedCompany> GetRelatedCompanies(string id)
        {
            return Parser.RelatedCompanies(Request("https://focus.kontur.ru/graph?order=29&query=" + id));
        }
        public List<Founder> GetFounders(string id)
        {
            return Parser.Founders(Request("https://focus.kontur.ru/founders?query=" + id)).ToList();
        }
        public List<Activity> GetActivities(string id)
        {
            return Parser.Activites(Request("https://focus.kontur.ru/activities?query=" + id)).ToList();
        }
        public ArbitrStat GetArbitrAsP(string id)
        {
            return Parser.ArbitrAsPlaitiff(Request("https://focus.kontur.ru/kad?type=0&years=0&query="+id));
        }
        public ArbitrStat GetArbitrAsR(string id)
        {
            return Parser.ArbitrAsRespondent(Request("https://focus.kontur.ru/kad?type=1&years=0&query=" + id));
        }
        public ArbitrStat GetArbitrAsT(string id)
        {
            return Parser.ArbitrAsThird(Request("https://focus.kontur.ru/kad?type=4&years=0&query=" + id));
        }
        public string GetCompanyId(string inn)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://focus.kontur.ru/search?region=&industry=&state=081077917&query=" + inn);
            if (this.proxy != null)
                request.Proxy = proxy;
            request.Headers.Add("Cookie", this.Cookie);

            string result = "";
            using (var response = request.GetResponse())
            {
                var match = System.Text.RegularExpressions.Regex.Match(response.ResponseUri.Query, "query=(?<id>[0-9]{13})");
                if (!match.Success)
                    new Exception("Ошибка получения id компании по входной информации заказа");

                result = match.Groups["id"].Value;
            }
            return result;
        }
    }
}