using System.Web;
using Druware.Server.Entities;
using Druware.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RESTfulFoundation.Server;

namespace Druware.Server.Controllers;

    /// <summary>
    /// The ProfileController provides all of the functionality for the
    /// currently logged in user to alter and adjust their user information and
    /// options.
    /// </summary>
    [Route("api/[controller]")]
    public class ProfileController : CustomController
	{
        public ProfileController(
            IConfiguration configuration,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ServerContext userContext) : base(
                configuration, userManager, signInManager, userContext)
        {
        }

        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var user = await UserManager.GetUserAsync(HttpContext.User);
                return user == null ? 
                    Ok(Result.Error("No User Logged In")) : 
                    Ok(user);
            }
            catch (Exception exc)
            {
                return Ok(Result.Error(exc.Message));
            }
        }

        [HttpGet("mfa")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<IActionResult> GetMfaToken()
        {
            try
            {
                var user = await UserManager.GetUserAsync(HttpContext.User);
                if (user == null) return Ok(Result.Error("No User Logged In"));

                var token = await UserManager.GetAuthenticatorKeyAsync(user);
                
                // otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6

                var tokenUrl =
                    $"otpauth://totp/{HttpUtility.UrlEncode("Trustwin")}:{HttpUtility.UrlEncode(user.Email)}?secret={token}&issuer={HttpUtility.UrlEncode("Trustwin")}&digits=6";
                
                
                if (token != null) return Ok(Result.Ok(tokenUrl));
                await UserManager.ResetAuthenticatorKeyAsync(user);
                token = await UserManager.GetAuthenticatorKeyAsync(user);
                return Ok(Result.Ok(tokenUrl));

            }
            catch (Exception exc)
            {
                return Ok(Result.Error(exc.Message));
            }
        }
        
        // TODO: Complete this implementation to enable round trip MFA 
        [HttpPost("mfa")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<IActionResult> VerifyAuthenticator([FromBody] MfaAuthenicator verifyAuthenticator)
        {
            // code omitted
            try
            {
                var user = await UserManager.GetUserAsync(HttpContext.User);
                if (user == null) return Ok(Result.Error("No User Logged In"));
                
                var verificationCode = verifyAuthenticator.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
     
                var is2FaTokenValid = await UserManager.VerifyTwoFactorTokenAsync(
                    user, UserManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
     
                await UserManager.SetTwoFactorEnabledAsync(user, true);

                var result = new Dictionary<string, string>();
                result.Add("Status", is2FaTokenValid ? "Success" : "Failed");
                result.Add("Message", is2FaTokenValid ? "Your authenticator app has been verified" : "Invalid Code");
     
                if (await UserManager.CountRecoveryCodesAsync(user) != 0) return Ok(result);
     
                var recoveryCodes = await UserManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                result.Add("RecoveryCodes", string.Join(",", recoveryCodes));
                return Ok(result);
            }
            catch (Exception exc)
            {
                return Ok(Result.Error(exc.Message));
            }
        }
        
        
        [HttpPut("")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<IActionResult> Put([FromBody] User model)
        {
            try
            {
                // get the current user
                var user = await UserManager.GetUserAsync(HttpContext.User);
                if (user == null) return Ok(Result.Error("No User Logged In")); 
                
                // compare the user to the model.
                if (model.Id != user.Id)
                    return BadRequest("Id mismatch");
                
                // update the user fields with the
                if (user.FirstName != model.FirstName)
                    user.FirstName = model.FirstName;
                if (user.LastName != model.LastName)
                    user.LastName = model.LastName;
                if (user.Email != model.Email)
                    user.Email = model.Email;
                if (user.PhoneNumber != model.PhoneNumber)
                    user.PhoneNumber = model.PhoneNumber;

                return await ServerContext.SaveChangesAsync() < 1 ? 
                    Ok(Result.Error("User Save Failed")) : Ok(user);
            }
            catch (Exception exc)
            {
                return Ok(Result.Error(exc.Message));
            }
        }

        [HttpGet("permission/{role}")]
        [Authorize(Roles = UserSecurityRole.Confirmed)]
        public async Task<IActionResult> CheckPermission(string role)
        {
            try
            {
                User? user = await UserManager.GetUserAsync(HttpContext.User);
                return (await UserManager.IsInRoleAsync(user, role)) ?
                    Ok(Result.Ok("Granted")) : Ok(Result.Error("Denied"));
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }
        

    }