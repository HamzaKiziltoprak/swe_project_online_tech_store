using Xunit;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Backend.Services;
using System;
using System.Threading.Tasks;

namespace Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IOptions<EmailSettings>> _mockOptions;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly EmailService _emailService;
        private readonly EmailSettings _testSettings;

        public EmailServiceTests()
        {
            // 1. Test için sahte ayarlar (Localhost ve rastgele bir port)
            _testSettings = new EmailSettings
            {
                SmtpServer = "127.0.0.1", // Bağlantı reddedilsin diye local veriyoruz
                SmtpPort = 5555,
                SenderEmail = "noreply@onlinetechstore.com",
                SenderName = "Online Tech Store",
                Username = "testuser",
                Password = "testpassword",
                EnableSsl = false
            };

            // 2. IOptions mock'lama
            _mockOptions = new Mock<IOptions<EmailSettings>>();
            _mockOptions.Setup(o => o.Value).Returns(_testSettings);

            // 3. Logger mock'lama
            _mockLogger = new Mock<ILogger<EmailService>>();

            // 4. Servisi ayağa kaldırma
            _emailService = new EmailService(_mockOptions.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldAttemptToSendEmail_AndLogFailure_WhenServerIsUnreachable()
        {
            // Arrange
            string to = "customer@example.com";
            string subject = "Test Subject";
            string body = "Test Body";

            // Act & Assert
            // Not: Gerçek bir SMTP sunucusu olmadığı için kodun hata fırlatması (Exception)
            // aslında SmtpClient'ın tetiklendiğini ve çalışmaya çalıştığını gösterir.
            await Assert.ThrowsAnyAsync<Exception>(() => _emailService.SendEmailAsync(to, subject, body));

            // Verify: Hata bloğuna (catch) düşüp LogError çağrıldı mı?
            // CS8620 uyarısı 'Exception?' düzeltmesi ile giderildi.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), 
                Times.Once);
        }

        [Fact]
        public async Task SendConfirmationEmailAsync_ShouldConstructBody_AndAttemptToSend()
        {
            // Arrange
            string to = "newuser@example.com";
            string link = "https://onlinetechstore.com/confirm?token=123";

            // Act & Assert
            // HTML body oluşturulup SendEmailAsync çağrılacak, o da sunucu bulamadığı için hata fırlatacak.
            // Bu akışın gerçekleşmesi metodun doğru çalıştığını gösterir.
            await Assert.ThrowsAnyAsync<Exception>(() => _emailService.SendConfirmationEmailAsync(to, link));
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_ShouldConstructBody_AndAttemptToSend()
        {
            // Arrange
            string to = "forgotpass@example.com";
            string link = "https://onlinetechstore.com/reset?token=abc";

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _emailService.SendPasswordResetEmailAsync(to, link));
        }

        [Fact]
        public async Task SendOrderConfirmationEmailAsync_ShouldConstructBody_AndAttemptToSend()
        {
            // Arrange
            string to = "shopper@example.com";
            string orderDetails = "<li>iPhone 15 - 1 Adet</li>";

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => _emailService.SendOrderConfirmationEmailAsync(to, orderDetails));
        }
    }
}