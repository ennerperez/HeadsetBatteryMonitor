using HeadsetBatteryMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HeadsetBatteryMonitor;

public static class Program
{
    #region IOC

    public static ILogger? Logger { get; private set; }
    public static ServiceCollection? Services { get; private set; }
    public static ServiceProvider? Container { get; private set; }

    #endregion

    public static IConfiguration? Configuration { get; private set; }

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        var args = Environment.GetCommandLineArgs();
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true)
#if DEBUG
            .AddJsonFile("appsettings.Development.json", true, true)
#endif
            .AddCommandLine(args)
            .Build();
        
        Services = new ServiceCollection();
        
        Services.AddSingleton(Configuration);
        Services.AddSingleton<Context>();
        Services.AddSingleton<BateryService>();
        
        Services.AddLogging(builder => { builder.SetMinimumLevel(LogLevel.Information); }).AddOptions();
        
        Container = Services.BuildServiceProvider();
        var factory = Container.GetService<ILoggerFactory>();
        if (factory != null) Logger = factory.CreateLogger(typeof(Program));
        
        var context = Container.GetService<Context>();
        
        Application.EnableVisualStyles();
        if (context != null) Application.Run(context);
    }
   
}
