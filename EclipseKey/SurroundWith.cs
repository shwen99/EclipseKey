using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    class SurroundWith : IBeforeKeyHandler
    {        
        public DTE2 DTE { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keypress"></param>
        /// <param name="selection"></param>
        /// <param name="inStatementCompletion"></param>
        /// <param name="cancelKeypress"></param>
        public bool BeforeKeyPress(string keypress, TextSelection selection, bool inStatementCompletion, ref bool cancelKeypress)
        {
            cancelKeypress = false;

            if (selection.IsEmpty)
            {
                return false;
            }

            var lang = selection.Language();

            if (lang != DteUtils.LanguageType.CSharp && lang != DteUtils.LanguageType.JavaScript)
            {
                return false;
            }

            var template = Templates.FirstOrDefault(t=>!t.Disable && t.Key == keypress);

            if (template == null)
            {
                return false;
            }

            if (!template.Inline && !selection.IsFullLine())
            {
                return false;
            }

            template.Apply(selection);
            cancelKeypress = true;

            return true;
        }


        internal List<SurroundTemplate> Templates;

    }
}
