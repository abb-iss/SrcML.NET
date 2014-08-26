/******************************************************************************
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ABB.SrcML.Utilities;
using log4net;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// Adapted from https://github.com/shanselman/RestoreAfterReloadVSIX
    /// IVsSolutionEvents: Listening interface that monitors any notifications of changes to the solution.
    /// </summary>
    public class SolutionChangeEventListener : IVsSolutionEvents {
        /// <summary>
        /// IVsSolution: Provides top-level manipulation or maintenance of the solution.
        /// solutionEventsCookie is used in registering/unregistering events.
        /// </summary>
        private IVsSolution solution;
        private uint solutionEventsCookie;

        /// <summary>
        /// TODO
        /// </summary>
        public event Action OnQueryUnloadProject;
        public event Action OnAfterOpenProject;

        /// <summary>
        /// Constructor.
        /// Register Solution events.
        /// </summary>
        public SolutionChangeEventListener() {
            InitNullEvents();

            solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if(solution != null) {
                solution.AdviseSolutionEvents(this, out solutionEventsCookie);
            }
        }

        /// <summary>
        /// Initialize null events.
        /// </summary>
        private void InitNullEvents() {
            OnQueryUnloadProject += () => { };

            OnAfterOpenProject += () => { };
        }

        #region IVsSolutionEvents Members
        /// <summary>
        /// Notifies listening clients that a solution has been closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnAfterCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project has been loaded.
        /// </summary>
        /// <param name="pStubHierarchy"></param>
        /// <param name="pRealHierarchy"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnAfterLoadProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project has been opened.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fAdded"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnAfterOpenProject()");
            OnAfterOpenProject();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the solution has been opened.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <param name="fNewSolution"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnAfterOpenSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project is about to be closed.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fRemoved"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnBeforeCloseProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the solution is about to be closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnBeforeCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Notifies listening clients that the project is about to be unloaded.
        /// </summary>
        /// <param name="pRealHierarchy"></param>
        /// <param name="pStubHierarchy"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            ////SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnBeforeUnloadProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the project can be closed.
        /// </summary>
        /// <param name="pHierarchy"></param>
        /// <param name="fRemoving"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnQueryCloseProject()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the solution can be closed.
        /// </summary>
        /// <param name="pUnkReserved"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnQueryCloseSolution()");
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Queries listening clients as to whether the project can be unloaded.
        /// </summary>
        /// <param name="pRealHierarchy"></param>
        /// <param name="pfCancel"></param>
        /// <returns></returns>
        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            SrcMLFileLogger.DefaultLogger.Info("=======> Triggered IVsSolutionEvents.OnQueryUnloadProject()");
            OnQueryUnloadProject();
            return VSConstants.S_OK;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Dispose.
        /// Unregister Solution events.
        /// </summary>
        public void Dispose() {
            if(solution != null && solutionEventsCookie != 0) {
                GC.SuppressFinalize(this);
                solution.UnadviseSolutionEvents(solutionEventsCookie);
                OnQueryUnloadProject = null;

                OnAfterOpenProject = null;


                solutionEventsCookie = 0;
                solution = null;
            }
        }
        #endregion
    }
}