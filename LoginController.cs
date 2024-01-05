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
using RESTfulFoundation.Server;
using Druware.Server.Entities;
using Druware.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Web;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace Druware.Server.Controllers
{

    [Route("api/[controller]")]
    public class LoginController : CustomController
    {
        private readonly IMapper _mapper;
        private readonly AppSettings _settings;

        public LoginController(
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

        [HttpGet("")]
        public async Task<IActionResult> Validate()
        {
            if (User.Identity == null)
                return Ok(Result.Error("No User Logged In"));
            
            User? user = await Entities.User.ByName(this.User.Identity?.Name, UserManager);
            if (user == null) return Ok(Result.Error("Unable to find User"));
            if (user.IsSessionExpired())
                await SignInManager.SignOutAsync();

            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            return (SignInManager.IsSignedIn(currentUser)) ?
                Ok(Result.Ok("")) : Ok(Result.Error(""));
        }

        [HttpPost("")]
        public async Task<ActionResult<User>> Login([FromBody] LoginModel model)
        {
            var user = await UserManager.FindByNameAsync(model.UserName);
            if (user == null) return Ok(Result.Error("Account Not Found"));

            var result = await SignInManager.PasswordSignInAsync(user, model.Password, false, false);

            user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
            await ServerContext.SaveChangesAsync();

            if (!result.Succeeded) return Ok(Result.Error("Account Not Found"));

            // All done
            return Ok(user);
        }

        [HttpDelete("")]
        public async Task<IActionResult> Logout()
        {
            User? user = await Entities.User.ByName(this.User.Identity?.Name, UserManager);
            // theoretically, this should never get here, but we perform the
            // check just to be safe.
            if (user == null) return Ok(Result.Error("Unable to find User"));
            user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
            await ServerContext.SaveChangesAsync();
            await SignInManager.SignOutAsync();
            return Ok(Result.Ok("Logged Out"));
        }
    }
}