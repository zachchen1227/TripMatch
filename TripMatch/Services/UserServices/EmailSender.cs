using Lab1224_Identity.Models;
using Lab1224_Identity.Models.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Lab1224_Identity.Services
{
    public class EmailSender : IEmailSender<ApplicationUser>
    {
        private readonly SendGridSettings _settings;

        public EmailSender(IOptions<SendGridSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
        {
            var client = new SendGridClient(_settings.SendGridKey);
            var from = new EmailAddress(_settings.FromEmail, "想想TripMatch");
            var to = new EmailAddress(email);
            var subject = "驗證您的電子郵件地址";
            var htmlContent = $"<h3>歡迎註冊！</h3><p>請點擊以下連結驗證您的信箱：</p><a href='{confirmationLink}'>立即驗證</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);
            await client.SendEmailAsync(msg);
        }

        public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
        public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
    }
}
