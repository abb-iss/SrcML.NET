/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.VisualStudio.SrcMLService {

    public class SourceMonitor : DirectoryScanningMonitor, IVsRunningDocTableEvents {
        private IVsRunningDocumentTable DocumentTable;
        private uint DocumentTableItemId;

        public SourceMonitor(Solution solution, double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : base(new List<string>() { GetSolutionPath(solution) }, scanInterval, baseDirectory, defaultArchive, otherArchives) { }

        public SourceMonitor(ICollection<string> directoriesToMonitor, double scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : base(directoriesToMonitor, scanInterval, baseDirectory, defaultArchive, otherArchives) { }

        public SourceMonitor(int scanInterval, string baseDirectory, IArchive defaultArchive, params IArchive[] otherArchives)
            : base(scanInterval, baseDirectory, defaultArchive, otherArchives) { }

        public Solution MonitoredSolution { get; private set; }

        public static string GetSolutionPath(Solution solution) {
            if(null == solution) {
                throw new ArgumentNullException("solution");
            }

            return Path.GetFullPath(Path.GetDirectoryName(solution.FullName));
        }

        /// <summary>
        /// Update the
        /// <paramref name="docCookie">file</paramref> if the
        /// <paramref name="grfAttribs">changed attribute</paramref> is
        /// <see cref="__VSRDTATTRIB.RDTA_DocDataReloaded"/>.
        /// </summary>
        /// <param name="docCookie">The cookie for the changed document</param>
        /// <param name="grfAttribs">The document attributes</param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK"/>. If it fails,
        /// it returns an error code.</returns>
        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) {
            if(grfAttribs == (uint) __VSRDTATTRIB.RDTA_DocDataReloaded) {
                return UpdateVisualStudioDocument(docCookie);
            }
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="pFrame">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="dwRDTLockType">Unused</param>
        /// <param name="dwReadLocksRemaining">Unused</param>
        /// <param name="dwEditLocksRemaining">Unused</param>
        ///<returns>returns <see cref="VSConstants.S_OK"/>.</returns>
        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Respond to visual studio file saves by updating the monitor if the file
        /// <see cref="DirectoryScanningMonitor.IsMonitoringFile(string)"/>
        /// </summary>
        /// <param name="docCookie"></param>
        /// <returns>If the method succeeds, it returns <see cref="VSConstants.S_OK"/>. If it fails,
        /// it returns an error code.</returns>
        public int OnAfterSave(uint docCookie) {
            return UpdateVisualStudioDocument(docCookie);
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="fFirstShow">Unused</param>
        /// <param name="pFrame">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Ignored.
        /// </summary>
        /// <param name="docCookie">Unused</param>
        /// <param name="dwRDTLockType">Unused</param>
        /// <param name="dwReadLocksRemaining">Unused</param>
        /// <param name="dwEditLocksRemaining">Unused</param>
        /// <returns>returns see cref="VSConstants.S_OK"/>.</returns>
        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) {
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Subscribe to the visual studio <see cref="IVsRunningDocumentTable"/> and then
        /// <see cref="DirectoryScanningMonitor.StartMonitoring()">start monitoring the
        /// directories</see>.
        /// </summary>
        public override void StartMonitoring() {
            SubscribeToEvents();
            base.StartMonitoring();
        }

        /// <summary>
        /// Unsubscribe from the <see cref="IVsRunningDocumentTable"/> and then
        /// <see cref="DirectoryScanningMonitor.StopMonitoring()">stop monitoring the
        /// directories.</see>
        /// </summary>
        public override void StopMonitoring() {
            UnsubscribeFromEvents();
            base.StopMonitoring();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }

        private void SubscribeToEvents() {
            DocumentTable = Package.GetGlobalService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;
            if(DocumentTable != null) {
                int result = DocumentTable.AdviseRunningDocTableEvents(this, out DocumentTableItemId);
                ErrorHandler.ThrowOnFailure(result);
            }
        }

        private void UnsubscribeFromEvents() {
            if(DocumentTable != null) {
                int result = DocumentTable.UnadviseRunningDocTableEvents(DocumentTableItemId);
                ErrorHandler.Succeeded(result);
            }
        }

        private int UpdateVisualStudioDocument(uint docCookie) {
            uint flags, readingLocks, editLocks, documentId;
            string filePath;
            IVsHierarchy hierarchy;
            IntPtr documentData;
            int status = DocumentTable.GetDocumentInfo(docCookie, out flags, out readingLocks, out editLocks, out filePath, out hierarchy, out documentId, out documentData);
            if(status == VSConstants.S_OK && IsMonitoringFile(filePath)) {
                UpdateFile(filePath);
            }
            return status;
        }
    }
}