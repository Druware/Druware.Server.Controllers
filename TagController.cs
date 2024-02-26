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
 * Copyright 2019-2024 by:
 *    Andy 'Dru' Satori @ Druware Software Designs
 *    All Rights Reserved
 */

using Druware.Server.Entities;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Druware.Server.Controllers
{
    /// <summary>
    /// TagController provides the foundation Tag pool for ALL objects within
    /// the various Druware.Server.* libraries.
    /// </summary>
    [Route("api/[controller]")]
    [Route("[controller]")]
    public class TagController : CustomController
    {
        public TagController(
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext context)
            : base(configuration, userManager, signInManager, context)
        { }

        // get - get a list of all the article, with count and offset built in
        [HttpGet("")]
        public IActionResult GetList([FromQuery] int page = 0, [FromQuery] int count = 1000)
        {
            var total = ServerContext.Tags.Count();
            var list = ServerContext.Tags
                .OrderBy(a => a.Name)
                .Skip(page * count)
                .Take(count)
                .ToList();
            return Ok(ListResult.Ok(list, total, 0, 1000));
        }

        [HttpGet("{value}")]
        public IActionResult GetTag(string value) =>
            Ok(Tag.ByNameOrId(ServerContext, value));
        
        [HttpPost("")]
        [HttpPut("{value}")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<ActionResult<Tag>> Add(
            [FromBody] Tag model, string? value)
        {
            var r = await UpdateUserAccess();
            if (r != null) return r;

            if (!ModelState.IsValid)
                return Ok(Result.Error("Invalid Model Received"));
            if (model.Name == null) return BadRequest("No name provided");
            
            var tag = Tag.ByNameOrId(ServerContext, value ?? model.Name!);
            if (value == null && tag.TagId > 0) return tag;

            tag.Name = model.Name!;
            
            if (tag.TagId is null or < 1) ServerContext.Tags.Add(model);
            await ServerContext.SaveChangesAsync();

            return Ok(Tag.ByNameOrId(ServerContext, model.Name!));
        }

        [HttpDelete("{value}")]
        [Authorize(Roles = UserSecurityRole.ManagerOrSystemAdministrator)]
        public async Task<IActionResult> DeleteObject(string value)
        {
            var r = await UpdateUserAccess();
            if (r != null) return r;

            var tag = Tag.ByNameOrId(ServerContext, value);

            // now that we have an entity, we will need to check if it is
            // referenced from other entities, like articles, but we are not
            // there yet.
            // ... Dru 2022.11.02

            ServerContext.Tags.Remove(tag);
            await ServerContext.SaveChangesAsync();

            // Should rework the save to return a success of fail on the delete
            return Ok(Result.Ok("Delete Successful"));


        }
    }
}

