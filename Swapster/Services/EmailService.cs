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
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(
                        "muhammadmeesam90@gmail.com",               // <-- change this
                        "lykw hgfs nhih hrye"                   // <-- change this
                    )
                };

                MailMessage message = new MailMessage();
                message.From = new MailAddress("YOUR_EMAIL@gmail.com");  // same email
                message.To.Add(toEmail);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                smtp.Send(message);

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
