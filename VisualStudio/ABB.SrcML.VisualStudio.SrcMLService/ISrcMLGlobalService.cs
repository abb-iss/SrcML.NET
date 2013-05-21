/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace ABB.SrcML.VisualStudio.SrcMLService {
    /// <summary>
    /// This is the interface that will be implemented by the global service exposed
    /// by the package defined in ABB.SrcML.VisualStudio.SrcMLService.
    /// Notice that we have to define this interface as COM visible so that 
    /// it will be possible to query for it from the native version of IServiceProvider.
    /// </summary>
    [Guid("ba9fe7a3-e216-424e-87f9-dee001228d04")]
    [ComVisible(true)]
    public interface ISrcMLGlobalService {
        //void GlobalServiceFunction();
        //int CallLocalService();

        event EventHandler<FileEventRaisedArgs> SourceFileChanged;
        event EventHandler<IsReadyChangedEventArgs> IsReadyChanged;
        event EventHandler<EventArgs> MonitoringStopped;

        void StartMonitoring();
        void StartMonitoring(bool useExistingSrcML, string srcMLBinaryDirectory);
        void StopMonitoring();
        SrcMLArchive GetSrcMLArchive();
        XElement GetXElementForSourceFile(string sourceFilePath);

    }

    /// <summary>
    /// The goal of this interface is actually just to define a Type (or Guid from the native
    /// client's point of view) that will be used to identify the service.
    /// In theory, we could use the interface defined above, but it is a good practice to always
    /// define a new type as the service's identifier because a service can expose different interfaces.
    /// 
    /// It is registered at: HKEY_USERS\S-1-5-21-1472859983-109138142-169162935-138973\Software\Microsoft\VisualStudio\11.0Exp_Config\Services\{fafafdfb-60f3-47e4-b38c-1bae05b44241}
    /// </summary>
    [Guid("fafafdfb-60f3-47e4-b38c-1bae05b44241")]
    public interface SSrcMLGlobalService {
    }
}
