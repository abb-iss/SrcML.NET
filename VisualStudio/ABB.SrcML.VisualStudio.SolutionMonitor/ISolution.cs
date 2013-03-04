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
using System.Linq;
using System.Text;
using EnvDTE;

namespace ABB.SrcML.VisualStudio.SolutionMonitor {
    /// <summary>
    /// Wrapper of the Visual Studio EnvDTE.Solution interface.
    /// </summary>
    public class SolutionWrapper {
        /// <summary>
        /// Locates an item in a project.
        /// </summary>
        /// <param name="name">The name of the project item.</param>
        /// <returns></returns>
        public virtual ProjectItem FindProjectItem(string name) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a collection of the projects currently in the solution.
        /// </summary>
        /// <returns></returns>
        public virtual Projects getProjects() {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an instance of a Visual Studio solution.
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        public static SolutionWrapper Create(Solution openSolution) {
            return new StandardSolutionWrapper(openSolution);
        }

        /// <summary>
        /// Gets the full path and name of the object's file.
        /// </summary>
        /// <returns></returns>
        public virtual string GetSolutionFullName() {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Standard wrapper of the Visual Studio EnvDTE.Solution interface.
    /// </summary>
    public class StandardSolutionWrapper : SolutionWrapper {
        private Solution _mySolution;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="s"></param>
        public StandardSolutionWrapper(Solution s) {
            _mySolution = s;
        }

        /// <summary>
        /// Locates an item in a project.
        /// </summary>
        /// <param name="name">The name of the project item.</param>
        /// <returns></returns>
        public override ProjectItem FindProjectItem(string name) {
            return _mySolution.FindProjectItem(name);
        }

        /// <summary>
        /// Gets a collection of the projects currently in the solution.
        /// </summary>
        /// <returns></returns>
        public override Projects getProjects() {
            return _mySolution.Projects;
        }

        /// <summary>
        /// Gets the full path and name of the object's file.
        /// </summary>
        /// <returns></returns>
        public override string GetSolutionFullName() {
            return _mySolution.FullName;
        }
    }
}
