using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using HeadsetBatteryMonitor.Forms;
using HeadsetBatteryMonitor.Models;
using HeadsetBatteryMonitor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Svg;
using static System.Drawing.Icon;

namespace HeadsetBatteryMonitor
{
    public class Application : ApplicationContext
    {
        private const string MutexName = "HeadsetBatteryMonitor";
        private readonly Mutex _mutexApplication;

        public NotifyIcon _trayIcon;

        private readonly BatteryService _batteryService;

        private readonly Device _device;
        private readonly ResourceManager _strings;
        private readonly ILogger _logger;

        public static Bitmap GetColoredIcon(string color, int w = 192, int h = 192)
        {
            Screen.PrimaryScreen.GetDpi(DpiType.Angular, out var dpiX, out var dpiY);
            if (dpiY != 96)
            {
                w = (int)(16.0 * (1 + dpiX / 96.0));
                h = (int)(16.0 * (1 + dpiY / 96.0));
            }

            var svgContent = Properties.Resources.ICON;
            var icon = new MemoryStream(svgContent);
            var doc = new XmlDocument();
            doc.Load(icon);
            var svgDocument = SvgDocument.Open(doc);
            svgDocument.Color = new SvgColourServer(ColorTranslator.FromHtml(color));
            var bitmap = svgDocument.Draw(w, h);

            return bitmap;
        }

        public Application(BatteryService batteryService, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            // Allow for multiple runs but only try and get the mutex once
            _mutexApplication = new Mutex(true, MutexName, out var firstApplicationInstance);

            if (!firstApplicationInstance) ExitCommand(this, EventArgs.Empty);

            _logger = loggerFactory.CreateLogger(GetType());

            var appIcon = ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            _trayIcon = new NotifyIcon() { Icon = appIcon, Visible = true, ContextMenuStrip = new ContextMenuStrip() };

            _strings = Messages.ResourceManager;

            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("RunAtStart"), null, RegisterInStartupCommand) { CheckOnClick = true, Checked = RunInStartup });
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("About"), null, AboutCommand));
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _trayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("Exit"), null, ExitCommand));

            _device = new Device();
            configuration.Bind("Device", _device);

            _batteryService = batteryService;
            batteryService.ValueChanged += BatteryServiceOnValueChanged;
            Task.Run(() => { batteryService.StartAsync(_device); });

            var newVersion = new Task(CheckForUpdateAsync);
            newVersion.Start();

        }

        private static async void CheckForUpdateAsync()
        {
#if DEBUG
            await Task.Yield();
#else
            await GitHubInfo.CheckForUpdateAsync();
#endif
        }

        private static void AboutCommand(object sender, EventArgs e)
        {
            var form = new FormAbout() { StartPosition = FormStartPosition.CenterScreen };
            form.ShowDialog();
        }

        private void BatteryServiceOnValueChanged(object sender, EventArgs e)
        {
            var value = _batteryService.Value;
            var currentLevel = _device.Levels.High;

            switch (value)
            {
                case >= 0 when value <= _device.Levels.Critical.Value:
                    currentLevel = _device.Levels.Critical;
                    break;
                case >= 0 when value <= _device.Levels.Low.Value:
                    currentLevel = _device.Levels.Low;
                    break;
                case >= 0 when value <= _device.Levels.Normal.Value:
                    currentLevel = _device.Levels.Normal;
                    break;
                case >= 0:
                case -2:
                    currentLevel = _device.Levels.High;
                    break;
            }

            var color = currentLevel.Color;
            var notification = currentLevel.Notification?.Enabled;
            var timeout = currentLevel.Notification?.Timeout;
            var sound = currentLevel.Notification?.Sound;

            var bitmap = GetColoredIcon(color, 16, 16);
            _trayIcon.Icon = FromHandle(bitmap.GetHicon());

            var text = $"{_device.Name} {value}%";
            var content = $"{_device.Name} battery level {value}%";
            switch (value)
            {
                case -1:
                    text = $"{_device.Name} ({_strings.GetString("Offline")})";
                    content = text;
                    break;
                case -2:
                    text = $"{_device.Name} ({_strings.GetString("Charging")})";
                    content = text;
                    break;
            }


            _trayIcon.Text = text;
            _logger.LogInformation(content);
        }

        private void ExitCommand(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _mutexApplication.Dispose();
            System.Windows.Forms.Application.Exit();
        }

        private void RegisterInStartupCommand(object sender, EventArgs e)
        {
            RunInStartup = ((sender as ToolStripMenuItem)!).Checked;
        }

        private readonly RegistryKey _startUpRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public bool RunInStartup
        {
            get => _startUpRegistryKey?.GetValue(Assembly.GetExecutingAssembly().GetName().Name) != null;
            set
            {
                var name = Assembly.GetExecutingAssembly().GetName().Name;
                if (value)
                {
                    var mainModuleFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (mainModuleFileName != null) _startUpRegistryKey?.SetValue(name, mainModuleFileName);
                }
                else
                {
                    if (name != null) _startUpRegistryKey?.DeleteValue(name);
                }
            }
        }
    }
}
