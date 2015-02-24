/******************************************************************************
 * Copyright (c) 2015 ABB Group
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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ABB.VisualStudio {
    /// <summary>
    /// The ICursorMonitorService interface represents an object that monitors the current 
    /// </summary>
    [Guid(GuidList.ICursorMonitorServiceId), ComVisible(true)]
    public interface ICursorMonitorService : INotifyPropertyChanged {
        /// <summary>The current file that the cursor is in</summary>
        string CurrentFileName { get; }

        /// <summary>The current line number in <see cref="CurrentFileName"/> that the cursor is on</summary>
        int CurrentLineNumber { get; }

        /// <summary>The current column number in <see cref="CurrentFileName"/> that the cursor is at</summary>
        int CurrentColumnNumber { get; }

        /// <summary>The last time that this monitor has updated</summary>
        DateTime LastUpdated { get; }
    }

    /// <summary>
    /// Service interface for getting an instance of <see cref="ICursorMonitorService"/>
    /// </summary>
    [Guid(GuidList.SCursorMonitorServiceId)]
    public interface SCursorMonitorService { }
}
