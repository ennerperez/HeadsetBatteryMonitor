using System;
using System.Drawing;
using System.Windows.Forms;

namespace HeadsetBatteryMonitor.Forms
{
    public partial class FormToast : Form
    {
        public FormToast()
        {
            InitializeComponent();
        }

        private void InitializeBinding()
        {
            if (Tag == null) return;

            DataBindings.Add("Text", Tag, "Text", false, DataSourceUpdateMode.OnPropertyChanged);
            DataBindings.Add("Icon", Tag, "Icon", false, DataSourceUpdateMode.OnPropertyChanged);
            DataBindings.Add("BackColor", Tag, "Color", false, DataSourceUpdateMode.OnPropertyChanged);
            pictureBoxImage.DataBindings.Add("Image", Tag, "Image", false, DataSourceUpdateMode.OnPropertyChanged);
            labelContent.DataBindings.Add("Text", Tag, "Content", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        protected override void OnLoad(EventArgs e)
        {
            InitializeBinding();
            base.OnLoad(e);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
