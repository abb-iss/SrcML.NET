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

namespace ABB.SrcML.Data {
    public class LockTimeoutExceededException : Exception {
        public LockTimeoutExceededException(DataRepository data, int lockTimeout)
            : base(string.Format("lock timeout ({0} ms) exceeded", lockTimeout)) {
            DataRepository = data;
            LockTimeout = lockTimeout;
        }

        public DataRepository DataRepository { get; private set; }
        public int LockTimeout { get; private set; }
    }
}
