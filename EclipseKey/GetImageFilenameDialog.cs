using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EclipseKey
{
    public partial class GetImageFilenameDialog : Form
    {
        public GetImageFilenameDialog()
        {
            InitializeComponent();
        }

        private void GetImageFilenameDialog_Load(object sender, EventArgs e)
        {
        }

        private static readonly Dictionary<string, HistoryItem> History = new Dictionary<string, HistoryItem>();

        public void Init(string baseFilename, string defaultFolder, string ext)
        {
            BaseFilename = Path.GetFullPath(baseFilename);
            BaseDir = Path.GetDirectoryName(BaseFilename);

            HistoryItem history;

            if (!History.TryGetValue(baseFilename, out history))
            {
                history = new HistoryItem()
                {
                    Folder = defaultFolder
                };

                History.Add(baseFilename, history);
            }

            txtFolder.DataSource = history.Folders.ToList();
            txtFolder.Text = history.Folder ?? defaultFolder;

            var filename = Guid.NewGuid().ToString("N");

            txtFilename.Text = filename + ext;
            txtFilename.Select(0, filename.Length);
            txtFilename.Focus();

            txtAltText.Text = filename;
            AutoUpdateAltText = true;
        }

        /// <summary>
        /// 当前正在编辑的文件名完整路径
        /// </summary>
        public string BaseFilename;

        /// <summary>
        /// 当前正在编辑的文件名所在文件夹完整路径
        /// </summary>
        public string BaseDir;

        /// <summary>
        /// 在 Filename 文本框中填写的文件名
        /// </summary>
        public string Filename;

        /// <summary>
        /// 在 Folder 文本框中填写的文件夹名
        /// </summary>
        public string Folder;

        /// <summary>
        /// 在 AltText 文本框中填写的文本
        /// </summary>
        public string AltText;

        /// <summary>
        /// Folder 的完整路径
        /// </summary>
        public string FullFolderPath;

        /// <summary>
        /// Filename 的完整路径
        /// </summary>
        public string FullFilePath;

        /// <summary>
        /// FullFilePath 相对 BaseDir 的相对路径
        /// </summary>
        public string RelativeFilePath;

        private bool _autoUpdateAltText;

        private bool AutoUpdateAltText
        {
            get { return _autoUpdateAltText; }
            set
            {
                _autoUpdateAltText = value;

                lblLink.Text = value ? "┐§┘" : "┐×┘";
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Filename = txtFilename.Text;
            Folder = txtFolder.Text;
            AltText = txtAltText.Text;
            FullFolderPath = Path.Combine(BaseDir, Folder);
            FullFilePath = Path.Combine(FullFolderPath, Filename);
            RelativeFilePath = PasteImage.GetRelativePath(BaseDir, FullFilePath);

            if (string.IsNullOrWhiteSpace(Filename))
            {
                MessageBox.Show(this, @"Filename can not be null or empty", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtFilename.Focus();
                return;
            }

            if (!Directory.Exists(FullFolderPath))
            {
                var dialogResult = MessageBox.Show(this, @"destination folder does not exists, do you really want to create it?

" + Folder, @"Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }

                Directory.CreateDirectory(FullFolderPath);
            }
            else if (File.Exists(FullFilePath))
            {
                var dialogResult = MessageBox.Show(this, @"destination file already exists, do you really want to replace it?

" + RelativeFilePath, @"Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.No)
                {
                    return;
                }
            }

            var cache = History[BaseFilename];

            cache.Folder = Folder;
            cache.Folders.Add(Folder);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        class HistoryItem
        {
            public string Folder;
            public readonly HashSet<string> Folders = new HashSet<string>();
        }

        private void txtFilename_TextChanged(object sender, EventArgs e)
        {
            if (AutoUpdateAltText)
            {
                var text = txtFilename.Text;
                var pos = text.LastIndexOf('.');

                txtAltText.Text = pos < 0 ? text : text.Substring(0, pos);
            }
        }

        private void txtAltText_TextChanged(object sender, EventArgs e)
        {
            AutoUpdateAltText = txtFilename.Text.StartsWith(txtAltText.Text);
        }

        private void lblLink_Click(object sender, EventArgs e)
        {
            AutoUpdateAltText = !AutoUpdateAltText;
        }

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            var currentDir = Path.Combine(BaseDir, txtFolder.Text);

            var dlg = new FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(currentDir) ? currentDir : BaseDir,
            };

            var dlgResult = dlg.ShowDialog(this);

            if (dlgResult != DialogResult.OK) return;

            txtFolder.Text = PasteImage.GetRelativePath(new DirectoryInfo(BaseDir), new DirectoryInfo(dlg.SelectedPath));
        }
    }
}
