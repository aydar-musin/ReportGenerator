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
        static int PAGES = 5;

        public string Cookie { get; set; }
        private WebProxy proxy;
        public KonturWorker()
        {
            if(Settings.UseProxy)
            {
                ChangeProxy();
            }

#if DEBUG
            PAGES = 2;
#endif
        }
        private void ChangeProxy()
        {
            if (Settings.ProxyList.Count > 0)
            {
                Random rnd = new Random();

                int rndIndex = rnd.Next(0, Settings.ProxyList.Count);

                this.proxy = new WebProxy(Settings.ProxyList[rndIndex]);
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
            int tries = 30;
            req: try
            {
                Random rnd = new Random();
                System.Threading.Thread.Sleep(rnd.Next(500, 2000));

                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                if (Settings.UseProxy && this.proxy != null)
                    request.Proxy = proxy;

                request.UserAgent="Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";
                request.Timeout = 10000;

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
                    ChangeProxy();
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

            //if (info.ArbitrationExists)
            //{
            //    info.ArbitrAsPlaintiff = GetArbitrAsP(order.CompanyId);
            //    info.ArbitrAsRespondent = GetArbitrAsR(order.CompanyId);
            //    info.ArbitrAsThird = GetArbitrAsT(order.CompanyId);
            //}

            //if(info.LicensiesExist)//!!!!!!!
            //{
            //    info.Lics = Parser.Lics(Request("https://focus.kontur.ru/lics?query=" + order.CompanyId));
            //}
            if(info.BailiffsExist)
            {
                var inf = GetBailiffs(order.CompanyId);
                if (info.BailiffsInfo != null)
                    inf.Sum = info.BailiffsInfo.Sum;

                info.BailiffsInfo = inf;
            }
            if(info.ContractsExist)
            {
                info.WonContracts = GetContracts(order.CompanyId, "customers"); 
                info.PostedContracts = GetContracts(order.CompanyId, "suppliers");
            }
            info.Founders = GetFounders(order.CompanyId);
            info.Activities = GetActivities(order.CompanyId);
            info.Predecessors = GetPredecessors(order.CompanyId);
            info.RelatedCompanies = GetRelatedCompanies(order.CompanyId);
            info.BankruptMessages = Parser.BankruptMessages(Request("https://focus.kontur.ru/bankrots?query="+order.CompanyId));
            return info;
        }

        public string GetGeneralInfoPage(string id)
        {
            var page = Request("https://focus.kontur.ru/entity?query=" + id);
            return page;
        }
        public List<RelatedCompany> GetPredecessors(string id)
        {
            List<RelatedCompany> result = new List<RelatedCompany>();
            for(int i=1;i<=PAGES;i++)
            {
                var items= Parser.RelatedCompanies(Request(string.Format("https://focus.kontur.ru/graph?page={0}&filterFlags=268435456&order=29&query={1}",i,id)));
                if (items != null)
                    result.AddRange(items);
                else
                    break;
            }
            if (result.Count == 0)
                return null;
            return result;
        }
        public List<RelatedCompany> GetRelatedCompanies(string id)
        {
            List<RelatedCompany> result = new List<RelatedCompany>();
            for (int i = 1; i <= PAGES; i++)
            {
                var items = Parser.RelatedCompanies(Request(string.Format("https://focus.kontur.ru/graph?page={0}&query={1}", i, id)));
                if (items != null)
                    result.AddRange(items);
                else
                    break;
            }
            if (result.Count == 0)
                return null;
            return result;
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
            return GetArbitr(id, "0");
        }
        public ArbitrStat GetArbitrAsR(string id)
        {
            return GetArbitr(id, "1");
        }
        public ArbitrStat GetArbitrAsT(string id)
        {
            return GetArbitr(id, "4");
        }
        private ArbitrStat GetArbitr(string id, string type)
        {
            Func<string, ArbitrStat> get = null ;
            switch (type)
            {
                case "0":
                    { get = new Func<string, ArbitrStat>(Parser.ArbitrAsPlaitiff); break; }
                case "1":
                    { get = new Func<string, ArbitrStat>(Parser.ArbitrAsRespondent); break; }
                case "4":
                    { get = new Func<string, ArbitrStat>(Parser.ArbitrAsThird); break; }
            }
            var arb = get(Request(string.Format("https://focus.kontur.ru/kad?type={0}&years=0&query={1}", type, id)));

            if (arb != null && arb.Count > 10)
            {
                for (int i = 2; i <= PAGES; i++)
                {
                    var add_arb = get(Request(string.Format("https://focus.kontur.ru/kad?type={0}&years=0&query={1}&page={2}", type, id, i)));
                    if (add_arb != null)
                    {
                        arb.Cases.AddRange(add_arb.Cases);
                    }
                    else
                        break;
                }
            }
            return arb;
        }
        private BailiffsInfo GetBailiffs(string id)
        {
            var info = Parser.Bailiffs(Request("https://focus.kontur.ru/fssp?query=" +id));
            if (info != null&&info.Count>0)
            {
                for (int i = 2; i < PAGES; i++)
                {
                    var add_info = Parser.Bailiffs(Request("https://focus.kontur.ru/fssp?query=" + id+"&page="+i.ToString()));
                    if (add_info != null)
                        info.Cases.AddRange(add_info.Cases);
                    else
                        break;
                }
            }
            return info;
        }
        private ContractsInfo GetContracts(string id, string type)
        {
            if (type == "customers")
            {
                var info = Parser.WonContracts(Request("https://focus.kontur.ru/contracts?type=customers&query="+id));
                if (info != null && info.Count > 19)
                {
                    for (int i = 2; i <= PAGES; i++)
                    {
                        var addInfo = Parser.WonContracts(Request("https://focus.kontur.ru/contracts?type=customers&query=" + id+"&page="+i.ToString()));
                        if (addInfo != null)
                            info.Contracts.AddRange(addInfo.Contracts);
                        else break;
                    }
                }
                return info;
            }
            else
            {
                var info = Parser.PostedContracts(Request("https://focus.kontur.ru/contracts?type=suppliers&query=" + id));
                if (info != null && info.Count > 19)
                {
                    for (int i = 2; i <= PAGES; i++)
                    {
                        var addInfo = Parser.PostedContracts(Request("https://focus.kontur.ru/contracts?type=suppliers&query=" + id + "&page=" + i.ToString()));
                        if (addInfo != null)
                            info.Contracts.AddRange(addInfo.Contracts);
                        else break;
                    }
                }
                return info;
            }
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

        public  CompanyInfo GetSample()
        {
            CompanyInfo info = new CompanyInfo();
            info.NalogCode = Guid.NewGuid().ToString();
            info.Name = Guid.NewGuid().ToString();
            info.Address = Guid.NewGuid().ToString();
            info.FSS = Guid.NewGuid().ToString();
            info.INN = Guid.NewGuid().ToString();
            info.KPP = Guid.NewGuid().ToString();
            info.MainActiviyCode = Guid.NewGuid().ToString();
            info.ManagerAmplua = Guid.NewGuid().ToString();
            info.ManagerName = Guid.NewGuid().ToString();
            info.OGRN = Guid.NewGuid().ToString();
            info.OKPO = Guid.NewGuid().ToString();
            info.PhoneNumbers =new List<string>(){ Guid.NewGuid().ToString()};
            info.RegDate = Guid.NewGuid().ToString();
            info.Status = Guid.NewGuid().ToString();
            info.UstFond = Guid.NewGuid().ToString();
            info.WonContracts = new ContractsInfo() { Contracts = new List<Contract>() { new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() }, new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() }, new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() } } };
            info.PostedContracts = new ContractsInfo() { Contracts = new List<Contract>() { new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() }, new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() }, new Contract() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Description = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Name = Guid.NewGuid().ToString(), Sum = "1000000", TimeInterval = Guid.NewGuid().ToString() } } };
            info.Predecessors = new List<RelatedCompany>() { new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() }, new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() }, new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() } };
            info.RelatedCompanies = new List<RelatedCompany>() { new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() }, new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() }, new RelatedCompany() { Name = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), OGRN = Guid.NewGuid().ToString(), INN = Guid.NewGuid().ToString() } };
            info.BailiffsInfo = new BailiffsInfo() { Cases = new List<BailiffsCase>() { new BailiffsCase() { Number = "1231231", Date = DateTime.Now, Sum = "10000", Type = Guid.NewGuid().ToString() } } };
            
            info.Activities = new List<Activity>() { new Activity() { Name = Guid.NewGuid().ToString(), Code = "111.11" }, new Activity() { Name = Guid.NewGuid().ToString(), Code = "111.11" }, new Activity() { Name = Guid.NewGuid().ToString(), Code = "111.11" }, new Activity() { Name = Guid.NewGuid().ToString(), Code = "111.11" }, new Activity() { Name = Guid.NewGuid().ToString(), Code = "111.11" } };
            info.Lics = new List<Licency>() { new Licency() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Department = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Activity = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Period = DateTime.Now }, new Licency() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Department = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Activity = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Period = DateTime.Now }, new Licency() { Number = Guid.NewGuid().ToString(), Date = DateTime.Now, Department = Guid.NewGuid().ToString(), Status = Guid.NewGuid().ToString(), Activity = Guid.NewGuid().ToString(), Address = Guid.NewGuid().ToString(), Period = DateTime.Now } };

            return info;
        }
    }
}