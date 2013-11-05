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

        IScope GetGlobalScope();

        bool IsReady { get; }

        void AddFile(string sourceFile);

        void AddFile(XElement fileUnitElement);

        void Clear();

        Collection<IMethodCall> FindMethodCalls(SourceLocation loc);

        Collection<IMethodCall> FindMethodCalls(string xpath);

        Collection<IMethodCall> FindMethodCalls(XElement element);

        IScope FindScope(SourceLocation loc);

        IScope FindScope(string xpath);

        IScope FindScope(XElement element);

        void InitializeData();

        void InitializeDataConcurrent();

        void InitializeDataConcurrent(TaskScheduler scheduler);

        void Load(string fileName);

        void RemoveFile(string sourceFile);

        void Save();

        void Save(string fileName);
    }
}