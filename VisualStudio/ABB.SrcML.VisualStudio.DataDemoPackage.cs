using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ABB.SrcML.VisualStudio.DataDemo
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
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.guidDataDemoPkgString)]
    //Autoload on UICONTEXT_NoSolution
    //[ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    //Autoload on UICONTEXT_SolutionExists
    [ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    public sealed class DataDemoPackage : Package {
        private DTE2 dte;
        private Events2 events;
        private TextEditorEvents textEditorEvents;
        private SelectionEvents selectionEvents;
        private TextDocumentKeyPressEvents textDocumentKeyPressEvents;

        private Guid outputPaneGuid;
        private IVsOutputWindowPane outputPane;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public DataDemoPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();


            dte = GetService(typeof(DTE)) as DTE2;
            //var cursor = ((EnvDTE.TextDocument)dte.ActiveDocument).Selection.ActivePoint as EnvDTE.TextPoint;

            events = dte.Events as Events2;

            textEditorEvents = events.TextEditorEvents;
            //textEditorEvents.LineChanged += textEditorEvents_LineChanged;
            selectionEvents = events.SelectionEvents;
            selectionEvents.OnChange += selectionEvents_OnChange;
            textDocumentKeyPressEvents = events.TextDocumentKeyPressEvents;
            textDocumentKeyPressEvents.AfterKeyPress += textDocumentKeyPressEvents_AfterKeyPress;

            PrintOutputLine("Events registered");

            
        }

        void textDocumentKeyPressEvents_AfterKeyPress(string Keypress, TextSelection Selection, bool InStatementCompletion) {
            if(Selection.Parent != null & Selection.Parent.Parent != null) {
                PrintOutputLine("Keypress: " + Keypress);
                PrintOutputLine(string.Format("ActivePoint: {0}:{1},{2}", Selection.Parent.Parent.FullName, Selection.ActivePoint.Line, Selection.ActivePoint.VirtualDisplayColumn));
            }
            
        }

        void textEditorEvents_LineChanged(TextPoint StartPoint, TextPoint EndPoint, int Hint) {
            if(StartPoint.Parent != null && StartPoint.Parent.Parent != null) {
                PrintOutputLine(string.Format("StartPoint: {0}:{1},{2}", StartPoint.Parent.Parent.FullName, StartPoint.Line, StartPoint.DisplayColumn));
            }
        }

        void selectionEvents_OnChange() {
            var doc = dte.ActiveDocument as EnvDTE.TextDocument;
            if(doc != null) {
                var cursor = doc.Selection.ActivePoint as EnvDTE.TextPoint;
                if(cursor != null) {
                    PrintOutputLine(string.Format("Cursor at {0}:{1},{2}", dte.ActiveDocument.Path, cursor.Line, cursor.DisplayColumn));
                }
            }
        }
        #endregion

        private void SetupOutputPane() {
            outputPaneGuid = Guid.NewGuid();
            outputPane = GetOutputPane(outputPaneGuid, "SrcML.Data");
            outputPane.Activate();
        }

        private void PrintOutput(string text) {
            if(outputPane == null) {
                SetupOutputPane();
            }
            outputPane.OutputString(text);
        }

        private void PrintOutputLine(string text) {
            if(outputPane == null) {
                SetupOutputPane();
            }
            outputPane.OutputString(text + Environment.NewLine);
        }
    }
}
