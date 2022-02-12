using System;
using System.IO;
using HeadsetBatteryMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace HeadsetBatteryMonitor
{
    public static class Program
    {

        public static System.Threading.SynchronizationContext SynchronizationContext { get; private set; }

        public static IServiceProvider ServiceProvider { get; set; }

        public static IConfiguration Configuration { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;

            var args = Environment.GetCommandLineArgs();
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", true, true)
#endif
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            Configuration = config;

            ConfigureServices();

            ILogger logger = null;
            var factory = ServiceProvider.GetService<ILoggerFactory>();
            if (factory != null) logger = factory.CreateLogger(typeof(Program));

            try
            {
                var context = ServiceProvider.GetService<Application>();
                logger?.LogInformation("Application Starting");

                System.Windows.Forms.Application.EnableVisualStyles();
                //System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

                if (context == null) return;

                Program.SynchronizationContext = System.Threading.SynchronizationContext.Current;
                System.Windows.Forms.Application.Run(context);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "The Application failed to start");
                throw;
            }
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .CreateLogger());
            });

            services.AddSingleton(Configuration);

            // Application
            services.AddSingleton<BatteryService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<Application>();

            ServiceProvider = services.BuildServiceProvider();
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;

            ILogger logger = null;
            if (ServiceProvider != null)
            {
                var factory = ServiceProvider.GetService<ILoggerFactory>();
                if (factory != null) logger = factory.CreateLogger(typeof(Program));

                if (logger != null)
                {
                    logger.LogError(ex, ex.Message);
                    return;
                }
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
        }
    }
}
