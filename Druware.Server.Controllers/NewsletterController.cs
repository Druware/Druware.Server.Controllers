using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RESTfulFoundation.Server;

namespace Druware.Server.Controllers
{
    [Route("api/[controller]")]
    public class NewsletterController : ControllerBase
    {
        private const string MailJetBaseUrl = "https://api.mailjet.com/v3/REST";
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public NewsletterController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(MailJetBaseUrl)
            };

            ConfigureMailJetAuthentication();
        }

        [HttpGet("")]
        public async Task<ActionResult<ListResult>> Newsletters()
        {
            var lists = await GetMailJetLists();

            if (!lists.Success)
            {
                return StatusCode(lists.StatusCode, Result.Error(lists.ErrorMessage));
            }

            var endpoints = new List<string>
            {
                "/ - this list",
                "/lists - list available MailJet newsletters",
                "/{newsletter}/subscribe - subscribe an email address to the named newsletter",
                "/{newsletter}/unsubscribe - unsubscribe an email address from the named newsletter"
            };

            endpoints.AddRange(lists.Value.Select(list => $"/{list.Name}/subscribe"));
            endpoints.AddRange(lists.Value.Select(list => $"/{list.Name}/unsubscribe"));

            return Ok(ListResult.Ok(endpoints));
        }

        [HttpGet("lists")]
        public async Task<ActionResult<ListResult>> Lists()
        {
            var lists = await GetMailJetLists();

            if (!lists.Success)
            {
                return StatusCode(lists.StatusCode, Result.Error(lists.ErrorMessage));
            }

            return Ok(ListResult.Ok(lists.Value.Select(list => list.Name).ToList()));
        }

        [HttpPost("{newsletter}/subscribe")]
        public async Task<ActionResult<Result>> Subscribe(
            string newsletter,
            [FromBody] NewsletterSubscriptionModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(Result.Error("Email is required."));
            }

            return await UpdateSubscription(newsletter, model.Email, "addnoforce");
        }

        [HttpPost("{newsletter}/unsubscribe")]
        public async Task<ActionResult<Result>> Unsubscribe(
            string newsletter,
            [FromBody] NewsletterSubscriptionModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return BadRequest(Result.Error("Email is required."));
            }

            return await UpdateSubscription(newsletter, model.Email, "unsub");
        }

        private void ConfigureMailJetAuthentication()
        {
            var apiKey = _configuration["MailJet:APIKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", token);
            Console.WriteLine($"MailJet API Key configured. {token}");
        }

        private async Task<ActionResult<Result>> UpdateSubscription(
            string newsletter,
            string email,
            string action)
        {
            var listResult = await GetMailJetList(newsletter);

            if (!listResult.Success)
            {
                return StatusCode(listResult.StatusCode, Result.Error(listResult.ErrorMessage));
            }

            var payload = new
            {
                Email = email,
                Action = action
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(
                $"contactslist/{listResult.Value.Id}/managecontact",
                content);

            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();

                return StatusCode(
                    (int)response.StatusCode,
                    Result.Error($"MailJet subscription update failed: {message}"));
            }

            var operation = action == "unsub" ? "unsubscribed from" : "subscribed to";

            return Ok(Result.Ok($"{email} has been {operation} {listResult.Value.Name}."));
        }

        private async Task<MailJetResult<MailJetList>> GetMailJetList(string newsletter)
        {
            var lists = await GetMailJetLists();

            if (!lists.Success)
            {
                return MailJetResult<MailJetList>.Error(lists.StatusCode, lists.ErrorMessage);
            }

            var list = lists.Value.FirstOrDefault(item =>
                string.Equals(item.Name, newsletter, StringComparison.OrdinalIgnoreCase));

            return list == null
                ? MailJetResult<MailJetList>.Error(404, $"Newsletter '{newsletter}' was not found.")
                : MailJetResult<MailJetList>.Ok(list);
        }

        private async Task<MailJetResult<List<MailJetList>>> GetMailJetLists()
        {
            if (string.IsNullOrWhiteSpace(_configuration["MailJet:APIKey"]))
            {
                return MailJetResult<List<MailJetList>>.Error(
                    500,
                    "MailJet API key is not configured.");
            }

            var response = await _httpClient.GetAsync("contactslist");

            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();

                return MailJetResult<List<MailJetList>>.Error(
                    (int)response.StatusCode,
                    $"MailJet list lookup failed: {message}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<MailJetListResponse>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return MailJetResult<List<MailJetList>>.Ok(data?.Data ?? new List<MailJetList>());
        }

        public class NewsletterSubscriptionModel
        {
            public string Email { get; set; } = string.Empty;
        }

        private class MailJetListResponse
        {
            public List<MailJetList> Data { get; set; } = new();
        }

        private class MailJetList
        {
            public long Id { get; set; }

            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("Address")]
            public string? Address { get; set; }
        }

        private class MailJetResult<T>
        {
            public bool Success { get; private init; }

            public int StatusCode { get; private init; }

            public string ErrorMessage { get; private init; } = string.Empty;

            public T Value { get; private init; } = default!;

            public static MailJetResult<T> Ok(T value) =>
                new()
                {
                    Success = true,
                    StatusCode = 200,
                    Value = value
                };

            public static MailJetResult<T> Error(int statusCode, string errorMessage) =>
                new()
                {
                    Success = false,
                    StatusCode = statusCode,
                    ErrorMessage = errorMessage
                };
        }
    }
}