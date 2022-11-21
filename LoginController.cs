using System;
using AutoMapper;
using Druware.Server.Entities;
using Druware.Server.Models;
using Druware.Server.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Web;

namespace Druware.Server.Controllers
{

    [Route("api/[controller]")]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private readonly ApplicationSettings _settings;

        public LoginController(
            IConfiguration configuration,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _configuration = configuration;
            _settings = new ApplicationSettings(_configuration);
            _mapper = mapper;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet("")]
        public IActionResult Validate()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            return (_signInManager.IsSignedIn(currentUser)) ?
                Ok(Result.Ok("")) : Ok(Result.Error(""));
        }

        [HttpPost("")]
        public async Task<ActionResult<Result>> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null) return Ok(Result.Error("Acocunt Not Found"));

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (!result.Succeeded) return Ok(Result.Error("Account Not Found"));

            // If this is the first and only account in the system, promote it
            // to administration
            if (_userManager.Users.Count() == 1)
            {
                await _userManager.RemoveFromRoleAsync(user, "Visitor");
                await _userManager.AddToRoleAsync(user, "Administrator");
            }

            // All done
            return Ok(Result.Ok("Logged In"));
        }

        [HttpDelete("")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(Result.Ok("Logged Out"));
        }


    }
}