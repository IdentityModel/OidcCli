using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentityModel.Client;
using IdentityModel.OidcClient;
using Microsoft.Extensions.Logging;

namespace OidcCli;

public class AuthenticateCommand : RootCommand
{
    private readonly ILogger<AuthenticateCommand> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public AuthenticateCommand(ILogger<AuthenticateCommand> logger, ILoggerFactory loggerFactory)
        : base("Authenticate using OpenID Connect")
    {
        _logger = logger;
        _loggerFactory = loggerFactory;

        var authorityOption = new Option<string>(
            aliases: new []{ "--authority", "-a" },
            description: "The authority (required)");

        AddOption(authorityOption);

        var clientIdOption = new Option<string>(
            name: "--clientid",
            description: "The client ID (required)");

        AddOption(clientIdOption);

        var scopeOption = new Option<string>(
            name: "--scope",
            description: "The scope (defaults to 'openid')",
            getDefaultValue: () => "openid");

        AddOption(scopeOption);

        var portOption = new Option<int>(
            name: "--port",
            description: "The callback port (defaults to a random port)",
            getDefaultValue: () => 8082); // todo: default to random value

        AddOption(portOption);

        var audienceOption = new Option<string?>(
            name: "--audience",
            description: "The audience");

        AddOption(audienceOption);

        this.SetHandler(async (authority, clientId, scope, port, audience) =>
        {
            using var cancellationTokenSource = new CancellationTokenSource();

            await AuthenticateAsync(authority, clientId, scope, port, audience,
                cancellationTokenSource.Token).ConfigureAwait(false);

        }, authorityOption, clientIdOption, scopeOption, portOption, audienceOption);
    }

    private async Task<bool> AuthenticateAsync(string authority, string clientId, string scope, int port,
        string? audience = null, CancellationToken cancellationToken = default)
    {
        var options = new OidcClientOptions
        {
            LoggerFactory = _loggerFactory,
            Policy = new Policy { RequireAccessTokenHash = false },
            Authority = authority,
            ClientId = clientId,
            LoadProfile = true,
            RedirectUri = $"http://127.0.0.1:{port}",
            PostLogoutRedirectUri = $"http://127.0.0.1:{port}",
            Scope = scope,
            Browser = new SystemBrowser(port)
        };

        var oidcClient = new OidcClient(options);

        var loginRequest = new LoginRequest();

        if (!string.IsNullOrEmpty(audience))
        {
            loginRequest.FrontChannelExtraParameters = new Parameters(new Dictionary<string, string>
            {
                { "audience", audience }
            });
        }

        var result = await oidcClient.LoginAsync(loginRequest, cancellationToken);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        if (result.IsError)
            return false;

        var output = new Output
        {
            id_token = result.IdentityToken,
            access_token = result.AccessToken,
            refresh_token = result.RefreshToken,
            expires_at = result.AccessTokenExpiration,
            claims = result.User.Claims.Select(c => new Output.claim { type = c.Type, value = c.Value })
        };

        _logger.LogInformation(JsonSerializer.Serialize(output, jsonOptions));

        return true;
    }
}
