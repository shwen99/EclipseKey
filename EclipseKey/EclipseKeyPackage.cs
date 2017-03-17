using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace EclipseKey
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidEclipseKeyPkgString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class EclipseKeyPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public EclipseKeyPackage()
        {
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            DeleteLines.Initialize(this);
            MoveLinesDown.Initialize(this);
            MoveLinesUp.Initialize(this);
            ToggleComments.Initialize(this);
            CopyLinesDown.Initialize(this);
            CopyLinesUp.Initialize(this);
            RegisterHandler();
        }
        #endregion

        private readonly List<IBeforeKeyHandler> _keyBeforeHandlers = new List<IBeforeKeyHandler>();
        private readonly List<IAfterKeyHandler> _keyAfterHandlers = new List<IAfterKeyHandler>();

        private void RegisterHandler()
        {
            var dte = (DTE2)GetService(typeof(DTE));

            using (var stream = File.OpenRead(GetType().Assembly.Location + ".config"))
            {
                var serializer = new XmlSerializer(typeof(Option));
                var option = (Option)serializer.Deserialize(stream);

                if (option.SmartSemicolon) _keyBeforeHandlers.Add(new SmartSemicolon());

                if (option.SmartKeyTemplates != null && option.SmartKeyTemplates.Count > 0)
                {
                    _keyBeforeHandlers.Add(new SmartKey(option.SmartKeyTemplates.ToArray()));
                }

                if (option.单引号自动补全) _keyBeforeHandlers.Add(new AutoPair("'", "''", -1));
                if (option.双引号自动补全) _keyBeforeHandlers.Add(new AutoPair("\"", "\"\"", -1));
                if (option.圆括号自动补全) _keyBeforeHandlers.Add(new AutoPair("(", "()", -1));
                if (option.方括号自动补全) _keyBeforeHandlers.Add(new AutoPair("[", "[]", -1));

                if (option.SurroundTemplates.Any(t => !t.Disable))
                {
                    _keyBeforeHandlers.Add(new SurroundWith
                    {
                        Templates = option.SurroundTemplates.Where(t => !t.Disable).ToList()
                    });
                }
            }

            foreach (var item in _keyBeforeHandlers)
            {
                item.DTE = dte;
            }

            foreach (var item in _keyAfterHandlers)
            {
                item.DTE = dte;
            }

            if (_textDocKeyEvents == null)
            {
                _textDocKeyEvents = ((Events2)dte.Events).TextDocumentKeyPressEvents;
                if (_keyBeforeHandlers.Count > 0) _textDocKeyEvents.BeforeKeyPress += OnBeforeKeyPress;
                if (_keyAfterHandlers.Count > 0) _textDocKeyEvents.AfterKeyPress += OnAfterKeyPress;
            }
        }

        private TextDocumentKeyPressEvents _textDocKeyEvents;

        private void OnBeforeKeyPress(string keypress, TextSelection selection, bool instatementcompletion, ref bool cancelkeypress)
        {
            for (int i = 0; i < _keyBeforeHandlers.Count; i++)
            {
                var handler = _keyBeforeHandlers[i];
                if (handler.BeforeKeyPress(keypress, selection, instatementcompletion, ref cancelkeypress))
                {
                    break;
                }
            }
        }

        private void OnAfterKeyPress(string keypress, TextSelection selection, bool instatementcompletion)
        {
            for (int i = 0; i < _keyAfterHandlers.Count; i++)
            {
                var handler = _keyAfterHandlers[i];
                if (handler.AfterKeyPress(keypress, selection, instatementcompletion))
                {
                    break;
                }
            }
        }
    }
}
