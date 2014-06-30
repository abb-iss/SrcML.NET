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

namespace ABB.SrcML.Data {
    /// <summary>
    /// Working set objects track a given <see cref="Archive">data archive</see>. They maintain a merged scope based on files
    /// in <see cref="Archive"/>. Sub-classes should maintain a subset of the <see cref="Archive"/> files  for use by their clients
    /// Working set implementations should be changed by doing the following:
    /// 
    /// <list type="number">
    /// <item><description>Obtain a write lock via <see cref="TryObtainWriteLock"/></description></item>
    /// <item><description></description>Modify the method possibly via calls to <see cref="TryAddOrUpdateFile"/>,
    /// <see cref="TryRemoveFile"/>, and <see cref="ContainsFile"/></item>
    /// <item><description>Release the write lock <see cref="ReleaseWriteLock"/></description></item>
    /// <item><description>If the working set has changed, call <see cref="OnChanged"/> to notify subscribed clients</description></item>
    /// </list>
    /// 
    /// Clients that use a working set can obtain the a <see cref="NamespaceDefinition.Merge(NamespaceDefinition)">merged scope</see> for
    /// the working set by calling <see cref="TryObtainReadLock"/> and <see cref="ReleaseReadLock"/>.
    /// </summary>
    public abstract class AbstractWorkingSet : IDisposable {
        private NamespaceDefinition _globalScope;
        private ReaderWriterLockSlim _globalScopeLock;
        private bool _disposed;

        /// <summary>
        /// Event that indicates this working set has changed
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Data archive for this working set
        /// </summary>
        public DataArchive Archive { get; private set; }

        private AbstractWorkingSet() { }

        /// <summary>
        /// Creates a new working set object
        /// </summary>
        /// <param name="archive">The archive to monitor</param>
        protected AbstractWorkingSet(DataArchive archive) {
            Archive = archive;
            _disposed = false;
            _globalScope = null;
            _globalScopeLock = new ReaderWriterLockSlim();
        }

        #region global scope modification
        /// <summary>
        /// Adds the specified file from the data set. If the file is not present in the archive, then nothing happens
        /// </summary>
        /// <param name="sourceFileName">the source file to add</param>
        public void AddOrUpdateFile(string sourceFileName) {
            if(_disposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            NamespaceDefinition globalScope;
            
            if(TryObtainWriteLock(Timeout.Infinite, out globalScope)) {
                try {
                    workingSetChanged = TryAddOrUpdateFile(globalScope, sourceFileName);
                } finally {
                    ReleaseWriteLock();
                }
            }

            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Clears the data in this working set
        /// </summary>
        public virtual void Clear() {
            if(_disposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            NamespaceDefinition globalScope;

            if(TryObtainWriteLock(Timeout.Infinite, out globalScope)) {
                try {
                    if(null != globalScope) {
                        globalScope = null;
                        workingSetChanged = true;
                    }
                } finally {
                    ReleaseWriteLock();
                }
            }
            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Removes <paramref name="sourceFileName"/> from the working set.
        /// If the file does not exist, nothing is done.
        /// </summary>
        /// <param name="sourceFileName">The source file to remove</param>
        public virtual void RemoveFile(string sourceFileName) {
            if(_disposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            NamespaceDefinition globalScope;

            if(TryObtainWriteLock(Timeout.Infinite, out globalScope)) {
                try {
                    workingSetChanged = TryRemoveFile(globalScope, sourceFileName);
                } finally {
                    ReleaseWriteLock();
                }
            }

            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }
        #endregion global scope modification

        #region global scope access
        /// <summary>
        /// Releases the read lock
        /// </summary>
        public void ReleaseReadLock() {
            if(_disposed) { throw new ObjectDisposedException(null); }
            _globalScopeLock.ExitReadLock();
        }

        /// <summary>
        /// Releases the write lock
        /// </summary>
        protected void ReleaseWriteLock() {
            if(_disposed) { throw new ObjectDisposedException(null); }
            _globalScopeLock.ExitWriteLock();
        }

        /// <summary>
        /// Gets a read lock for this working set. If timeout is exceeded, then false is returned and <paramref name="globalScope"/> will be null.
        /// If the read lock is obtained, true is returned and <paramref name="globalScope"/> will contain the global scope object.
        /// </summary>
        /// <param name="millisecondsTimeout">the timeout</param>
        /// <param name="globalScope">out parameter for the global scope</param>
        /// <returns>True if the read lock was obtained; false otherwise</returns>
        public bool TryObtainReadLock(int millisecondsTimeout, out NamespaceDefinition globalScope) {
            if(_disposed) { throw new ObjectDisposedException(null); }
            if(_globalScopeLock.TryEnterReadLock(millisecondsTimeout)) {
                globalScope = this._globalScope;
                return true;
            }
            globalScope = null;
            return false;
        }

        /// <summary>
        /// Gets a write lock for this working set. If timeout is exceeded, then false is returned and <paramref name="globalScope"/> will be null.
        /// If the write lock is obtained, true is returned and <paramref name="globalScope"/> will contain the global scope object.
        /// </summary>
        /// <param name="millisecondsTimeout">the timeout</param>
        /// <param name="globalScope">out parameter for the global scope</param>
        /// <returns>True if the write lock was obtained; false otherwise</returns>
        protected bool TryObtainWriteLock(int millisecondsTimeout, out NamespaceDefinition globalScope) {
            if(_disposed) { throw new ObjectDisposedException(null); }

            if(_globalScopeLock.TryEnterWriteLock(millisecondsTimeout)) {
                globalScope = _globalScope;
                return true;
            }
            globalScope = null;
            return false;
        }

        #endregion global scope access

        /// <summary>
        /// Dispose of this working set. The methods on this class will throw an ObjectDisposedException if they are called after Dispose is called.
        /// This will also call <see cref="AbstractArchive.Dispose()"/> on the <see cref="Archive"/>.
        /// </summary>
        public void Dispose() {
            if(!_disposed) {
                Clear();
                Archive.Dispose();
                _disposed = true;
                Changed = null;
                _globalScopeLock.Dispose();
            }
        }

        /// <summary>
        /// Checks to see if the given <paramref name="globalScope"/> object contains <paramref name="sourceFileName"/>.
        /// </summary>
        /// <param name="globalScope">The global scope object</param>
        /// <param name="sourceFileName">The source file to check for</param>
        /// <returns>True if <paramref name="globalScope"/> contains <paramref name="sourceFileName"/></returns>
        protected bool ContainsFile(NamespaceDefinition globalScope, string sourceFileName) {
            if(_disposed) { throw new ObjectDisposedException(null); }
            if(null == sourceFileName) { throw new ArgumentNullException("sourceFileName"); }

            return globalScope.Locations.Any(l => l.SourceFileName.Equals(sourceFileName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Adds or updates <paramref name="sourceFileName"/> in the given <paramref name="globalScope"/>.
        /// </summary>
        /// <param name="globalScope">The global scope object</param>
        /// <param name="sourceFileName">The source file to check for</param>
        /// <returns>True if <paramref name="globalScope"/> was modified; false otherwise</returns>
        protected bool TryAddOrUpdateFile(NamespaceDefinition globalScope, string sourceFileName) {
            if(_disposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            bool workingSetChanged = false;
            var data = Archive.GetData(sourceFileName);

            if(null != data) {
                if(null == globalScope) {
                    globalScope = data;
                } else {
                    TryRemoveFile(globalScope, sourceFileName);
                    globalScope = globalScope.Merge(data);
                }
                workingSetChanged = true;
            }

            return workingSetChanged;
        }

        /// <summary>
        /// Removes <paramref name="sourceFileName"/> from <paramref name="globalScope"/>
        /// </summary>
        /// <param name="globalScope">The global scope object</param>
        /// <param name="sourceFileName">the source file to remove</param>
        /// <returns>True if <paramref name="globalScope"/> was modified; false otherwise</returns>
        protected bool TryRemoveFile(NamespaceDefinition globalScope, string sourceFileName) {
            if(_disposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;

            if(ContainsFile(globalScope, sourceFileName)) {
                globalScope.RemoveFile(sourceFileName);
                workingSetChanged = true;
            }

            return workingSetChanged;
        }

        /// <summary>
        /// Raises the <see cref="Changed"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        protected virtual void OnChanged(EventArgs e) {
            if(_disposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = Changed;
            if(null != handler) {
                handler(this, e);
            }
        }
    }
}
