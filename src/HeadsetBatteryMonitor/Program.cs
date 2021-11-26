using HeadsetBatteryMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

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
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        
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

        Services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddSerilog();
        }).AddOptions();
        
        //Initialize Logger
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .CreateLogger();

        Container = Services.BuildServiceProvider();
        var factory = Container.GetService<ILoggerFactory>();
        if (factory != null) Logger = factory.CreateLogger(typeof(Program));

        var context = Container.GetService<Context>();

        Application.EnableVisualStyles();

        try
        {
            Log.Information("Application Starting");
            if (context != null) Application.Run(context);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "The Application failed to start");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        if (Logger != null)
        {
            Logger.LogError(ex, ex.Message);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}
