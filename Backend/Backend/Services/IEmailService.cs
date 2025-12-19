namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendConfirmationEmailAsync(string to, string confirmationLink);
        Task SendPasswordResetEmailAsync(string to, string resetLink);
        Task SendOrderConfirmationEmailAsync(string to, string orderDetails);
    }
}
