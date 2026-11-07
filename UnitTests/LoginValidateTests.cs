using System.Security.Claims;
using Druware.Server;
using Druware.Server.Controllers;
using Druware.Server.Email;
using Druware.Server.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RESTfulFoundation.Server;

namespace UnitTests;

/// <summary>
/// Exercises LoginController.Validate (GET /login/) against a real Identity
/// stack backed by an in-memory Sqlite ServerContext.
/// </summary>
[TestFixture]
public class LoginValidateTests
{
    private const string UserName = "session.user@example.com";

    private SqliteConnection _connection = null!;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IConfiguration _configuration = null!;

    private UserManager<User> UserManager =>
        _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    [SetUp]
    public void SetUp()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["API:Database:ConnectionType"] = "SQLITE",
                ["API:Database:ConnectionString"] = "DataSource=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_configuration);
        services.AddDbContext<ServerContext>(o => o.UseSqlite(_connection));
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();
        services.AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<ServerContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _scope.ServiceProvider.GetRequiredService<ServerContext>()
            .Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    private async Task CreateUser(DateTime? sessionExpires)
    {
        var user = new User
        {
            UserName = UserName,
            Email = UserName,
            SessionExpires = sessionExpires
        };
        var result = await UserManager.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.That(result.Succeeded, Is.True,
            string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    // The request principal carries the Identity application scheme, as it
    // does when the cookie authenticated the request.
    private LoginController NewController()
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, UserName)],
                IdentityConstants.ApplicationScheme))
        };

        var signInManager =
            _scope.ServiceProvider.GetRequiredService<SignInManager<User>>();
        signInManager.Context = httpContext;

        return new LoginController(_configuration, UserManager, signInManager,
            _scope.ServiceProvider.GetRequiredService<ServerContext>(),
            new UnconfiguredEmailSender())
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static Result ResultOf(IActionResult response)
    {
        Assert.That(response, Is.TypeOf<OkObjectResult>());
        return (Result)((OkObjectResult)response).Value!;
    }

    private static string SetCookie(ControllerBase controller) =>
        controller.HttpContext.Response.Headers["Set-Cookie"].ToString();

    [Test]
    public async Task Validate_ExpiredSession_SignsOutAndReturnsExpiredError()
    {
        await CreateUser(DateTime.UtcNow.AddMinutes(-1));
        var controller = NewController();

        var result = ResultOf(await controller.Validate());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Info,
                Does.Contain("Session has expired, user must log in."));
            Assert.That(SetCookie(controller),
                Does.Contain(".AspNetCore.Identity.Application="));
        });
    }

    [Test]
    public async Task Validate_ActiveSession_ReturnsOk()
    {
        await CreateUser(DateTime.UtcNow.AddMinutes(5));
        var controller = NewController();

        var result = ResultOf(await controller.Validate());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(SetCookie(controller), Is.Empty);
        });
    }

    [Test]
    public async Task Validate_UnknownUser_ReturnsUnableToFindUser()
    {
        var result = ResultOf(await NewController().Validate());

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Info, Does.Contain("Unable to find User"));
        });
    }

    private sealed class UnconfiguredEmailSender : IEmailSender
    {
        public bool IsConfigured => false;

        public Task<EmailSendResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Validate sends no email.");
    }
}
