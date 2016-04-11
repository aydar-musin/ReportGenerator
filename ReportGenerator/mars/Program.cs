using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Threading.Tasks;

namespace KonturGeneralInfoParser
{
    class Program
    {
        static void Main(string[] args)
        {
            int numberOfThreads = int.Parse(ConfigurationManager.AppSettings["numberOfThreads"]);
            Task[] tasks = new Task[numberOfThreads];
            Task.Factory.StartNew(new Action(activateStatisticsSender));

            for (int i = 0; i < numberOfThreads; i++)
                tasks[i] = Task.Factory.StartNew(new Action(collectData));

            Task.WaitAll(tasks);

            //getOneCompanyInfo(); // тестовый метод

            Console.WriteLine("Completed!");
            Console.ReadKey();
        }

        enum NeedTo { Download, Parse, Insert };

        static InputDataManager inputDataManager = new InputDataManager();
        static int maxErrors = int.Parse(ConfigurationManager.AppSettings["maxErrors"]);

        static void collectData()
        {
            string ogrn;

            while (!string.IsNullOrEmpty(ogrn = inputDataManager.GetNextLine()))
            {
                int errorsCount = 0;
                var nextStep = NeedTo.Download;
                bool isComplete = false;
                WebProxy proxy = new WebProxy();
                var companyInfo = new CompanyInfo();
                string page = default(string);

                while (!isComplete)
                {
                    try
                    {
                        if (nextStep == NeedTo.Download)
                        {
                            proxy = ProxyProvider.GetProxy();
                            page = WebWorker.GetPage(ogrn, proxy);
                            nextStep = NeedTo.Parse;
                        }

                        if (nextStep == NeedTo.Parse)
                        {
                            companyInfo = Parser.Parse(page);
                            nextStep = NeedTo.Insert;
                        }

                        if (nextStep == NeedTo.Insert)
                            DataBaseManager.InsertData(companyInfo);

                        isComplete = inputDataManager.Log(ogrn, InputDataManager.Status.Succes);
                    }
                    catch (Exception ex)
                    {
                        errorsCount++;

                        if (errorsCount > maxErrors)
                            isComplete = inputDataManager.Log(ogrn, InputDataManager.Status.Error, ex);
                        else
                        {
                            if (ex.Message.Contains("String or binary data would be truncated.")) // слишком длинное значение (обычно, названия)
                                isComplete = inputDataManager.Log(ogrn, InputDataManager.Status.Error, ex);
                            else if (ex.Message.Contains("Слишком много запросов к серверу.")) // TODO: обработать бан
                            {
                                //ProxyProvider.MarkAsBanned(proxy);
                                Console.WriteLine(ex.Message + "Меняем прокси и пробуем ещё.");
                            }
                            else if (ex.Message.Contains("Не найдена организация или индивидуальный предприниматель"))
                                isComplete = inputDataManager.Log(ogrn, InputDataManager.Status.NotFound);
                            else if (ex.Message.Contains("Невозможно разрешить удаленное имя:")) // TODO: обработать отсутствие интернета 
                                Console.WriteLine(errorsCount + " " + ogrn + " " + ex.Message + " " + ex.StackTrace + Environment.NewLine);
                            else if (ex.StackTrace.Contains("строка 31"))
                                Console.WriteLine(errorsCount + " " + ogrn + " " + "WW" + " " + ex.Message);
                            else
                                Console.WriteLine(errorsCount + " " + ogrn + " " + ex.Message + " " + ex.StackTrace + Environment.NewLine);
                        }
                    }

                    delay(1, 2, ValueOfTime.Seconds, errorsCount);
                }
            }
        }

        /// <summary>
        /// Величина времени.
        /// </summary>
        enum ValueOfTime { Day, Hours, Minutes, Seconds, Milliseconds }

        /// <summary>
        /// Случайная задержка потока в рамках переданных значений.
        /// </summary>
        /// <param name="minValue">Минимальное значение.</param>
        /// <param name="maxValue">Максимальное значение.</param>
        /// <param name="valueOfTime">Тип величины времени.</param>
        private static void delay(int minValue, int maxValue, ValueOfTime valueOfTime, int multiplier = 1)
        {
            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            int milliseconds = 0;

            if (multiplier != 0)
            {
                minValue = multiplier * multiplier * minValue;
                maxValue = multiplier * multiplier * maxValue;
            }

            int randomValue = new Random().Next(minValue, maxValue);

            switch (valueOfTime)
            {
                case ValueOfTime.Day:
                    days = randomValue;
                    break;
                case ValueOfTime.Hours:
                    hours = randomValue;
                    break;
                case ValueOfTime.Minutes:
                    minutes = randomValue;
                    break;
                case ValueOfTime.Seconds:
                    seconds = randomValue;
                    break;
                case ValueOfTime.Milliseconds:
                    milliseconds = randomValue;
                    break;
            }

            var time = new TimeSpan(days, hours, minutes, seconds, milliseconds);
            Task.Delay(time).Wait();
        }

        private static void report()
        {
            Dictionary<string, int> values = new Dictionary<string, int>();
            values.Add("collected", inputDataManager.Count);
            StatisticsSender.SendSnapshot("kontur_general_info_parser", values);
        }

        private static void activateStatisticsSender()
        {
            while (true)
            {
                Task.Delay(new TimeSpan(0, 5, 0)).Wait();
                report();
            }
        }

        #region Test

        static string sampleOGRN = "1037739571286";
        //static string sampleOGRN = "1026104027388"; // сбивающие <>
        //static string sampleOGRN = "1027700132195"; // СБЕР
        //static string sampleOGRN = "1025300780295"; // Контур
        //static string sampleOGRN = "1077763213000"; // мало информации
        //static string sampleOGRN = "1097760000458"; // мало инфы
        //static string sampleOGRN = "1037739872939"; // с предыдущими адресами
        //static string sampleOGRN = "1057747421247"; // полная история
        //static string sampleOGRN = "1022501302834"; // с ОФИСОМ!
        //static string sampleOGRN = "1022501302966"; //  два директора

        private static void getOneCompanyInfo()
        {
            var page = WebWorker.GetPage(sampleOGRN/*, ProxyProvider.GetRandomProxy()*/);
            var companyInfo = Parser.Parse(page);
            //DataBaseManager.InsertData(companyInfo);
            showResult(companyInfo);
        }

        private static void showResult(CompanyInfo companyInfo)
        {
            long INN = companyInfo.INN;
            long KPP = companyInfo.KPP;
            long OGRN = companyInfo.OGRN;
            long OKPO = companyInfo.OKPO;

            bool BailiffsExist = companyInfo.BailiffsExist; // Исп. производства
            bool ArbitrationExists = companyInfo.ArbitrationExists; // Арбитраж
            bool ContractsExist = companyInfo.ContractsExist; // Госконтракты
            bool LicensiesExist = companyInfo.LicensiesExist; // Лицензии
            bool TrademarksExist = companyInfo.TrademarksExist; // Тов. знаки

            string Address = companyInfo.Address;
            DateTime AddressDate = companyInfo.AddressAddedDate;

            DateTime ManagerDate = companyInfo.ManagerAddedDate;

            long FoundersCount = companyInfo.FoundersCount;

            string MainActiviyCode = companyInfo.MainActiviyCode;

            long ArbitrDefendantCount = companyInfo.ArbitrDefendantCount; // ответчик
            long ArbitrPlaintiffCount = companyInfo.ArbitrPlaintiffCount; // истец
            long ArbitrOtherCount = companyInfo.ArbitrOtherCount; // другое
            long ArbitrBankruptcyCount = companyInfo.ArbitrBankruptcyCount; // банкротство

            long WonContractsCount = companyInfo.WonContractsCount;
            long PlacedContractsCount = companyInfo.PlacedContractsCount;

            List<ValueWithDate> OtherNames = companyInfo.OtherNames;
            List<ValueWithDate> OtherAddresses = companyInfo.OtherAddresses;

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("*******************************************************");

            Console.WriteLine("ИНН: {0}", INN);
            Console.WriteLine("КПП: {0}", KPP);
            Console.WriteLine("ОГРН: {0}", OGRN);
            Console.WriteLine("ОКПО: {0}", OKPO);

            if (BailiffsExist)
                Console.WriteLine("Исп. производства присутствуют.", BailiffsExist);
            if (ArbitrationExists)
                Console.WriteLine("Арбитраж присутствует.", ArbitrationExists);
            if (ContractsExist)
                Console.WriteLine("Госконтракты присутствуют.", ContractsExist);
            if (LicensiesExist)
                Console.WriteLine("Лицензии присутствуют.", LicensiesExist);
            if (TrademarksExist)
                Console.WriteLine("Тов. знаки присутствуют.", TrademarksExist);

            Console.WriteLine("Адрес: {0}", Address.Replace('\t', ' '));
            Console.WriteLine("Дата внесения адреса: {0}", AddressDate.ToShortDateString());

            Console.WriteLine("Дата внесения менеджера: {0}", ManagerDate.ToShortDateString());

            Console.WriteLine("Основателей: {0}", FoundersCount);

            Console.WriteLine("ОКВЭД: {0}", MainActiviyCode);

            Console.WriteLine("Количество дел в качестве ответчика: {0}", ArbitrDefendantCount);
            Console.WriteLine("Количество дел в качестве истца: {0}", ArbitrPlaintiffCount);
            Console.WriteLine("Количество дел в другом качестве: {0}", ArbitrOtherCount);
            Console.WriteLine("Дела о банкротстве: {0}", ArbitrBankruptcyCount);

            Console.WriteLine("Выигранные контракты: {0}", WonContractsCount);
            Console.WriteLine("Размещенные контракты: {0}", PlacedContractsCount);

            if (OtherNames.Count != 0)
                Console.WriteLine("Другие имена:");
            foreach (var name in OtherNames)
                Console.WriteLine("{0}", name.ToString());


            if (OtherAddresses.Count != 0)
                Console.WriteLine("Другие адреса:");
            foreach (var address in OtherAddresses)
                Console.WriteLine("{0}", address.ToString());

            Console.WriteLine("*******************************************************");
        }

        #endregion
    }
}