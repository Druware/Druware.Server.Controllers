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
 *    Andy 'Dru' Satori @ Druware Software Designs
 *    All Rights Reserved
 */

using Druware.Server.Entities;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Druware.Server.Controllers
{
    /// <summary>
    /// AccessController provides read-only access to the Access Log, the audit
    /// trail of who accessed what resource, when, from where and how. The log
    /// itself is populated by the framework (see CustomController.LogRequest and
    /// UpdateUserAccess) so this controller exposes query and statistics
    /// endpoints only. All endpoints are restricted to SystemAdministrators.
    /// </summary>
    [Route("api/[controller]")]
    [Route("[controller]")]
    [Authorize(Roles = UserSecurityRole.SystemAdministrator)]
    public class AccessController : CustomController
    {
        public AccessController(
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext context)
            : base(configuration, userManager, signInManager, context)
        { }

        /// <summary>
        /// Returns a paged list of Access Log entries ordered by 'When' in
        /// descending order (most recent first). The list may be filtered by any
        /// combination of who, what, where and how using case-insensitive
        /// partial matches.
        /// </summary>
        [HttpGet("")]
        public async Task<ActionResult<ListResult>> GetList(
            [FromQuery] string? who = null,
            [FromQuery] string? what = null,
            [FromQuery] string? where = null,
            [FromQuery] string? how = null,
            [FromQuery] int page = 0,
            [FromQuery] int count = 1000)
        {
            var r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                var query = ApplyFilters(
                    ServerContext.AccessLog.AsQueryable(),
                    who, what, where, how);

                var total = query.Count();
                var list = query
                    .OrderByDescending(a => a.When)
                    .Skip(page * count)
                    .Take(count)
                    .AsNoTracking()
                    .ToList();

                return Ok(ListResult.Ok(list, total, page, count));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        /// <summary>
        /// Returns a single Access Log entry by its Id.
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<ActionResult<Access>> Get(long id)
        {
            var r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                var access = ServerContext.AccessLog
                    .AsNoTracking()
                    .FirstOrDefault(a => a.Id == id);

                return (access != null) ? Ok(access) : BadRequest("Not Found");
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        /// <summary>
        /// Returns statistics for the Access Log scoped to the last 30 days:
        /// the total number of requests, along with counts grouped by who, what
        /// and where.
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult> Statistics()
        {
            var r = await UpdateUserAccess();
            if (r != null) return r;

            try
            {
                var since = DateTime.UtcNow.AddDays(-30);
                var scoped = ServerContext.AccessLog
                    .AsNoTracking()
                    .Where(a => a.When >= since);

                var total = scoped.Count();

                var byWho = scoped
                    .GroupBy(a => a.Who)
                    .Select(g => new AccessStatistic
                    {
                        Value = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                var byWhat = scoped
                    .GroupBy(a => a.What)
                    .Select(g => new AccessStatistic
                    {
                        Value = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                var byWhere = scoped
                    .GroupBy(a => a.Where)
                    .Select(g => new AccessStatistic
                    {
                        Value = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(s => s.Count)
                    .ToList();

                return Ok(new AccessStatistics
                {
                    Days = 30,
                    Since = since,
                    Total = total,
                    ByWho = byWho,
                    ByWhat = byWhat,
                    ByWhere = byWhere
                });
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }

        /// <summary>
        /// Applies the optional who/what/where/how filters to the supplied
        /// query using case-insensitive partial matches.
        /// </summary>
        private static IQueryable<Access> ApplyFilters(
            IQueryable<Access> query,
            string? who, string? what, string? where, string? how)
        {
            if (!string.IsNullOrWhiteSpace(who))
            {
                var value = who.ToLower();
                query = query.Where(a =>
                    a.Who != null && a.Who.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(what))
            {
                var value = what.ToLower();
                query = query.Where(a =>
                    a.What != null && a.What.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(where))
            {
                var value = where.ToLower();
                query = query.Where(a =>
                    a.Where != null && a.Where.ToLower().Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(how))
            {
                var value = how.ToLower();
                query = query.Where(a =>
                    a.How != null && a.How.ToLower().Contains(value));
            }

            return query;
        }

        /// <summary>
        /// A single grouped statistic: the value being grouped on and the number
        /// of requests that matched it within the scoped window.
        /// </summary>
        public class AccessStatistic
        {
            public string? Value { get; set; }
            public int Count { get; set; }
        }

        /// <summary>
        /// The statistics payload returned by the Statistics endpoint.
        /// </summary>
        public class AccessStatistics
        {
            public int Days { get; set; }
            public DateTime Since { get; set; }
            public int Total { get; set; }
            public List<AccessStatistic> ByWho { get; set; } = new();
            public List<AccessStatistic> ByWhat { get; set; } = new();
            public List<AccessStatistic> ByWhere { get; set; } = new();
        }
    }
}
