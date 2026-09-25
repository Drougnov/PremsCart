using System.Net;
using System.Net.Mail;

namespace PremsCart.Api.Authentication;

public sealed class VerificationEmailSender(IConfiguration config, IWebHostEnvironment environment, ILogger<VerificationEmailSender> logger)
{
    public async Task SendAsync(string email, string code, string purpose = "verification")
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("SMTP settings are required outside Development.");
            logger.LogWarning("Development verification code for {Email}: {Code}", email, code);
            return;
        }
        using var client = new SmtpClient(host, int.Parse(config["Smtp:Port"] ?? "587"))
        {
            EnableSsl = bool.Parse(config["Smtp:EnableSsl"] ?? "true"),
            Credentials = new NetworkCredential(config["Smtp:Username"], config["Smtp:Password"])
        };
        using var message = new MailMessage(
            config["Smtp:From"] ?? throw new InvalidOperationException("Smtp:From is required."),
            email, $"PremsCart {purpose} code", $"Your PremsCart {purpose} code is {code}. It expires in 10 minutes.");
        await client.SendMailAsync(message);
    }
}
