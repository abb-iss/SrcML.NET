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

using ABB.SrcML;
using ABB.SrcML.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ABB.VisualStudio {
    /// <summary>
    /// This is the interface that will be implemented by the global service exposed by the package
    /// defined in ABB.VisualStudio.SrcMLService. Notice that we have to define this interface
    /// as COM visible so that it will be possible to query for it from the native version of
    /// IServiceProvider.
    /// </summary>
    [Guid(GuidList.ITaskManagerServiceId)]
    [ComVisible(true)]
    public interface ITaskManagerService {
        TaskScheduler GlobalScheduler { get; }

        TaskFactory GlobalFactory { get; }

        event EventHandler SchedulerIdled;

        void Start();
        void Stop();
    }


    /// <summary>
    /// The goal of this interface is actually just to define a Type (or Guid from the native
    /// client's point of view) that will be used to identify the service. In theory, we could use
    /// the interface defined above, but it is a good practice to always define a new type as the
    /// service's identifier because a service can expose different interfaces.
    ///
    /// It is registered at: HKEY_USERS\S-1-5-21-1472859983-109138142-169162935-138973\Software\Microsoft\VisualStudio\11.0Exp_Config\Services\{4B917BC0-C42E-447C-B732-6A675EDF4EB9}
    /// </summary>
    [Guid(GuidList.STaskManagerServiceId)]
    public interface STaskManagerService {

    }
}
