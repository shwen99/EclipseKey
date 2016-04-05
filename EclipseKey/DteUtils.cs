using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    static class DteUtils
    {
        public static List<string> Lines(this string s)
        {
            var list = new List<string>();

            using (var reader = new StringReader(s))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }

            return list;
        }

        public static Document ActiveTextView(this DTE2 dte)
        {
            return dte.ActiveDocument;
        }

        public static TextSelection Selection(this DTE2 dte)
        {
            return (TextSelection) dte.ActiveTextView().Selection;
        }

        public static void BeginUpdate(this DTE2 dte, string name)
        {
            dte.UndoContext.Open(name);
        }

        public static void EndUpdate(this DTE2 dte)
        {
            dte.UndoContext.Close();
        }

        public static void BeginUpdate(this TextSelection selection, string name)
        {
            selection.DTE.UndoContext.Open(name);
        }

        public static void EndUpdate(this TextSelection selection)
        {
            selection.DTE.UndoContext.Close();
        }

        /// <summary>
        /// 将当前选择扩展到整行：
        ///  * 如果没有选区，则选择当前行
        ///  * 如果选择没有跨行，则选择当前行
        ///  * 选择包含选择区域的整行
        /// </summary>
        /// <returns></returns>
        public static bool ExtendSelection(this DTE2 dte)
        {

            var view = dte.ActiveTextView();

            if (view == null)
            {
                return false;
            }

            var selection = dte.Selection();

            ExtendToFullLine(selection);

            return true;
        }

        public static void ExtendToFullLine(this TextSelection selection)
        {
            var line1 = selection.TopLine;
            var line2 = selection.BottomLine;

            if (selection.IsEmpty || !selection.BottomPoint.AtStartOfLine)
            {
                line2 += 1;
            }

            selection.GotoLine(line1);
            selection.MoveToLineAndOffset(line2, 1, true);
        }

        public static bool IsFullLine(this TextSelection selection)
        {
            return !selection.IsEmpty && selection.TopPoint.AtStartOfLine && selection.BottomPoint.AtStartOfLine;
        }

        public static LanguageType Language(this TextSelection selection)
        {
            switch (selection.Parent.Language.ToLower())
            {
                case "csharp" :
                    return LanguageType.CSharp;
                case "javascript" :
                    return LanguageType.JavaScript;
                case "vbscript" :
                    return LanguageType.VBScript;
                case "visualbasic" :
                    return LanguageType.VisualBasic;
                case "xml" :
                    return LanguageType.Xml;
                case "html" :
                    return LanguageType.Html;
                case "css":
                    return LanguageType.CssStyle;
                default:
                    return LanguageType.Unknown;
            }
        }

        public enum LanguageType
        {
            Unknown,
            CSharp,
            JavaScript,
            VisualBasic,
            VBScript,
            Html,
            Xml,
            CssStyle,
        }
    }
}
