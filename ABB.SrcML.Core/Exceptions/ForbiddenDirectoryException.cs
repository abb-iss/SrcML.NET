/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public class ForbiddenDirectoryException : Exception {

        public const string ISROOT = "root drive";
        public const string ISSPECIALDIR = "special directory";
        /// <summary>
        /// The forbidden directory
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// The reason the directory is forbidden
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Create a new forbidden directory exception
        /// </summary>
        /// <param name="directory">The forbidden directory</param>
        /// <param name="reason">The reason it is forbidden</param>
        public ForbiddenDirectoryException(string directory, string reason)
            : base(string.Format("{0} is a forbidden directory because it is a {1}", directory, reason)) {
            Directory = directory;
            Reason = reason;
        }
    }
}
