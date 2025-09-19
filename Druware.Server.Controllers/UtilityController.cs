using System;
using Druware.Extensions;
using RESTfulFoundation.Server;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    //[Route("[controller]")]
    public class UtilityController : ControllerBase
    {
        [HttpGet("encode/{value}")]
        public ActionResult<Result> Encrypt(string value)
        {
            var name = Assembly.GetEntryAssembly()!.GetName()
                .Name ?? "Druware.Server.Controllers";
            return (Assembly.GetEntryAssembly()?.GetName().Name == null)
                ? Ok(Result.Error("No Assembly Found"))
                : Ok(Result.Ok(
                    value.Encrypt(name, "4D584868CCA84221823D53D80DB30FCB")));
        }
        
        [HttpGet("")]
        public ActionResult<ListResult> Utilities()
        {
            List<string> endPoints = new()
            {
                "/ - this list",
                "/encode/{value} - getEncodedValue"
            };
            return Ok(ListResult.Ok(endPoints));
        }
    }
}