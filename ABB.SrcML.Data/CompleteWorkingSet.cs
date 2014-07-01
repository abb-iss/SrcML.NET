/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The complete working set mirrors the internal <see cref="AbstractWorkingSet.Archive"/>.
    /// Use of this working set should be done like this:
    /// 
    /// <example><code>
    /// var workingSet = new CompleteWorkingSet(archive);
    /// workingSet.Changed += (o,e) => { };
    /// workingSet.Initialize();
    /// workingSet.StartMonitoring();
    /// </code></example>
    /// </summary>
    public class CompleteWorkingSet : AbstractWorkingSet {
        /// <summary>
        /// Creates a new complete working set object
        /// </summary>
        /// <param name="archive">The data archive to monitor</param>
        public CompleteWorkingSet(DataArchive archive) : this(archive, Task.Factory) { }

        /// <summary>
        /// Creates a new complete working set object
        /// </summary>
        /// <param name="archive">The data archive to monitor</param>
        /// <param name="factory">The task factory for asynchronous methods</param>
        public CompleteWorkingSet(DataArchive archive, TaskFactory factory) : base(archive, factory) { }

        /// <summary>
        /// Initialize the working set by reading the entire archive into one merged scope
        /// </summary>
        public void Initialize() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            bool workingSetChanged = false;
            var mergedScopeFromArchive = ReadArchive();

            if(null != mergedScopeFromArchive) {
                NamespaceDefinition globalScope;
                if(TryObtainWriteLock(Timeout.Infinite, out globalScope)) {
                    try {
                        globalScope = mergedScopeFromArchive;
                        workingSetChanged = true;
                    } finally {
                        ReleaseWriteLock();
                    }
                }
            }
            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Asynchronously initialize the working set by reading the entire archive into one merged scope
        /// </summary>
        /// <returns></returns>
        public Task InitializeAsync() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            return Factory.StartNew(() => {
                var mergedScopeFromArchive = ReadArchive();

                if(null != mergedScopeFromArchive) {
                    NamespaceDefinition globalScope;
                    if(TryObtainWriteLock(Timeout.Infinite, out globalScope)) {
                        try {
                            globalScope = mergedScopeFromArchive;
                            return true;
                        } finally {
                            ReleaseWriteLock();
                        }
                    }
                }
                return false;
            }).ContinueWith((t) => {
                if(t.Result) {
                    OnChanged(new EventArgs());
                }
            });
        }

        /// <summary>
        /// Loads all of the files in the archive into a merged scope
        /// </summary>
        /// <returns>A global scope for the archive</returns>
        protected NamespaceDefinition ReadArchive() {
            NamespaceDefinition globalScope = null;
            var allData = from fileName in Archive.GetFiles()
                          select Archive.GetData(fileName);
            foreach(var data in allData) {
                globalScope = (null == globalScope ? data : globalScope.Merge(data));
            }

            return globalScope;
        }
    }
}
