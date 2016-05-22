using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenPop.Common.Logging;
using OpenPop.Mime;
using OpenPop.Mime.Decode;
using OpenPop.Mime.Header;
using OpenPop.Pop3;

namespace ReportGenerator
{
    class OrdersProvider
    {
        private static string hostname= "pop.mail.ru";
        private static int port=995;
        private static bool useSsl=true;
        private static string username= "bezrisk@mail.ru";
        private static string password= "n9iC:g3wjLOI";

        private static List<OpenPop.Mime.Message> FetchAllMessages()
        {
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                List<OpenPop.Mime.Message> allMessages = new List<OpenPop.Mime.Message>(messageCount);

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Add(client.GetMessage(i));
                    //client.GetMessage(i).Headers.Subject - так получаем тему письма
                }

                // Now return the fetched messages
                return allMessages;
            }
        }
        public static List<Order> GetOrders()
        {
            try
            {
                List<Order> result = new List<Order>();
                var messages = FetchAllMessages();
                foreach (var msg in messages.Where(m => m.Headers.From.Address.Contains("inform@money.yandex.ru") &&(m.Headers.Subject.Contains("пополнен")|| m.Headers.Subject.Contains("поступил"))))
                {
                    try
                    {
                        var order = Parser.Order(msg.FindFirstHtmlVersion().GetBodyAsText());
                        result.Add(order);
                    }
                    catch
                    {

                    }
                }
                return result;
            }
            catch(Exception ex)
            {
                throw new Exception("Ошибка получения заказов", ex);
            }
        }
    }
}
