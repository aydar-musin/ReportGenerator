using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace KonturGeneralInfoParser
{
    class InputDataManager
    {
        private static string inputPath = ConfigurationManager.AppSettings["inputFileWithOGRNs"];
        //private static string filteredPath = ConfigurationManager.AppSettings["filteredInputFileWithOGRNs"];
        private static string oldLogPath = "oldLog.txt";
        private static string logPath = "log.txt";
        private static char delimeter = '|';
        StreamReader inputFileReader;

        public InputDataManager()
        {
            Console.WriteLine("Обновляем список входных значений...");
            int filteredCount = 0;

            if (File.Exists(logPath))
                using (var logReader = new StreamReader(logPath))
                {
                    string logLine;
                    var OGRNs = File.ReadAllLines(inputPath);
                    var forExcept = new List<string>();

                    while (!string.IsNullOrEmpty(logLine = logReader.ReadLine()))
                    {
                        var values = logLine.Split(delimeter);
                        var status = getStatus(values[0]);
                        var ogrn = values[1];

                        if (status == Status.Succes || status == Status.NotFound)
                            forExcept.Add(ogrn);

                        File.AppendAllText(oldLogPath, logLine + Environment.NewLine);
                    }

                    var filteredOGRNs = OGRNs.Except(forExcept);
                    filteredCount = filteredOGRNs.Count();

                    File.WriteAllLines(inputPath, filteredOGRNs);
                }

            File.Delete(logPath);
            restoreCounter(filteredCount);
            inputFileReader = new StreamReader(inputPath);

            Console.WriteLine("Входной файл обновлён. Собираем данные...");
        }

        public int Count { get; private set; }

        private void restoreCounter(int filtered) // TODO: ПРОВЕРИТЬ!!!! ВОЗМОЖНА ОШИБКА, что учитывает лишние строки
        {
            int all = 9002265; // TODO: считывать лучше с оригинального входного файла

            Count = all - filtered;
        }

        private static object readLocker = new object();

        public string GetNextLine()
        {
            lock (readLocker)
            {
                return inputFileReader.ReadLine();
            }
        }

        public enum Status { Succes, NotFound, Error };

        private static object writeLocker = new object();

        public bool Log(string id, Status status, Exception exception = null)
        {
            lock (writeLocker)
            {
                if (status == Status.Succes || status == Status.NotFound)
                    Count++;

                string firstPart = string.Format("{1}{0}{2}", delimeter, getStatusChar(status), id);

                string exMessage = default(string);
                if (exception != null)
                    exMessage = delimeter + exception.Message.Replace(Environment.NewLine, "\t");

                string record = string.Format("{1}{2}", delimeter, firstPart, exMessage);
                File.AppendAllText(logPath, record + Environment.NewLine);
                Console.WriteLine(record);

                return true;
            }
        }

        private static char getStatusChar(Status status)
        {
            if (status == Status.Succes)
                return '+';
            else if (status == Status.NotFound)
                return '-';
            else if (status == Status.Error)
                return '!';
            else
                return '?';
        }

        private static Status getStatus(char statusChar)
        {
            if (statusChar == '+')
                return Status.Succes;
            else if (statusChar == '-')
                return Status.NotFound;
            else
                return Status.Error;
        }

        private static Status getStatus(string statusChar)
        {
            return getStatus(statusChar.ToCharArray().First());
        }
    }
}
