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
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Druware.Server.Email;

namespace Druware.Server.Controllers;

[Route("api/[controller]")]
// [Route("[controller]")]
public class UserController : CustomController
{
    // private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly RoleManager<Role> _roleManager;
    private readonly IEmailSender _emailSender;
    private readonly IRegistrationConfirmationEmailFactory _confirmationEmailFactory;

    private readonly AppSettings _settings;

    public UserController(
        IConfiguration configuration,
        IMapper mapper,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        SignInManager<User> signInManager,
        ServerContext context,
        IEmailSender emailSender,
        IRegistrationConfirmationEmailFactory confirmationEmailFactory)
        : base(configuration, userManager, signInManager, context)
    {
        _settings = new AppSettings(Configuration);
        _mapper = mapper;
        _roleManager = roleManager;
        _emailSender = emailSender;
        _confirmationEmailFactory = confirmationEmailFactory;
    }

    #region Base
    
    [HttpGet("")]
    [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
    public async Task<ActionResult<ListResult>> GetList([FromQuery] int page = 0, [FromQuery] int count = 1000)
    {
        var r = await UpdateUserAccess();
        if (r != null) return r;

        try
        {
            var total = UserManager.Users.Count();
            var list = UserManager.Users
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .Skip(page * count)
                .Take(count)
                .AsNoTracking()
                .ToList();
            var result = ListResult.Ok(list, total, page, count);
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
        var r = await UpdateUserAccess();
        if (r != null) return r;

        try
        {
            var user = await UserManager.FindByIdAsync(value);
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

            if (!_emailSender.IsConfigured)
                throw new Exception("Mail Services Not  Configured");

            var link = string.Format("{0}?email={2}&token={1}",
                _settings.ConfirmationUrl,
                HttpUtility.UrlEncode(token),
                user.Email);

            var message = _confirmationEmailFactory.Create(user, link);
            var sendResult = await _emailSender.SendAsync(
                message, HttpContext.RequestAborted);
            if (!sendResult.Succeeded)
                Console.Error.WriteLine(
                    $"Unable to send user confirmation email: {sendResult.ErrorMessage}");

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
    
    #endregion 

    #region Roles
    
    [HttpGet("{value}/role")]
    [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
    public async Task<ActionResult> GetUserRoles(string value)
    {
        var r = await UpdateUserAccess();
        if (r != null) return r;

        try
        {
            var user = await UserManager.FindByIdAsync(value);
            if (user == null) return BadRequest("Not Found");
            var roles = await UserManager.GetRolesAsync(user);

            return Ok(roles.ToList());
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
        var r = await UpdateUserAccess();
        if (r != null) return r;

        try
        {
            var total = _roleManager.Roles.Count();
            var list = _roleManager.Roles
                .OrderBy(a => a.Name)
                .Skip(page * count)
                .Take(count)
                .AsNoTracking()
                .ToList();
            var result = ListResult.Ok(list!, total, page, count);

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
            var user = await UserManager.FindByIdAsync(value);
            if (user == null) return BadRequest("Not Found");

            var allRoles = _roleManager.Roles.ToList(); // adding the ToList() prevents keeping the connection open
            var userRoles = await UserManager.GetRolesAsync(user);

            // loop the allRoles to ensure that we add/remove properly
            foreach (var role in allRoles)
            {
                // if the role exists in the userRoles, but not the posted roles
                // then remove it.
                if (role.Name != null && !roles.Contains(role.Name) && userRoles.Contains(role.Name))
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
    
    #endregion
    
    #region Mfa
    
    
    
    #endregion
    

}



