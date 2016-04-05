//------------------------------------------------------------------------------
// <copyright file="ToggleComments.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Linq;


namespace EclipseKey
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ToggleComments
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4133;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = GuidList.guidEclipseKeyCmdSet;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleComments"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToggleComments(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            _package = package;

            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
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
        public static ToggleComments Instance
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
            Instance = new ToggleComments(package);
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

            if (dte.Documents == null || dte.ActiveTextView() == null)
            {
                return;
            }

            var lang = dte.Selection().Language();

            string commentStartChar = null;
            string commentEndChar = null;

            switch (lang)
            {
                case DteUtils.LanguageType.VisualBasic:
                case DteUtils.LanguageType.VBScript:
                    commentStartChar = "'";
                    commentEndChar = null;
                    break;
                case DteUtils.LanguageType.CssStyle:
                case DteUtils.LanguageType.CSharp:
                case DteUtils.LanguageType.JavaScript:
                    commentStartChar = "//";
                    commentEndChar = null;
                    break;
                case DteUtils.LanguageType.Html:
                case DteUtils.LanguageType.Xml:
                    commentStartChar = "<!--";
                    commentEndChar = "-->";
                    break;
                default:
                    dte.StatusBar.Text = "unknown language type " + dte.ActiveDocument.Language + ", please contact the author";
                    return;
            }

            if (!dte.ExtendSelection())
                return;

            var selection = dte.Selection();
            var lineno = selection.TopLine;
            var lineCount = selection.BottomLine - selection.TopLine;

            selection.Untabify();
            selection.GotoLine(lineno);
            selection.MoveToLineAndOffset(lineno + lineCount, 1, true);
            
            var sr = new StringReader(selection.Text);
            var lines = selection.Text.Lines().Select(l => new CodeLine(l)).ToList();

            var isCommented = lines.All(s => s.IsCommented(commentStartChar));

            if (isCommented)
            {
                foreach (var item in lines)
                {
                    item.TrimComment(commentStartChar, commentEndChar);
                }
            }
            else
            {
                var minLeadingSpaceCount = lines.Min(s => (s.IsBlank ? int.MaxValue : s.LeadingSpaceCount));

                foreach (var item in lines)
                {
                    item.Comment(minLeadingSpaceCount, commentStartChar, commentEndChar);
                }
            }

            dte.BeginUpdate("toggle comments");

            var newText = new StringBuilder();

            foreach (var codeLine in lines)
            {
                newText.AppendLine(codeLine.GetFullLine());
            }

            selection.Delete();
            selection.Insert(newText.ToString(), (int)vsInsertFlags.vsInsertFlagsCollapseToStart);
            selection.MoveTo(selection.TopLine + lineCount, 1, true);

            dte.EndUpdate();

        }

        private class CodeLine
        {
            public int LeadingSpaceCount;
            public string CommentStart;
            public string Code;

            public string CommentEnd;
            /// <summary>
            /// Initializes a new instance of the CodeLine class.
            /// </summary>
            public CodeLine(string s)
            {
                //s = LeadingTabToSpaces(s);
                Code = s.TrimStart();
                LeadingSpaceCount = s.Length - Code.Length;
            }

            public string GetFullLine()
            {
                var sb = new StringBuilder(LeadingSpaceCount + Code.Length + 10);

                if (LeadingSpaceCount > 0)
                {
                    sb.Append(' ', LeadingSpaceCount);
                }

                if ((CommentStart != null))
                {
                    sb.Append(CommentStart);
                }

                sb.Append(Code);

                if ((CommentEnd != null))
                {
                    sb.Append(CommentEnd);
                }

                return sb.ToString();
            }

            public bool IsBlank
            {
                get { return string.IsNullOrWhiteSpace(Code); }
            }

            public bool IsCommented(string commentStart)
            {
                return IsBlank || Code.StartsWith(commentStart);
            }

            public void TrimComment(string commentStartChar, string commentEndChar)
            {
                if (IsBlank)
                {
                    return;
                }

                if (Code.StartsWith(commentStartChar))
                {
                    Code = Code.Remove(0, commentStartChar.Length);
                }

                if ((commentEndChar != null) && Code.EndsWith(commentEndChar))
                {
                    Code = Code.Substring(0, Code.Length - commentEndChar.Length);
                }
            }

            public void Comment(int leadingSpaceCount, string commentStartChar, string commentEndChar)
            {
                if (this.LeadingSpaceCount > leadingSpaceCount)
                {
                    Code = Code.PadLeft(Code.Length + this.LeadingSpaceCount - leadingSpaceCount);
                }

                this.LeadingSpaceCount = leadingSpaceCount;
                CommentStart = commentStartChar;
                CommentEnd = commentEndChar;
            }

            //private static string LeadingTabToSpaces(string s)
            //{
            //    const char TAB = '\t';
            //    const char SPACE = ' ';

            //    for (int i = 0; i <= s.Length - 1; i++)
            //    {
            //        char ch = s[i];
            //        if (ch == TAB)
            //        {
            //            break; // TODO: might not be correct. Was : Exit For
            //        }
            //        else if (ch != SPACE)
            //        {
            //            return s;
            //        }
            //    }

            //    var sb = new StringBuilder(s.Length + 64);

            //    for (int i = 0; i <= s.Length - 1; i++)
            //    {
            //        char ch = s[i];
            //        if (ch == TAB)
            //        {
            //            sb.Append("    ");
            //        }
            //        else if (ch == SPACE)
            //        {
            //            sb.Append(ch);
            //        }
            //        else
            //        {
            //            sb.Append(s, i, s.Length - i + 1);
            //            break;
            //        }
            //    }

            //    return sb.ToString();
            //}
        }
    }
}
