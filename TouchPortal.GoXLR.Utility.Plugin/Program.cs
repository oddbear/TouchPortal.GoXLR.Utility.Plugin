using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TouchPortal.GoXLR.Utility.Plugin.Client;
using TouchPortal.GoXLR.Utility.Plugin.Modules;
using TouchPortal.GoXLR.Utility.Plugin.Plugin;
using TouchPortalSDK.Configuration;

namespace TouchPortal.GoXLR.Utility.Plugin;

internal class Program
{
    static void Main(string[] args)
    {
        //Used in debug to copy the entry.tp file if changed, and restart Touch Portal:
        EntryCopy.RefreshEntryFile();

        var configurationRoot = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
            .AddJsonFile("appsettings.json")
            .Build();

        var services = new ServiceCollection();

        services.AddTouchPortalSdk(configurationRoot);
        services.AddLogging(configure =>
        {
            configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ");
            configure.AddConfiguration(configurationRoot.GetSection("Logging"));
        });
        
        AddServices(services);

        var serviceProvider = services.BuildServiceProvider(true);

        serviceProvider
            .GetRequiredService<GoXlrUtilityPlugin>()
            .Run();

        serviceProvider
            .GetRequiredService<GoXlrUtilityClient>()
            .Start();
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddSingleton<GoXlrUtilityClient>();
        services.AddSingleton<GoXlrUtilityPlugin>();
        services.AddSingleton<Faders>();
        services.AddSingleton<Channels>();
        services.AddSingleton<Effects>();
        services.AddSingleton<Routing>();
        services.AddSingleton<Microphone>();
    }
}