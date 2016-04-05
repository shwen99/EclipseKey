using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    interface IBeforeKeyHandler
    {
        DTE2 DTE { get; set; }

        bool BeforeKeyPress(string key, TextSelection selection, bool inStatementCompletion, ref bool cancelKeyPress);
    }
}
