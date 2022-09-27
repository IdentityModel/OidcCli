using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using IdentityModel.OidcClient.Browser;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace OidcCli;

/// <summary>
///     Opens the default system browser for the purposes of performing an OpenID Connect Authorization Code
///     exchange. Listens on <c>http://127.0.0.1</c> with the specified port.
/// </summary>
public class SystemBrowser : IBrowser
{
    public SystemBrowser(int port)
    {
        Port = port;
    }

    public int Port { get; }

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken)
    {
        using var listener = new LoopbackHttpListener(Port);

        OpenBrowser(options.StartUrl);

        try
        {
            var result = await listener.WaitForCallbackAsync();

            return string.IsNullOrWhiteSpace(result)
                ? new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Empty response." }
                : new BrowserResult { Response = result, ResultType = BrowserResultType.Success };
        }
        catch (TaskCanceledException ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = ex.Message };
        }
        catch (Exception ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
        }
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // HACK: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}

public class LoopbackHttpListener : IDisposable
{
    private const int DefaultTimeout = 60 * 5;

    private readonly IWebHost _host;
    private readonly TaskCompletionSource<string> _source = new();

    public LoopbackHttpListener(int port)
    {
        Url = $"http://127.0.0.1:{port}";

        _host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(Url)
            .ConfigureServices(services =>
            {
                services.AddLogging();
                services.AddRouting();
            })
            .Configure(Configure)
            .Build();

        _host.Start();
    }

    public string Url { get; }

    public void Dispose()
    {
        Task.Run(async () =>
        {
            await Task.Delay(500);
            _host.Dispose();
        });
    }

    private void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync("<h1>You can now return to the application.</h1>");

                _source.TrySetResult(context.Request.QueryString.Value!);
            });

            endpoints.MapPost("/", async context =>
            {
                if (!context.Request.ContentType!.Equals("application/x-www-form-urlencoded",
                        StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                }
                else
                {
                    using var sr = new StreamReader(context.Request.Body, Encoding.UTF8);
                    var body = await sr.ReadToEndAsync();

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync("<h1>Invalid request.</h1>");

                    _source.TrySetResult(body);
                }
            });
        });
    }

    public Task<string> WaitForCallbackAsync(int timeoutInSeconds = DefaultTimeout)
    {
        Task.Run(async () =>
        {
            await Task.Delay(timeoutInSeconds * 1000);
            _source.TrySetCanceled();
        });

        return _source.Task;
    }
}
