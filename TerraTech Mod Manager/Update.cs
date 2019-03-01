using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TerraTechModManager
{
    public partial class Update : Form
    {
        public int Return = 0;

        public Update(Downloader.GetUpdateInfo.GithubReleaseItem Release)
        {
            InitializeComponent();
            richTextBoxBody.Rtf = MarkdownToRTF.ToRTFString(Release.body);
            labelVersionNumber.Text = Release.tag_name;
            labelReleaseName.Text = Release.name;
            labelVersionCurrent.Text = "Current Version:\n" + Start.Version_Number;
        }

        private void buttonIgnore_Click(object sender, EventArgs e)
        {
            Return = 0;
            this.Close();
        }

        private void buttonDownloadUpdate_Click(object sender, EventArgs e)
        {
            Return = 1;
            this.Close();
        }
    }
}
