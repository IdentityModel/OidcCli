using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OidcCli;

var serviceProvider = BuildServiceProvider();

var authenticateCommand = serviceProvider.GetRequiredService<AuthenticateCommand>();

return await authenticateCommand.InvokeAsync(args);

static IServiceProvider BuildServiceProvider()
{
    var services = new ServiceCollection();

    services.AddLogging(opts =>
    {
        opts.AddConsole()
            .AddDebug();
    });

    services.AddTransient<AuthenticateCommand>();

    return services.BuildServiceProvider();
}