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
using Microsoft.Win32;
using Svg;
using static System.Drawing.Icon;

namespace HeadsetBatteryMonitor
{
    public class Context : ApplicationContext
    {
        private const string MutexName = "HeadsetBatteryMonitor";
        private bool _firstApplicationInstance;
        private Mutex _mutexApplication;

        public NotifyIcon TrayIcon;

        private readonly BatteryService _batteryService;
        private readonly Device _device;
        private readonly ResourceManager _strings;

        public Context(BatteryService batteryService, IConfiguration configuration)
        {
            // Allow for multiple runs but only try and get the mutex once
            if (_mutexApplication == null)
                _mutexApplication = new Mutex(true, MutexName, out _firstApplicationInstance);

            if (!_firstApplicationInstance) ExitCommand(this, EventArgs.Empty);

            var appIcon = ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            TrayIcon = new NotifyIcon() {Icon = appIcon, Visible = true, ContextMenuStrip = new ContextMenuStrip()};

            _strings = Messages.ResourceManager;

            TrayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("RunAtStart"), null, RegisterInStartupCommand) {CheckOnClick = true});
            TrayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("About"), null, AboutCommand));
            TrayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            TrayIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(_strings.GetString("Exit"), null, ExitCommand));

            _device = new Device();
            configuration.Bind("Device", _device);

            _batteryService = batteryService;
            Task.Run(() => { batteryService.StartAsync(_device); });
            batteryService.ValueChanged += BatteryServiceOnValueChanged;

            var newVersion = new Task(CheckForUpdateAsync);
            newVersion.Start();
        }

        private async void CheckForUpdateAsync()
        {
#if DEBUG
            await Task.Yield();
#else
            await GitHubInfo.CheckForUpdateAsync();
#endif
        }

        private void AboutCommand(object? sender, EventArgs e)
        {
            var form = new FormAbout() {StartPosition = FormStartPosition.CenterScreen};
            form.ShowDialog();
        }

        private void BatteryServiceOnValueChanged(object? sender, EventArgs e)
        {
            var color = "#fff";
            var value = _batteryService.Value;
            if (value >= _device.Success || value == -2) color = "#198754";
            else if (value >= _device.Warning) color = "#FFC107";
            else if (value <= _device.Danger || value == -1) color = "#DC3545";

            uint dpiX, dpiY;
            int w = 16, h = 16;
            Screen.PrimaryScreen.GetDpi(DpiType.Angular, out dpiX, out dpiY);
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
            TrayIcon.Icon = FromHandle(bitmap.GetHicon());

            var text = $"{_device.Name} {value}%";
            if (value == -1) text = $"{_device.Name} ({_strings.GetString("Offline")})";
            else if (value == -2) text = $"{_device.Name} ({_strings.GetString("Charging")})";

            TrayIcon.Text = text;
        }

        private void ExitCommand(object? sender, EventArgs e)
        {
            TrayIcon.Visible = false;
            _mutexApplication.Dispose();
            Application.Exit();
        }

        private void RegisterInStartupCommand(object? sender, EventArgs e)
        {
            RunInStartup = ((sender as ToolStripMenuItem)!).Checked;
        }

        private RegistryKey? startUpRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public bool RunInStartup
        {
            get => startUpRegistryKey?.GetValue(Assembly.GetExecutingAssembly().GetName().Name) != null;
            set
            {
                var name = Assembly.GetExecutingAssembly().GetName().Name;
                if (value)
                {
                    var mainModuleFileName = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (mainModuleFileName != null) startUpRegistryKey?.SetValue(name, mainModuleFileName);
                }
                else
                {
                    if (name != null) startUpRegistryKey?.DeleteValue(name);
                }
            }
        }
    }
}
