// ****************************************************************************
// @Copyright: (C) 2010-2013 广东省电信工程有限公司 版权所有
// @File:  SmartKey.cs
// @Create: 2017-03-17
// @Author: sunhaiwen<sunhaiwen99@gmail.com>
// @History:
//  - yyyy-mm-dd by Author[<@email>] : brief
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace EclipseKey
{
    class SmartKey : IBeforeKeyHandler
    {
        /// <summary>
        /// 初始化 <see cref="T:System.Object"/> 类的新实例。
        /// </summary>
        public SmartKey(SmartKeyTemplate[] keys, Package package)
        {
            _commonTemplates = keys.Where(t => string.IsNullOrEmpty(t.FileType)).ToArray();

            _fileTemplates = keys.Where(t => !string.IsNullOrEmpty(t.FileType))
                .GroupBy(t => t.FileType)
                .Select(g => new KeyValuePair<string, SmartKeyTemplate[]>(g.Key, g.ToArray()))
                .ToArray();

            _package = package;
        }

        public DTE2 DTE { get; set; }

        private readonly KeyValuePair<string, SmartKeyTemplate[]>[] _fileTemplates;
        private readonly SmartKeyTemplate[] _commonTemplates;

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider { get { return _package; } }

        /// <summary>
        /// 当前已匹配的部分
        /// </summary>
        private string _match = string.Empty;

        /// <summary>
        /// 当前已匹配的部分对应的文档及位置
        /// </summary>
        private string _lastDocument;
        private int _lastCharOffset;

        public bool BeforeKeyPress(string key, TextSelection selection, bool inStatementCompletion, ref bool cancelKeyPress)
        {
           var filename = selection.Parent.Parent.Name.ToLower();

            if (!selection.IsEmpty || selection.ActivePoint.AbsoluteCharOffset != _lastCharOffset + 1 || _lastDocument != filename)
            {
                ClearMatch();
            }

            if (_match.Length > 0)
            {
                foreach (var template in GetTemplatesOfFile(filename))
                {
                    var templateKey = template.Key;

                    if (templateKey.Length == _match.Length + 1 && key[0] == templateKey[_match.Length] && templateKey.StartsWith(_match))
                    {
                        cancelKeyPress = Apply(selection, template.Value);
                        ClearMatch();
                        return true;
                    }
                }
            }

            _match += key;

            foreach (var template in GetTemplatesOfFile(filename))
            {
                if (!template.Key.StartsWith(_match)) continue;

                _lastDocument = filename;
                _lastCharOffset = selection.ActivePoint.AbsoluteCharOffset;
                return true;
            }

            ClearMatch();

            return false;
        }

        private IEnumerable<SmartKeyTemplate> GetTemplatesOfFile(string filename)
        {
            for (int i = 0; i < _commonTemplates.Length; i++)
            {
                yield return _commonTemplates[i];
            }

            for (int i = 0; i < _fileTemplates.Length; i++)
            {
                var group = _fileTemplates[i];
                if (filename.EndsWith(group.Key))
                {
                    var templates = @group.Value;

                    for (int j = 0; j < templates.Length; j++)
                    {
                        yield return templates[i];
                    }
                }
            }
        } 

        private static bool FileTypeMatch(string filename, string filetype)
        {
            return string.IsNullOrEmpty(filetype) || filename.EndsWith(filetype);
        }

        private void ClearMatch()
        {
            _match = String.Empty;
            _lastDocument = null;
            _lastCharOffset = 0;
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