using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
namespace ReportGenerator
{
    class ReportGenerator
    {
        private const string BR = "<br><br><br>";
        private static void ConvertDocToDocx(string path)
        {
            int tries = 2;

            while (tries > 0)
            {
                try
                {
                    CloseWord();
                    Application word = new Application();

                    if (path.ToLower().EndsWith(".doc"))
                    {
                        var sourceFile = new FileInfo(path);
                        var document = word.Documents.Open(sourceFile.FullName);

                        string newFileName = sourceFile.FullName.Replace(".doc", ".docx");
                        document.SaveAs2(newFileName, WdSaveFormat.wdFormatXMLDocument,
                                         CompatibilityMode: WdCompatibilityMode.wdWord2010);

                        word.ActiveDocument.Close();
                        word.Quit();

                        //File.Delete(path);
                    }
                }
                catch (Exception ex)
                {
                    tries--;
                    if (tries == 0)
                        throw ex;
                }
            }
        }
        private static void CloseWord()
        {
            try
            {
                var prcs = System.Diagnostics.Process.GetProcessesByName("WINWORD");
                foreach (var prc in prcs)
                    prc.Kill();

                System.Threading.Thread.Sleep(1000);

                prcs = System.Diagnostics.Process.GetProcessesByName("WINWORD");
                foreach (var prc in prcs)
                    prc.Kill();
            }
            catch
            {

            }
        }
        private static string GenerateTR(params object[] input)
        {
            string result = "<div>";

            for (int i = 0; i < input.Length; i++)
            {
                result += "<p>" + input[i].ToString() + "<td/>";
            }
            result += "<tr/>";
            return result;
        }
        public static string Generate(CompanyInfo info, Order order)
        {
            string html = GeneralInfo(info) +BR+SpecialReestrs(info)+ BR + Bankrupt(info.BankruptMessages) + BR + Founders(info) + BR + Activities(info) + BR + Lics(info) + BR + Finance(info) + BR + Arbitr(info) + Contracts(info) + BR + Bailiffs(info) + BR + History(info) + BR + RelatedCompanies(info.RelatedCompanies, "Связанные организации") + BR + RelatedCompanies(info.Predecessors, "Предшественники");
            html = GetTemplate() + html + "</body> </html>";

            string fileName = "temp/" + order.CompanyINNOGRN + "_" + order.CustomerEmail + ".doc";

            File.WriteAllText(fileName, html);
            ConvertDocToDocx(fileName);

            return fileName.Replace(".doc", ".docx");
        }
        private static string GetTemplate()
        {
            string html = "<html  xmlns:v=\"urn:schemas-microsoft-com:vml\" xmlns:o=\"urn:schemas-microsoft-com:office:office\" xmlns:x=\"urn:schemas-microsoft-com:office:word\" xmlns=\"http://www.w3.org/TR/REC-html40\">" +
@"<style type='text/css'>"+style()+ @"
</style>

<body>
     <DIV TYPE = HEADER ALIGN=RIGHT  class='silversmall'>
          <P STYLE = 'margin-bottom: 0in' >
       
                               <IMG SRC = 'http://s24.postimg.org/aebxprm5t/logo.jpg?noCache=1459941007' NAME = 'Рисунок 1'  ALIGN = RIGHT  HSPACE = 12 WIDTH = 84 HEIGHT = 91 BORDER = 0 >
                        </P >
<b>
                        <span style='font-size:16px;'>Сервис</span><br>
                              <span   style='font-size:16px;'> для проверки</span><br>
     <span style='font-size:16px;'>контрагентов</span>
</b>
         </DIV >";
            return html;
        }
        private static string GeneralInfo(CompanyInfo info)
        {
            string html = string.Format(@"<p><h3 style='font-size:30px;'>{0}</h3></p>
                    </p >
                    {1} ", info.Name, info.FullName);

            html += string.Format(@"<table WIDTH=699 CELLPADDING=7 CELLSPACING=0 style='font-family:&quot;Calibri&quot;,&quot;sans-serif&quot;font-size:8px;'>
	<COL WIDTH=203>
	<COL WIDTH=468>
	<TR VALIGN=TOP>
		<TD WIDTH=203 HEIGHT=356 STYLE='border: none; padding: 0in'>
            <P STYLE = 'margin-bottom: 0in; page-break-inside: avoid' > <br>ИНН:</br>
			{0} <BR > <br>КПП:</br> {1} <BR > <br>ОГРН:</br> {2} <BR > <br>ОКПО:</br>
			{3} </P >
            <P STYLE = 'margin-top: 0.17in; margin-bottom: 0in; page-break-inside: avoid' >
              <br>Дата образования:</br>
            {4} <BR > {5} </P >
                <P STYLE = 'margin-top: 0.17in; margin-bottom: 0in; page-break-inside: avoid' >
                  {6} 
<span class='silversmall'> {7} еще компании[{8}]</span></P >
            <P STYLE = 'margin-top: 0.17in; margin-bottom: 0in; page-break-inside: avoid' >
             {9} </P >
                {10}
                       <P STYLE = 'margin-top: 0.13in; margin-bottom: 0in; page-break-inside: avoid' >
                         <BR >{11} </P >
            <P STYLE = 'margin-top: 0.25in; page-break-inside: avoid' > <br>Код
            налогового органа: </br>{12} </P >
        </TD >
        <TD WIDTH = 468 STYLE = 'border: none; padding: 0in' >
            <P STYLE = 'margin-left: 1.17in; margin-bottom: 0.13in; page-break-inside: avoid' >
             {13}<BR ><BR >
            
            </P>
            {14}
        </TD >
    </TR >
</table>", info.INN, info.KPP, info.OGRN, info.OKPO, info.RegDate, info.Status, info.Address, info.AddressAddedDate.ToShortDateString(), info.AddressCount, info.PhoneNumbers.FirstOrDefault(), Manager(info), info.OtherCodes, info.NalogCode, string.IsNullOrEmpty(info.UstFond) ? "" : "Уставной капитал: " + info.UstFond, PreActivities(info));
            return html;
        }
        private static string SpecialReestrs(CompanyInfo info)
        {
            string html = "";

            if(!string.IsNullOrEmpty(info.SpecialReestrs))
            {
                html = info.SpecialReestrs;
                
            }
            return html;
        }
        private static string Manager(CompanyInfo info)
        {
            if (string.IsNullOrEmpty(info.ManagerName))
                return "";

            string html = string.Format(@"<P STYLE='margin - top: 0.17in; margin - bottom: 0in; page -break-inside: avoid'><A NAME='_GoBack'></A>
                  {0}<BR > {1}{2}
                                                      <span class='silversmall'> {3} еще компании [{4}] </span><BR ><BR >
                       </P > ", info.ManagerAmplua, info.ManagerName, info.ManagerAddedDate.ToShortDateString(), info.ManagerINN ?? "ИНН: " + info.ManagerINN, info.ManagerCount);
            return html;

        }
        private static string PreActivities(CompanyInfo info)
        {
            string html = "";

            if (info.Activities != null)
            {
                html += @"<P STYLE='margin - left: 1.17in; margin - bottom: 0.13in; page -break-inside: avoid'>
                 <B > Виды
            деятельности </B ></P > ";

                foreach (var act in info.Activities.Take(3))
                {
                    html += string.Format(@"<span>
                    {0}:  {1}
                </span><br> ", act.Code, act.Name);
                }

                html += "";

            }
            return html;
        }
        private static string Activities(CompanyInfo info)
        {
            string html = "<br clear='all' style='page -break-before:always' />";

            if (info.Activities != null)
            {
                html += string.Format(@"<P STYLE='margin - left: 1.17in; margin - bottom: 0.13in; page -break-inside: avoid'>
                 <B > <h3 class='h3class'>Виды
            деятельности (<span class='silversmall'>{0}</span>)</h3></B ></P >", info.Activities.Count);

                foreach (var act in info.Activities)
                {
                    html += string.Format(@"<span> {0} {1} </span> <br>", act.Code, act.Name);
                }

                html += "";

            }
            return html;
        }
        private static string Lics(CompanyInfo info)
        {
            string html = "";

            if (info.Lics != null)
            {
                html = string.Format(@"<P STYLE='margin - top: 0.25in; margin - bottom: 0.25in; page -break-inside: avoid; page -break-before: always; page -break-after: avoid'>
        <B > <h3 class='h3class'>Лицензии (<span class='silversmall'>{0}</span>) </h3>
                 </B > </P >
                                <P STYLE = 'margin-top: 0.13in; margin-bottom: 0.19in; page-break-inside: avoid; page-break-after: avoid' ></P > <ol>
                                      ", info.Lics.Count);

                foreach (var lic in info.Lics)
                {
                    html += "<li>"+lic.Activity+"</li><br>";
                }
                html += "</ol>";
            }
            return html;
        }
        private static string Finance(CompanyInfo info)
        {
            string html = "";

            if ((!string.IsNullOrEmpty(info.FinBalance) || (!string.IsNullOrEmpty(info.FinProfit)) || (!string.IsNullOrEmpty(info.FinNetProfit))))
            {
                html = string.Format(@"<P STYLE='margin - top: 0.25in; margin - bottom: 0.25in; page -break-inside: avoid; page -break-before: always; page -break-after: avoid'>
        <B > <h3 class='h3class'>Финансовое состояние на <span class='silversmall'>{0}</span> год</h3>
                 </B > </P >
                                      ", info.FinYear);
                html += string.Format(@"
            <span> Баланс {0}</span><br>        
            <span> Выручка
                    {1}</span><br>
            <span> Чистая прибыль
                     {2}</span><br>
        ", info.FinBalance, info.FinProfit, info.FinNetProfit);

            }
            return html;
        }
        private static string Arbitr(CompanyInfo info)
        {
            string html = "";
            html += ArbitrTable("истца",info.ArbitrAsPlaintiff);
            html += ArbitrTable("ответчика",info.ArbitrAsRespondent);
            html += ArbitrTable("третего лица",info.ArbitrAsThird);

            return html;
        }
        private static string ArbitrTable(string name,ArbitrStat info)
        {
            string html = "";
            if (info != null)
            {
                html += string.Format(@"
<h3 class='h3class'> Арбитражные дела в качестве {2}</h3><span> Всего (<span class='silversmall'>{0}</span>) Общая сумма (<span class='silversmall'>{1}</span>)<span>
<br>
<br>
<ol>", info.Count, info.Sum,name);

                foreach (var _case in info.Cases)
                {
                    html += string.Format("<li>{0}</li><br>", _case.Number);
                }
                html += "</ol>";
            }
            return html;
        }
        private static string Contracts(CompanyInfo info)
        {
            string html = "";
            html += ContractsTable(info.WonContracts, "Выигранные государственные контракты");
            html += ContractsTable(info.PostedContracts, "Размещенные государственные контракты");
            return html;
        }
        private static string ContractsTable(ContractsInfo info, string name)
        {
            string html = "";
            if (info != null)
            {
                html += string.Format(@"
<h3 class='h3class'>{2}</h3> <span>Всего (<span class='silversmall'>{0}</span>). Общая сумма (<span class='silversmall'>{1}</span>)</span>
<br>
<br>
<ol>", info.Count, info.Sum, name);

                foreach (var contract in info.Contracts)
                {
                    html += string.Format("{0}<br>",contract.Number );
                }
                html += "</ol>";
            }
            return html;
        }
        private static string Bailiffs(CompanyInfo info)
        {
            string html = "";

            if (info.BailiffsInfo != null)
            {
                html += string.Format(@"
<h3 class='h3class'>Исполнительные производства </h3> <span>Всего (<span class='silversmall'>{0}</span>) Остаток суммы к взысканию (<span class='silversmall'>{1}</span>)</span>
<br>
<br>
<ol>
", info.BailiffsInfo.Count, info.BailiffsInfo.Sum);

                foreach (var _case in info.BailiffsInfo.Cases)
                {
                    html += string.Format("{0}<br>", _case.Number);
                }
                html += "</ol>";
            }
            return html;
        }
        private static string Founders(CompanyInfo info)
        {
            string html = "";

            if (info.Founders != null)
            {
                html = string.Format("<div> <h3 class='h3class'> Учредители </h3> <ol>");

                foreach (var founder in info.Founders)
                {
                    html += string.Format("<li>{0}</li><br>", founder.Name);
                }
                html += "</ol>";
            }
            return html;
        }
        private static string History(CompanyInfo info)
        {
            return info.History+"<br><br>";
        }

        private static string RelatedCompanies(List<RelatedCompany> list, string name)
        {
            string html = "";

            if (list != null)
            {
                html += "<h3 class='h3class'>" + name + "</h3> <ol>";

                foreach (var item in list)
                {
                    html += "<li>" + item.Name + "</li>";
                }
                html += "</ol>";
            }
            return html;
        }
        private static string WithPre(string value, string pre)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            else
                return string.Format("<tr> <td class='silversmall'>{0} </td> <td> {1}</td></tr>", pre, value);
        }

        private static string Bankrupt(List<string> messages)
        {
            string html = "";

            if(messages!=null)
            {
                html=string.Format("<h3>Сообщения о банкротстве</h3> <ol>");

                foreach (var mes in messages)
                {
                    html += mes + "<br><br>";
                }
                html += "</ol>";
            }
            return html;
        }
        private static string style()
        {
            return File.ReadAllText("style.txt");
        }
    
    }
}
