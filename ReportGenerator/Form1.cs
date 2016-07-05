using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace ReportGenerator
{
    public partial class Form1 : Form
    {
        bool IsWorking = false;

        System.Threading.Timer timer;
        private List<Order> Sucess = new List<Order>();
        private List<Order> Fail = new List<Order>();

        public Form1()
        {
            InitializeComponent();
            Init();

#if DEBUG
            TryMakeSavedSO();
#endif
        }
        public void Init()
        {
            Settings.LoadConfig();
            IntervalNumericUpDown.Value = Settings.Interval;
            CookieTextBox.Text = Settings.Cookie;
            OrdersEmailtextBox.Text = Settings.OrdersEmail;
            OrdersEmailPasstextBox.Text = Settings.OrdersEmailPass;
            ErrorsEmailTextBox.Text = Settings.ErrorsReportEmail;
            ProxyCheckBox.Checked = Settings.UseProxy;
        }
        public void SaveConfig()
        {
            Settings.Cookie = CookieTextBox.Text;
            Settings.Interval = (int)IntervalNumericUpDown.Value;
            Settings.OrdersEmail = OrdersEmailtextBox.Text;
            Settings.OrdersEmailPass = OrdersEmailPasstextBox.Text;
            Settings.ErrorsReportEmail = ErrorsEmailTextBox.Text;
            Settings.UseProxy = ProxyCheckBox.Checked;
            Settings.SaveConfig();
        }

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (!IsWorking)
            {
                StartStopButton.Text = "Stop";
                SettingsBox.Enabled = false;

                SaveConfig();
                KonturWorker konturW = new KonturWorker();
                konturW.Cookie = CookieTextBox.Text;

                Message(string.Format("Доступно {0} прокси", Settings.ProxyList.Count));

                timer = new System.Threading.Timer(new TimerCallback((obj) =>
                 {
                     if (IsWorking)
                         return;

                     IsWorking = true;
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
                             IsWorking = false;
                             return;
                         }


                         Message("Получено заказов " + orders.Count);

                         foreach (var order in orders)
                         {
                             try
                             {
                                 Message("Обработка " + order.CompanyINNOGRN + " " + order.CustomerEmail);
                                 var info = konturW.Process(order);

                                 var file=ReportGenerator.Generate(info,order);
#if DEBUG
                                 string realEmail = order.CustomerEmail;
                                 order.CustomerEmail = "21pomni@gmail.com";
#endif
                                 string body = string.Format("Добрый день! \nИнформация по организации {0} ИНН {1}\n\nТехническая поддержка:\ne-mail: bezrisk@mail.ru\nskype: bezrisk_support",info.Name,info.INN);
                                 EmailSender.SendEmail(order.CustomerEmail, "Отчет по компании ИНН" + info.INN,body, file);
#if DEBUG
                                 order.CustomerEmail = realEmail;
#endif
                                 SaveProcessedOrder(order);
                                 Message("Успешно обработан " + order.CompanyINNOGRN + " " + order.CustomerEmail);
                                 Sucess.Add(order);
                             }
                             catch (Exception ex)
                             {
                                 ErrorLog(ex);
                                 Message("Ошибка при обработке заказа " + order.CompanyINNOGRN + " " + order.CustomerEmail +" "+ex.Message);
                                 Fail.Add(order);
                             }
                             finally
                             {
                                 RefreshStat();
                             }
                         }
                         IsWorking = false;
                     }
                     catch (Exception ex)
                     {
                         IsWorking = false;
                         Message(ex.Message);
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
                    writer.WriteLine(order.ToString());
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
                    order.TimeStamp = DateTime.Parse(args[0]);
                    order.CompanyINNOGRN = args[2];
                    order.CustomerEmail = args[1];
                    orders.Add(order);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Ошибка чтения файла orders.log");
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

        private void ProxyTestButton_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                System.Collections.Concurrent.ConcurrentQueue<string> proxies = new System.Collections.Concurrent.ConcurrentQueue<string>(Settings.ProxyList);

                Task[] tasks = new Task[20];
                int work = 0;

                for (int i = 0; i < 20; i++)
                {
                    tasks[i] = Task.Factory.StartNew(() =>
                      {
                          string proxy = "";
                          while (proxies.TryDequeue(out proxy))
                          {
                              try
                              {
                                  HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create("https://focus.kontur.ru/entity?query=1126686008701");
                                  request.Proxy = new WebProxy(proxy);

                                  request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36";
                                  request.Timeout = 2000;
                                  var resp=request.GetResponse();
                                  resp.Close();

                                  Message("+ ");
                                  work++;
                              }
                              catch
                              {
                                  Message("-");
                              }
                          }
                      });
                }
            

            Task.WaitAll(tasks);
            Message(string.Format("{0} из {1} прокси работают",work,Settings.ProxyList.Count));
            });
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(TestTextBox.Text))
            {
                SaveConfig();

                Order order = new Order();
                order.CompanyINNOGRN = TestTextBox.Text.Trim();
                order.TimeStamp = DateTime.Now;
                KonturWorker konturW = new KonturWorker();
                konturW.Cookie = CookieTextBox.Text;

                Task.Factory.StartNew(() =>
                {
                    ProcessOrder(konturW,order);
                });
            }
        }
        private void ProcessOrder(KonturWorker konturW,Order order)
        {
            try
            {
                Message("Обработка " + order.CompanyINNOGRN + " " + order.CustomerEmail);

                var info = konturW.Process(order);

                var file = ReportGenerator.Generate(info, order);

                SaveProcessedOrder(order);
                Message("Успешно обработан " + order.CompanyINNOGRN + " " + order.CustomerEmail);
#if DEBUG
                SaveSO(info);
#endif
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
                Message("Ошибка при обработке заказа " + order.CompanyINNOGRN + " " + order.CustomerEmail + " " + ex.Message);
            }
        }

        private void RefreshStat()
        {
            this.Invoke(new Action(() =>
            {
                SuccessListBox.Items.Clear();
                FailListBox.Items.Clear();

                SuccessListBox.Items.AddRange(Sucess.Select(o => o.CompanyINNOGRN + " " + o.CustomerEmail).ToArray());
                FailListBox.Items.AddRange(Fail.Select(o => o.CompanyINNOGRN + " " + o.CustomerEmail).ToArray());

                SuccessLabel.Text = "Успешно- " + Sucess.Count;
                FailLabel.Text = "Ошибка- "+Fail.Count;
            }));
        }
        private static void SaveSO(CompanyInfo info)
        {
            System.IO.File.WriteAllText("savedSO.txt", Newtonsoft.Json.JsonConvert.SerializeObject(info));
        }
        private static void TryMakeSavedSO()
        {
            try
            {
                 var info=Newtonsoft.Json.JsonConvert.DeserializeObject<CompanyInfo>(System.IO.File.ReadAllText("savedSO.txt"));
                 ReportGenerator.Generate(info,new Order() { CompanyINNOGRN="test", CustomerEmail="test", TimeStamp=DateTime.Now });
            }
            catch
            {
                
            }
        }
    }
}
