/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    public interface IDataRepository : IDisposable {

        event EventHandler<ErrorRaisedArgs> ErrorRaised;

        event EventHandler<FileEventRaisedArgs> FileProcessed;

        event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        ISrcMLArchive Archive { get; }

        string FileName { get; }

        bool IsReady { get; }

        void AddFile(string sourceFile);

        void AddFile(XElement fileUnitElement);

        void Clear();

        Collection<IMethodCall> FindMethodCalls(SourceLocation loc);

        Collection<IMethodCall> FindMethodCalls(string xpath);

        Collection<IMethodCall> FindMethodCalls(XElement element);

        T Findscope<T>(XElement element) where T : class, IScope;

        IScope FindScope(SourceLocation loc);

        IScope FindScope(string xpath);

        IScope FindScope(XElement element);

        T FindScope<T>(SourceLocation loc) where T : class, IScope;

        T FindScope<T>(string xpath) where T : class, IScope;

        IScope GetGlobalScope();

        void InitializeData();

        Task InitializeDataAsync();

        void Load(string fileName);

        /// <summary>
        /// Releases the global scope lock
        /// </summary>
        void ReleaseGlobalScopeLock();

        void RemoveFile(string sourceFile);

        void Save();

        void Save(string fileName);

        /// <summary>
        /// try to look the global scope. Returns true if the lock is obtained. Returns false after <paramref name="millisecondsTimeout"/> passes.
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout (in milliseconds)</param>
        /// <returns>True if the lock is obtained; false otherwise</returns>
        bool TryLockGlobalScope(int millisecondsTimeout);
    }
}