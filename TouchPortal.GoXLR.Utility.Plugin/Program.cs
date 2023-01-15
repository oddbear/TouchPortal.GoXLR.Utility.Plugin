using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TouchPortal.GoXLR.Utility.Plugin.Modules;
using TouchPortalSDK.Configuration;

namespace TouchPortal.GoXLR.Utility.Plugin;

internal class Program
{
    static void Main(string[] args)
    {
        //Used in debug to copy the entry.tp file if changed, and restart Touch Portal:
        EntryCopy.RefreshEntryFile();

        var configurationRoot = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddJsonFile("appsettings.json")
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTouchPortalSdk(configurationRoot);
        serviceCollection.AddLogging(configure =>
        {
            configure.AddSimpleConsole(options => options.TimestampFormat = "[yyyy.MM.dd HH:mm:ss] ");
            configure.AddConfiguration(configurationRoot.GetSection("Logging"));
        });

        serviceCollection.AddTouchPortalSdk(configurationRoot);
        serviceCollection.AddSingleton<GoXlrUtilityClient>();
        serviceCollection.AddSingleton<GoXlrUtilityPlugin>();
        serviceCollection.AddSingleton<Faders>();
        serviceCollection.AddSingleton<Channels>();
        serviceCollection.AddSingleton<Effects>();
        serviceCollection.AddSingleton<Routing>();
        
        var serviceProvider = serviceCollection.BuildServiceProvider(true);

        var plugin = serviceProvider.GetRequiredService<GoXlrUtilityPlugin>();
        plugin.Run();

        var client = serviceProvider.GetRequiredService<GoXlrUtilityClient>();
        client.Start();


        Console.WriteLine("Press key to exit.");
        Console.ReadLine();
    }
}