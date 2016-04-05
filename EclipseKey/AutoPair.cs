using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    class AutoPair : IBeforeKeyHandler
    {
        public DTE2 DTE { get; set; }

        private readonly string _key;
        private readonly string _template;
        private readonly int _caret;

        public AutoPair(string key, string template, int caret = 0)
        {
            _key = key;
            _template = template;
            _caret = caret;
        }

        public bool BeforeKeyPress(string key, TextSelection selection, bool inStatementCompletion, ref bool cancelKeyPress)
        {
            if (!selection.IsEmpty || key != _key)
            {
                return false;
            }
            
            cancelKeyPress = true;

            var closeUndoContext = !DTE.UndoContext.IsOpen;
            if (closeUndoContext)
            {
                selection.BeginUpdate("insert " + _template);
            }

            selection.Insert(_template);
            if (_caret != 0) selection.CharLeft(false, -_caret);

            if (closeUndoContext)
            {
                selection.EndUpdate();
            }

            return true;
        }
    }
}
