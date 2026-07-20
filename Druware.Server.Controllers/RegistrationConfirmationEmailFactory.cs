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

using Druware.Server.Email;
using Druware.Server.Entities;

namespace Druware.Server.Controllers;

/// <summary>
/// Creates the message sent after an account is created. Applications can
/// replace this service to provide their own branded HTML and plain-text
/// content without replacing the registration controller or sending a second
/// confirmation message.
/// </summary>
public interface IRegistrationConfirmationEmailFactory
{
    EmailMessage Create(User user, string confirmationLink);
}

/// <summary>
/// Produces the original Druware confirmation message: the confirmation link
/// as plain text with the subject "Confirmation email link".
/// </summary>
public sealed class DefaultRegistrationConfirmationEmailFactory
    : IRegistrationConfirmationEmailFactory
{
    public EmailMessage Create(User user, string confirmationLink)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
            throw new InvalidOperationException(
                "A confirmation email cannot be created for a user without an email address.");

        return new EmailMessage
        {
            To = { new EmailRecipient(user.Email) },
            Subject = "Confirmation email link",
            PlainTextBody = confirmationLink
        };
    }
}
