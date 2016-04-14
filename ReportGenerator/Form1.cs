using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace ReportGenerator
{
    public partial class Form1 : Form
    {
        bool IsWorking = false;

        System.Threading.Timer timer;

        public Form1()
        {
            InitializeComponent();
            Init();
        }
        public void Init()
        {
            Settings.LoadConfig();
            IntervalNumericUpDown.Value = Settings.Interval;
            CookieTextBox.Text = Settings.Cookie;
            OrdersEmailtextBox.Text = Settings.OrdersEmail;
            OrdersEmailPasstextBox.Text = Settings.OrdersEmailPass;
            ErrorsEmailTextBox.Text = Settings.ErrorsReportEmail;
        }
        public void SaveConfig()
        {
            Settings.Cookie = CookieTextBox.Text;
            Settings.Interval = (int)IntervalNumericUpDown.Value;
            Settings.OrdersEmail = OrdersEmailtextBox.Text;
            Settings.OrdersEmailPass = OrdersEmailPasstextBox.Text;
            Settings.ErrorsReportEmail = ErrorsEmailTextBox.Text;

            Settings.SaveConfig();
        }

        public void Work()
        {
            var orders = OrdersProvider.GetOrders();

            KonturWorker konturW = new KonturWorker();

            foreach(var order in orders)
            {
                if(order.CompanyId.Length==10)
                {
                    order.CompanyId = konturW.GetCompanyId(order.CompanyId);
                }

            }
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (!IsWorking)
            {
                StartStopButton.Text = "Stop";
                IsWorking = true;
                SettingsBox.Enabled = false;

                SaveConfig();
                KonturWorker konturW = new KonturWorker();
                konturW.Cookie = CookieTextBox.Text;

                timer = new System.Threading.Timer(new TimerCallback((obj) =>
                 {
                     try
                     {
                         Message("Начало проверки заказов");
                         var orders = OrdersProvider.GetOrders();
                         var processedOrders = GetProcessedOrders();
                         if (processedOrders.Count > 0)
                         {
                             orders = orders.Except(processedOrders).ToList();
                         }
                         if (orders.Count == 0)
                         {
                             Message("Новых заказов не получено");
                             return;
                         }


                         Message("Получено заказов " + orders.Count);

                         foreach (var order in orders)
                         {
                             try
                             {
                                 Message("Обработка " + order.CompanyINNOGRN + " " + order.CustomerEmail);
#if DEBUG
                                 order.CompanyINNOGRN = "1027700132195";
#endif
                                 var info = konturW.GetSample();

                                 var file=ReportGenerator.Generate(info,order);
#if DEBUG
                                 order.CustomerEmail = "21pomni@gmail.com";
#endif
                                 EmailSender.SendEmail(order.CustomerEmail, "Отчет о компании " + order.CompanyINNOGRN, "", file);
                                 SaveProcessedOrder(order);
                                 Message("Успешно обработан " + order.CompanyINNOGRN + " " + order.CustomerEmail);
                             }
                             catch (Exception ex)
                             {
                                 ErrorLog(ex);
                                 Message("Ошибка при обработке заказа " + order.CompanyINNOGRN + " " + order.CustomerEmail +" "+ex.Message);
                             }
                         }
                     }
                     catch (Exception ex)
                     {

                     }
                 }), null, 500, (int)IntervalNumericUpDown.Value * 60000);
            }
            else
            {
                timer.Dispose();
                StartStopButton.Text = "Start";
                IsWorking = false;
                SettingsBox.Enabled = true;
            }
           
        }
        private void Message(string msg)
        {
            this.Invoke(new Action(() =>
            {
                LogTextBox.Text = LogTextBox.Text.Insert(0, string.Format("{0}: {1}" + Environment.NewLine, DateTime.Now, msg));
            }));
        }
        private void SaveProcessedOrder(Order order)
        {
            try
            {
                using (var writer = new System.IO.StreamWriter("orders.log",true))
                {
                    writer.WriteLine(order.CompanyINNOGRN + "\t" + order.CustomerEmail);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Ошибка сохранения лога обработанных заказов", ex);
            }
        }
        private List<Order> GetProcessedOrders()
        {
            List<Order> orders = new List<Order>();
            try
            {
                var lines = System.IO.File.ReadAllLines("orders.log");

                foreach(var line in lines)
                {
                    Order order = new Order();
                    var args = line.Split('\t');
                    order.CompanyINNOGRN = args[0];
                    order.CustomerEmail = args[1];
                    orders.Add(order);
                }
            }
            catch(Exception ex)
            {

            }
            return orders;
        }
        private void ErrorLog(Exception ex)
        {
            System.IO.File.AppendAllText("errorLog.log", string.Format("{0}\t{1}",DateTime.Now,ex.ToString()));
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}
