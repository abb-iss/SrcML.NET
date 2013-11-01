/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Moved IArchive to ABB.SrcML.Core.dll
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML
{
    public interface IArchive : IDisposable
    {
        string ArchivePath { get; }

        bool IsReady { get; }

        bool IsEmpty { get; }

        ICollection<string> SupportedExtensions { get; }

        event EventHandler<FileEventRaisedArgs> FileChanged;

        event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;

        void AddOrUpdateFile(string fileName);

        void AddOrUpdateFileAsync(string fileName);

        void DeleteFile(string fileName);

        void DeleteFileAsync(string fileName);

        void RenameFile(string oldFileName, string newFileName);

        void RenameFileAsync(string oldFileName, string newFileName);

        bool ContainsFile(string fileName);

        bool IsOutdated(string fileName);

        Collection<string> GetFiles();

        void Save();
    }
}
