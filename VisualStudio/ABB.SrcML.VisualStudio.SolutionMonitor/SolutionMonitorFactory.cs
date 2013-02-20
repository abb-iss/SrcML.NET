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
    /// This class was adapted from Sando.
    /// Now most likely this class would not be needed any more in SrcML.NET. Sando would maintain its own SolutionMonitorFactory class.
    /// However for SrcML.NET Service, this class seems to be useful.
    /// </summary>
    public class SolutionMonitorFactory {
        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// </summary>
        /// <returns></returns>
        public static SolutionMonitor CreateMonitor(List<AbstractArchive> listOfArchives) {
            var openSolution = GetOpenSolution();
            return CreateMonitor(listOfArchives, openSolution);
        }

        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        private static SolutionMonitor CreateMonitor(List<AbstractArchive> listOfArchives, Solution openSolution) {
            Contract.Requires(openSolution != null, "A solution must be open");

            var currentMonitor = new SolutionMonitor(listOfArchives, SolutionWrapper.Create(openSolution));
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