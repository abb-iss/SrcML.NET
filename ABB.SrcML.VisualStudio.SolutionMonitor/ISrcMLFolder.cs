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

namespace ABB.SrcML.VisualStudio.SolutionMonitor
{
    public interface ISrcMLFolder
    {
        event EventHandler<SrcMLEventArgs> SrcMLFileChanged;

        string FullFolderPath
        {
            get;
            set;
        }

        void StartWatching();
        void StopWatching();
    }

    public enum SrcMLEventType
    {
        Added,
        Changed,
        Deleted,
        Renamed
    }

    public class SrcMLEventArgs : System.EventArgs
    {
        public SrcMLEventArgs(string SrcMLPath, SrcMLEventType eventType)
            : this(SrcMLPath, SrcMLPath, eventType)
        {

        }

        public SrcMLEventArgs(string SrcMLPath, string oldSrcMLPath, SrcMLEventType eventType)
        {
            this.SrcMLFilePath = SrcMLPath;
            this.OldSrcMLFilePath = oldSrcMLPath;
            this.EventType = eventType;
        }

        protected SrcMLEventArgs()
        {

        }

        public SrcMLEventType EventType
        {
            get;
            set;
        }

        public string OldSrcMLFilePath
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
