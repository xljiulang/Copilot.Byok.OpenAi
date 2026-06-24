using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Copilot.Byok.OpenAi.Handlers
{
    sealed class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IOptionsMonitor<OpenAiOptions> _openAiOptions;

        public ApiKeyAuthHandler(
            IOptionsMonitor<OpenAiOptions> openAiOptions,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder)
        {
            this._openAiOptions = openAiOptions;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (_openAiOptions.CurrentValue.ApiKeys.Count == 0)
            {
                return Task.FromResult(Success(null, Scheme.Name));
            }

            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out var auth))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing API Key"));
            }

            var apiKey = auth[0]?.Split(' ').LastOrDefault();
            return apiKey == null || _openAiOptions.CurrentValue.ApiKeys.Contains(apiKey) == false
                ? Task.FromResult(AuthenticateResult.Fail("Invalid API Key"))
                : Task.FromResult(Success(apiKey, Scheme.Name));

            static AuthenticateResult Success(string? apiKey, string scheme)
            {
                var claims = string.IsNullOrEmpty(apiKey)
                    ? Array.Empty<Claim>()
                    : [new Claim(ClaimTypes.NameIdentifier, apiKey)];

                var identity = new ClaimsIdentity(claims, scheme);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, scheme);
                return AuthenticateResult.Success(ticket);
            }
        }
    }
}
