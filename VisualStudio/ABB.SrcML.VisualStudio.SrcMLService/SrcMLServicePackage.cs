﻿/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using log4net;
using ABB.SrcML.Utilities;

namespace ABB.SrcML.VisualStudio.SrcMLService {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// 
    /// Reference: Microsoft sample
    /// This is the package that exposes the Visual Studio services.
    /// In order to expose a service a package must implement the IServiceProvider interface (the one 
    /// defined in the Microsoft.VisualStudio.OLE.Interop.dll interop assembly, not the one defined in the
    /// .NET Framework) and notify the shell that it is exposing the services.
    /// The implementation of the interface can be somewhat difficult and error prone because it is not 
    /// designed for managed clients, but using the Managed Package Framework (MPF) we don’t really need
    /// to write any code: if our package derives from the Package class, then it will get for free the 
    /// implementation of IServiceProvider from the base class.
    /// The notification to the shell about the exported service is done using the IProfferService interface
    /// exposed by the SProfferService service; this service keeps a list of the services exposed globally 
    /// by the loaded packages and allows the shell to find the service even if the service provider that 
    /// exposes it is not inside the currently active chain of providers. If we simply use this service, 
    /// then the service will be available for all the clients when the package is loaded, but the service
    /// will be not usable when the package is not loaded. To avoid this problem and tell the shell that 
    /// it has to make sure that this package is loaded when the service is queried, we have to register 
    /// the service and package inside the services section of the registry. The MPF exposes the 
    /// ProvideServiceAttribute registration attribute to add the information needed inside the registry, 
    /// so that all we have to do is to use it in the definition of the class that implements the package.
    /// </summary>

    /// <summary>
    /// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is a package.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]

    /// <summary>
    /// This attribute is used to register the information needed to show this package in the Help/About dialog of Visual Studio.
    /// </summary>
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]

    /// <summary>
    /// This attribute is needed to let the shell know that this package exposes some menus.
    /// </summary>
    [ProvideMenuResource("Menus.ctmenu", 1)]

    /// <summary>
    /// Step 1: Add the ProvideServiceAttribute to the VSPackage that provides the global service.
    /// ProvideServiceAttribute registers SSrcMLGlobalService with Visual Studio. Only the global service must be registered.
    /// </summary>
    [ProvideService(typeof(SSrcMLGlobalService))]

    /// <summary>
    /// Get the Guid.
    /// </summary>
    [Guid(GuidList.guidSrcMLServicePkgString)]

    /// <summary>
    /// This attribute starts up this extension early so that it can listen to solution events.
    /// </summary>
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]

    public sealed class SrcMLServicePackage : Package {

        /// <summary>
        /// SrcML.NET Service.
        /// </summary>
        private ISrcMLGlobalService srcMLService;

        /// <summary>
        /// Events relating to the state of the environment.
        /// </summary>
        private DTEEvents DteEvents;

        /// <summary>
        /// Events for changes to a solution.
        /// </summary>
        private SolutionEvents SolutionEvents;

        /// <summary>
        /// Listening interface that monitors any notifications of changes to the solution.
        /// </summary>
        private SolutionChangeEventListener SolutionChangeListener;

        /// <summary>
        /// Visual Studio's Activity Logger.
        /// </summary>
        private IVsActivityLog ActivityLog;

        /// <summary>
        /// log4net logger.
        /// </summary>
        private ILog logger;

        /// <summary>
        /// The path of the SrcML.NET Service VS extension
        /// </summary>
        private string extensionDirectory;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SrcMLServicePackage() {
            //WriteActivityLog("SrcMLServicePackage.SrcMLServicePackage()");    // Leave this here as an example of how to use Activity Log
            SrcMLFileLogger.DefaultLogger.Info(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            // Step 2: Add callback methods to the service container to create the services.
            // Here we update the list of the provided services with the ones specific for this package.
            // Notice that we set to true the boolean flag about the service promotion for the global:
            // to promote the service is actually to proffer it globally using the SProfferService service.
            // For performance reasons we don’t want to instantiate the services now, but only when and 
            // if some client asks for them, so we here define only the type of the service and a function
            // that will be called the first time the package will receive a request for the service. 
            // This callback function is the one responsible for creating the instance of the service 
            // object.
            // The SrcML local service has not been used so far.
            IServiceContainer serviceContainer = this as IServiceContainer;
            ServiceCreatorCallback callback = new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SSrcMLGlobalService), callback, true);
            serviceContainer.AddService(typeof(SSrcMLLocalService), callback);
        }

        /// <summary>
        /// Step 3: Implement the callback method.
        /// This is the function that will create a new instance of the services the first time a client
        /// will ask for a specific service type. It is called by the base class's implementation of
        /// IServiceProvider.
        /// </summary>
        /// <param name="container">The IServiceContainer that needs a new instance of the service.
        ///                         This must be this package.</param>
        /// <param name="serviceType">The type of service to create.</param>
        /// <returns>The instance of the service.</returns>
        private object CreateService(IServiceContainer container, Type serviceType) {
            SrcMLFileLogger.DefaultLogger.Info("    SrcMLServicePackage.CreateService()");

            // Check if the IServiceContainer is this package.
            if(container != this) {
                Trace.WriteLine("ServicesPackage.CreateService called from an unexpected service container.");
                return null;
            }

            // Find the type of the requested service and create it.
            if(typeof(SSrcMLGlobalService) == serviceType) {
                // Build the global service using this package as its service provider.
                return new SrcMLGlobalService(this, extensionDirectory);
            }
            if(typeof(SSrcMLLocalService) == serviceType) {
                // Build the local service using this package as its service provider.
                return new SrcMLLocalService(this);
            }

            // If we are here the service type is unknown, so write a message on the debug output
            // and return null.
            Trace.WriteLine("ServicesPackage.CreateService called for an unknown service type.");
            return null;
        }

        #region Package Members
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            SrcMLFileLogger.DefaultLogger.Info("Initializing SrcML.NET Service ...");

            base.Initialize();

            SetUpSrcMLServiceExtensionDirectory();

            SetUpLogger();

            SetUpActivityLogger();

            //SetUpCommand(); // SrcML.NET does not need any command so far.

            SetUpSrcMLService();

            SetUpDTEEvents();

            SrcMLFileLogger.DefaultLogger.Info("Initialization completed.");
        }
        #endregion

        /// <summary>
        /// Set up the working directory of the SrcMLService extention.
        /// </summary>
        /// <returns></returns>
        private void SetUpSrcMLServiceExtensionDirectory() {
            //pluginDirectory = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            var uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            extensionDirectory = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));

            SrcMLFileLogger.DefaultLogger.Info("> Set up working directory. [" + extensionDirectory + "]");
        }

        /// <summary>
        /// Set up log4net logger.
        /// </summary>
        private void SetUpLogger() {
            //var logFilePath = Path.Combine("D:\\Data\\", this.ToString() + ".log");
            //logger = SrcMLFileLogger.CreateFileLogger(this.ToString() + "Logger", logFilePath);
            var logFilePath = Path.Combine(extensionDirectory, "SrcML.NETService.log");
            logger = SrcMLFileLogger.CreateFileLogger("SrcMLServiceLogger", logFilePath);
            SrcMLFileLogger.DefaultLogger.Info("> Set up log4net logger.");
        }

        /// <summary>
        /// Set up Visual Studio activity logger.
        /// </summary>
        private void SetUpActivityLogger() {
            SrcMLFileLogger.DefaultLogger.Info("> Set up Visual Studio activity logger.");

            ActivityLog = GetService(typeof(SVsActivityLog)) as IVsActivityLog;
            if(null == ActivityLog) {
                Trace.WriteLine("Can not get the Activity Log service.");
            }
        }

        /// <summary>
        /// Set up command handlers for menu (commands must exist in the .vsct file)
        /// </summary>
        private void SetUpCommand() {
            SrcMLFileLogger.DefaultLogger.Info("> Set up command handlers for menu.");

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(null != mcs) {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidSrcMLServiceCmdSet, (int)PkgCmdIDList.SrcML);
                MenuCommand menuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e) {
            SrcMLFileLogger.DefaultLogger.Info("    SrcMLServicePackage.MenuItemCallback()");

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "Our own SrcMLService",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        /// <summary>
        /// Set up SrcML Service.
        /// </summary>
        private void SetUpSrcMLService() {
            SrcMLFileLogger.DefaultLogger.Info("> Set up SrcML Service.");

            srcMLService = GetService(typeof(SSrcMLGlobalService)) as ISrcMLGlobalService;
            if(null == srcMLService) {
                Trace.WriteLine("Can not get the SrcML global service.");
            }
        }

        /// <summary>
        /// Register Visual Studio DTE events.
        /// </summary>
        private void SetUpDTEEvents() {
            SrcMLFileLogger.DefaultLogger.Info("> Register Visual Studio DTE events.");

            DTE2 dte = GetService(typeof(DTE)) as DTE2;
            if(dte != null) {
                DteEvents = dte.Events.DTEEvents;
                // Register the Visual Studio DTE event that occurs when the environment has completed initializing.
                DteEvents.OnStartupComplete += DTEStartupCompleted;
                // Register the Visual Studio DTE event that occurs when the development environment is closing.
                DteEvents.OnBeginShutdown += DTEBeginShutdown;
            }
        }

        /// <summary>
        /// Respond to the Visual Studio DTE event that occurs when the environment has completed initializing.
        /// </summary>
        private void DTEStartupCompleted() {
            SrcMLFileLogger.DefaultLogger.Info("Respond to the Visual Studio DTE event that occurs when the environment has completed initializing.");

            /*
            if(GetDte().Version.StartsWith("10")) {
                //only need to do this in VS2010, and it breaks things in VS2012
                Solution openSolution = GetOpenSolution();
                if(openSolution != null && !String.IsNullOrWhiteSpace(openSolution.FullName) && _currentMonitor == null) {
                    SolutionOpened();
                }
            }
            */

            RegisterSolutionEvents();
        }

        /// <summary>
        /// Register solution events.
        /// </summary>
        private void RegisterSolutionEvents() {
            SrcMLFileLogger.DefaultLogger.Info("> Register solution events.");

            DTE2 dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if(dte != null) {
                SolutionEvents = dte.Events.SolutionEvents;
                // Register the Visual Studio event that occurs when a solution is being opened.
                SolutionEvents.Opened += SolutionOpened;
                // Register the Visual Studio event that occurs when a solution is about to close.
                SolutionEvents.BeforeClosing += SolutionBeforeClosing;
            }

            // Queries listening clients as to whether the project can be unloaded.
            SolutionChangeListener = new SolutionChangeEventListener();
            SolutionChangeListener.OnQueryUnloadProject += () => {
                SolutionBeforeClosing();
                SolutionOpened();
            };
        }

        /// <summary>
        /// Respond to the Visual Studio event that occurs when a solution is being opened.
        /// </summary>
        private void SolutionOpened() {
            SrcMLFileLogger.DefaultLogger.Info("Respond to the Visual Studio event that occurs when a solution is being opened.");

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += new DoWorkEventHandler(RespondToSolutionOpened);
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// SrcML service starts to monitor the opened solution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void RespondToSolutionOpened(object sender, DoWorkEventArgs eventArgs) {
            SrcMLFileLogger.DefaultLogger.Info("> SrcML service starts monitoring the opened solution.");

            //srcMLService.StartMonitoring();

            bool useExistingSrcML = true;
            string src2SrcmlDir = SrcMLHelper.GetSrcMLDefaultDirectory();
            srcMLService.StartMonitoring(useExistingSrcML, src2SrcmlDir);
        }

        /// <summary>
        /// Respond to the Visual Studio event that occurs when a solution is about to close.
        /// </summary>
        private void SolutionBeforeClosing() {
            SrcMLFileLogger.DefaultLogger.Info("Respond to the Visual Studio event that occurs when a solution is about to close.");
            SrcMLFileLogger.DefaultLogger.Info("> SrcML service stops monitoring the opened solution.");

            srcMLService.StopMonitoring();
        }

        /// <summary>
        /// Respond to the Visual Studio DTE event that occurs when the development environment is closing.
        /// </summary>
        private void DTEBeginShutdown() {
            SrcMLFileLogger.DefaultLogger.Info("Respond to the Visual Studio DTE event that occurs when the development environment is closing.");

            //UnregisterSolutionEvents(); // TODO if necessary
            //UnregisterDTEEvents(); // TODO if necessary
        }

        /// <summary>
        /// Write Visual Studio's Activity Log.
        /// See %AppData%\Roaming\Microsoft\VisualStudio\11.0Exp\ActivityLog.xml
        /// </summary>
        /// <param name="str"></param>
        private void WriteActivityLog(string str) {
            if(ActivityLog != null) {
                int hr = ActivityLog.LogEntry((UInt32)__ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION, this.ToString(), str);
            }
        }
    }
}
