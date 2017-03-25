// ****************************************************************************
// @Copyright: (C) 2010-2013 广东省电信工程有限公司 版权所有
// @File:  PasteImage.cs
// @Create: 2017-03-19
// @Author: sunhaiwen<sunhaiwen99@gmail.com>
// @History:
//  - yyyy-mm-dd by Author[<@email>] : brief
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace EclipseKey
{
    internal sealed class PasteImage
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4136;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = GuidList.guidEclipseKeyCmdSet;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="CopyLinesUp"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PasteImage(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PasteImage Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider { get { return _package; } }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new PasteImage(package);
        }

        class SimpleWindow : IWin32Window
        {
            public SimpleWindow(int handle) : this (new IntPtr(handle))
            {
            }

            public SimpleWindow(IntPtr handle)
            {
                Handle = handle;
            }

            /// <summary>
            /// 获取由实施者表示的窗口句柄。
            /// </summary>
            /// <returns>
            /// 由实施者表示的窗口句柄。
            /// </returns>
            public IntPtr Handle { get; private set; }
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            var dte = (DTE2) ServiceProvider.GetService(typeof (DTE));

            var docFile = dte.ActiveDocument.FullName;
            var selectedText = dte.Selection().Text;
            var imageText = SimpleText.Parse(selectedText) as ImageText;

            if (imageText == null)
            {
                var docType = Path.GetExtension(docFile).ToLower();

                switch (docType)
                {
                    case ".md":
                        imageText = MarkdownImageText.Parse(selectedText);
                        break;
                    case ".js":
                    case ".html":
                        imageText = HtmlImgText.Parse(selectedText);
                        break;
                }
            }

            if (imageText == null) imageText = new SimpleText() {Text = selectedText};

            imageText.ParentWindow = new SimpleWindow(dte.MainWindow.HWnd);
            imageText.FullFilename = docFile;

            var newText = imageText.PasteFromClipboard();

            if (newText == null) return;

            if (newText.Length == 0)
            {
                dte.Selection().CharRight();
                return;
            }

            dte.BeginUpdate("paste image");

            dte.Selection().DestructiveInsert(newText);

            dte.EndUpdate();
        }

        private static GetImageFilenameDialog OpenGetImageFilenameDialog(IWin32Window parentWindow, string baseFilename, string defaultFolder)
        {
            var dlg = new GetImageFilenameDialog();

            dlg.Init(baseFilename, defaultFolder, ".png");

            var dialogResult = dlg.ShowDialog(parentWindow);

            if (dialogResult != DialogResult.OK) return null;

            Debug.Assert(Directory.Exists(dlg.FullFolderPath));

            return dlg;
        }

        /// <summary>
        /// 抽象的图片文本
        /// </summary>
        public abstract class ImageText
        {
            public IWin32Window ParentWindow { get; set; }

            /// <summary>
            /// 所在文件名的完整路径
            /// </summary>
            public string FullFilename { get; set; }

            public string ImageUrl { get; set; }

            public string AltText { get; set; }

            public string PasteFromClipboard()
            {
                var image = Clipboard.GetImage();

                if (image == null)
                {
                    // 剪贴板中没有图片，尝试从剪贴板中提取图片文件名
                    var filenames = GetImageFilenames();

                    return filenames == null ? null : GetText(filenames);
                }

                if (ImageUrl == null || new Uri(ImageUrl).Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
                {
                    // 现已选中的部分不是一个 ImageText，保存图片文件并生成相应的文本
                    var dlg = OpenGetImageFilenameDialog(ParentWindow, FullFilename, GetDefaultFolder());

                    if (dlg == null)
                    {
                        // 用户取消
                        return null;
                    }
                    
                    image.Save(dlg.FullFilePath);

                    return GetText(dlg.AltText, dlg.RelativeFilePath);
                }
                
                image.Save(ImageUrl);

                return string.Empty;
            }

            /// <summary>
            /// 获取 FullFilename 文件对应的保存图片文件的相对路径
            /// </summary>
            /// <returns></returns>
            public virtual string GetDefaultFolder()
            {
                return Path.GetFileName(FullFilename) + ".files";
            }

            protected abstract string GetText(string altText, string url);

            protected abstract string GetText(string[] filenames);
        }

        /// <summary>
        /// 文本直接就是一个图片文件名
        /// </summary>
        public class SimpleText : ImageText
        {
            public string Text;

            public static SimpleText Parse(string text)
            {
                return IsImageFile(text) ? new SimpleText() {Text = text, ImageUrl = text} : null;
            }

            protected override string GetText(string altText, string url)
            {
                return url;
            }

            protected override string GetText(string[] filenames)
            {
                return string.Join(", ", filenames);
            }
        }

        /// <summary>
        /// Markdown 语法的图片文本
        /// </summary>
        public class MarkdownImageText : ImageText
        {
            public string Title;
            public string[] Urls;

            private static readonly Regex ImageRegex = new Regex(@"\A\!\[(?<altText>[^\]]*)\]\s*\((?<url>[^\)]*)\)\s*(\""(?<title>[^\""]*)\"")?\s*\Z", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

            public static MarkdownImageText Parse(string text)
            {
                var match = ImageRegex.Match(text);

                if (!match.Success) return null;

                var md = new MarkdownImageText()
                {
                    AltText = match.Groups["altText"].Value,
                    ImageUrl = match.Groups["url"].Value,
                    Title = match.Groups["title"].Success ? match.Groups["title"].Value : null,
                };

                return md;
            }

            protected override string GetText(string altText, string url)
            {
                return string.Format("![{0}]({1})\"{2}\"", altText, url, Title);
            }

            protected override string GetText(string[] filenames)
            {
                var sb = new StringBuilder();

                foreach (var url in Urls)
                {
                    sb.AppendFormat("![{0}]({1})\"{2}\"", AltText, url, Title).AppendLine();
                }

                return sb.ToString();
            }
        }

        class HtmlImgText : ImageText
        {
            private string Text;

            private const RegexOptions RegOptions = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;

            private static readonly Regex ImageRegex = new Regex(@"\A \s* <img [^>]+ > \s* \z", RegOptions);
            private static readonly Regex AltRegex = new Regex(@"alt \s* = "" (?<value>[^""]*) """, RegOptions);
            private static readonly Regex SrcRegex = new Regex(@"src \s* = "" (?<value>[^""]*) """, RegOptions);

            public HtmlImgText(string text)
            {
                Text = text;

                if (ImageRegex.IsMatch(text))
                {
                    AltText = AltRegex.Match(text).Groups["value"].Value;
                    ImageUrl = SrcRegex.Match(text).Groups["value"].Value;
                }
            }

            public static ImageText Parse(string text)
            {
                return ImageRegex.IsMatch(text) ? new SimpleText(){Text = text} : null;
            }

            protected override string GetText(string altText, string url)
            {
                var s = ReplaceAlt(Text, altText);
                s = ReplaceSrc(s, url);
                return s;
            }

            public static string ReplaceAlt(string text, string alt)
            {
                return AltRegex.Replace(text, string.Format(@"alt=""{0}""", alt));
            }

            public static string ReplaceSrc(string text, string src)
            {
                return AltRegex.Replace(text, string.Format(@"src=""{0}""", src));
            }

            protected override string GetText(string[] filenames)
            {
                var sb = new StringBuilder();

                foreach (var filename in filenames)
                {
                    sb.AppendLine(GetText(AltText, filename));
                }

                return sb.ToString();
            }
        }

        public static string GetRelativePath(FileSystemInfo basePath, FileSystemInfo path2)
        {
            if (basePath == null) throw new ArgumentNullException("basePath");
            if (path2 == null) throw new ArgumentNullException("path2");

            Func<FileSystemInfo, string> getFullName = delegate(FileSystemInfo path)
            {
                var fullName = path.FullName;

                if (path is DirectoryInfo)
                {
                    if (fullName[fullName.Length - 1] != Path.DirectorySeparatorChar)
                    {
                        fullName += Path.DirectorySeparatorChar;
                    }
                }
                return fullName;
            };

            var path1FullName = getFullName(basePath);
            var path2FullName = getFullName(path2);

            var uri1 = new Uri(path1FullName);
            var uri2 = new Uri(path2FullName);
            var relativeUri = uri1.MakeRelativeUri(uri2);

            return Uri.UnescapeDataString(relativeUri.OriginalString);
        }

        public static string AddPathSeparator(string dir)
        {
            if (dir[dir.Length - 1] == Path.DirectorySeparatorChar) return dir;

            return dir + Path.DirectorySeparatorChar;
        }

        public static string GetRelativePath(string basePath, string path)
        {
            if (basePath == null) throw new ArgumentNullException("basePath");
            if (path == null) throw new ArgumentNullException("path");

            var uri1 = new Uri(basePath);
            var uri2 = new Uri(path);
            var relativeUri = uri1.MakeRelativeUri(uri2);

            return Uri.UnescapeDataString(relativeUri.OriginalString);
        }

        private static readonly string[] ImageFileExts = {".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif"};

        public static bool HasImage()
        {
            return Clipboard.ContainsImage();
        }

        public static string[] GetImageFilenames()
        {
            if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                var list = files.Cast<string>().Where(IsImageFile).ToArray();
                return list.Length == 0 ? null : list;
            }

            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (IsImageFile(text)) return new []{text};
            }

            return null;
        } 

        public static bool IsImageFile(string filename)
        {
            return ImageFileExts.Any(t => filename.EndsWith(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}