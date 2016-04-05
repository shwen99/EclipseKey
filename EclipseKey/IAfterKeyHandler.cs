using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    interface IAfterKeyHandler
    {
        DTE2 DTE { get; set; }

        bool AfterKeyPress(string key, TextSelection selection, bool inStatementCompletion);
    }
}
