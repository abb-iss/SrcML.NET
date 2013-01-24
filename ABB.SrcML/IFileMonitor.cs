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
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// This interface is designed for all events that can be raised from SrcML.NET.
    /// </summary>
    public interface IFileMonitor
    {
        event EventHandler<FileEventRaisedArgs> FileEventRaised;

        void StartMonitoring();
        void StopMonitoring();
        List<string> GetMonitoredFiles(System.ComponentModel.BackgroundWorker worker);
    }

    /// <summary>
    /// Event type enumeration.
    /// </summary>
    public enum FileEventType
    {
        FileAdded,    // Raised when a file is added
        FileChanged,  // Raised when a file is changed
        FileDeleted,  // Raised when a file is deleted
        FileRenamed,  // Raised when a file is renamed
    }

    /// <summary>
    /// Event data of SrcML.NET events.
    /// </summary>
    public class FileEventRaisedArgs : System.EventArgs
    {
        protected FileEventRaisedArgs()
        {
        }

        public FileEventRaisedArgs(string SourcePath, FileEventType eventType)
            : this(SourcePath, SourcePath, null, eventType)
        {
        }

        public FileEventRaisedArgs(string SourcePath, XElement xelement, FileEventType eventType)
            : this(SourcePath, SourcePath, xelement, eventType)
        {
        }

        public FileEventRaisedArgs(string SourcePath, string oldSourcePath, FileEventType eventType)
            : this(SourcePath, oldSourcePath, null, null, eventType)
        {
        }

        public FileEventRaisedArgs(string SourcePath, string oldSourcePath, XElement xelement, FileEventType eventType)
            : this(SourcePath, oldSourcePath, xelement, null, eventType)
        {
        }

        public FileEventRaisedArgs(string SourcePath, string oldSourcePath, XElement xelement, string SrcMLPath, FileEventType eventType)
        {
            this.SourceFilePath = SourcePath;
            this.OldSourceFilePath = oldSourcePath;
            this.SrcMLXElement = xelement;
            this.SrcMLFilePath = SrcMLPath;
            this.EventType = eventType;
        }

        public FileEventType EventType
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

        public XElement SrcMLXElement
        {
            get;
            set;
        }

        public string SrcMLFilePath
        {
            get;
            set;
        }
    }
}
