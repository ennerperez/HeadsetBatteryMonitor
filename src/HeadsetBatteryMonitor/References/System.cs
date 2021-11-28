// ----------------------------------------
// System References
// Version 1.2.0
// Updated 2021-11-20
// ----------------------------------------

using System;
using System.Collections.Generic;
using GitHub;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using HeadsetBatteryMonitor;

namespace System
{
    namespace Diagnostics
    {
        public static class DefaultBrowser
        {
            public static void Open(string? url)
            {
                try
                {
                    if (url != null)
                    {
                        Process.Start(url);
                    }
                }
                catch
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url?.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        if (url != null)
                        {
                            Process.Start("xdg-open", url);
                        }
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        if (url != null)
                        {
                            Process.Start("open", url);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }

    namespace Windows
    {
        namespace Forms
        {
#if DEBUG

            internal static partial class FormHelper
            {
                public static void ExtractResources(Image? image, string name)
                {
                    if (image != null)
                    {
                        var assemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                        var dirPath = $"..\\..\\{assemblyName}\\Resources\\";
                        if (!Directory.Exists(dirPath))
                            Directory.CreateDirectory(dirPath);
#pragma warning disable CA1416
                        image.Save($"..\\..\\{assemblyName}\\Resources\\{name}.png");
#pragma warning restore CA1416
                    }
                }

                public static void ExtractResources(ToolStrip source)
                {
                    foreach (var item in source.Items.OfType<ToolStripButton>().Where(i => i.Image != null))
                        ExtractResources(item.Image, item.Name);
                    foreach (var item in source.Items.OfType<ToolStripDropDownButton>().Where(i => i.Image != null))
                        ExtractResources(item.Image, item.Name);
                }
            }

#endif

            internal static partial class FormHelper
            {
                public static Rectangle GetWorkingArea()
                {
                    int minx, miny, maxx, maxy;
                    minx = miny = int.MaxValue;
                    maxx = maxy = int.MinValue;

                    foreach (var screen in Screen.AllScreens)
                    {
                        var bounds = screen.Bounds;
                        minx = Math.Min(minx, bounds.X);
                        miny = Math.Min(miny, bounds.Y);
                        maxx = Math.Max(maxx, bounds.Right);
                        maxy = Math.Max(maxy, bounds.Bottom);
                    }

                    return new Rectangle(0, 0, (maxx - minx), (maxy - miny));
                }
            }

            public static class ScreenExtensions
            {
                public static void GetDpi(this Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
                {
                    var pnt = new Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
                    var mon = MonitorFromPoint(pnt, 2 /*MONITOR_DEFAULTTONEAREST*/);
                    GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
                }

                //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
                [DllImport("User32.dll")]
                private static extern IntPtr MonitorFromPoint([In] Point pt, [In] uint dwFlags);

                //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
                [DllImport("Shcore.dll")]
                private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);
            }

            public enum DpiType
            {
                Effective = 0,
                Angular = 1,
                Raw = 2,
            }
        }
    }

    namespace Reflection
    {
        [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
        public class GitHubAttribute : Attribute
        {
            public GitHubAttribute()
            {
            }

            public GitHubAttribute(string? owner, string? repo, string? assetName = "")
            {
                Owner = owner;
                Repo = repo;
                AssetName = assetName;
            }

            public string? Owner { get; private set; }
            public string? Repo { get; private set; }
            public string? AssetName { get; private set; }

            public override string ToString()
            {
                return $"https://github.com/{Owner}/{Repo}";
            }
        }

        internal static class ApplicationInfo
        {
            public static Assembly Assembly => Assembly.GetCallingAssembly();

            public static Version? Version => Assembly.GetName().Version;
            public static string? Title => Assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            public static string? Product => Assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            public static string? Description => Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
            public static string? Copyright => Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            public static string? Company => Assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

            public static string? Guid => Assembly.GetCustomAttribute<GuidAttribute>()?.Value;

            internal static Dictionary<string, string?> GetCommandLine()
            {
                var commandArgs = new Dictionary<string, string?>();

                var assembly = string.Format(@"""{0}"" ", Assembly.GetExecutingAssembly().Location);
                var collection = Environment.CommandLine.Replace(assembly, "").Split(' ').Select(a => a.ToLower()).ToList();

                if (collection.Any())
                    foreach (var item in collection.Where(m => m.StartsWith("/")))
                        if (collection.Count - 1 > collection.IndexOf(item))
                            commandArgs.Add(item.ToLower(), collection[collection.IndexOf(item) + 1].Replace(@"""", @""));
                        else
                            commandArgs.Add(item.ToLower(), null);

                return commandArgs;
            }
        }

        internal static class GitHubInfo
        {
            public static string? Repo => ApplicationInfo.Assembly.GetCustomAttribute<GitHubAttribute>()?.ToString();
            public static string? Owner => ApplicationInfo.Assembly.GetCustomAttribute<GitHubAttribute>()?.Owner;
            public static string? Name => ApplicationInfo.Assembly.GetCustomAttribute<GitHubAttribute>()?.Repo;
            public static string? AssetName => ApplicationInfo.Assembly.GetCustomAttribute<GitHubAttribute>()?.AssetName;
            public static string? Release => $"{Repo}/releases/latest";

            public static Release? Latest { get; set; }

            public static async Task GetLatestReleaseAsync()
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        var url = new Uri($"https://api.github.com/repos/{Owner}/{Name}/releases/latest");
                        client.DefaultRequestHeaders.Add("User-Agent", ApplicationInfo.Title);
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                            Latest = JsonConvert.DeserializeObject<Release>(await response.Content.ReadAsStringAsync());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            public static async Task CheckForUpdateAsync()
            {
                try
                {
                    await GetLatestReleaseAsync();
                    if (ApplicationInfo.Version < Latest.GetVersion())
                    {
                        var updateMessage = Messages.NewVersion;
                        updateMessage = updateMessage.Replace("{VERSION}", Latest.GetVersion()?.ToString());
                        updateMessage = updateMessage.Replace("{CREATEDAT}", Latest?.CreatedAt.UtcDateTime.ToShortDateString());
                        if (MessageBox.Show(updateMessage, ApplicationInfo.Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            var assetName = AssetName;
                            if (string.IsNullOrEmpty(assetName)) assetName = $"{ApplicationInfo.Product}.zip";
                            var assetUrl = Latest?.Assets.FirstOrDefault(m => m.Name == assetName);
                            var url = Latest?.AssetsUrl;
                            if (assetUrl != null) url = assetUrl.BrowserDownloadUrl;
                            if (string.IsNullOrEmpty(url)) url = Repo;
                            if (!string.IsNullOrEmpty(url)) DefaultBrowser.Open(url);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            public static Version? GetVersion(this Release? release)
            {
                Version.TryParse(release?.TagName?.Replace("v", ""), out var result);
                return result;
            }
        }
    }
}

namespace GitHub
{
    internal class Release
    {
        public Release()
        {
            Assets = new HashSet<Asset>();
        }

        [JsonProperty("tarball_url")]
        public string? TarballUrl { get; set; }

        //[JsonProperty("author")]
        //public Author Author { get; set; }

        [JsonProperty("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("draft")]
        public bool Draft { get; set; }

        [JsonProperty("body")]
        public string? Body { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("target_commitish")]
        public string? TargetCommitish { get; set; }

        [JsonProperty("tag_name")]
        public string? TagName { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("upload_url")]
        public string? UploadUrl { get; set; }

        [JsonProperty("assets_url")]
        public string? AssetsUrl { get; set; }

        [JsonProperty("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("zipball_url")]
        public string? ZipballUrl { get; set; }

        [JsonProperty("assets")]
        public ICollection<Asset> Assets { get; set; }
    }

    internal class Asset
    {
        [JsonProperty("url")]
        public string? Url { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("label")]
        public string? Label { get; set; }

        [JsonProperty("state")]
        public string? State { get; set; }

        [JsonProperty("content_type")]
        public string? ContentType { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }

        [JsonProperty("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        //[JsonProperty("uploader")]
        //public Author Uploader { get; set; }
    }
}
