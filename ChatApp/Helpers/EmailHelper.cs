using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Configuration;

namespace ChatApp.Helpers
{
    public class EmailHelper
    {
        public string DestinationEmail { get; set; }
        public string EmailBody { get; set; }
        public string EmailHeader { get; set; }

        private SmtpClient client{ get; set; }
        private string from = WebConfigurationManager.AppSettings["fromEmail"];
        private string pass = WebConfigurationManager.AppSettings["fromEmailPass"];
        private MailMessage mailMessage{get; set;}
        public EmailHelper(string emailBody, string emailHeader, string destinationEmail)
        {
            EmailBody = emailBody;
            EmailHeader = emailHeader;
            DestinationEmail = (destinationEmail == null) ? from : destinationEmail;
            mailMessage = new MailMessage(from, DestinationEmail);
        }
        public void AddCc(string newEmail){
            Debug.WriteLine("Adding to cc " + newEmail);
            mailMessage.CC.Add(new MailAddress(newEmail));
        }
        public void AddBcc(string newEmail)
        {
            Debug.WriteLine("Adding to bcc " + newEmail);
            mailMessage.Bcc.Add(new MailAddress(newEmail));
        }
        public void Send(){
            SmtpClient client = new SmtpClient("smtp.yandex.ru", 25);

            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(from, pass);
            client.EnableSsl = true;

            mailMessage.Subject = EmailHeader;
            mailMessage.Body = EmailBody;
            mailMessage.IsBodyHtml = true;
            client.Send(mailMessage);
        }
    }
}