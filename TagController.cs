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
using System.Reflection;
using System.Web;
using System.Xml.Linq;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;
using RESTfulFoundation.Server;
using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Druware.Server;

using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace Druware.Server.Controllers
{
    /// <summary>
    /// TagController provides the foundation Tag pool for ALL objects within
    /// the various Druware.Server.* libraries.
    /// </summary>
    [Route("api/[controller]")]
    public class TagController : CustomController
    {
        private readonly IMapper _mapper;
        private readonly ApplicationSettings _settings;

        public TagController(
            IConfiguration configuration,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext context)
            : base(configuration, userManager, signInManager, context)
        {
            _settings = new ApplicationSettings(Configuration);
            _mapper = mapper;
        }

        // get - get a list of all the article, with count and offset built in
        [HttpGet("")]
        public IActionResult GetList([FromQuery] int page = 0, [FromQuery] int count = 1000)
        {
            if (ServerContext.Tags == null) return Ok(Result.Ok("No Data Available")); // think I want to alter this to not need the Ok()

            var total = ServerContext.Tags?.Count() ?? 0;
            var list = ServerContext.Tags?
                .OrderBy(a => a.Name)
                .Skip(page * count)
                .Take(count)
                .ToList();
            if (list == null)
            {
                return Ok(ListResult.Error("No List Returned"));
            }
            ListResult result = ListResult.Ok(list, total, 0, 1000);
            return Ok(result);
        }

        [HttpGet("{value}")]
        public IActionResult GetTag(string value)
        {
            Tag? tag = Tag.ByNameOrId(ServerContext, value);
            return (tag != null) ? Ok(tag) : BadRequest("Not Found");
        }
        
        [HttpPost("")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<ActionResult<Tag>> Add(
            [FromBody] Tag model)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            if (!ModelState.IsValid)
                return Ok(Result.Error("Invalid Model Recieved"));

            if (ServerContext.Tags == null)
                return Ok(Result.Ok("No Data Available")); // think I want to alter this to not need the Ok()

            ServerContext.Tags.Add(model);
            await ServerContext.SaveChangesAsync();

            return Ok(model);
        }

        [HttpDelete("{value}")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<IActionResult> DeleteObject(string value)
        {
            ActionResult? r = await UpdateUserAccess();
            if (r != null) return r;

            Tag? tag = Tag.ByNameOrId(ServerContext, value);
            if (tag == null) return BadRequest("Not Found");

            // now that we have an entity, we will need to check if it is
            // referenced from other entities, like artictles, but we are not
            // there yet.
            // ... Dru 2022.11.02

            ServerContext.Tags.Remove(tag);
            ServerContext.SaveChanges();

            // Should rework the save to return a success of fail on the delete
            return Ok(Result.Ok("Delete Successful"));


        }
    }
}

