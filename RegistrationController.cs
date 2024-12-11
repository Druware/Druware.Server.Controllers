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
 * Copyright 2019-2023 by:
 *    Andy 'Dru' Satori @ Satori & Associates, Inc.
 *    All Rights Reserved
 */

using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Routing;

namespace Druware.Server.Controllers
{

    [Route("api/[controller]")]
    //[Route("[controller]")]
    public class RegistrationController : CustomController
    {
        private readonly IMapper _mapper;
        private readonly AppSettings _settings;

        public RegistrationController(
            IConfiguration configuration,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext context)
            : base(configuration, userManager, signInManager, context)
        {
            _settings = new AppSettings(Configuration);
            _mapper = mapper;
        }

        /// <summary>
        /// While probably not needed this provides and empty registration model
        /// to be filled in and sent back to the server for processing.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetEmptyModel() =>
            Ok(new UserRegistrationModel());
        
        /// <summary>
        /// A quick and easy way to update the access log
        /// </summary>
        private async Task UpdateAccessLog(string how, string? who = null, string? message = null)
        {
            var data = HttpContext.GetRouteData();
            var what =
                $"{data.Values["controller"]?.ToString() ?? ""}.{data.Values["action"]?.ToString() ?? ""}";
            what += message ?? "";

            var where = HttpContext.Connection.RemoteIpAddress.ToString();
            Access access = new()
            {
                Who = who ?? "none",
                When = DateTime.UtcNow,
                What = what,
                How = how,
                Where = where
            };

            ServerContext.AccessLog.Add(access);

            await ServerContext.SaveChangesAsync();
        }
        
        /// <summary>
        /// Confirms an email after a registration
        /// </summary>
        /// <param name="token"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("confirm")]
        public async Task<ActionResult<Result>> ConfirmEmail([FromQuery] string token, [FromQuery] string email)
        {
            // await UpdateAccessLog("GET"); 
            // Confirm the Email
            var user = await UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                await UpdateAccessLog("GET", "Unknown", "User Not Found"); 
                return Ok(Result.Error("Cannot Confirm this Account"));
            }

            var result = await UserManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                await UserManager.RemoveFromRoleAsync(user, UserSecurityRole.Unconfirmed.ToUpper());
                await UserManager.AddToRoleAsync(user, UserSecurityRole.Confirmed.ToUpper());
                if (UserManager.Users.Count() == 1)
                    await UserManager.AddToRoleAsync(user, UserSecurityRole.SystemAdministrator.ToUpper());
                await UpdateAccessLog("GET", user.NormalizedUserName); 
                return Ok(Result.Ok("Account Confirmed"));
            }

            var errorResult = Result.Error("Cannot Confirm this Account");
            foreach (var error in result.Errors)
                errorResult.Info?.Add(error.Description);
            await UpdateAccessLog("GET", user.NormalizedUserName, errorResult.ToString()); 
            return Ok(errorResult);
        }

        /// <summary>
        /// Handle the post of a registratoin.  While this is fine for the moment
        /// at some point, it will probably require a good bit of expansion in
        /// order to prevent bot/spam registrations
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("")]
        public async Task<ActionResult<Result>> Register(
            [FromBody] UserRegistrationModel model)
        {
            await UpdateAccessLog("POST");

            if (_settings.Smtp == null) throw new Exception("Mail Services Not  Configured");

            if (!ModelState.IsValid)
                return Ok(Result.Error("Invalid Model Received"));

            var user = _mapper.Map<User>(model);

            var result = await UserManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errorResult = Result.Error("Unable to create user.");
                foreach (var error in result.Errors)
                    errorResult.Info?.Add(error.Description);
                return Ok(errorResult);
            }

            var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);

            // build out the email message to the registered email with the
            // confirmation link that provides the path to confirm the email.

            var link = string.Format("{0}?email={2}&token={1}",
                _settings.ConfirmationUrl,
                HttpUtility.UrlEncode(token),
                user.Email);

            // TODO: Flesh this out to provide a nice email for confirmation,
            //       preferably with multiple output formats ( templaste loaded
            //       from resources perhaps )
            if (Assembly.GetEntryAssembly()?.GetName()?.Name == null)
                return Ok(Result.Error("Assembly Not Found"));
            if (_settings.Notification == null)
                return Ok(Result.Error("Settings Not Found"));
            
            Console.WriteLine(link);
            
            var helper = new MailHelper(_settings.Smtp, Assembly.GetEntryAssembly()!.GetName()!.Name!);
            helper.SendAsync(
                user.Email,
                _settings.Notification!.From!,
                _settings.Notification!.From!,
                "Confirmation email link",
                link!
            );

            await UserManager.AddToRoleAsync(user, UserSecurityRole.Unconfirmed.ToUpper());

#if DEBUG
            return Ok(Result.Ok($"{HttpUtility.UrlEncode(token)}"));
#else
            return Ok(Result.Ok("User Created, limited to Visitor Status Until Confirmed."));
#endif
        }

    }
}

