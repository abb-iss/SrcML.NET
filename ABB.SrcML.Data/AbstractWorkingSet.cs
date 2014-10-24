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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Working set objects track a given <see cref="Archive">data archive</see>. They maintain a merged scope based on files
    /// in <see cref="Archive"/>. Sub-classes should maintain a subset of the <see cref="Archive"/> files  for use by their clients
    /// Working set implementations should be changed by doing the following:
    /// 
    /// <list type="number">
    /// <item><description>Obtain a write lock via <see cref="TryObtainWriteLock"/></description></item>
    /// <item><description></description>Modify the method possibly via calls to <see cref="TryAddOrUpdateFile"/>,
    /// <see cref="TryRemoveFile"/>, and <see cref="ContainsFile(NamespaceDefinition,string)"/></item>
    /// <item><description>Release the write lock <see cref="ReleaseWriteLock"/></description></item>
    /// <item><description>If the working set has changed, call <see cref="OnChanged"/> to notify subscribed clients</description></item>
    /// </list>
    /// 
    /// Clients that use a working set can obtain the a <see cref="NamespaceDefinition.Merge(NamespaceDefinition)">merged scope</see> for
    /// the working set by calling <see cref="TryObtainReadLock"/> and <see cref="ReleaseReadLock"/>.
    /// </summary>
    public abstract class AbstractWorkingSet : IDisposable {
        private GlobalScopeManager _globalScopeManager;
        private ReaderWriterLockSlim _globalScopeLock;
        private bool _isMonitoring;
        private bool _isUpdating;
        
        /// <summary>
        /// Event that indicates this working set has changed
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Event that indicates that this working set is not monitoring <see cref="Archive"/>
        /// </summary>
        public event EventHandler MonitoringStopped;

        /// <summary>
        /// Event that indicates that this working has started monitoring <see cref="Archive"/>
        /// </summary>
        public event EventHandler MonitoringStarted;
        
        /// <summary>
        /// Event that indicates that an update has completed
        /// </summary>
        public event EventHandler UpdateCompleted;

        /// <summary>
        /// Event that indicates that an update has started
        /// </summary>
        public event EventHandler UpdateStarted;

        /// <summary>
        /// Data archive for this working set
        /// </summary>
        public DataArchive Archive { get; set; }

        /// <summary>
        /// The task factory to use for asynchronous methods
        /// </summary>
        public TaskFactory Factory { get; set; }

        /// <summary>
        /// Returns true if <see cref="Dispose"/> has been called
        /// </summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// True if the working set is currently monitoring <see cref="Archive"/>; false, otherwise
        /// </summary>
        public bool IsMonitoring {
            get { return _isMonitoring; }
            protected set { SetBooleanField(ref _isMonitoring, value, OnMonitoringStarted, OnMonitoringStopped); }
        }

        /// <summary>
        /// True if this working set is currently updating; false otherwise
        /// </summary>
        public bool IsUpdating {
            get { return _isUpdating; }
            protected set { SetBooleanField(ref _isUpdating, value, OnUpdateStarted, OnUpdateCompleted); }
        }
        /// <summary>
        /// If true, this working set will use asynchronous methods in <see cref="Archive_FileChanged"/>. By default, this is false.
        /// </summary>
        public bool UseAsynchronousMethods { get; set; }

        /// <summary>
        /// Creates a new working set object
        /// </summary>
        protected AbstractWorkingSet() : this(null, Task.Factory) { }

        /// <summary>
        /// Creates a new working set object
        /// </summary>
        /// <param name="archive">The archive to monitor</param>
        /// <param name="factory">The task factory</param>
        protected AbstractWorkingSet(DataArchive archive, TaskFactory factory) {
            Archive = archive;
            Factory = factory;
            IsDisposed = false;
            UseAsynchronousMethods = false;
            _globalScopeManager = new GlobalScopeManager();
            _globalScopeLock = new ReaderWriterLockSlim();
        }

        /// <summary>
        /// Checks to see if the working set contains <paramref name="sourceFileName"/>. This calls 
        /// <see cref="ContainsFile(string,int)"/> with <see cref="Timeout.Infinite"/> for the timeout.
        /// </summary>
        /// <param name="sourceFileName">The source file name to search for</param>
        /// <returns>True if the working set contains <paramref name="sourceFileName"/>, False if not</returns>
        public virtual bool ContainsFile(string sourceFileName) {
            return ContainsFile(sourceFileName, Timeout.Infinite);
        }

        /// <summary>
        /// Checks to see if the working set contains <paramref name="sourceFileName"/>
        /// </summary>
        /// <param name="sourceFileName">The source file name to search for</param>
        /// <param name="readLockTimeout">The timeout in milliseconds to wait for the read lock</param>
        /// <returns>True if the working set contains <paramref name="sourceFileName"/>, False otherwise</returns>
        public virtual bool ContainsFile(string sourceFileName, int readLockTimeout) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(String.IsNullOrWhiteSpace(sourceFileName)) { throw new ArgumentException("Argument is null or empty", "sourceFileName"); }

            NamespaceDefinition globalScope;

            if(TryObtainReadLock(readLockTimeout, out globalScope)) {
                try {
                    return ContainsFile(globalScope, sourceFileName);
                } finally {
                    ReleaseReadLock();
                }
            }
            throw new TimeoutException();
        }

        /// <summary>
        /// Sets up the working set
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Sets up the working set asynchronously
        /// </summary>
        /// <returns>The initialization task</returns>
        public abstract Task InitializeAsync();

        /// <summary>
        /// Starts monitoring <see cref="Archive"/> by responding to
        /// <see cref="AbstractArchive.FileChanged"/> with <see cref="Archive_FileChanged"/>.
        /// </summary>
        public virtual void StartMonitoring() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            SubscribeToArchive();
            IsMonitoring = true;
        }

        /// <summary>
        /// Stops monitoring <see cref="Archive"/>
        /// </summary>
        public virtual void StopMonitoring() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            UnsubscribeFromArchive();
            IsMonitoring = false;
        }

        #region global scope modification
        /// <summary>
        /// Adds the specified file from the data set. If the file is not present in the archive, then nothing happens
        /// </summary>
        /// <param name="sourceFileName">the source file to add</param>
        public virtual void AddOrUpdateFile(string sourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            GlobalScopeManager scopeManager;
            
            if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                try {
                    workingSetChanged = TryAddOrUpdateFile(scopeManager, sourceFileName);
                } finally {
                    ReleaseWriteLock();
                }
            }

            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Adds the specified file from the data set. If the file is not present in the archive, then nothing happens
        /// </summary>
        /// <param name="sourceFileName">the source file to add</param>
        /// <returns>A task for this file update</returns>
        public virtual Task AddOrUpdateFileAsync(string sourceFileName) {
            return Factory.StartNew(() => AddOrUpdateFile(sourceFileName));
        }

        /// <summary>
        /// Clears the data in this working set
        /// </summary>
        public virtual void Clear() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            GlobalScopeManager scopeManager;

            if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                try {
                    if(null != scopeManager) {
                        scopeManager.GlobalScope = null;
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
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            GlobalScopeManager scopeManager;

            if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                try {
                    workingSetChanged = TryRemoveFile(scopeManager, sourceFileName);
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
        /// <returns>A task for this file deletion</returns>
        public virtual Task RemoveFileAsync(string sourceFileName) {
            return Factory.StartNew(() => RemoveFile(sourceFileName));
        }

        /// <summary>
        /// Renames <paramref name="oldSourceFileName"/> to <paramref name="newSourceFileName"/>
        /// </summary>
        /// <param name="oldSourceFileName">the old file name to be removed</param>
        /// <param name="newSourceFileName">the new file name to be added</param>
        public virtual void RenameFile(string oldSourceFileName, string newSourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            bool workingSetChanged = false;
            GlobalScopeManager scopeManager;

            if(TryObtainWriteLock(Timeout.Infinite, out scopeManager)) {
                try {
                    workingSetChanged = TryRenameFile(scopeManager, oldSourceFileName, newSourceFileName);
                } finally {
                    ReleaseWriteLock();
                }
            }

            if(workingSetChanged) {
                OnChanged(new EventArgs());
            }
        }

        /// <summary>
        /// Renames <paramref name="oldSourceFileName"/> to <paramref name="newSourceFileName"/>
        /// </summary>
        /// <param name="oldSourceFileName">the old file name to be removed</param>
        /// <param name="newSourceFileName">the new file name to be added</param>
        /// <returns>A task for this file rename</returns>
        public virtual Task RenameFileAsync(string oldSourceFileName, string newSourceFileName) {
            return Factory.StartNew(() => RenameFile(oldSourceFileName, newSourceFileName));
        }
        #endregion global scope modification

        #region global scope access
        /// <summary>
        /// Releases the read lock
        /// </summary>
        public void ReleaseReadLock() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            _globalScopeLock.ExitReadLock();
        }

        /// <summary>
        /// Releases the write lock
        /// </summary>
        protected void ReleaseWriteLock() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
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
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(_globalScopeLock.TryEnterReadLock(millisecondsTimeout)) {
                globalScope = this._globalScopeManager.GlobalScope;
                return true;
            }
            globalScope = null;
            return false;
        }

        /// <summary>
        /// Gets a write lock for this working set. If timeout is exceeded, then false is returned and <paramref name="scopeManager"/> will be null.
        /// If the write lock is obtained, true is returned and <paramref name="scopeManager"/> will contain the internal scope manager for this object.
        /// </summary>
        /// <param name="millisecondsTimeout">the timeout</param>
        /// <param name="scopeManager">out parameter for the global scope manager</param>
        /// <returns>True if the write lock was obtained; false otherwise</returns>
        protected bool TryObtainWriteLock(int millisecondsTimeout, out GlobalScopeManager scopeManager) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            if(_globalScopeLock.TryEnterWriteLock(millisecondsTimeout)) {
                scopeManager = _globalScopeManager;
                return true;
            }
            scopeManager = null;
            return false;
        }

        #endregion global scope access

        /// <summary>
        /// Dispose of this working set. The methods on this class will throw an ObjectDisposedException if they are called after Dispose is called.
        /// This will also call <see cref="AbstractArchive.Dispose()"/> on the <see cref="Archive"/>.
        /// </summary>
        public void Dispose() {
            if(!IsDisposed) {
                StopMonitoring();
                _globalScopeManager.GlobalScope = null;
                _globalScopeManager = null;

                if(null != Archive) {
                    Archive.Dispose();
                }

                IsDisposed = true;
                Changed = null;
                _globalScopeLock.Dispose();
            }
        }

        /// <summary>
        /// Responds to <see cref="AbstractArchive.FileChanged"/> events from <see cref="Archive"/>.
        /// Subclasses should override this method and only respond when the 
        /// </summary>
        /// <param name="sender">The event sender</param>
        /// <param name="e">The event argument</param>
        protected virtual void Archive_FileChanged(object sender, FileEventRaisedArgs e) {
            switch(e.EventType) {
                case FileEventType.FileAdded:
                    goto case FileEventType.FileChanged;
                case FileEventType.FileChanged:
                    if(UseAsynchronousMethods) {
                        AddOrUpdateFileAsync(e.FilePath);
                    } else {
                        AddOrUpdateFile(e.FilePath);
                    }
                    break;
                case FileEventType.FileDeleted:
                    if(UseAsynchronousMethods) {
                        RemoveFileAsync(e.FilePath);
                    } else {
                        RemoveFile(e.FilePath);
                    }
                    break;
                case FileEventType.FileRenamed:
                    if(UseAsynchronousMethods) {
                        RenameFileAsync(e.OldFilePath, e.FilePath);
                    } else {
                        RenameFile(e.OldFilePath, e.FilePath);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Invalid FileEventType");
            }
        }

        /// <summary>
        /// Checks to see if the given <paramref name="globalScope"/> object contains <paramref name="sourceFileName"/>.
        /// </summary>
        /// <param name="globalScope">The global scope object</param>
        /// <param name="sourceFileName">The source file to check for</param>
        /// <returns>True if <paramref name="globalScope"/> contains <paramref name="sourceFileName"/></returns>
        protected bool ContainsFile(NamespaceDefinition globalScope, string sourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(String.IsNullOrWhiteSpace(sourceFileName)) { throw new ArgumentNullException("sourceFileName"); }

            return globalScope.Locations.Any(l => l.SourceFileName.Equals(Path.GetFullPath(sourceFileName), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// This method should only be used when initializing a working set. After initialization,
        /// <see cref="TryAddOrUpdateFile"/> should be used as it first attempts to remove a file
        /// </summary>
        /// <param name="globalScope">The global scope object</param>
        /// <param name="sourceFileName">True if <paramref name="globalScope"/> was modified; false otherwise</param>
        /// <returns></returns>
        protected bool TryAddFile(NamespaceDefinition globalScope, string sourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            bool workingSetChanged = false;
            var data = Archive.GetData(sourceFileName);

            if(null != data) {
                if(null == globalScope) {
                    globalScope = data;
                } else {
                    globalScope = globalScope.Merge(data);
                }
                workingSetChanged = true;
            }

            return workingSetChanged;
        }
        /// <summary>
        /// Adds or updates <paramref name="sourceFileName"/> in the given <paramref name="scopeManager"/>.
        /// The file is removed from the global scope in <paramref name="scopeManager"/> if it already exists via <see cref="TryRemoveFile"/>.
        /// </summary>
        /// <param name="scopeManager">The global scope manager</param>
        /// <param name="sourceFileName">The source file to check for</param>
        /// <returns>True if the global scope was modified; false otherwise</returns>
        protected bool TryAddOrUpdateFile(GlobalScopeManager scopeManager, string sourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }
            if(null == scopeManager) { throw new ArgumentNullException("scopeManager"); }

            bool workingSetChanged = false;
            var data = Archive.GetData(sourceFileName);

            if(null != data) {
                if(null == scopeManager.GlobalScope) {
                    scopeManager.GlobalScope = data;
                } else {
                    TryRemoveFile(scopeManager, sourceFileName);
                    scopeManager.GlobalScope = scopeManager.GlobalScope.Merge(data);
                }
                workingSetChanged = true;
            }

            return workingSetChanged;
        }

        /// <summary>
        /// Removes <paramref name="sourceFileName"/> from <paramref name="scopeManager"/>
        /// </summary>
        /// <param name="scopeManager">The global scope manager</param>
        /// <param name="sourceFileName">the source file to remove</param>
        /// <returns>True if the global scope was modified; false otherwise</returns>
        protected bool TryRemoveFile(GlobalScopeManager scopeManager, string sourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == scopeManager) { throw new ArgumentNullException("scopeManager"); }

            bool workingSetChanged = false;

            if(ContainsFile(scopeManager.GlobalScope, sourceFileName)) {
                scopeManager.GlobalScope.RemoveFile(sourceFileName);
                workingSetChanged = true;
            }

            return workingSetChanged;
        }

        /// <summary>
        /// <see cref="TryRemoveFile">Removes</see> <paramref name="oldSourceFileName"/> and
        /// <see cref="TryAddOrUpdateFile">adds or updates</see> <paramref name="newSourceFileName"/>from
        /// the global scope.
        /// </summary>
        /// <param name="scopeManager">The global scope manager</param>
        /// <param name="oldSourceFileName">The old file name to remove</param>
        /// <param name="newSourceFileName">The new file name to add or update</param>
        /// <returns>True if the global scope was modified; false otherwise</returns>
        protected bool TryRenameFile(GlobalScopeManager scopeManager, string oldSourceFileName, String newSourceFileName) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == scopeManager) { throw new ArgumentNullException("scopeManager"); }

            bool workingSetChanged = TryRemoveFile(scopeManager, oldSourceFileName);
            workingSetChanged = TryAddOrUpdateFile(scopeManager, newSourceFileName);
            return workingSetChanged;
        }

        /// <summary>
        /// Raises the <see cref="Changed"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        protected virtual void OnChanged(EventArgs e) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = Changed;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="MonitoringStarted"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        private void OnMonitoringStarted(EventArgs e) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = MonitoringStarted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="MonitoringStopped"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        protected virtual void OnMonitoringStopped(EventArgs e) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = MonitoringStopped;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="UpdateCompleted"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        protected virtual void OnUpdateCompleted(EventArgs e) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = UpdateCompleted;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="UpdateStarted"/> event
        /// </summary>
        /// <param name="e">empty event args</param>
        protected virtual void OnUpdateStarted(EventArgs e) {
            if(IsDisposed) { throw new ObjectDisposedException(null); }

            EventHandler handler = UpdateStarted;
            if(null != handler) {
                handler(this, e);
            }
        }
        /// <summary>
        /// Subscribes <see cref="Archive_FileChanged"/> to <see cref="Archive"/>
        /// </summary>
        protected void SubscribeToArchive() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            Archive.FileChanged += Archive_FileChanged;
        }

        /// <summary>
        /// Unsubscribes <see cref="Archive_FileChanged"/> from <see cref="Archive"/>
        /// </summary>
        protected void UnsubscribeFromArchive() {
            if(IsDisposed) { throw new ObjectDisposedException(null); }
            if(null == Archive) { throw new InvalidOperationException("Archive is null"); }

            Archive.FileChanged -= Archive_FileChanged;
        }

        /// <summary>
        /// Sets <paramref name="field"/> to <paramref name="value"/> and then executes the appropriate event handler if <paramref name="field"/> has changed
        /// </summary>
        /// <param name="field">The private field to set</param>
        /// <param name="value">The value</param>
        /// <param name="startEventHandler">The event handler to execute if <paramref name="value"/> is true</param>
        /// <param name="endEventHandler">The event handler to execute if <paramref name="value"/> is false</param>
        /// <returns>True if the <paramref name="field"/> has changed; false otherwise</returns>
        protected bool SetBooleanField(ref bool field, bool value, Action<EventArgs> startEventHandler, Action<EventArgs> endEventHandler) {
            if(field == value) { return false; }
            field = value;
            (field ? startEventHandler : endEventHandler)(new EventArgs());
            return true;
        }
        /// <summary>
        /// The global scope manager provides a reference to a global scope object. It is returned via <see cref="TryObtainWriteLock"/>.
        /// the global scope manager allows you to 
        /// </summary>
        protected class GlobalScopeManager {
            /// <summary>
            /// Create a new global scope manager
            /// </summary>
            public GlobalScopeManager() {
                GlobalScope = null;
            }

            /// <summary>
            /// The global scope managed by this object
            /// </summary>
            public NamespaceDefinition GlobalScope { get; set; }

        }
    }
}
