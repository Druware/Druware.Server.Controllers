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
using Microsoft.AspNetCore.Authorization;
using System.Data;

// TODO: Split this into the reperate controllers
// TODO: Isolate the controllers into their own library and package

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        private readonly ApplicationSettings _settings;

        public UserController(
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

        // this should handle all the profile information as well as password
        // resets.

        // get - *IF* the current user has User MAnagement rights, they will
        //     get a list of the users ( of the group(s) that they have rights
        //     to.  Otherwise, they will get a list of 1 user, the user that
        //     the currently authorized user is associated with.
        [Authorize]
        [HttpGet("")]
        public IActionResult GetProfile()
        {
            System.Security.Claims.ClaimsPrincipal currentUser = this.User;
            return (_signInManager.IsSignedIn(currentUser)) ?
                Ok(Result.Ok("")) : Ok(Result.Error(""));
        }

        // get(id) - if the ID matches the curent user, return that.  If not,
        //     then only proceed if the current user ALSO has administration
        //     rights to the group that the requested member belongs to.

        // post - IF the current user has usermanangement create rights and
        //     sufficient rights to the group of the posted user, then proceed,
        //     otherwiser throw a BadRequest(), not an error.

        // get / password - handle a password reset request
        // put - update the user profile

    }
}

