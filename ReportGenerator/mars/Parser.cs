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
                        else if (fullmarginBlocks.InnerText.ToLower().Contains("директор"))
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
                    string currentSectionName = default(string);

                    var historyNodes = block.ChildNodes;
                    foreach (var node in historyNodes)
                    {
                        if (node.Name == "p")
                            currentSectionName = node.InnerText;


                        if (currentSectionName == "Другие названия" && node.Name == "div") // TODO: желательно проверить эту часть тщательней
                        {
                            // TODO: на страницах, где есть текст с такими скобками <> неправильно определяется нода, исправить

                            string previousName = node.ChildNodes[0].InnerText.Trim();
                            DateTime previousNameDate = default(DateTime);

                            var dateNode = node.ChildNodes.Where(s => s.InnerHtml.Contains("<span title=\"Дата внесения сведений в ЕГРЮЛ\">")).FirstOrDefault();
                            if (dateNode != null)
                                previousNameDate = DateTime.ParseExact(dateNode.InnerText.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture);

                            if (previousNameDate != default(DateTime))
                            {
                                var nameWithSameDate = companyInfo.OtherNames.Where(name => name.AddedDate == previousNameDate).FirstOrDefault();
                                if (nameWithSameDate != null)
                                    if (previousName.Length > nameWithSameDate.Value.Length)
                                        companyInfo.OtherNames.Remove(nameWithSameDate);

                                companyInfo.OtherNames.Add(new ValueWithDate(previousName, previousNameDate));
                            }
                        }
                        else if (currentSectionName == "Другие адреса" && node.Name == "div")
                        {
                            string previousAddress = default(string);
                            DateTime previousAddressDate = default(DateTime);

                            // Так было до 20.02.16 (R.I.P.) ????????????????
                            var addressNode = node.SelectSingleNode(".//span[@class=' underline']");
                            if (addressNode != null)
                                previousAddress = addressNode.InnerText;
                            else
                                previousAddress = node.FirstChild.InnerText.Trim();

                            var dateNode = node.ChildNodes.Where(s => s.InnerHtml.Contains("<span title=\"Дата внесения сведений в ЕГРЮЛ\">")).FirstOrDefault();
                            if (dateNode != null)
                                previousAddressDate = DateTime.ParseExact(dateNode.InnerText.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture);

                            companyInfo.OtherAddresses.Add(new ValueWithDate(previousAddress, previousAddressDate));
                        }
                        else if (currentSectionName == "Руководители" && node.Name == "div")
                        {
                            Manager man = new Manager();
                            var items = node.InnerText.Trim().Replace("\t", "").Replace("\r", "").Split(new char[] { '\n', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            man.Amplua = items[0];
                            man.Name = items[1];

                            if (items.Length > 2)
                                man.Count = int.Parse(items[2]);

                            foreach (var item in items)
                            {
                                if (char.IsDigit(item[0]) && item.Contains("."))
                                    man.Date = DateTime.Parse(item);
                                else if (char.IsDigit(item[0]) && item.Length == 12)
                                    man.INN = item;
                            }

                            companyInfo.LastManagers.Add(man);
                            //}
                        }
                        else if (currentSectionName == "Учредители" && node.Name == "div")
                        {
                            Founder f = new Founder();
                            f.Name = GetHTMLDecoded(node.InnerText);
                            companyInfo.LastFounders.Add(f);
                        }
                        else if (currentSectionName == "Уставный капитал" && node.Name == "div")
                        {
                            var items = node.InnerText.Replace("\t","").Replace("\r","").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            companyInfo.LastFonds.Add(new ValueWithDate(items[0], items.Length>1?DateTime.Parse(items[1]):DateTime.MinValue));
                        }
                    }
                }
                #endregion history

                #region finace
                if(block.InnerText.Contains("Финансовое состояние"))
                {
                    var nodes = block.SelectNodes("div/span");
                    if(nodes!=null)
                    {
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
                founder.Name = tds[1].Element("a").InnerText.GetHTMLDecoded();
                founder.Percent = tds[2].InnerText.GetHTMLDecoded();
                founder.Rubles = tds[3].InnerText.GetHTMLDecoded();

                DateTime.TryParse(tds[5].InnerText.GetHTMLDecoded(),out founder.Date);

                string args = tds[1].Element("div").InnerText.GetHTMLDecoded();
                var match = Regex.Match(args, "ОГРН: (?<ogrn>[0-9]{13})");
                if (match.Success)
                    founder.OGRN = match.Groups["ogrn"].Value;

                match = Regex.Match(args, "ИНН: (?<inn>[0-9]{10})");
                if (match.Success)
                    founder.INN = match.Groups["inn"].Value;

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

            var items = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[5]/div/div[5]/ul/li");
            if (items == null) return null;
            foreach(var item in items)
            {
                ArbitrCase _case = new ArbitrCase();
                var dateEl = item.SelectSingleNode("div[1]/div[2]/div[1]/div[1]");
                _case.Date = DateTime.Parse(dateEl.InnerText.GetHTMLDecoded().Replace("\n",""));
                _case.Number = dateEl.NextSibling.NextSibling.InnerText.GetHTMLDecoded();

                var TypeMatch = Regex.Match(dateEl.ParentNode.OuterHtml, "<br>(?<type>.*)<br>");
                if (TypeMatch.Success)
                    _case.Type = TypeMatch.Groups["type"].Value;

                var sumNode=dateEl.ParentNode.SelectSingleNode(".//span[@class='brown']");
                if (sumNode != null)
                    _case.Sum = sumNode.InnerText.GetHTMLDecoded();
                
                foreach(var sides in item.SelectNodes(".//div[@class='noMargin target ']/div"))
                {
                    if (sides.SelectSingleNode("div[1]").InnerText.Contains("Истец"))
                        _case.Plaintiff = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Ответчик"))
                        _case.Respondent = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Третье лицо"))
                        _case.Thirds = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                }
                stat.Cases.Add(_case);
            }
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

            var items = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[5]/div/div[5]/ul/li");
            if (items == null) return null;
            foreach (var item in items)
            {
                ArbitrCase _case = new ArbitrCase();
                var dateEl = item.SelectSingleNode("div[1]/div[2]/div[1]/div[1]");
                _case.Date = DateTime.Parse(dateEl.InnerText.GetHTMLDecoded().Replace("\n", ""));
                _case.Number = dateEl.NextSibling.NextSibling.InnerText.GetHTMLDecoded();
                var TypeMatch = Regex.Match(dateEl.ParentNode.OuterHtml, "<br>(?<type>.*)<br>");
                if (TypeMatch.Success)
                    _case.Type = TypeMatch.Groups["type"].Value;
                var sumNode = dateEl.ParentNode.SelectSingleNode(".//span[@class='brown']");
                if (sumNode != null)
                    _case.Sum = sumNode.InnerText.GetHTMLDecoded();

                foreach (var sides in item.SelectNodes(".//div[@class='noMargin target ']/div"))
                {
                    if (sides.SelectSingleNode("div[1]").InnerText.Contains("Истец"))
                        _case.Plaintiff = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Ответчик"))
                        _case.Respondent = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Третьи лица"))
                        _case.Thirds = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                }
                var others = item.SelectNodes(".//div[@class='noMargin target kad-item-row_hidden hidden']/div");
                if(others!=null)
                {
                    foreach(var sides in others)
                    {
                        if (sides.SelectSingleNode("div[1]").InnerText.Contains("Третьи лица"))
                            _case.Thirds = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    }
                }
                stat.Cases.Add(_case);
            }
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

            var items = doc.DocumentNode.SelectNodes("/html/body/div[1]/div[5]/div/div[5]/ul/li");
            if (items == null) return null;
            foreach (var item in items)
            {
                ArbitrCase _case = new ArbitrCase();
                var dateEl = item.SelectSingleNode("div[1]/div[2]/div[1]/div[1]");
                _case.Date = DateTime.Parse(dateEl.InnerText.GetHTMLDecoded().Replace("\n", ""));
                _case.Number = dateEl.NextSibling.NextSibling.InnerText.GetHTMLDecoded();

                var sumNode = dateEl.ParentNode.SelectSingleNode(".//span[@class='brown']");
                if (sumNode != null)
                    _case.Sum = sumNode.InnerText.GetHTMLDecoded();

                var TypeMatch = Regex.Match(dateEl.ParentNode.OuterHtml, "<br>(?<type>.*)<br>");
                if (TypeMatch.Success)
                    _case.Type = TypeMatch.Groups["type"].Value;

                foreach (var sides in item.SelectNodes(".//div[@class='noMargin target ']/div"))
                {
                    if (sides.SelectSingleNode("div[1]").InnerText.Contains("Истец"))
                        _case.Plaintiff = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Ответчик"))
                        _case.Respondent = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    else if (sides.SelectSingleNode("div[1]").InnerText.Contains("Третье лицо"))
                        _case.Thirds = sides.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                }
                stat.Cases.Add(_case);
            }
            return stat;
        }
        public static BailiffsInfo Bailiffs(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);


            BailiffsInfo info = new BailiffsInfo();

            var countEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[1]");
            if (countEl != null)
            {
                var match = Regex.Match(countEl.InnerText.GetHTMLDecoded(), "Найдено (?<count>[0-9]{1,10}) исполнительное");
                if (match.Success)
                    info.Count = match.Groups["count"].Value;
            }

            var kadItems = doc.DocumentNode.SelectNodes(".//li[@class='block relative kad-item size13']");

            

            foreach(var item in kadItems)
            {
                BailiffsCase _case = new BailiffsCase();
                _case.Date = DateTime.Parse(item.SelectSingleNode("div/div[2]/div/span[1]").InnerText.GetHTMLDecoded());
                _case.Number = item.SelectSingleNode("div/div[2]/div/span[2]/span").InnerText.GetHTMLDecoded();
                var sumEl = item.SelectSingleNode("div/div[2]/div/div[@class='kad-item-sum marT3_480']");
                if (sumEl != null) _case.Sum = sumEl.InnerText.GetHTMLDecoded();

                _case.Type = item.SelectSingleNode("div/div[3]").InnerText.GetHTMLDecoded();
                info.Cases.Add(_case);
            }
            return info;
        }

        public static List<Licency> Lics(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes(".//div[@class='block relative size13']");
            if (items == null)
                return null;
            List<Licency> result = new List<Licency>();
            
            foreach(var item in items)
            {
                Licency lic = new Licency();
                lic.Date = DateTime.Parse(item.SelectSingleNode("div/div[1]").InnerText.GetHTMLDecoded());
                lic.Number = item.SelectSingleNode("div/p[1]").InnerText.GetHTMLDecoded();

                foreach(var subItem in item.SelectNodes("div/div"))
                {
                    if(subItem.InnerText.Contains("Статус"))
                    {
                        lic.Status = subItem.NextSibling.InnerText.GetHTMLDecoded();
                    }
                    else if(subItem.InnerText.Contains("Период действия"))
                    {
                        DateTime date;
                        if (DateTime.TryParse(subItem.NextSibling.InnerText.GetHTMLDecoded(), out date))
                            lic.Date = date;
                        
                    }
                    else if(subItem.InnerText.Contains("Вид деятельности"))
                    {
                        var el = subItem.SelectSingleNode("div[2]");
                        if (el != null)
                            lic.Activity = el.InnerText.GetHTMLDecoded();
                        else
                            lic.Activity = subItem.NextSibling.NextSibling.InnerText;
                            
                    }
                    else if(subItem.InnerText.Contains("Орган"))
                    {
                        lic.Department = subItem.SelectSingleNode("p[1]").InnerText.GetHTMLDecoded();
                    }
                    else if(subItem.InnerText.Contains("Адрес"))
                    {
                        lic.Address = subItem.SelectSingleNode("div[2]").InnerText.GetHTMLDecoded();
                    }
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
            info.Sum = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[2]/div[1]/ul/li[1]/span/i").InnerText.GetHTMLDecoded();

            var items = doc.DocumentNode.SelectNodes(".//li[@class='block relative size13']");
            if (items == null)
                return null;

            foreach(var item in items)
            {
                Contract contr = new Contract();
                contr.Date = DateTime.Parse(item.SelectSingleNode("div/div[1]/div[1]").InnerText.GetHTMLDecoded());
                contr.Name = item.SelectSingleNode("div/div[1]/div[2]/a").InnerText.GetHTMLDecoded();
                contr.Sum = item.SelectSingleNode("div/div[1]/div[3]/span").InnerText.GetHTMLDecoded();

                foreach (var subItem in item.SelectNodes("div"))
                {
                    if(subItem.FirstChild.InnerText.GetHTMLDecoded().Contains("Описание"))
                    {
                        contr.Description = subItem.InnerText.Replace("Описание","").GetHTMLDecoded();
                    }
                    else if(subItem.FirstChild.InnerText.GetHTMLDecoded()=="")
                    {
                        contr.Number = subItem.InnerText.GetHTMLDecoded();
                    }
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
                Contract contr = new Contract();
                contr.Date = DateTime.Parse(item.SelectSingleNode("div/div[1]/div[1]").InnerText.GetHTMLDecoded());
                contr.Name = item.SelectSingleNode("div/div[1]/div[2]/a").InnerText.GetHTMLDecoded();
                contr.Sum = item.SelectSingleNode("div/div[1]/div[3]/span").InnerText.GetHTMLDecoded();

                foreach (var subItem in item.SelectNodes("div/div"))
                {
                    if (subItem.InnerText.GetHTMLDecoded().Contains("Описание"))
                    {
                        contr.Description = subItem.InnerText.Replace("Описание", "").GetHTMLDecoded();
                    }
                    else if (subItem.InnerText.GetHTMLDecoded().Contains("№"))
                    {
                        contr.Number = subItem.InnerText.GetHTMLDecoded();
                    }
                }
                info.Contracts.Add(contr);
            }

            return info;
        }
        public static List<RelatedCompany> RelatedCompanies(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            var elements = doc.DocumentNode.SelectNodes("//div[@class='rigidIndent graphInfoInBlock js-graph-subject']");
            if (elements == null)
                return null;

            List<RelatedCompany> result = new List<RelatedCompany>();

            foreach (var element in elements)
            {
                RelatedCompany company = new RelatedCompany();
                company.Name = element.SelectSingleNode(".//a[@class='graph-line-link marR12']").InnerText.GetHTMLDecoded();

                var ogrninn = element.SelectSingleNode("div[@class='ogrn-inn']");
                if (ogrninn != null)
                {
                    string innerText = ogrninn.InnerText.GetHTMLDecoded();
                    var match = Regex.Match(innerText, "ИНН: (?<inn>[0-9]{9})");
                    if (match.Success)
                        company.INN = match.Groups["inn"].Value;
                    
                    match = Regex.Match(innerText,"ОГРН: (?<ogrn>[0-9]{13})");
                    if(match.Success)
                        company.OGRN=match.Groups["ogrn"].Value;
                }

                var statusEl = element.SelectSingleNode(".//div[@class='green']");
                if (statusEl != null)
                    company.Status = statusEl.InnerText.GetHTMLDecoded();
                else
                {
                    statusEl = element.SelectSingleNode(".//div[@class='alert']");
                    if (statusEl != null)
                        company.Status = statusEl.InnerText.GetHTMLDecoded();
                }
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
