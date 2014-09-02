/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The abstract working set factory creates and sets up <see cref="AbstractWorkingSet"/> objects via the
    /// <see cref="CreateWorkingSet(string,DataArchive,TaskFactory)"/> method (and similar methods)
    /// </summary>
    public abstract class AbstractWorkingSetFactory {
        /// <summary>
        /// Creates a new abstract working set factory
        /// </summary>
        protected AbstractWorkingSetFactory() { }

        /// <summary>
        /// Creates and configures a new working set object
        /// </summary>
        /// <param name="basePath">The base path to store any data required by the working set</param>
        /// <param name="dataArchive">The data archive that this working set is based on</param>
        /// <returns>A new working set</returns>
        public AbstractWorkingSet CreateWorkingSet(string basePath, DataArchive dataArchive) { return CreateWorkingSet(basePath, dataArchive, Task.Factory); }

        /// <summary>
        /// Creates and configures a new working set object
        /// </summary>
        /// <param name="basePath">The base directory to store any data required by the working set</param>
        /// <param name="dataArchive">The data archive that this working set is based on</param>
        /// <param name="taskFactory">The task factory to use for any working set tasks</param>
        /// <returns>A new working set</returns>
        public abstract AbstractWorkingSet CreateWorkingSet(string basePath, DataArchive dataArchive, TaskFactory taskFactory);
    }

    /// <summary>
    /// Creates a new working set object of type <typeparamref name="TWorkingSetFactory"/>. For this factory the base directory path is unused.
    /// </summary>
    /// <typeparam name="TWorkingSetFactory">The working set type to create</typeparam>
    public class DefaultWorkingSetFactory<TWorkingSetFactory> : AbstractWorkingSetFactory where TWorkingSetFactory : AbstractWorkingSet, new() {
        /// <summary>
        /// Creates and configures a new working set object
        /// </summary>
        /// <param name="basePath">The base directory to store any data required by the working set. This is unused by this factory</param>
        /// <param name="dataArchive">The data archive that this working set is based on</param>
        /// <param name="taskFactory">The task factory to use for any working set tasks</param>
        /// <returns>A new working set</returns>
        public override AbstractWorkingSet CreateWorkingSet(string basePath, DataArchive dataArchive, TaskFactory taskFactory) {
            TWorkingSetFactory factory = new TWorkingSetFactory();
            factory.Archive = dataArchive;
            factory.Factory = taskFactory;
            return factory;
        }
    }
}
