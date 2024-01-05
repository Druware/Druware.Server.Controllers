using Druware.Extensions;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet("")]
        public ActionResult<Result> IsOn()
        {
            return Ok(Result.Ok("Server is on, but not on fire"));
        }
    }
}

