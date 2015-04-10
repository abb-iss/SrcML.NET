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
using ABB.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// Provides data services based on ABB.SrcML.Data to Visual Studio client
    /// </summary>
    [Guid(GuidList.ISrcMLDataServiceId), ComVisible(true)]
    public interface ISrcMLDataService {
        /// <summary>
        /// Raised when a file changed is processed
        /// </summary>
        event EventHandler<FileEventRaisedArgs> FileProcessed;

        /// <summary>
        /// Raised when an update is started
        /// </summary>
        event EventHandler UpdateStarted;

        /// <summary>
        /// Raised when an update is completed
        /// </summary>
        event EventHandler UpdateCompleted;

        /// <summary>
        /// Raised when monitoring is started
        /// </summary>
        event EventHandler MonitoringStarted;

        /// <summary>
        /// Raised when monitoring is stopped
        /// </summary>
        event EventHandler MonitoringStopped;

        /// <summary>
        /// The working set for the current solution
        /// </summary>
        AbstractWorkingSet CurrentWorkingSet { get; }

        /// <summary>
        /// The data archive for the current solution
        /// </summary>
        DataArchive CurrentDataArchive { get; }

        /// <summary>
        /// True if the service is currently monitoring; false otherwise (<see cref="MonitoringStarted" /> will be called before
        /// this is set to true, and <see cref="MonitoringStopped"/> will be called when it is set to false.
        /// </summary>
        bool IsMonitoring { get; }

        /// <summary>
        /// True if the service is currently updating; false otherwise (<see cref="UpdateStarted" /> will be called before
        /// this is set to true, and <see cref="UpdateCompleted"/> will be called when it is set to false.
        /// </summary>
        bool IsUpdating { get; }
    }

    /// <summary>
    /// Service interface for <see cref="ISrcMLDataService"/>
    /// </summary>
    [Guid(GuidList.SSrcMLDataServiceId)]
    public interface SSrcMLDataService { }
}
