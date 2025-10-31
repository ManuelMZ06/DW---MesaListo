using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace MesaListo.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
        {
            // Leemos la sección EmailSettings del appsettings.json
            var emailSettings = _configuration.GetSection("EmailSettings");
            string fromName = emailSettings["FromName"];
            string fromAddress = emailSettings["FromAddress"];
            string password = emailSettings["Password"];
            string smtpServer = emailSettings["SmtpServer"];
            int smtpPort = int.Parse(emailSettings["SmtpPort"]);

            // Construimos el mensaje
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(fromName, fromAddress));
            mensaje.To.Add(new MailboxAddress(destinatario, destinatario));
            mensaje.Subject = asunto;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = cuerpo // Puedes usar texto plano con TextBody si prefieres
            };
            mensaje.Body = bodyBuilder.ToMessageBody();

            // Enviamos el correo
            using (var cliente = new SmtpClient())
            {
                await cliente.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await cliente.AuthenticateAsync(fromAddress, password);
                await cliente.SendAsync(mensaje);
                await cliente.DisconnectAsync(true);
            }
        }
    }
}
