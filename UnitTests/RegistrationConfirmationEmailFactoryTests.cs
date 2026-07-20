using Druware.Server.Controllers;
using Druware.Server.Email;
using Druware.Server.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests;

public class RegistrationConfirmationEmailFactoryTests
{
    [Test]
    public void Create_PreservesOriginalDruwareMessageContent()
    {
        var factory = new DefaultRegistrationConfirmationEmailFactory();
        var user = new User { Email = "new.user@example.com" };

        var message = factory.Create(user, "https://example.com/confirm?token=abc");

        Assert.Multiple(() =>
        {
            Assert.That(message.To.Single().Address, Is.EqualTo(user.Email));
            Assert.That(message.Subject, Is.EqualTo("Confirmation email link"));
            Assert.That(message.PlainTextBody,
                Is.EqualTo("https://example.com/confirm?token=abc"));
            Assert.That(message.HtmlBody, Is.Null);
        });
    }

    [Test]
    public void AddDruwareServerControllers_DoesNotReplaceApplicationFactory()
    {
        var services = new ServiceCollection();
        var applicationFactory = new ApplicationFactory();
        services.AddSingleton<IRegistrationConfirmationEmailFactory>(
            applicationFactory);

        services.AddDruwareServerControllers();

        using var provider = services.BuildServiceProvider();
        Assert.That(
            provider.GetRequiredService<IRegistrationConfirmationEmailFactory>(),
            Is.SameAs(applicationFactory));
    }

    private sealed class ApplicationFactory
        : IRegistrationConfirmationEmailFactory
    {
        public EmailMessage Create(User user, string confirmationLink) =>
            new()
            {
                To = { new EmailRecipient(user.Email!) },
                Subject = "Application subject",
                HtmlBody = confirmationLink
            };
    }
}
