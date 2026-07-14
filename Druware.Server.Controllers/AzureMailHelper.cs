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
 *    Andy 'Dru' Satori @ Satori & Associates, Inc.
 *    All Rights Reserved
 */

using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;

namespace Druware.Server.Controllers;

/// <summary>
/// Sends mail through the Azure Communication Services Email API, replacing
/// the SMTP based MailHelper that shipped with Druware.Server.
///
/// The connection string is a secret, and belongs in the environment rather
/// than in appsettings.json. It is resolved, in order, from:
///   1. The COMMUNICATION_SERVICES_CONNECTION_STRING environment variable
///      ( the name used by Azure App Service )
///   2. The API__Mail__Azure__ConnectionString environment variable, which the
///      configuration provider surfaces as API:Mail:Azure:ConnectionString
///   3. The API:Mail:Azure:ConnectionString configuration key, retained only
///      so existing appsettings.json based deployments keep working
/// </summary>
public class AzureMailHelper
{
    public const string ConnectionStringKey = "API:Mail:Azure:ConnectionString";
    public const string ConnectionStringEnvironmentKey =
        "COMMUNICATION_SERVICES_CONNECTION_STRING";

    private readonly EmailClient? _client;

    public AzureMailHelper(IConfiguration configuration)
    {
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentKey)
            ?? configuration.GetValue<string>(ConnectionStringKey);

        if (!string.IsNullOrWhiteSpace(connectionString))
            _client = new EmailClient(connectionString);
    }

    /// <summary>
    /// True when a connection string was found, and mail can be delivered.
    /// </summary>
    public bool IsConfigured => _client != null;

    public async Task SendAsync(string to, string from, string replyTo,
        string subject, string body)
    {
        var content = new EmailContent(subject) { PlainText = body };
        await SendAsync(to, from, replyTo, content);
    }

    public async Task SendHtmlAsync(string to, string from, string replyTo,
        string subject, string htmlBody, string? plainTextBody = null)
    {
        var content = new EmailContent(subject)
        {
            Html = htmlBody,
            PlainText = plainTextBody ?? StripHtmlTags(htmlBody)
        };
        await SendAsync(to, from, replyTo, content);
    }

    private async Task SendAsync(string to, string from, string replyTo,
        EmailContent content)
    {
        if (_client == null)
            throw new InvalidOperationException(
                "Azure Communication Services mail is not configured. Set the " +
                $"{ConnectionStringEnvironmentKey} environment variable.");

        var message = new EmailMessage(
            from,
            new EmailRecipients(new List<EmailAddress> { new(to) }),
            content);

        if (!string.IsNullOrWhiteSpace(replyTo))
            message.ReplyTo.Add(new EmailAddress(replyTo));

        try
        {
            await _client.SendAsync(WaitUntil.Completed, message);
        }
        catch (RequestFailedException e)
        {
            Console.Error.WriteLine(
                $"An Error Occurred while trying to send an email: {e.Message}");
        }
    }

    private static string StripHtmlTags(string html) =>
        Regex.Replace(html, "<[^>]*>", "");
}
