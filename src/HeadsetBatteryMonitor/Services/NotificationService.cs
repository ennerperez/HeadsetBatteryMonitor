using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;
using static System.Drawing.Icon;

namespace HeadsetBatteryMonitor.Services
{
    public class NotificationService
    {
        private static bool s_isActive;

        public void ShowNotification<TForm>(string title, string message, int timeout = 50000, string color = "", string sound = "") where TForm : Form
        {
            if (s_isActive) return;
            var bitmap = Application.GetColoredIcon(color, 256, 256);
            var notification = (Form)Activator.CreateInstance<TForm>();
            var timer = new Timer() { Enabled = timeout != -1, Interval = timeout > 0 ? timeout : 5000 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                notification.Close();
            };

            notification.Tag = new
            {
                Image = bitmap,
                Icon = FromHandle(bitmap.GetHicon()),
                Text = title,
                Content = message,
                Color = ControlPaint.Dark(ColorTranslator.FromHtml(color), 0.5f),
                Sound = sound
            };

            notification.Load += (_, _) =>
            {
                notification.StartPosition = FormStartPosition.Manual;
                notification.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 10 - notification.Width, Screen.PrimaryScreen.WorkingArea.Height - 10 - notification.Height);
            };
            notification.Shown += (_, _) =>
            {
                if (timeout <= 0) return;
                timer?.Start();
            };
            notification.Closed += (_, _) => s_isActive = false;
            notification.Show();
            switch (sound)
            {
                case "Exclamation":
                    SystemSounds.Exclamation.Play();
                    break;
                case "Asterisk":
                    SystemSounds.Asterisk.Play();
                    break;
                case "Beep":
                    SystemSounds.Beep.Play();
                    break;
                case "Hand":
                    SystemSounds.Hand.Play();
                    break;
                case "Question":
                    SystemSounds.Question.Play();
                    break;
            }

            s_isActive = true;
        }
    }
}
