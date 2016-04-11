using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace ReportGenerator
{
    class Parser
    {
        public static GeneralInfo ParseGeneralInfo(string page)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);

            GeneralInfo info = new GeneralInfo();
            info.Name = doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[1]/div[1]/div[2]/div[1]/h1").InnerText;
            info.FullName = doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/h4").InnerText;
            info.INN = doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/dl[1]/dd[1]").InnerText;
            info.KPP= doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/dl[1]/dd[2]").InnerText;
            info.OGRN= doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/dl[1]/dd[3]").InnerText;
            info.OKPO= doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/dl[1]/dd[4]").InnerText;
            info.RegDate= doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/div[2]/p").InnerText;
            
            string addrStr= doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/div[3]").InnerText;
            info.NalogCode = doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/div[4]").InnerText;
            info.PhoneNumber = doc.DocumentNode.SelectSingleNode("//*[@id=\"org - content\"]/div[2]/div/div[2]/div/div/div[5]/div[1]/div[2]/a/span[1]").InnerText;
            return info;
        }
        
        public static Info GetLicensies(string page)
        {
            Info info = new Info();
            info.Name = "Лицензии";
            info.Items = new List<Dictionary<string, string>>();

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);
            var elements = doc.DocumentNode.SelectNodes("//*[contains(concat(' ', @class, ' '), ' block relative size13 ')]");

            foreach (var item in elements)
            {
                Dictionary<string, string> values = new Dictionary<string, string>();
                values.Add("date", item.SelectSingleNode("div/div[0]").InnerText);
                values.Add("number", item.SelectSingleNode("div/p[0]").InnerText);
                values.Add("period", item.SelectSingleNode("div/p[1]").InnerText);
                values.Add("activity", item.SelectSingleNode("div/p[2]").InnerText);
                values.Add("organ", item.SelectSingleNode("div/div[3]/p[0]").InnerText);
                values.Add("address", item.SelectSingleNode("div/div[4]/div[1]").InnerText);
                info.Items.Add(values);
            }

            var countersEl = doc.DocumentNode.SelectSingleNode("/html/body/div[1]/div[5]/div/div[1]/div");
            foreach(var row in countersEl.Elements("div"))
            {
                string text = row.InnerText;
                var match = Regex.Match(text, @".\((?<count>\d{1,3})\).");
                if (match.Success)
                    info.TotalCount += int.Parse(match.Groups["count"].Value);
            }
            return info;
        }
        public static Info ContractsWon(string page)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(page);

            var elements = doc.DocumentNode.SelectNodes("//*[contains(concat(' ', @class, ' '), ' block relative size13 ')]");

            return null;
        }

    }
}
