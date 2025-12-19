using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Backend.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = null!;
        public int SmtpPort { get; set; }
        public string SenderEmail { get; set; } = null!;
        public string SenderName { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public bool EnableSsl { get; set; }
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName);
                message.To.Add(new MailAddress(to));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var smtpClient = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort);
                smtpClient.Credentials = new NetworkCredential(_emailSettings.Username, _emailSettings.Password);
                smtpClient.EnableSsl = _emailSettings.EnableSsl;

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {to}");
                throw;
            }
        }

        public async Task SendConfirmationEmailAsync(string to, string confirmationLink)
        {
            var subject = "Email Doğrulama - Online Tech Store";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Email Adresinizi Doğrulayın</h2>
                        <p>Merhaba,</p>
                        <p>Online Tech Store hesabınızı oluşturduğunuz için teşekkür ederiz!</p>
                        <p>Email adresinizi doğrulamak için aşağıdaki linke tıklayın:</p>
                        <div style='margin: 30px 0;'>
                            <a href='{confirmationLink}' 
                               style='background-color: #007bff; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Email Adresimi Doğrula
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            Bu linkin geçerlilik süresi 24 saattir.
                        </p>
                        <p style='color: #666; font-size: 14px;'>
                            Eğer bu hesabı siz oluşturmadıysanız, bu emaili görmezden gelebilirsiniz.
                        </p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #999; font-size: 12px;'>
                            Online Tech Store | E-Ticaret Platformu
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string to, string resetLink)
        {
            var subject = "Şifre Sıfırlama - Online Tech Store";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #333;'>Şifre Sıfırlama Talebi</h2>
                        <p>Merhaba,</p>
                        <p>Hesabınız için şifre sıfırlama talebinde bulunuldu.</p>
                        <p>Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
                        <div style='margin: 30px 0;'>
                            <a href='{resetLink}' 
                               style='background-color: #dc3545; color: white; padding: 12px 30px; 
                                      text-decoration: none; border-radius: 5px; display: inline-block;'>
                                Şifremi Sıfırla
                            </a>
                        </div>
                        <p style='color: #666; font-size: 14px;'>
                            Bu linkin geçerlilik süresi 1 saattir.
                        </p>
                        <p style='color: #666; font-size: 14px;'>
                            Eğer bu talebi siz yapmadıysanız, lütfen bu emaili görmezden gelin ve şifrenizi değiştirin.
                        </p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #999; font-size: 12px;'>
                            Online Tech Store | E-Ticaret Platformu
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendOrderConfirmationEmailAsync(string to, string orderDetails)
        {
            var subject = "Sipariş Onayı - Online Tech Store";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2 style='color: #28a745;'>✓ Siparişiniz Alındı!</h2>
                        <p>Merhaba,</p>
                        <p>Siparişiniz başarıyla alınmıştır ve hazırlanmaya başlanmıştır.</p>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                            <h3 style='color: #333; margin-top: 0;'>Sipariş Detayları:</h3>
                            {orderDetails}
                        </div>
                        <p>Kargo takip numaranız hazır olduğunda size bildirilecektir.</p>
                        <p style='color: #666; font-size: 14px;'>
                            Siparişinizle ilgili sorularınız için müşteri hizmetlerimizle iletişime geçebilirsiniz.
                        </p>
                        <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                        <p style='color: #999; font-size: 12px;'>
                            Online Tech Store | E-Ticaret Platformu
                        </p>
                    </div>
                </body>
                </html>
            ";

            await SendEmailAsync(to, subject, body);
        }
    }
}
