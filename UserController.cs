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

using System;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Routing;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class UserController : CustomController
    {
        // private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly RoleManager<Role> _roleManager;

        private readonly AppSettings _settings;

        public UserController(
            IConfiguration configuration,
            IMapper mapper,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            SignInManager<User> signInManager,
            ServerContext context)
            : base(configuration, userManager, signInManager, context)
        {
            _settings = new AppSettings(Configuration);
            _mapper = mapper;
            _roleManager = roleManager;
        }

        [HttpGet("")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult<ListResult>> GetList([FromQuery] int page = 0, [FromQuery] int count = 1000)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                if (UserManager.Users == null) return BadRequest(); // think I want to alter this to not need the Ok()

                var total = UserManager.Users?.Count() ?? 0;
                var list = UserManager.Users?
                    .OrderBy(a => a.LastName)
                    .ThenBy(a => a.FirstName)
                    .Skip(page * count)
                    .Take(count)
                    .AsNoTracking()
                    .ToList();
                if (list == null)
                {
                    return BadRequest("No List Returned");
                }
                ListResult result = ListResult.Ok(list, total, page, count);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("{value}")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult<User>> Get(string value)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                User? user = await UserManager.FindByIdAsync(value);
                return (user != null) ? Ok(user) : BadRequest("Not Found");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpDelete("{value}")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult<Result>> Remove(string value)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                User? user = await UserManager.FindByIdAsync(value);
                if (user == null) return BadRequest("Not Found");

                IdentityResult result = await UserManager.DeleteAsync(user);

                return (result.Succeeded) ?
                    Ok(Result.Ok("Success")) :
                    Ok(Result.Error(result.Errors.ToString()));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("{value}/role")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult> GetUserRoles(string value)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                User? user = await UserManager.FindByIdAsync(value);
                if (user == null) return BadRequest("Not Found");

                var roles = await UserManager.GetRolesAsync(user);

                return (roles != null) ? Ok(roles.ToList()) : BadRequest("Not Found");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpGet("role")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult> GetAvailableRoles([FromQuery] int page = 0, [FromQuery] int count = 1000)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                var total = _roleManager.Roles?.Count() ?? 0;
                var list = _roleManager.Roles?
                    .OrderBy(a => a.Name)
                    .Skip(page * count)
                    .Take(count)
                    .AsNoTracking()
                    .ToList();
                ListResult result = ListResult.Ok(list!, total, page, count);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        [HttpPut("{value}/role")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult> UpdateUserRole(
            string value, [FromBody] List<string> roles)
        {
            try
            {
                User? user = await UserManager.FindByIdAsync(value);
                if (user == null) return BadRequest("Not Found");

                var allRoles = _roleManager.Roles.ToList(); // adding the ToList() prevents keeping the connection open
                var userRoles = await UserManager.GetRolesAsync(user);

                // loop the allRoles to ensure that we add/remove properly
                foreach (Role role in allRoles)
                {
                    // if the role exists in the userRoles, but not the posted roles
                    // then remove it.
                    if (!roles.Contains(role.Name) && userRoles.Contains(role.Name))
                        await UserManager.RemoveFromRoleAsync(user, role.Name);

                    // if the role exists in the posted roles but not the userRoles
                    // the add it.
                    if (roles.Contains(role.Name) && !userRoles.Contains(role.Name))
                        await UserManager.AddToRoleAsync(user, role.Name);
                }

                userRoles = await UserManager.GetRolesAsync(user);

                return (userRoles != null) ? Ok(userRoles.ToList()) : BadRequest("Not Found");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        
        [HttpPost("")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<ActionResult<Result>> Add(
            [FromBody] UserRegistrationModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return Ok(Result.Error("Invalid Model Recieved"));

                var user = _mapper.Map<User>(model);

                var result = await UserManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    Result errorResult = Result.Error("Unable to create user.");
                    foreach (var error in result.Errors)
                        errorResult.Info?.Add(error.Description);
                    return Ok(errorResult);
                }

                var token = await UserManager.GenerateEmailConfirmationTokenAsync(user);

                // build out the email message to the registered email with the
                // confirmation link that provides the path to confirm the email.

                if (_settings.Smtp == null) throw new Exception("Mail Services Not  Configured");

                var link = string.Format("{0}?email={2}&token={1}",
                    _settings.ConfirmationUrl,
                    HttpUtility.UrlEncode(token),
                    user.Email);

                // TODO: Flesh this out to provide a nice email for confirmation,
                //       preferably with multiple output formats ( templaste loaded from resources perhaps )
                if (Assembly.GetEntryAssembly()?.GetName()?.Name == null)
                    return Ok(Result.Error("Assembly Not Found"));
                if (_settings.Notification == null)
                    return Ok(Result.Error("Settings Not Found"));
                Console.WriteLine(Assembly.GetEntryAssembly()!.GetName()!.Name!);

                MailHelper helper = new MailHelper(_settings.Smtp, Assembly.GetEntryAssembly()!.GetName()!.Name!);
                helper.Send(
                    user.Email,
                    _settings.Notification.From!,
                    _settings.Notification.From!,
                    "Confirmation email link",
                    link!
                );

                await UserManager.AddToRoleAsync(user, UserSecurityRole.Unconfirmed.ToUpper());

#if DEBUG
                return Ok(Result.Ok(string.Format("{0}", HttpUtility.UrlEncode(token))));
#else
            return Ok(Result.Ok("User Created, limited to Visitor Status Until Confirmed."));
#endif
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
    }

}

