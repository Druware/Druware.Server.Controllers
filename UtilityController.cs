using System;
using Druware.Extensions;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    public class UtilityController : ControllerBase
    {
        [HttpGet("encode/{value}")]
        public ActionResult<Result> Encrypt(string value)
        {
            return (Assembly.GetEntryAssembly()?.GetName()?.Name == null) ?
                Ok(Result.Error("No Assembly Found")) :
                Ok(Result.Ok(value.Encrypt(Assembly.GetEntryAssembly()!.GetName()!.Name!)));
        }

        [HttpGet("up")]
        public ActionResult<Result> IsOn()
        {
            return Ok(Result.Ok("Server is on, but not on fire"));
        }

        [HttpGet("")]
        public ActionResult<ListResult> Utilities()
        {
            List<string> endPoints = new();
            endPoints.Add("/ - this list");
            endPoints.Add("/up - isServerUp");
            endPoints.Add("/encode/{value} - getEncodedValue");

            return Ok(ListResult.Ok(endPoints));
        }
    }
}

