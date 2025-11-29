using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Configuration;

namespace quan_li_thuoc.Helpers
{
    public class MailHelper
    {
        public static bool SendMail(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = ConfigurationManager.AppSettings["MailUser"];
                var fromPassword = ConfigurationManager.AppSettings["MailPassword"];

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(fromEmail, fromPassword)
                };

                var message = new MailMessage(fromEmail, toEmail, subject, body);
                message.IsBodyHtml = true;

                smtp.Send(message);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}