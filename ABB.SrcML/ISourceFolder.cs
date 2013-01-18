/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML
{
    public interface ISourceFolder
    {
        event EventHandler<SourceEventArgs> SourceFileChanged;

        string FullFolderPath
        {
            get;
            set;
        }

        void StartWatching();
        void StopWatching();
    }

    public enum SourceEventType
    {
        Added,
        Changed,
        Deleted,
        Renamed
    }

    public class SourceEventArgs : System.EventArgs
    {
        public SourceEventArgs(string sourcePath, SourceEventType eventType)
            : this(sourcePath, sourcePath, eventType)
        {

        }

        public SourceEventArgs(string sourcePath, string oldSourcePath, SourceEventType eventType)
        {
            this.SourceFilePath = sourcePath;
            this.OldSourceFilePath = oldSourcePath;
            this.EventType = eventType;
        }

        protected SourceEventArgs()
        {

        }

        public SourceEventType EventType
        {
            get;
            set;
        }

        public string OldSourceFilePath
        {
            get;
            set;
        }

        public string SourceFilePath
        {
            get;
            set;
        }
    }
}
