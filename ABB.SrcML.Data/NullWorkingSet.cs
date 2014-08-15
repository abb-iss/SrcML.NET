/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The null working set object always has a null global scope object.
    /// </summary>
    public class NullWorkingSet : AbstractWorkingSet {

        /// <summary>
        /// Creates a new null working set object
        /// </summary>
        public NullWorkingSet() : base() { }

        /// <summary>
        /// Creates a new null working set object
        /// </summary>
        /// <param name="archive">The data archive</param>
        /// <param name="factory">The task factory</param>
        public NullWorkingSet(DataArchive archive, TaskFactory factory) : base(archive, factory) { }

        /// <summary>
        /// Initializes the working set. This does nothing.
        /// </summary>
        public override void Initialize() {
            
        }

        /// <summary>
        /// Initializes the working set asynchronously. This does nothing.
        /// </summary>
        /// <returns></returns>
        public override Task InitializeAsync() {
            return Task.Factory.StartNew(Initialize);
        }

        /// <summary>
        /// Method used to monitor <see cref="AbstractWorkingSet.Archive"/>. It does nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void Archive_FileChanged(object sender, FileEventRaisedArgs e) { }
    }
}
