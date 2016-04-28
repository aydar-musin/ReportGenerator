using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Collections.Generic;
using ReportGenerator;

namespace ReportGenerator
{
    static class Parser
    {
        public static Order Order(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            Order order = new Order();
            var match=Regex.Match(doc.DocumentNode.InnerText, "Проверка организации;(?<input>[0-9]{10,13})");
            if (!match.Success)
                throw new Exception("Ошибка получения ИНН или ОГРН заказа");

            string inputNum = match.Groups["input"].Value;
            order.CompanyINNOGRN = inputNum;
            match = Regex.Match(doc.DocumentNode.InnerText, @"Данные отправителя(?<email>.*)Доступно");

            if (!match.Success)
                throw new Exception("Ошибка получения email заказчика");
            order.CustomerEmail = match.Groups["email"].Value;

            match = Regex.Match(doc.DocumentNode.InnerText, "Дата и время(?<date>.*)Пополнение");
            if (!match.Success)
                throw new Exception("Ошибка получения даты заказа");

            order.TimeStamp = DateTime.Parse(match.Groups["date"].Value);

            return order;
        }
        public static CompanyInfo GeneralInfo(string html)
        {
            var companyInfo = new CompanyInfo();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var navLinksNodes = htmlDocument.DocumentNode.SelectNodes("//div[@class='relative navLinks']//a[@class='navLink ']");
            if (navLinksNodes != null)
                foreach (var node in navLinksNodes)
                {
                    string value = node.InnerText.Trim();
                    switch (value)
                    {
                        case "Исп.&nbsp;производства":
                            companyInfo.BailiffsExist = true;
                            break;
                        case "Арбитраж":
                            companyInfo.ArbitrationExists = true;
                            break;
                        case "Госконтракты":
                            companyInfo.ContractsExist = true;
                            break;
                        case "Лицензии":
                            companyInfo.LicensiesExist = true;
                            break;
                        case "Тов. знаки":
                            companyInfo.TrademarksExist = true;
                            break;
                    }
                }

            var mainBlocks = htmlDocument.DocumentNode.SelectNodes("//div[@class='unevenIndent']");
            foreach (var block in mainBlocks)
            {
                #region codes
                if (block.InnerHtml.Contains("<dt>ИНН</dt>") || block.InnerHtml.Contains("<dt>КПП</dt>") ||
                    block.InnerHtml.Contains("<dt>ОГРН</dt>") || block.InnerHtml.Contains("<dt>ОКПО</dt>"))
                {
                    if (string.IsNullOrEmpty(companyInfo.FullName))
                    { 
                        var nameEl = htmlDocument.DocumentNode.SelectSingleNode(@"//*[@id='org-content']/div[1]/div[1]/div[2]/div[1]/h1");
                        if (nameEl != null)
                            companyInfo.Name = nameEl.InnerText.GetHTMLDecoded();

                        companyInfo.FullName = block.SelectSingleNode("./h4[1]").InnerText;

                        if (block.InnerHtml.Contains("Наименование на иностранном языке"))
                            companyInfo.LatName = block.SelectSingleNode("./h4[2]").InnerText.Trim();
                    }
                    var codesNodes = block.SelectNodes(".//dl//dt");
                    foreach (var node in codesNodes)
                        switch (node.InnerText)
                        {
                            case "ИНН":
                                companyInfo.INN = node.NextSibling.InnerText;
                                break;
                            case "КПП":
                                companyInfo.KPP = node.NextSibling.InnerText;
                                break;
                            case "ОГРН":
                                companyInfo.OGRN = node.NextSibling.InnerText;
                                break;
                            case "ОКПО":
                                companyInfo.OKPO = node.NextSibling.InnerText;
                                break;
                            case "ФОМС":
                                companyInfo.FOMS = node.NextSibling.InnerText;
                                break;
                            case "ФСС":
                                companyInfo.FSS = node.NextSibling.InnerText;
                                break;
                        }
                }
                #endregion codes

                #region status

                if (string.IsNullOrEmpty(companyInfo.Status))
                {
                    HtmlNode statusnode = null;

                    if (block.InnerHtml.Contains("fullMargin alert"))
                        statusnode = block.SelectSingleNode(".//div[@class='fullMargin alert']");
                    else if (block.InnerHtml.Contains("fullMargin green"))
                        statusnode = block.SelectSingleNode(".//div[@class='fullMargin green']");
                    else if (block.InnerHtml.Contains("fullMargin warning"))
                        statusnode = block.SelectSingleNode(".//div[@class='fullMargin warning']");

                    if (statusnode != null)
                        companyInfo.Status = statusnode.InnerText.Trim();
                }
                #endregion status

                #region address
                if (block.InnerHtml.Contains("<span class=\"underline\">Осмотреть</span>"))
                {
                    var innerBlocks = block.SelectNodes(".//div[@class='fullMargin']");
                    foreach (var innerBlock in innerBlocks)
                        if (innerBlock.InnerHtml.Contains("<span class=\"underline\">Осмотреть</span>"))
                        {
                            var addressBlock = innerBlock;

                            var addressDateNode = addressBlock.SelectSingleNode(".//span[@title='Дата внесения сведений в ЕГРЮЛ']");
                            companyInfo.AddressAddedDate = DateTime.ParseExact(addressDateNode.InnerText, "dd.MM.yyyy", CultureInfo.InvariantCulture);

                            var match = Regex.Match(addressBlock.InnerHtml, "<span class=\"smallText lightGrey nowrap\" title=\"Приблизительное число результатов поиска\"><span class=\"ico ico_loupe\"></span>(?<count>.*)</span></a>");
                            if (match.Success)
                                companyInfo.AddressCount = int.Parse(match.Groups["count"].Value);

                            addressBlock.InnerHtml = Regex.Replace(addressBlock.InnerHtml, "<span class=\"smallText lightGrey nowrap\" title=\"Приблизительное число результатов поиска\"><span class=\"ico ico_loupe\"></span>.*</span></a>", string.Empty);
                            addressBlock.InnerHtml = Regex.Replace(addressBlock.InnerHtml, "<a class=\"non-printable nowrap noLine map-link\" target=\"_blank\" href=.*><span class=\"ico ico_map\"></span><span class=\"underline\">Осмотреть</span></a>", string.Empty);
                            addressBlock.InnerHtml = Regex.Replace(addressBlock.InnerHtml, "<p class=\"noMargin smallText \"><span title=\"Дата внесения сведений в ЕГРЮЛ\">.*</span></p>", string.Empty);

                            foreach (var node in addressBlock.ChildNodes)
                            {
                                string innerText = node.InnerText.Trim();
                                if (!string.IsNullOrEmpty(innerText))
                                    companyInfo.Address += innerText + "\t";
                            }

                            companyInfo.Address = companyInfo.Address.Trim();
                        }
                }
                #endregion address

                #region manager

                var fullMarginBloksCollection = block.SelectNodes(".//div[@class='fullMargin']");
                if (fullMarginBloksCollection != null)
                {
                    foreach (var fullmarginBlocks in fullMarginBloksCollection)
                    {
                        if (fullmarginBlocks.InnerHtml.Contains("company-phone-block"))
                        {
                            var nodes = fullmarginBlocks.InnerText.Replace("\t", "").Replace("\r", "").Replace("&nbsp", "").Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var node in nodes.Where(s => s.StartsWith("+7")))
                                companyInfo.PhoneNumbers.Add(node.Trim());

                        }
                        else if (fullmarginBlocks.InnerText.Contains("Код налогового органа: "))
                        {
                            companyInfo.NalogCode = fullmarginBlocks.FirstChild.InnerText.Split(':')[1].Trim();

                            if (fullmarginBlocks.InnerText.Contains("Дата постановки на учет:"))
                            {
                                companyInfo.RegDate = fullmarginBlocks.ChildNodes[2].InnerText.Split(':')[1].Trim();
                            }
                        }
                        else if (fullmarginBlocks.InnerText.ToLower().Contains("директор")|| 
                            fullmarginBlocks.InnerText.ToLower().Contains("президент") ||
                            fullmarginBlocks.InnerText.ToLower().Contains("управляющий") ||
                            fullmarginBlocks.InnerText.ToLower().Contains("председатель")
                            )
                        {
                            var items = fullmarginBlocks.InnerText.Trim().Replace("\t", "").Replace("\r", "").Replace("&nbsp", "").Split(new char[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
                            companyInfo.ManagerAmplua = items[0];
                            companyInfo.ManagerName = items[1];

                            if (items.Length > 2)
                                companyInfo.ManagerCount = int.Parse(items[2]);

                            foreach (var item in items)
                            {
                                if (char.IsDigit(item[0]) && item.Contains("."))
                                    companyInfo.ManagerAddedDate = DateTime.Parse(item);
                                else if (char.IsDigit(item[0]) && item.Length == 12)
                                    companyInfo.ManagerINN = item;
                            }
                        }
                    }
                }
                //var  ergherh = block.SelectNodes(".//div[@class='fullMargin']//div[@class='smallTextBlock']//p[@class='noMargin smallText ']");
                //var managerBlock = block.SelectSingleNode(".//div[@class='fullMargin']//div[@class='smallTextBlock']//p[@class='noMargin smallText ']");
                //if (managerBlock != null)
                //{
                //    string managerDate = managerBlock.SelectSingleNode(".//span[@title='Дата внесения сведений в ЕГРЮЛ']").InnerHtml;
                //    companyInfo.ManagerAddedDate = DateTime.ParseExact(managerDate, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                //}

                #endregion manager

                #region history
                if (block.InnerHtml.Contains("<h3>История</h3>"))
                {
                    var addEl = block.SelectNodes(".//a[@class='connectionsLink']");
                    if (addEl != null)
                    {
                        foreach (var el in addEl)
                            el.InnerHtml = "";
                    }

                    companyInfo.History = block.OuterHtml;
                    companyInfo.History = companyInfo.History.Replace("href","attr").Replace("noMargin grey size15", "silversmall");
                }
                #endregion history

                #region finace
                if(block.InnerText.Contains("Финансовое состояние"))
                {
                    
                    var nodes = block.SelectNodes("div/span");
                    if(nodes!=null)
                    {
                        var match = Regex.Match(block.InnerText, "Финансовое состояние на (?<year>[0-9]{4}) год");
                        companyInfo.FinYear = match.Groups["year"].Value;

                        foreach (var row in nodes)
                        {
                            if (row.InnerText.Contains("Баланс"))
                                companyInfo.FinBalance = row.InnerText.GetHTMLDecoded().Replace("Баланс —", "");
                            else if (row.InnerText.Contains("Выручка"))
                                companyInfo.FinProfit = row.InnerText.GetHTMLDecoded().Replace("Выручка —", "");
                            else if (row.InnerText.Contains("Чистая прибыль"))
                                companyInfo.FinNetProfit = row.InnerText.GetHTMLDecoded().Replace("Чистая прибыль —", "");
                        }
                    }
                }
                #endregion finance

                if(block.InnerText.Contains("Уставный капитал:"))
                {
                    var el = block.ChildNodes.First(n => n.InnerText.Contains("Уставный капитал:"));
                    var match = Regex.Match(el.InnerText.GetHTMLDecoded(), "Уставный капитал:(?<sum>.*)руб.");
                    if(match.Success) companyInfo.UstFond=match.Groups["sum"].Value;
                }

                if(block.InnerText.Contains("Особые реестры ФНС"))
                {
                    companyInfo.SpecialReestrs = block.InnerHtml.Replace("href","attr").Replace("green underline", "alert underline").Replace("источник","").Replace("<h3>Особые реестры ФНС</h3>", "<h3 style=\"color: red; \">Особые реестры ФНС</h3>");
                }
            }

            return companyInfo;
        }
        public static IEnumerable<Activity> Activites(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<Activity> result = new List<Activity>();

            if (html.Contains("Найдено 0 вида деятельности"))
                return result;

            var el = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[3]/div/div");

            var divs = el.Elements("div").ToArray();

            for (int i = 0; i < divs.Length; i += 2)
            {
                Activity act = new Activity();
                act.Code = GetHTMLDecoded(divs[i].InnerText);
                act.Name = GetHTMLDecoded(divs[i].NextSibling.NextSibling.InnerText);
                result.Add(act);
            }

            return result;
        }
        public static IEnumerable<Founder> Founders(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var egrulItem = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[3]/div[1]/div/table");
            if (egrulItem == null)
                egrulItem = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[3]/div[2]/div/table");

            if (egrulItem == null)
                return new List<Founder>();

            var trs = egrulItem.Element("tbody").Elements("tr");

            List<Founder> result = new List<Founder>();

            foreach (var tr in trs.Skip(1))
            {
                Founder founder = new Founder();
                var tds = tr.Elements("td").ToArray();
                tds[0].InnerHtml = "";
                founder.Name = tr.InnerHtml.Replace("href","attr").Replace("td","p");

                //founder.Percent = tds[2].InnerText.GetHTMLDecoded();
                //founder.Rubles = tds[3].InnerText.GetHTMLDecoded();

                //DateTime.TryParse(tds[5].InnerText.GetHTMLDecoded(),out founder.Date);

                //string args = tds[1].Element("div").InnerText.GetHTMLDecoded();
                //var match = Regex.Match(args, "ОГРН: (?<ogrn>[0-9]{13})");
                //if (match.Success)
                //    founder.OGRN = match.Groups["ogrn"].Value;

                //match = Regex.Match(args, "ИНН: (?<inn>[0-9]{10})");
                //if (match.Success)
                //    founder.INN = match.Groups["inn"].Value;

                result.Add(founder);
            }
            return result;
        }

        public static ArbitrStat ArbitrAsPlaitiff(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            ArbitrStat stat = new ArbitrStat();

            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[2]/span");
            var match = Regex.Match(countEl.InnerText.GetHTMLDecoded(), @"Истец \((?<count>[0-9]*)\)");
            stat.Count = int.Parse(match.Groups["count"].Value);

            var sumEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[2]/span/i");
            stat.Sum = sumEl != null ? sumEl.InnerText.GetHTMLDecoded() : "";

            stat.Cases = ParseArbitrCases(doc);
            if (stat.Cases == null)
                return null;
            return stat;
        }
        public static ArbitrStat ArbitrAsRespondent(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            ArbitrStat stat = new ArbitrStat();

            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[1]/span");
            var match = Regex.Match(countEl.InnerText.GetHTMLDecoded(), @"Ответчик \((?<count>[0-9]*)\)");
            stat.Count = int.Parse(match.Groups["count"].Value);

            var sumEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[1]/span/i");
            stat.Sum = sumEl != null ? sumEl.InnerText.GetHTMLDecoded() : "";

            stat.Cases = ParseArbitrCases(doc);
            if (stat.Cases == null)
                return null;
            return stat;
        }
        public static ArbitrStat ArbitrAsThird(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            ArbitrStat stat = new ArbitrStat();

            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[3]/span");
            var match = Regex.Match(countEl.InnerText.GetHTMLDecoded(), @"Другое \((?<count>[0-9]*)\)");
            stat.Count = int.Parse(match.Groups["count"].Value);

            var sumEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div/ul/li[3]/span/i");
            stat.Sum = sumEl != null ? sumEl.InnerText.GetHTMLDecoded() : "";

            stat.Cases = ParseArbitrCases(doc);
            if (stat.Cases == null)
                return null;
            return stat;
        }
        private static List<ArbitrCase> ParseArbitrCases(HtmlDocument doc)
        {
            List<ArbitrCase> cases = new List<ArbitrCase>();
   
            var items = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[5]/div/div[5]/ul/li/div");
            if (items == null) return null;
            foreach (var item in items)
            {
                ArbitrCase _case = new ArbitrCase();

                var countEl = item.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (countEl != null) countEl.InnerHtml = "";
                var addEl = item.SelectSingleNode(".//*[contains(text(),'Ещё')]");
                if (addEl != null)
                    addEl.InnerHtml = "";
                addEl = item.SelectSingleNode(".//*[contains(text(),'Показать всех ')]");
                if (addEl != null)
                    addEl.InnerHtml = "";

                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        row.Name = "span";
                    }
                    _case.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы","");
                    _case.Number = _case.Number.Replace("href","attr");
                }
                
                cases.Add(_case);
            }
            return cases;
        }

        public static BailiffsInfo Bailiffs(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);


            BailiffsInfo info = new BailiffsInfo();

            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[1]");
            if (countEl != null)
            {
                var match = Regex.Match(countEl.InnerText.GetHTMLDecoded(), "Найдено (?<count>[0-9]{1,10}) ((исполнительных)|(исполнительное))");
                if (match.Success)
                    info.Count = int.Parse(match.Groups["count"].Value);
            }

            var kadItems = doc.DocumentNode.SelectNodes(".//li[@class='block relative kad-item size13']");
            if (kadItems == null)
                return null;
            

            foreach(var item in kadItems)
            {
                BailiffsCase _case = new BailiffsCase();

                var addEl = item.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (addEl != null) addEl.InnerHtml = "";
                addEl = item.SelectSingleNode(".//*[contains(text(),'Должник')]");
                if (addEl != null)
                {
                    addEl.InnerHtml = "";
                    addEl.ParentNode.InnerHtml = "";
                }
                addEl = item.SelectSingleNode(".//*[contains(text(),'должника')]");

                if (addEl != null) addEl.InnerHtml = "";

                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        row.Name = "span";
                    }
                    _case.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    _case.Number = _case.Number.Replace("href", "attr");
                    _case.Number = _case.Number.Replace("div","p");
                    _case.Number = _case.Number.Replace("block relative kad-item size13","");
                }
                info.Cases.Add(_case);
            }
            return info;
        }

        public static List<Licency> Lics(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes(".//div[@class='unevenIndent']");
            if (items == null)
                return null;
            List<Licency> result = new List<Licency>();
            
            foreach(var item in items)
            {
                Licency lic = new Licency();

                var countEl = item.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (countEl != null) countEl.InnerHtml = "";
                lic.Activity = item.OuterHtml;

                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if(rows!=null)
                {
                    foreach(var row in rows)
                    {
                        string old = row.OuterHtml;
                        row.Name = "span";

                        item.OuterHtml.Replace(old, row.OuterHtml);
                    }
                    lic.Activity = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("<p class=\"noMargin\">", "").Replace("<div class=\"silversmall\">", "<p class=\"noMargin\"><div class=\"silversmall\">");
                    lic.Activity = lic.Activity.Replace("div","span");
                }
                result.Add(lic);
            }
            return result;
        }

        public static ContractsInfo WonContracts(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            ContractsInfo info = new ContractsInfo();
            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[2]/span/sup");
            if (countEl == null) return null;
            info.Count = int.Parse(countEl.InnerText.GetHTMLDecoded());
            info.Sum = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[2]/span/i").InnerText.GetHTMLDecoded();

            var items = doc.DocumentNode.SelectNodes(".//li[@class='block relative size13']");
            if (items == null)
                return null;

            foreach(var item in items)
            {
                var addEl = item.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (addEl != null) addEl.InnerHtml = "";

                Contract contr = new Contract();
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        row.Name = "span";
                    }
                    contr.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    contr.Number = contr.Number.Replace("href", "attr");
                    contr.Number = contr.Number.Replace("div", "p");
                    contr.Number = contr.Number.Replace("block relative size13", "");
                }
                info.Contracts.Add(contr);
            }

            return info;
        }
        public static ContractsInfo PostedContracts(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            ContractsInfo info = new ContractsInfo();
            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[2]/span/sup");
            if (countEl == null) return null;
            info.Count = int.Parse(countEl.InnerText.GetHTMLDecoded());
            info.Sum = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[2]/span/i").InnerText.GetHTMLDecoded();

            var items = doc.DocumentNode.SelectNodes(".//li[@class='block relative size13']");
            if (items == null)
                return null;

            foreach (var item in items)
            {
                var addEl = item.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (addEl != null) addEl.InnerHtml = "";

                Contract contr = new Contract();
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        row.Name = "span";
                    }
                    contr.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    contr.Number = contr.Number.Replace("href", "attr");
                    contr.Number = contr.Number.Replace("div", "p");
                    contr.Number = contr.Number.Replace("block relative size13", "");
                }
                info.Contracts.Add(contr);
            }

            return info;
        }
        public static List<RelatedCompany> RelatedCompanies(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var elements = doc.DocumentNode.SelectNodes("//div[@class='leftColumnGraph']");
            if (elements == null)
                return null;

            List<RelatedCompany> result = new List<RelatedCompany>();

            foreach (var element in elements)
            {
                var addEl = element.SelectSingleNode(".//div[@class='peripheral textRight peripheral_lt480']");
                if (addEl != null) addEl.InnerHtml = "";
                addEl = element.SelectSingleNode(".//div[@class='stickOut stickOut__display1024 textRight leftNum']");
                if (addEl != null) addEl.InnerHtml = "";

                RelatedCompany company = new RelatedCompany();
                company.Name = element.ParentNode.InnerHtml.Replace("href","attr");
                company.Name += "<br><br>"+element.ParentNode.NextSibling.NextSibling.InnerHtml.Replace("href", "attr");

                result.Add(company);
            }
            return result;
        }
        private static string GetHTMLDecoded(this string input)
        {
            return HttpUtility.HtmlDecode(input).Replace("\t", "").Replace("\r", "").Replace("\n"," ").Trim();
        }
    }
}
