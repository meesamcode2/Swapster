using System.Net;
using System.Net.Mail;

namespace Swapster.Services
{
    public class EmailService
    {
        public bool SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var fromEmail = "swastersupport@gmail.com";
                var appPassword = "zuou voaw uzzy otlb";

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(fromEmail, appPassword);

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(fromEmail);
                        message.To.Add(toEmail);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = true;

                        smtp.Send(message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
                return false;
            }
        }
    }
}