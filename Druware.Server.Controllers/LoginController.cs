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

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Web;
using RESTfulFoundation.Server;
using Druware.Server.Entities;
using Druware.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Druware.Server.Controllers;

public class LoginResult
{
    public static LoginResult Ok(User user) => new LoginResult { User = user };
    public static LoginResult Ok(bool requiresTwoFactor) => new LoginResult { RequiresTwoFactor = requiresTwoFactor };
    
    public bool RequiresTwoFactor { get; set; }
    public User? User { get; set; }
}

public class MfaRequired
{
    public MfaRequired()
    {
    }

    public bool RequiresTwoFactor { get; set; } = true;

    [Required(ErrorMessage = "User Name is required")]
    [EmailAddress]
    public string? UserName { get; set; }

    [Required(ErrorMessage = "MFA token is required")]
    public string? Token { get; set; }
}

public class ForgotPasswordModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}    

public class PasswordResetModel
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
    [Required]
    public string? Token { get; set; }
    [Required]
    public string? Password { get; set; }
    [Required]
    public string? ConfirmPassword { get; set; }
}


[Route("api/[controller]")]
// [Route("[controller]")]
public class LoginController(
    IConfiguration configuration,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ServerContext context)
    : CustomController(configuration, userManager, signInManager, context)
{
    private readonly AppSettings _settings = new(configuration);

    /// <summary>
    /// Validate the user is logged in, and return the current user
    /// information
    /// </summary>
    /// <returns>An action result containing either the user, or an error</returns>
    [HttpGet("")]
    public async Task<IActionResult> Validate()
    {
        if (User.Identity == null)
            return Ok(Result.Error("No User Logged In"));

        var user = await Entities.User.ByName(User.Identity?.Name, UserManager);
        if (user == null) return Ok(Result.Error("Unable to find User"));
        if (user.IsSessionExpired())
            await SignInManager.SignOutAsync();

        var currentUser = User;
        return (SignInManager.IsSignedIn(currentUser))
            ? Ok(Result.Ok(""))
            : Ok(Result.Error(""));
    }

    /// <summary>
    /// Attempt to login the user, returning an Ok result if successful,
    /// or an error result if not.  It is possible, probable even, that
    /// this phase of the login will return an MFA Required result, in which
    /// case the result will be an Ok() result, with the Data Entity being
    /// an MFARequired parameter.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost("")]
    public async Task<ActionResult> Login([FromBody] LoginModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (model.UserName == null) return BadRequest("No Username Provided");
        if (model.Password == null) return BadRequest("No Password Provided");

        var user = await UserManager.FindByNameAsync(model.UserName);
        if (user == null) return Ok(Result.Error("Account Not Found"));

        var result =
            await SignInManager.PasswordSignInAsync(user, model.Password, false,
                false);

        // handle the MFA Required

        // If 2FA is set, this will need to return a result requesting the 
        // 2FA token
        if (result.RequiresTwoFactor)
        {
            // 
            var providers =
                await UserManager.GetValidTwoFactorProvidersAsync(user);
            // we *prefer* an authenticator, and if that is an option, we will default to that
            var selectedProvider = "";
            foreach (var provider in providers)
            {
                if (provider.Contains("Authenticator"))
                {
                    selectedProvider = "Authenticator";
                    break;
                }

                if (provider.Contains("Email")) selectedProvider = "Email";
            }

            if (selectedProvider == "")
                return Ok(Result.Error("No provider found"));

            var token =
                await UserManager.GenerateTwoFactorTokenAsync(user,
                    selectedProvider);

            if (selectedProvider != "Email")
                return Ok(LoginResult.Ok(true));
            if (_settings.Smtp == null)
                throw new Exception("Mail Services Not Configured");

            var link = $"token={HttpUtility.UrlEncode(token)}";

            if (Assembly.GetEntryAssembly()?.GetName().Name == null)
                return Ok(
                    Result.Error("Assembly Not Found"));
            if (_settings.Notification == null)
                return Ok(
                    Result.Error("Settings Not Found"));

            // get the assembly name from the entry assembly
            var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ??
                               "Druware.Server.API";
            var helper = new MailHelper(_settings.Smtp, assemblyName);
            if (user.Email != null)
                helper.Send(
                    user.Email,
                    _settings.Notification.From,
                    _settings.Notification.From,
                    "Two Factor Authentication Code",
                    link
                );

            return Ok(LoginResult.Ok(true));
        }

        user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
        await ServerContext.SaveChangesAsync();

        return Ok(!result.Succeeded
            ? Result.Error("Account Not Found")
            : LoginResult.Ok(user));
    }

    /// <summary>
    /// Log the user out of the system.  This will invalidate the session,
    /// </summary>
    /// <returns></returns>
    [HttpDelete("")]
    public async Task<IActionResult> Logout()
    {
        var user = await Entities.User.ByName(User.Identity?.Name, UserManager);
        // theoretically, this should never get here, but we perform the
        // check just to be safe.
        if (user == null) return Ok(Result.Error("Unable to find User"));
        user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
        await ServerContext.SaveChangesAsync();
        await SignInManager.SignOutAsync();
        return Ok(Result.Ok("Logged Out"));
    }


    [HttpPost("mfa")]
    public async Task<ActionResult> LoginMfa(
        [FromBody] MfaRequired model)
    {
        if (!ModelState.IsValid) return Ok(Result.Error("Invalid Post Format"));

        var user = await SignInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return Ok(Result.Error("Invalid Code"));


        var providers = await UserManager.GetValidTwoFactorProvidersAsync(user);
        // we *prefer* an authenticator, and if that is an option we will default to that
        var selectedProvider = "";
        foreach (var provider in providers)
        {
            if (provider.Contains("Authenticator"))
            {
                selectedProvider = "Authenticator";
                break;
            }

            if (provider.Contains("Email")) selectedProvider = "Email";
        }

        if (selectedProvider == "")
            return Ok(Result.Error("No provider found"));

        if (model.Token != null)
        {
            var result = await SignInManager.TwoFactorSignInAsync(
                selectedProvider, model.Token, false, rememberClient: false);
            if (result.Succeeded)
            {
                user.SessionExpires = DateTime.UtcNow.AddMinutes(30);
                await ServerContext.SaveChangesAsync();

                // All done
                return Ok(Result.Ok(user));
            }

            if (result.IsLockedOut)
            {
                //Same logic as in the Login action
                ModelState.AddModelError("", "The account is locked out");
                return Ok(Result.Error(ModelState));
            }

            ModelState.AddModelError("", "Invalid Login Attempt");
            return Ok(Result.Error(ModelState));
        }

        return Ok(Result.Error("Unable to complete MFA Login"));
    }

    [HttpGet("reset")]
    public async Task<ActionResult<Result>> SendResetRequest(
        [FromQuery] string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return Ok(
                Result.Error("Unable to reset this password at this time..."));
        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        // build out the email message to the registered email with the
        // confirmation link that provides the path to confirm the email.

        if (_settings.Smtp == null)
            throw new Exception("Mail Services Not  Configured");

        var link = string.Format("{0}?email={2}&token={1}",
            _settings.ConfirmationUrl, HttpUtility.UrlEncode(token),
            user.Email);

        if (Assembly.GetEntryAssembly()?.GetName()?.Name == null)
            return Ok(Result.Error("Assembly Not Found"));
        if (_settings.Notification == null)
            return Ok(Result.Error("Settings Not Found"));


        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name ??
                           "Druware.Server.API";
        var helper = new MailHelper(_settings.Smtp, assemblyName);
        if (user.Email != null)
            helper.Send(
                user.Email,
                _settings.Notification.From,
                _settings.Notification.From,
                "Password reset email link",
                link!
            );

        Console.WriteLine($"Token: {token}");
        return Ok(Result.Ok("Your password reset request has been started. - " +
                            token));
    }

    [HttpPost("reset")]
    public async Task<ActionResult<Result>> ConfirmPasswordReset(
        [FromBody] PasswordResetModel model)
    {
        if (!ModelState.IsValid)
            return Ok(Result.Error("Invalid Model Received"));

        // Confirm the Email
        var user = await userManager.FindByEmailAsync(model.Email);

        if (user == null)
            return Ok(Result.Error("Cannot Find This Account"));

        var resetPassResult =
            await userManager.ResetPasswordAsync(user, model.Token,
                model.Password);
        if (resetPassResult.Succeeded)
        {
            return Ok(Result.Ok(
                "Reset Successful, you should be able to login now."));
        }

        var errorResult =
            Result.Error("Cannot Reset the password at this time.");
        foreach (var error in resetPassResult.Errors)
            errorResult.Info?.Add(error.Description);
        return Ok(errorResult);
    }

}