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
                            default:
                                {
                                    companyInfo.OtherCodes = node.ParentNode.InnerHtml.Replace("<dt>", "<br><dt>").Replace("dd", "span").Replace("dt", "span");
                                    break;
                                }
                                
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

                    RemoveElements(block, ".//span[@class=' oldConnection strikeThrough']");
                    RemoveElements(block, ".//a[@class='connectionsLink']");
                    RemoveElements(block, ".//span[@class='smallText lightGrey nowrap']");

                    RemoveP(block);

                    companyInfo.History = block.OuterHtml;
                    companyInfo.History = companyInfo.History.Replace("href", "attr").Replace("class=\"noMargin grey size15\"", "style='font-weight: bold;'").Replace("<h3>", "<h3 class='h3class'>");
                    companyInfo.History = GlobalReplacements(companyInfo.History);
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
                if (block.InnerText.Contains("Исполнительные производства"))
                {
                    companyInfo.BailiffsInfo = new BailiffsInfo();
                    var el=block.SelectSingleNode(".//*[@class='nowrap']");
                    if(el!=null)
                        companyInfo.BailiffsInfo.Sum = el.InnerText;
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

                ChangeNodeName(tr, ".//tr", "p");
                RemoveP(tr);
                RemoveElements(tr, ".//span[@class=' oldConnection strikeThrough']");
                RemoveElements(tr, ".//a[@class='connectionsLink']");

                founder.Name = tr.OuterHtml;

                founder.Name = GlobalReplacements(founder.Name);

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

                RemoveElements(item, ".//div[@class='peripheral textRight']");
                RemoveElements(item, ".//*[contains(text(),'Ещё')]");
                RemoveElements(item, ".//*[contains(text(),'Показать всех ')]");
                RemoveElements(item, ".//p[@class='kad-item-exLinkRow-p']");
                RemoveElements(item, ".//div[@class='halfMargin kad-item-instances hidden']");
                RemoveElements(item, ".//div[@class='kad-item-exLinkRow']");

                ChangeNodeName(item, ".//div[@class='kad-item-side']", "span");
                

                ReplaceElements(item, ".//div[@class='stickOut textRight stickOut__displayed stickOut_noBreak']", ".//span[contains(text(),'№')]");
                ChangeNodeName(item, ".//div[@class='stickOut textRight stickOut__displayed stickOut_noBreak']", "span");
                RemoveElements(item, ".//div[contains(@class, 'kli_tooltip inlineBlock')]");

                var sumEl = item.SelectSingleNode(".//*[@class='brown']");
                if(sumEl!= null)
                {
                    sumEl.PreviousSibling.PreviousSibling.InnerHtml = sumEl.OuterHtml + sumEl.PreviousSibling.PreviousSibling.InnerHtml;
                    sumEl.Remove();
                }

                var nodes = item.SelectNodes(".//*[@class='kad-item-side']");

                if (nodes!= null)
                {
                    for (int i = 2; i < nodes.Count; i += 2)
                    {
                        var prev = nodes[i].PreviousSibling;
                        if (prev != null)
                        {
                            prev = prev.PreviousSibling;

                            if (prev == null)
                                continue;

                            if (prev.InnerText.Contains("Истец") || prev.InnerText.Contains("Ответчик")|| prev.InnerText.Contains("Третье лицо") || prev.InnerText.Contains("Третьи лица") )
                                continue;
                            nodes[i].ParentNode.InsertBefore(HtmlNode.CreateNode("<br>"), nodes[i]);
                        }
                    }
                }
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        row.Name = "span";
                    }

                    _case.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы","");
                    _case.Number = _case.Number.Replace("href","attr");
                    _case.Number = _case.Number.Replace("class=\"block relative\"", "style='border: 1px solid #cdcdcd'");
                    _case.Number = _case.Number.Replace("underline", "kad-item-docLink");
                    _case.Number = _case.Number.Replace("style='display:none'","").Replace("Ответчики","");
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

                RemoveElements(item, ".//div[@class='peripheral textRight']");
                RemoveElements(item, ".//*[contains(text(),'Должник')]");
                RemoveElements(item, ".//*[contains(text(),'должника')]");
                RemoveElements(item, ".//*[@class='js-fssp-item-debtor hidden']");

                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    ChangeNodeName(item, ".//div[@class='stickOut kad-stickOut stickOut__displayed']", "span");
                    RemoveP(item);
                    
                    _case.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    _case.Number = _case.Number.Replace("href", "attr");
                    _case.Number = _case.Number.Replace("block relative kad-item size13","").Replace("fssp-item-department","").Replace("unevenIndent js-fssp-item", "");
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

                RemoveElements(item, ".//div[@class='peripheral textRight']");
                RemoveElementsAndNextNext(item, ".//*[contains(text(),'Орган')]");
                RemoveElementsAndNextNext(item, ".//*[contains(text(),'Работы')]");
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");

                if(rows!=null)
                {
                    RemoveP(item);

                    lic.Activity = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("<p class=\"noMargin\">", "").Replace("<div class=\"silversmall\">", "<div class=\"silversmall\">");
                    lic.Activity = lic.Activity.Replace("div","span");
                    lic.Activity = lic.Activity.Replace("noMargin alert", "alert");
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
            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[1]/span/sup");
            if (countEl == null) return null;
            info.Count = int.Parse(countEl.InnerText.GetHTMLDecoded());
            info.Sum = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[1]/span/i").InnerText.GetHTMLDecoded();

            var items = doc.DocumentNode.SelectNodes(".//li[@class='block relative size13']");
            if (items == null)
                return null;

            foreach(var item in items)
            {
                RemoveElements(item, ".//div[@class='peripheral textRight']");
                ChangeNodeName(item, ".//div[@class='stickOut textRight stickOut__displayed line23 marB3i']","span");
                ChangeNodeName(item, ".//div[@class='noMargin fauxh3']", "span");

                Contract contr = new Contract();
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    ChangeNodeName(item, ".//div[@class='stickOut kad-stickOut stickOut__displayed']", "span");
                    RemoveP(item);
                    
                    contr.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    contr.Number = contr.Number.Replace("href", "attr");
                    
                    contr.Number = contr.Number.Replace("block relative size13", "").Replace("</span><br>\r\n\t\t\r\n\t\t</span><br>", "</span></span><br>");
                    contr.Number = contr.Number.Replace("<span class=\"brown\">", "<span class='silversmall'>Сумма- </span><span class=\"brown\">");
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
                RemoveElements(item, ".//div[@class='peripheral textRight']");
                ChangeNodeName(item, ".//div[@class='stickOut textRight stickOut__displayed line23 marB3i']", "span");
                ChangeNodeName(item, ".//div[@class='noMargin fauxh3']", "span");

                Contract contr = new Contract();
                var rows = item.SelectNodes(".//div[@class='stickOut kad-stickOut stickOut__displayed']");
                if (rows != null)
                {
                    ChangeNodeName(item, ".//div[@class='stickOut kad-stickOut stickOut__displayed']", "span");
                    RemoveP(item);

                    contr.Number = item.OuterHtml.Replace("stickOut kad-stickOut stickOut__displayed", "silversmall").Replace("Показать все документы", "");
                    contr.Number = contr.Number.Replace("href", "attr");

                    contr.Number = contr.Number.Replace("block relative size13", "").Replace("</span><br>\r\n\t\t\r\n\t\t</span><br>", "</span></span><br>");
                    contr.Number = contr.Number.Replace("<span class=\"brown\">", "<span class='silversmall'>Сумма- </span><span class=\"brown\">");
                }
                info.Contracts.Add(contr);
            }

            return info;
        }
        public static List<RelatedCompany> RelatedCompanies(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            html = GlobalReplacements(html);
            doc.LoadHtml(html);

            var elements = doc.DocumentNode.SelectNodes("//div[@class='leftColumnGraph']");
            if (elements == null)
                return null;

            List<RelatedCompany> result = new List<RelatedCompany>();

            foreach (var element in elements)
            {
                RemoveElements(element, ".//span[@class='smallText lightGrey nowrap']");
                RemoveElements(element, ".//div[@class='peripheral textRight peripheral_lt480']");
                RemoveElements(element, ".//div[@class='stickOut stickOut__display1024 textRight leftNum']");
                RemoveElements(element, ".//*[contains(@class,'graphItemElement') and contains(.,'strikeThrough')]");
                RemoveElements(element, ".//*[contains(text(),'Подробнее')]");
                RemoveElements(element, ".//*[contains(text(),'Показать')]");

                RemoveElements(element, ".//a[@class='connectionsLink']");
                RemoveElements(element, ".//span[@class='percentUp']");
                RemoveElements(element, ".//span[@class='percentDown']");

                ChangeNodeName(element, ".//div[@class='inlineBlock floatRight ']", "span");
                ChangeNodeName(element, ".//div[@class='inlineBlock']", "span");
                
                RemoveP(element);

                var gitems = element.SelectNodes(".//*[contains(@class,'graphItemElement')]");
                if(gitems!= null)
                {
                    foreach(var it in gitems)
                    {
                        if(it.InnerHtml.Contains("strikeThrough"))
                        {
                            it.Remove();
                        }
                    }
                }

                RelatedCompany company = new RelatedCompany();
                company.Name = element.ParentNode.InnerHtml.Replace("href","attr").Replace("class=\"halfMargin mobile-stickOut graphItemElement\"","style=\"margin-left:30px;margin-top:10px;\"");

                try
                {
                    var el = element.ParentNode.NextSibling.NextSibling;

                    ChangeNodeName(el, ".//div[@class='inlineBlock floatRight ']", "span");
                    ChangeNodeName(el, ".//div[@class='inlineBlock']", "span");
                
                    RemoveElements(el, ".//*[contains(text(),'интернете')]");
                    RemoveElements(el, ".//*[contains(text(),'Учрежденные')]");
                    RemoveElements(el, ".//*[contains(text(),'ТПП')]");
                    RemoveElements(el, ".//*[contains(text(),'Лицензии')]");
                    RemoveElements(el, ".//*[contains(text(),'Товарные')]");

                    RemoveElements(el, ".//span[@class='percentUp']");
                    RemoveElements(el, ".//span[@class='percentDown']");

                    company.Name += "<br><br>" + el.InnerHtml.Replace("href", "attr");
                    company.Name = company.Name.Replace("<span>","<span class='silversmall'>");
                }
                catch { }

                result.Add(company);
            }
            return result;
        }
        public static List<string> BankruptMessages(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            
            var elements = doc.DocumentNode.SelectNodes(".//li[@class='block relative size13']");

            if (elements == null)
                return null;

            List<string> result = new List<string>();

            foreach (var element in elements)
            {
                var addEl = element.SelectSingleNode(".//div[@class='peripheral textRight']");
                if (addEl != null) addEl.InnerHtml = "";
                addEl = element.SelectSingleNode(".//*[contains(text(),'далее')]");
                if (addEl != null) addEl.InnerHtml = "";

                RemoveElements(element, ".//div[@class='peripheral textRight']");
                RemoveElements(element, ".//*[contains(text(),'далее')]");

                string message = element.OuterHtml.Replace("href","attr").Replace("class","classattr").Replace("classattr=\"stickOut textRight grey stickOut__displayed\"", "class=\"silversmall\"");
                result.Add(message);
            }
            return result;
        }
        public static List<RelatedCompany> Predecessors(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var elements = doc.DocumentNode.SelectNodes("//div[@class='leftColumnGraph']");
            if (elements == null)
                return null;

            List<RelatedCompany> result = new List<RelatedCompany>();

            foreach (var element in elements)
            {
                RelatedCompany comp = new RelatedCompany();
                comp.Name = element.SelectSingleNode(".//*[@class='graph-line-link marR12']").InnerText.GetHTMLDecoded();
                comp.OGRN = element.SelectSingleNode(".//*[@class='ogrn-inn']").InnerText;
                result.Add(comp);
            }
            return result;
        }
        private static string GetHTMLDecoded(this string input)
        {
            return HttpUtility.HtmlDecode(input).Replace("\t", "").Replace("\r", "").Replace("\n"," ").Trim();
        }

        private static void RemoveP(HtmlNode node)
        {
            var elements = node.SelectNodes(".//p");
            if(elements!=null)
            {
                foreach(var element in elements)
                {
                    element.ParentNode.InsertAfter(HtmlNode.CreateNode("<br>"),element);
                    element.Name = "span";
                }
            }
        }
        private static void RemoveElements(HtmlNode node, string xPath)
        {
            var elements = node.SelectNodes(xPath);
            if(elements!=null)
            {
                foreach(var element in elements)
                {
                    element.Remove();
                }
            }
        }
        private static void RemoveElementsAndNextNext(HtmlNode node, string xPath)
        {
            var elements = node.SelectNodes(xPath);
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    element.NextSibling.NextSibling.Remove();
                    element.Remove();
                }
            }
        }

        private static void ChangeNodeName(HtmlNode node,string xPath,string nodeName)
        {
            var elements = node.SelectNodes(xPath);
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    element.InnerHtml = element.OuterHtml.Replace(element.Name, nodeName);
                    element.Name = nodeName;
                }
            }
        }

        private static string GlobalReplacements(string html)
        {
            return html.Replace("noMargin smallText inline", "silversmall").Replace("ind2em", "silversmall").Replace("noMargin smallText smallTextBlock","silversmall").Replace("size11 darkGrey","silversmall").Replace("href","attr");
        }

        public static void ReplaceElements(HtmlNode node, string xPath1,string xPath2)
        {
            var el1 = node.SelectSingleNode(xPath1);
            var el2 = node.SelectSingleNode(xPath2);

            if (el1 != null && el2 != null)
            {
                string html1 = el1.InnerHtml;
                el1.InnerHtml = el2.InnerHtml;
                el2.InnerHtml = html1;
            }
        }
    }
}
