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
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ABB.SrcML.VisualStudio.SrcMLService {
    /// <summary>
    /// Step 5: Implement the local service class.
    /// This is the class that implements the local service. It implements ISrcMLLocalService
    /// because this is the interface that we want to use, but it also implements the empty
    /// interface SSrcMLLocalService in order to notify the service creator that it actually
    /// implements this service.
    /// </summary>
    public class SrcMLLocalService : ISrcMLLocalService, SSrcMLLocalService {
        /// <summary>
        /// Store a reference to the service provider that will be used to access the shell's services
        /// </summary>
        private IServiceProvider provider;

        /// <summary>
        /// Public constructor of this service. This will use a reference to a service provider to
        /// access the services provided by the shell.
        /// </summary>
        public SrcMLLocalService(IServiceProvider sp) {
            Trace.WriteLine("Constructing a new instance of SrcMLLocalService");
            provider = sp;
        }

        //// Implement the methods of ISrcMLLocalService here.
        #region ISrcMLLocalService Members
        /*
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
        public int LocalServiceFunction() {
            string outputText = "Local SrcML Service Function called.\n";
            HelperFunctions.WriteOnOutputWindow(provider, outputText);
            return 0;
        }
        */
        #endregion
    }
}
