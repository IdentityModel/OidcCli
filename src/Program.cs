using IdentityModel.OidcClient;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using TokenTool;

namespace ConsoleClientWithBrowser
{
    public class Program
    {
        /// <summary>
        /// Command-line client for OpenID Connect
        /// </summary>
        /// <param name="a">The authority (required)</param>
        /// <param name="c">The client ID (required)</param>
        /// <param name="s">The scope (defaults to 'openid')</param>
        /// <param name="p">The callback port (defaults to a random port)</param>
        ///<param name="d">Enables diagnostics</param>
        static async Task<int> Main(string a, string c, string s = "openid", int p = 0, bool d = false)
        {
            if (string.IsNullOrEmpty(a))
            {
                Console.WriteLine("authority is required. Use -h for help.");
                return 1;
            }

            if (string.IsNullOrEmpty(c))
            {
                Console.WriteLine("client id is required. Use -h for help.");
                return 1;
            }

            SystemBrowser browser;
            if (p == 0)
            {
                browser = new SystemBrowser();
            }
            else
            {
                browser = new SystemBrowser(p);
            }

            var options = new OidcClientOptions
            {
                Authority = a,
                ClientId = c,
                RedirectUri = $"http://127.0.0.1:{browser.Port}",
                Scope = s,
                FilterClaims = false,
                Browser = browser,
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                ResponseMode = OidcClientOptions.AuthorizeResponseMode.Redirect
            };

            if (d)
            {
                var serilog = new LoggerConfiguration()
                  .MinimumLevel.Verbose()
                  .Enrich.FromLogContext()
                  .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                  .CreateLogger();

                options.LoggerFactory.AddSerilog(serilog);
            }

            var oidcClient = new OidcClient(options);
            var result = await oidcClient.LoginAsync(new LoginRequest());

            return ShowResult(result);
        }

        static int ShowResult(LoginResult result)
        {
            var settings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            if (result.IsError)
            {
                var outputError = new Output
                {
                    error = result.Error
                };

                Console.WriteLine(JsonConvert.SerializeObject(outputError, settings));

                return 1;
            }

            var output = new Output
            {
                id_token = result.IdentityToken,
                access_token = result.AccessToken,
                refresh_token = result.RefreshToken,
                expires_at = result.AccessTokenExpiration,
                claims = result.User.Claims.Select(c => new Output.claim { type = c.Type, value = c.Value })
            };

            Console.WriteLine(JsonConvert.SerializeObject(output, settings));

            return 0;
        }
    }
}