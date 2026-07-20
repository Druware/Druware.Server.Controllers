/* This file is part of the Druware.Server API Library
 *
 * Foobar is free software: you can redistribute it and/or modify it under the
 * terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 3 of the License, or (at your option) any later
 * version.
 *
 * The Druware.Server API Library is distributed in the hope that it will be
 * useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
 * Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along with
 * the Druware.Server API Library. If not, see <https://www.gnu.org/licenses/>.
 *
 * Copyright 2019-2026 by:
 *    Andy 'Dru' Satori @ Druware Software Designs.
 *    All Rights Reserved
 */

using System.Text.RegularExpressions;
using Druware.Server.Email;
using Microsoft.Extensions.Configuration;

namespace Druware.Server.Controllers;

/// <summary>
/// Compatibility wrapper for callers that previously used the controller
/// package's Azure-specific helper directly. New code should depend on
/// <see cref="IEmailSender"/>.
/// </summary>
[Obsolete("Inject Druware.Server.Email.IEmailSender instead.")]
public class AzureMailHelper(IEmailSender emailSender)
{
    public const string ConnectionStringKey =
        AzureCommunicationEmailSender.ConnectionStringKey;
    public const string ConnectionStringEnvironmentKey =
        AzureCommunicationEmailSender.ConnectionStringEnvironmentKey;

    public AzureMailHelper(IConfiguration configuration)
        : this(new AzureCommunicationEmailSender(configuration))
    {
    }

    public bool IsConfigured => emailSender.IsConfigured;

    public async Task SendAsync(string to, string from, string replyTo,
        string subject, string body)
    {
        var result = await emailSender.SendAsync(CreateMessage(
            to, replyTo, subject, null, body));
        LogIfFailed(result);
    }

    public async Task SendHtmlAsync(string to, string from, string replyTo,
        string subject, string htmlBody, string? plainTextBody = null)
    {
        var result = await emailSender.SendAsync(CreateMessage(
            to,
            replyTo,
            subject,
            htmlBody,
            plainTextBody ?? StripHtmlTags(htmlBody)));
        LogIfFailed(result);
    }

    private static EmailMessage CreateMessage(string to, string replyTo,
        string subject, string? htmlBody, string? plainTextBody)
    {
        var message = new EmailMessage
        {
            To = { new EmailRecipient(to) },
            Subject = subject,
            HtmlBody = htmlBody,
            PlainTextBody = plainTextBody
        };

        if (!string.IsNullOrWhiteSpace(replyTo))
            message.ReplyTo.Add(new EmailRecipient(replyTo));

        return message;
    }

    private static void LogIfFailed(EmailSendResult result)
    {
        if (!result.Succeeded)
            Console.Error.WriteLine(
                $"An error occurred while trying to send an email: " +
                (result.ErrorMessage ?? "Unable to send email."));
    }

    private static string StripHtmlTags(string html) =>
        Regex.Replace(html, "<[^>]*>", "");
}
