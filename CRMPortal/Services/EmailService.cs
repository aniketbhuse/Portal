using System.Net;
using System.Net.Mail;

namespace CRMPortal.Services
{
    public class EmailService
    {
        public void SendEmail( string toEmail, string subject, string body)
        {
            try
            {
                MailMessage mail = new MailMessage();

                mail.From = new MailAddress("noreply@marstechsol.com");

                mail.To.Add(toEmail);

                mail.Subject = subject;

                mail.Body = body;

                mail.IsBodyHtml = true;

                SmtpClient smtp =  new SmtpClient("mail.marstechsol.com", 465);

                smtp.Credentials = new NetworkCredential("noreply@marstechsol.com", "YOUR_PASSWORD_HERE");

                smtp.EnableSsl = true;

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Email Error : "
                    + ex.Message
                    + " | "
                    + ex.InnerException?.Message);
            }
        }
    }
}