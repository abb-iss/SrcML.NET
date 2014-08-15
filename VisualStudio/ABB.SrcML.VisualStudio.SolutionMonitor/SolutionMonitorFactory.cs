/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Diagnostics.Contracts;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace ABB.SrcML.VisualStudio.SolutionMonitor {
    /// <summary>
    /// Factory class for creating the instance of solution monitor.
    /// </summary>
    public class SolutionMonitorFactory {
        /// <summary>
        /// Create an instance of solution monitor.
        /// </summary>
        /// <param name="SrcMLServiceDirectory"></param>
        /// <param name="lastModifiedArchive"></param>
        /// <param name="CurrentSrcMLArchive"></param>
        /// <returns></returns>
        public static SolutionMonitor CreateMonitor(string SrcMLServiceDirectory, AbstractArchive lastModifiedArchive, params AbstractArchive[] CurrentSrcMLArchive) {
            var openSolution = GetOpenSolution();
            return CreateMonitor(openSolution, SrcMLServiceDirectory, lastModifiedArchive, CurrentSrcMLArchive);
        }

        /// <summary>
        /// Create an instance of solution monitor.
        /// </summary>
        /// <param name="openSolution"></param>
        /// <param name="SrcMLServiceDirectory"></param>
        /// <param name="lastModifiedArchive"></param>
        /// <param name="CurrentSrcMLArchive"></param>
        /// <returns></returns>
        private static SolutionMonitor CreateMonitor(Solution openSolution, string SrcMLServiceDirectory, AbstractArchive lastModifiedArchive, params AbstractArchive[] CurrentSrcMLArchive) {
            Contract.Requires(openSolution != null, "A solution must be open");

            var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution), SrcMLServiceDirectory, lastModifiedArchive, CurrentSrcMLArchive);
            return currentMonitor;
        }

        /// <summary>
        /// Get the open solution.
        /// </summary>
        /// <returns></returns>
        public static Solution GetOpenSolution() {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if(dte != null) {
                var openSolution = dte.Solution;
                return openSolution;
            } else {
                return null;
            }
        }
    }
}