using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace ReportGenerator
{
    class EmailSender
    {
        public static void SendEmail(string toEmail,string subj,string body,string attachedFile=null)
        {
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient("smtp.mail.ru");
            mail.From = new MailAddress(Settings.OrdersEmail);
            mail.To.Add(toEmail);
            mail.Subject = subj;
            mail.Body = body;

            if (!string.IsNullOrEmpty(attachedFile))
            {
                System.Net.Mail.Attachment attachment;
                attachment = new System.Net.Mail.Attachment(attachedFile);
                mail.Attachments.Add(attachment);
            }

            SmtpServer.Port = 2525;
            SmtpServer.Credentials = new System.Net.NetworkCredential(Settings.OrdersEmail,Settings.OrdersEmailPass);
            SmtpServer.EnableSsl = true;

            SmtpServer.Send(mail);
        }
    }
}
