using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    class SmartSemicolon : IBeforeKeyHandler
    {
        public DTE2 DTE { get; set; }

        // 当需要回退时应该去到的位置
        private bool _smartSemicolonFallback;
        private int _smartSemicolonLine;
        private int _smartSemicolonColumn;
        private bool _smartSemicolonDeleteLineEnd;

        /// <summary>
        /// 智能分号：
        ///  * 如果在一行的中间任何位置按下分号，则跳到行结尾添加分号，如果行未已经有一个分号则不重复添加
        ///  * 再按一次分号，则删除在行尾添加的分号，回到刚才的位置插入一个分号
        /// </summary>
        /// <param name="keypress"></param>
        /// <param name="selection"></param>
        /// <param name="inStatementCompletion"></param>
        /// <param name="cancelKeypress"></param>
        public bool BeforeKeyPress(string keypress, TextSelection selection, bool inStatementCompletion, ref bool cancelKeypress)
        {
            if (!selection.IsEmpty || keypress != ";")
            {
                _smartSemicolonFallback = false;
                return false;
            }

            if (_smartSemicolonFallback && selection.CurrentLine == _smartSemicolonLine
                && selection.ActivePoint.AtEndOfLine && selection.ActivePoint.CreateEditPoint().GetText(-1) == ";")
            {
                // 重复输入分号, 删除原来行尾的分号，并退回到原位置插入分号
                if (_smartSemicolonDeleteLineEnd) selection.DeleteLeft(1);
                selection.MoveTo(_smartSemicolonLine, _smartSemicolonColumn, false);
                _smartSemicolonFallback = false;
                cancelKeypress = false;
            }
            else
            {
                // 智能分号，记录位置并移动到行尾插入分号
                _smartSemicolonFallback = true;
                _smartSemicolonLine = selection.ActivePoint.Line;
                _smartSemicolonColumn = selection.ActivePoint.DisplayColumn;

                selection.EndOfLine();
                var caret = selection.ActivePoint.CreateEditPoint();

                if (caret.GetText(-1) == ";")
                {
                    cancelKeypress = true;
                    _smartSemicolonDeleteLineEnd = false;
                }
                else
                {
                    cancelKeypress = false;
                    _smartSemicolonDeleteLineEnd = true;
                }
            }

            return true;

        }
    }
}
