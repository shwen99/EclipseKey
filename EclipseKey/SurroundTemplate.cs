using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using EnvDTE;

namespace EclipseKey
{
    public class SurroundTemplate
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        [DefaultValue(false)]
        public bool Disable { get; set; }

        [XmlText]
        public string Template
        {
            get { return _template; }
            set
            {
                var template = value.Replace("\r\n", "\n");

                if (template.IndexOf("\n    ...\n", StringComparison.Ordinal) >= 0)
                {
                    template = template.Replace("\n    ...\n", "...");
                    Inline = false;
                    _extendSelection = true;
                    _indentSelection = true;
                }
                else if (template.IndexOf("\n...\n", StringComparison.Ordinal) >= 0)
                {
                    template = template.Replace("\n...\n", "...");
                    Inline = false;
                    _extendSelection = true;
                    _indentSelection = false;
                }
                else
                {
                    Inline = true;
                    _extendSelection = false;
                    _indentSelection = false;
                }
                
                _template = template.Trim('\n', ' ', '\t');

                var items = _template.Split(new[] { "..." }, 2, StringSplitOptions.None);
                _preText = items[0].Lines().ToArray();
                _postText = items[1].Lines().ToArray();

                _caretLine = -1;
                _caretCol = -1;

                for (int i = 0; i < _preText.Length; i++)
                {
                    var s = _preText[i];
                    if (s.IndexOf('|') >= 0)
                    {
                        _preText[i] = s.Replace("|", "");
                        _caretLine = i;
                        _caretCol = s.IndexOf('|') + 1;
                    }
                }

                for (int i = 0; i < _postText.Length; i++)
                {
                    var s = _postText[i];
                    if (s.IndexOf('|') >= 0)
                    {
                        _postText[i] = s.Replace("|", "");
                        _caretLine = i + _preText.Length;
                        _caretCol = s.IndexOf('|') + 1;
                    }
                }
            }
        }

        private string[] _preText;
        private string[] _postText;
        private int _caretLine; // 模板中插入光标所在行，从 0 开始，小于 0 表示未指定插入光标
        private int _caretCol;  // 模板中插入光标所在列，从 1 开始

        private bool _indentSelection;
        private bool _extendSelection;
        internal bool Inline;
        private string _template;

        public void Apply(TextSelection selection)
        {
            if (Inline)
            {
                ApplyInline(selection);
                return;
            }

            var closeUndoContext = false;
            if (!selection.DTE.UndoContext.IsOpen)
            {
                selection.BeginUpdate("Surround With " + Name);
                closeUndoContext = true;
            }

            if (_extendSelection) selection.ExtendToFullLine();
            var indentSize = selection.Text.Lines().Where(l => !string.IsNullOrWhiteSpace(l)).Min(l => l.Length - l.TrimStart().Length);

            var content = selection.Text.Lines();

            var leadingSpace = new string(' ', indentSize);
            var indent = new string(' ', selection.Parent.IndentSize);

            var sb = new StringBuilder();

            foreach (var s in _preText)
            {
                sb.Append(leadingSpace).AppendLine(s);
            }

            foreach (var s in content)
            {
                if (_indentSelection)
                {
                    sb.Append(indent);
                }
                sb.AppendLine(s);
            }

            foreach (var s in _postText)
            {
                sb.Append(leadingSpace).AppendLine(s);
            }

            var line = selection.TopLine;
            var col = selection.TopPoint.DisplayColumn;

            selection.Delete();
            selection.Insert(sb.ToString());

            if (_caretLine >= _preText.Length)
            {
                selection.MoveToLineAndOffset(line + _caretLine + content.Count, _caretCol + indentSize, false);
            }
            else if (_caretLine >= 0)
            {
                selection.MoveToLineAndOffset(line + _caretLine, _caretCol + indentSize, false);
            }
            else
            {
                selection.MoveToDisplayColumn(line, col, true);
            }

            if (closeUndoContext)
            {
                selection.EndUpdate();
            }
        }

        private void ApplyInline(TextSelection selection)
        {
            var closeUndoContext = false;
            if (!selection.DTE.UndoContext.IsOpen)
            {
                selection.BeginUpdate("Surround With " + Name);
                closeUndoContext = true;
            }

            var sb = new StringBuilder();
            sb.Append(_preText[0]);
            sb.Append(selection.Text);
            sb.Append(_postText[0]);

            var line = selection.TopLine;
            var col = selection.TopPoint.DisplayColumn;

            selection.Delete();
            selection.Insert(sb.ToString());

            if (_caretLine >= 0)
            {
                selection.MoveToDisplayColumn(line, col + _caretCol - 1);
            }
            else
            {
                selection.MoveToDisplayColumn(line, col, true);
            }

            if (closeUndoContext)
            {
                selection.EndUpdate();
            }
        }

        public override string ToString()
        {
            return _template;
        }

    }
}