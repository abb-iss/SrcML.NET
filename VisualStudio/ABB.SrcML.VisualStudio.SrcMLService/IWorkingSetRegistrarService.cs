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
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// The working set registrar service lets 3rd party developers provide <see cref="AbstractWorkingSet"/> implementations
    /// for the <see cref="SrcMLDataService"/>. You register a working set implementation by writing an <see cref="AbstractWorkingSetFactory"/>
    /// and then calling <see cref="RegisterWorkingSetFactory(AbstractWorkingSetFactory)"/> in <see cref="Microsoft.VisualStudio.Shell.Package.Initialize"/>
    /// </summary>
    [Guid(GuidList.IWorkingSetRegistrarServiceId), ComVisible(true)]
    public interface IWorkingSetRegistrarService {
        /// <summary>
        /// The default working set factory
        /// </summary>
        AbstractWorkingSetFactory Default { get; }

        /// <summary>
        /// Registered working set factories
        /// </summary>
        ObservableCollection<AbstractWorkingSetFactory> WorkingSetFactories { get; }

        /// <summary>
        /// This event will be raised whenever a working set factory is added to <see cref="WorkingSetFactories"/>
        /// </summary>
        event EventHandler<WorkingSetFactoryAddedEventArgs> WorkingSetFactoryAdded;

        /// <summary>
        /// Registers a new working set factory and makes it the default
        /// </summary>
        /// <param name="factory"></param>
        void RegisterWorkingSetFactory(AbstractWorkingSetFactory factory);
    }

    /// <summary>
    /// Service interface for <see cref="IWorkingSetRegistrarService"/>
    /// </summary>
    [Guid(GuidList.SWorkingSetRegistrarServiceId)]
    public interface SWorkingSetRegistrarService { }
}
