/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// The working set registrar provides the default implementation of <see cref="IWorkingSetRegistrarService"/>
    /// </summary>
    public class WorkingSetRegistrarService : IWorkingSetRegistrarService, SWorkingSetRegistrarService {
        IServiceProvider _serviceProvider;

        /// <summary>
        /// Creates a working set registrar object
        /// </summary>
        /// <param name="serviceProvider">The service provider where this service is sited</param>
        public WorkingSetRegistrarService(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
            WorkingSetFactories = new ObservableCollection<AbstractWorkingSetFactory>();
            WorkingSetFactories.CollectionChanged += WorkingSetFactories_CollectionChanged;
        }

        /// <summary>
        /// The default working set factory
        /// </summary>
        public AbstractWorkingSetFactory Default { get; private set; }

        /// <summary>
        /// The registered working set factories
        /// </summary>
        public ObservableCollection<AbstractWorkingSetFactory> WorkingSetFactories { get; private set; }

        /// <summary>
        /// Raised when a working set factory is added
        /// </summary>
        public event EventHandler<WorkingSetFactoryAddedEventArgs> WorkingSetFactoryAdded;

        /// <summary>
        /// Registers a new working set object and makes it the default working set
        /// </summary>
        /// <param name="factory">The working set factory to register</param>
        public void RegisterWorkingSetFactory(AbstractWorkingSetFactory factory) {
            WorkingSetFactories.Add(factory);
            Default = factory;
        }

        /// <summary>
        /// Raises <see cref="WorkingSetFactoryAdded"/>
        /// </summary>
        /// <param name="e">The event arguments</param>
        protected virtual void OnWorkingSetFactoryAdded(WorkingSetFactoryAddedEventArgs e) {
            var handler = WorkingSetFactoryAdded;
            if(null != handler) {
                handler(this, e);
            }
        }

        /// <summary>
        /// For each new item in <paramref name="e"/>, <see cref="OnWorkingSetFactoryAdded"/> is called
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The event arguments</param>
        private void WorkingSetFactories_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {

            foreach(var factory in e.NewItems.Cast<AbstractWorkingSetFactory>().Where(f => null != f)) {
                OnWorkingSetFactoryAdded(new WorkingSetFactoryAddedEventArgs(factory));
            }
        }
    }
}
