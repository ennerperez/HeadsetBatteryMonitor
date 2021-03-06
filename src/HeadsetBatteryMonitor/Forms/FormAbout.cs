using System;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace HeadsetBatteryMonitor.Forms
{
    public partial class FormAbout : Form
    {
        public FormAbout()
        {
            InitializeComponent();

            var filePath = Assembly.GetEntryAssembly()?.Location;
            if (filePath != null)
            {
                Icon = Icon.ExtractAssociatedIcon(filePath);
            }

            labelAbout.Text = string.Format(labelAbout.Text,
                ApplicationInfo.Description,
                ApplicationInfo.Version?.ToString(),
                ApplicationInfo.Copyright);

            richTextBoxChangeLog.Text = System.Text.Encoding.UTF8.GetString(Properties.Resources.CHANGELOG);
            richTextBoxLicense.Text = System.Text.Encoding.UTF8.GetString(Properties.Resources.LICENSE);

            linkLabelWeb.Text = GitHubInfo.Repo;
            buttonUpdate.Visible = (GitHubInfo.Latest != null && (GitHubInfo.Latest.GetVersion() > ApplicationInfo.Version));

            pictureBoxIcon.Image = Icon?.ToBitmap();
        }

        private void ButtonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LinkLabelWeb_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = (sender as LinkLabel)?.Text;
            if (url != null) DefaultBrowser.Open(url);
        }

        private void ButtonUpdate_Click(object sender, EventArgs e)
        {
            var url = GitHubInfo.Release;
            if (!string.IsNullOrWhiteSpace(url)) DefaultBrowser.Open(url);
        }

        private async void FormAbout_Load(object sender, EventArgs e)
        {
            if (GitHubInfo.Latest == null) await GitHubInfo.GetLatestReleaseAsync();
            buttonUpdate.Visible = (GitHubInfo.Latest != null && (GitHubInfo.Latest.GetVersion() > ApplicationInfo.Version));
        }
    }
}
