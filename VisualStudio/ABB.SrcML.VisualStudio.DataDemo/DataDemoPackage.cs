using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using ABB.SrcML;
using ABB.SrcML.Data;
using ABB.SrcML.VisualStudio.SrcMLService;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;

namespace ABB.SrcML.VisualStudio.DataDemo {
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
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidDataDemoPkgString)]
    //Autoload on UICONTEXT_SolutionExists
    //[ProvideAutoLoad("f1536ef8-92ec-443c-9ed7-fdadf150da82")]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string)]
    public sealed class DataDemoPackage : Package {
        private DTE2 dte;
        private SolutionEvents solutionEvents;
        private ISrcMLGlobalService srcMLService;
        
        private DataArchive dataArchive;

        private Guid outputPaneGuid;
        private IVsOutputWindowPane outputPane;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public DataDemoPackage() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(null != mcs) {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidDataDemoCmdSet, (int)PkgCmdIDList.cmdidSrcMLData);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);

                var initMenuCommandID = new CommandID(GuidList.guidDataDemoCmdSet, (int)PkgCmdIDList.cmdidSrcMLDataInitialize);
                var initMenuItem = new MenuCommand(InitializeMenuItemCallback, initMenuCommandID);
                mcs.AddCommand(initMenuItem);
            }

            if(dte == null) {
                dte = GetService(typeof(DTE)) as DTE2;
                if(dte == null) {
                    PrintOutputLine("Could not get DTE!");
                    return;
                }
                //subscribe to solution events
                solutionEvents = dte.Events.SolutionEvents;
                solutionEvents.Opened += SolutionEvents_Opened;
                solutionEvents.BeforeClosing += solutionEvents_BeforeClosing;
            }

            //GetSrcMLService();
            //if(srcMLService != null) {
            //    srcMLService.StartupCompleted += srcMLService_StartupCompleted;
            //    if(srcMLService.IsStartupCompleted) {
            //        srcMLService_StartupCompleted(srcMLService, null);
            //    }
            //}
        }
        
        #endregion

        
        private void SolutionEvents_Opened() {
            if(srcMLService == null) {
                GetSrcMLService();
                if(srcMLService != null) {
                    srcMLService.StartupCompleted += srcMLService_StartupCompleted;
                }
            }
            srcMLService.StartMonitoring();
        }

        private void solutionEvents_BeforeClosing() {
            GetSrcMLService();
            srcMLService.StopMonitoring();
            dataArchive = null;
        }

        /// <summary>
        /// Gets the global SrcMLService and stores it in the field srcMLService.
        /// If the service has already been gotten, it won't be gotten again.
        /// </summary>
        /// <returns>False if the service could not be gotten, True otherwise.</returns>
        private bool GetSrcMLService() {
            if(srcMLService != null) {
                return true;
            }
            srcMLService = GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
            if(srcMLService == null) {
                PrintOutputLine("Could not get SrcMLGlobalService!");
                return false;
            }
            return true;
        }

        void srcMLService_StartupCompleted(object sender, EventArgs e) {
            var service = sender as ISrcMLGlobalService;
            if(service != null) {
                var archive = service.GetSrcMLArchive();
                PrintOutputLine("Generated srcML for {0} files in {1:mm\\:ss\\.ff}", archive.GetFiles().Count, service.StartupElapsed);
                InitializeDataArchive(archive);
            }
        }

        private void InitializeDataArchive(SrcMLArchive srcMLArchive) {
            PrintOutputLine("Initializing DataArchive...");
            var sw = Stopwatch.StartNew();
            dataArchive = new DataArchive(srcMLArchive);
            sw.Stop();
            PrintOutputLine("Initialization completed in {0:mm\\:ss\\.ff} seconds", sw.Elapsed);
        }

        private void InitializeMenuItemCallback(object sender, EventArgs e) {
            //we do nothing here. This exists only to initialize the Package without searching for a given scope.
            return;
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e) {
            if(dte == null) {
                dte = GetService(typeof(DTE)) as DTE2;
                if(dte == null) {
                    PrintOutputLine("Could not get DTE!");
                    return;
                }
            }
            if(dataArchive == null) {
                PrintOutputLine("DataArchive not available. Please try again in a few moments?");
                return;
            }

            var doc = dte.ActiveDocument;
            if(doc != null) {
                var sel = doc.Selection;
                var cursor = ((TextSelection)sel).ActivePoint;
                outputPane.Clear();
                PrintOutputLine("{0}({1},{2}) : cursor position", dte.ActiveDocument.FullName, cursor.Line, cursor.LineCharOffset);

                var scope = dataArchive.FindScope(new SourceLocation(dte.ActiveDocument.FullName, cursor.Line, cursor.LineCharOffset));
                if(scope == null) {
                    PrintOutputLine("Scope not found!");
                } else {
                    PrintOutputLine(scope.ToString());
                    PrintOutputLine(Environment.NewLine + "** Children **");
                    foreach(var child in scope.ChildScopes) {
                        PrintOutputLine(">> {0}", child.ToString());
                    }

                    PrintOutputLine(Environment.NewLine + "** Parent **");
                    var parent = scope.ParentScope;
                    string parentString;
                    if(parent == null) {
                        parentString = "None";
                    } else if(parent is NamespaceDefinition) {
                        parentString = "Namespace: " + ((NamespaceDefinition)parent).GetFullName();
                    } else {
                        parentString = parent.ToString();
                    }
                    PrintOutputLine(">> {0}", parentString);

                    PrintOutputLine(Environment.NewLine + "** Declared Variables **");
                    foreach(var vd in scope.DeclaredVariables) {
                        PrintOutputLine(">> {0}", vd.ToString());
                    }

                    PrintOutputLine(Environment.NewLine + "** Method Calls **");
                    foreach(var mc in scope.MethodCalls) {
                        var methodMatches = mc.FindMatches().Distinct().ToList();
                        PrintOutputLine(">> Call {0}, matches: {1}", mc.ToString(), methodMatches.Any() ? "" : "No matches");
                        foreach(var match in methodMatches) {
                            SrcMLLocation loc;
                            if(match.DefinitionLocations.Any()) {
                                loc = match.DefinitionLocations.First();
                            } else {
                                loc = match.Locations.First();
                            }
                            PrintOutputLine("{0}({1},{2}) : {3}", loc.SourceFileName, loc.StartingLineNumber, loc.StartingColumnNumber, match.ToString());
                        }
                    }
                }
            }

            
        }

        
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

        private void PrintOutputLine(string format, params object[] args) {
            PrintOutput(string.Format(format, args) + Environment.NewLine);
        }
    }
}
