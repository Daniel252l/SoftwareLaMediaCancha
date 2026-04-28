using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace LaMediaCancha.Services
{
    public class EmailService
    {
        private readonly string _correoOrigen;
        private readonly string _claveApp;
        private readonly string _nombreOrigen;

        public EmailService()
        {
            _correoOrigen = ConfigurationManager.AppSettings["EmailOrigen"];
            _claveApp = ConfigurationManager.AppSettings["EmailPassword"];
            _nombreOrigen = ConfigurationManager.AppSettings["EmailNombre"];
        }

        public async Task EnviarEmailAsync(string destino, string asunto, string cuerpoHtml)
        {
            using (var cliente = new SmtpClient("smtp.gmail.com", 587))
            {
                cliente.EnableSsl = true;
                cliente.UseDefaultCredentials = false;
                cliente.Credentials = new NetworkCredential(_correoOrigen, _claveApp);

                var mensaje = new MailMessage
                {
                    From = new MailAddress(_correoOrigen, _nombreOrigen),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                mensaje.To.Add(destino);
                await cliente.SendMailAsync(mensaje);
            }
        }
    }
}