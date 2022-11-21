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
    public class RegistrationController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private readonly ApplicationSettings _settings;

        public RegistrationController(
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

        /// <summary>
        /// While probably not needed this provides and empty registration model
        /// to be filled in and sent back to the server for processing.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult GetEmptyModel() =>
            Ok(new Models.UserRegistrationModel());

        /// <summary>
        /// Confirms an email after a registration
        /// </summary>
        /// <param name="token"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet("confirm")]
        public async Task<ActionResult<Results.Result>> ConfirmEmail([FromQuery] string token, [FromQuery] string email)
        {
            // Confirm the Email
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return Ok(Result.Error("Cannot Confirm this Account"));
            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded) return Ok(Result.Ok("Account Confirmed"));

            var errorResult = Result.Error("Cannot Confirm this Account");
            foreach (var error in result.Errors)
                errorResult.Info?.Add(error.Description);
            return Ok(errorResult);
        }


        /// <summary>
        /// Handle the post of a registratoin.  While this is fine for the moment
        /// at some point, it will probably require a good bit of expansion in
        /// order to prevent bot/spam registrations
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("")]
        public async Task<ActionResult<Result>> Register(
            [FromBody] UserRegistrationModel model)
        {
            if (!ModelState.IsValid)
                return Ok(Results.Result.Error("Invalid Model Recieved"));

            var user = _mapper.Map<User>(model);

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                Results.Result errorResult = Results.Result.Error("Unable to create user.");
                foreach (var error in result.Errors)
                    errorResult.Info?.Add(error.Description);
                return Ok(errorResult);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // build out the email message to the registered email with the
            // confirmation link that provides the path to confirm the email.

            if (_settings.Smtp == null) throw new Exception("Mail Services Not  Configured");

            var link = string.Format("{0}?email={2}&token={1}",
                _settings.ConfirmationUrl,
                HttpUtility.UrlEncode(token),
                user.Email);

            // TODO: Flesh this out to provide a nice email for confirmation,
            //       preferably with multiple output formats ( templaste loaded
            //       from resources perhaps )
            if (Assembly.GetEntryAssembly()?.GetName()?.Name == null)
                return Ok(Result.Error("Assembly Not Found"));
            if (_settings.Notification == null)
                return Ok(Result.Error("Settings Not Found"));

            MailHelper helper = new MailHelper(_settings.Smtp, Assembly.GetEntryAssembly()!.GetName()!.Name!);
            helper.Send(
                user.Email,
                _settings.Notification!.From!,
                _settings.Notification!.From!,
                "Confirmation email link",
                link!
            );

            await _userManager.AddToRoleAsync(user, "Visitor");

            return Ok(Results.Result.Ok("User Created, limited to Visitor Status Until Confirmed."));
        }

    }
}

