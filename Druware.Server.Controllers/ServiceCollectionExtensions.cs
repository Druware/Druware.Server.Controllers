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

using Druware.Server.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class DruwareServerControllersServiceCollectionExtensions
{
    /// <summary>
    /// Registers the default services used by Druware.Server.Controllers.
    /// Callers may register their own registration confirmation email factory
    /// before this method to retain application-specific branding.
    /// </summary>
    public static IServiceCollection AddDruwareServerControllers(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IRegistrationConfirmationEmailFactory,
            DefaultRegistrationConfirmationEmailFactory>();
        return services;
    }
}
