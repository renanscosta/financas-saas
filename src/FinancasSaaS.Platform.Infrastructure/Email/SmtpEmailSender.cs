using System.Net;
using System.Net.Mail;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace FinancasSaaS.Platform.Infrastructure.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(options.Value.Host, options.Value.Port)
        {
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            EnableSsl = false
        };
        using var message = new MailMessage(options.Value.From, recipient, subject, body);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
