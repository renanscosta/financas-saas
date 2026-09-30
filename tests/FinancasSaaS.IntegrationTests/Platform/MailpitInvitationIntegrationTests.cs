using System.Net.Http.Json;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Email;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class MailpitInvitationIntegrationTests
{
    [Fact]
    public async Task MailpitRecebeConviteComLinkESemSenha()
    {
        using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8025") };
        var subject = $"Convite-{Guid.NewGuid():N}";
        var recipient = $"mailpit-{Guid.NewGuid():N}@example.com";
        var body = "Conclua seu cadastro pelo link: http://localhost:5080/concluir-cadastro?token=plain-token";
        var sender = new SmtpEmailSender(Options.Create(new EmailOptions()));

        await sender.SendAsync(recipient, subject, body);

        using var response = await httpClient.GetAsync("/api/v1/messages");
        response.EnsureSuccessStatusCode();
        var listing = await response.Content.ReadFromJsonAsync<MailpitListing>();
        var message = Assert.Single(listing!.Messages, item => item.Subject == subject);

        Assert.Contains(message.To, item => item.Address == recipient);
        Assert.Contains("concluir-cadastro?token=plain-token", message.Snippet, StringComparison.Ordinal);
        Assert.DoesNotContain("senha", message.Snippet, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MailpitListing(IReadOnlyList<MailpitMessage> Messages);
    private sealed record MailpitMessage(string Subject, IReadOnlyList<MailpitRecipient> To, string Snippet);
    private sealed record MailpitRecipient(string Name, string Address);
}