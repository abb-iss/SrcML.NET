/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio.Interfaces {
    [Guid("3331EA7E-2877-45F5-9E14-31FF0F5B761A"), ComVisible(true)]
    public interface ISrcMLDataService {
        event EventHandler<FileEventRaisedArgs> FileProcessed;
        event EventHandler UpdateStarted;
        event EventHandler UpdateCompleted;
        event EventHandler MonitoringStarted;
        event EventHandler MonitoringStopped;

        AbstractWorkingSet CurrentWorkingSet { get; }

        bool IsMonitoring { get; }

        bool IsUpdating { get; }
    }
    [Guid("4F09B16E-9048-40BA-89FA-31F692C5D8E0")]
    public interface SSrcMLDataService { }
}
