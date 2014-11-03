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
using System.Collections.Concurrent;
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
        public CompleteWorkingSet() : this(null, Task.Factory) { }
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
        public override void Initialize() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            bool workingSetChanged = false;
            try {
                IsUpdating = true;
                
                var mergedScopeFromArchive = ReadArchive();
                if(null != mergedScopeFromArchive) {
                    GlobalScopeManager scopeManager;
                    if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                        try {
                            scopeManager.GlobalScope = mergedScopeFromArchive;
                            workingSetChanged = true;
                        } finally {
                            ReleaseWriteLock();
                        }
                    }
                }
            } finally {
                IsUpdating = false;
            }
            
            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Asynchronously initialize the working set by reading the entire archive into one merged scope
        /// </summary>
        /// <returns></returns>
        public override Task InitializeAsync() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            IsUpdating = true;

            return ReadArchiveAsync().ContinueWith((t) => {
                bool globalScopeChanged = false;

                try {
                    if(null != t.Result) {
                        GlobalScopeManager scopeManager;
                        if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                            try {
                                scopeManager.GlobalScope = t.Result;
                                globalScopeChanged = true;
                            } finally {
                                ReleaseWriteLock();
                            }
                        }
                    }
                } finally {
                    IsUpdating = false;
                    if(globalScopeChanged) {
                        OnChanged(new EventArgs());
                    }
                }
            });
        }

        /// <summary>
        /// Loads all of the files in the archive into a merged scope
        /// </summary>
        /// <returns>A global scope for the archive</returns>
        protected NamespaceDefinition ReadArchive() {
            NamespaceDefinition globalScope = null;
            IsUpdating = true;
                
            var allData = from fileName in Archive.GetFiles()
                            select Archive.GetData(fileName);
            foreach(var data in allData) {
                globalScope = (null == globalScope ? data : globalScope.Merge(data));
            }

            return globalScope;
        }

        protected Task<NamespaceDefinition> ReadArchiveAsync() {
            BlockingCollection<NamespaceDefinition> fileScopes = new BlockingCollection<NamespaceDefinition>();

            var readTask = Factory.StartNew(() => {
                var options = new ParallelOptions() { TaskScheduler = Factory.Scheduler };
                Parallel.ForEach(Archive.GetFiles(), options, (fileName) => {
                    var data = Archive.GetData(fileName);
                    if(null != data) {
                        fileScopes.Add(data);
                    }
                });
                fileScopes.CompleteAdding();
            });

            var mergeTask = Factory.StartNew(() => {
                NamespaceDefinition globalScope = null;
                foreach(var fileScope in fileScopes.GetConsumingEnumerable()) {
                    globalScope = (null == globalScope ? fileScope : globalScope.Merge(fileScope));
                }
                return globalScope;
            });

            return mergeTask;
        }
    }
}
