// ****************************************************************************
// @Copyright: (C) 2010-2013 广东省电信工程有限公司 版权所有
// @File:  SmartKey.cs
// @Create: 2017-03-17
// @Author: sunhaiwen<sunhaiwen99@gmail.com>
// @History:
//  - yyyy-mm-dd by Author[<@email>] : brief
// ****************************************************************************

using System;
using System.CodeDom;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;

namespace EclipseKey
{
    class SmartKey : IBeforeKeyHandler
    {
        /// <summary>
        /// 初始化 <see cref="T:System.Object"/> 类的新实例。
        /// </summary>
        public SmartKey(SmartKeyTemplate[] keys)
        {
            _keys = keys;
        }

        public DTE2 DTE { get; set; }

        private readonly SmartKeyTemplate[] _keys;

        /// <summary>
        /// 当前已匹配的部分
        /// </summary>
        private string _match = string.Empty;

        public bool BeforeKeyPress(string key, TextSelection selection, bool inStatementCompletion, ref bool cancelKeyPress)
        {
            for (int i = 0; i < _keys.Length; i++)
            {
                var template = _keys[i].Key;

                if (template.Length == _match.Length + 1 && key[0] == template[_match.Length] && template.StartsWith(_match))
                {
                    cancelKeyPress = Apply(selection, _keys[i].Value);
                    _match = string.Empty;
                    return true;
                }
            }

            _match += key;

            for (int i = 0; i < _keys.Length; i++)
            {
                if (_keys[i].Key.StartsWith(_match))
                {
                    return true;
                }
            }

            _match = String.Empty;

            return false;
        }

        private bool Apply(TextSelection selection, string value)
        {
            selection.CharLeft(true, _match.Length);
            
            if (selection.Text != _match)
            {
                selection.CharRight(false);
                return false;
            }

            selection.Insert(value, (int) vsInsertFlags.vsInsertFlagsContainNewText);
            selection.CharRight(false);

            return true;
        }
    }
}