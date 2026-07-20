using Druware.Server.Controllers;
using Druware.Server.Email;

namespace UnitTests;

#pragma warning disable CS0618
public class AzureMailHelperTests
{
    [Test]
    public async Task SendHtmlAsync_DelegatesExactlyOnceToSharedSender()
    {
        var sender = new RecordingEmailSender();
        var helper = new AzureMailHelper(sender);

        await helper.SendHtmlAsync(
            "recipient@example.com",
            "configured-sender@example.com",
            "reply@example.com",
            "Subject",
            "<strong>Hello</strong>");

        var message = sender.Messages.Single();
        Assert.Multiple(() =>
        {
            Assert.That(message.To.Single().Address,
                Is.EqualTo("recipient@example.com"));
            Assert.That(message.ReplyTo.Single().Address,
                Is.EqualTo("reply@example.com"));
            Assert.That(message.Subject, Is.EqualTo("Subject"));
            Assert.That(message.HtmlBody, Is.EqualTo("<strong>Hello</strong>"));
            Assert.That(message.PlainTextBody, Is.EqualTo("Hello"));
        });
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public bool IsConfigured => true;
        public List<EmailMessage> Messages { get; } = [];

        public Task<EmailSendResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(EmailSendResult.Success("operation-id"));
        }
    }
}
#pragma warning restore CS0618
