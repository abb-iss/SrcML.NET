/******************************************************************************
 * Copyright (c) 2011 ABB Group
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

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Definition of the progress event arguments
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        /// <summary>
        /// the progress event message
        /// </summary>
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// The srcML file name being processed.
        /// </summary>
        public string FileName
        {
            get;
            set;
        }
        
        /// <summary>
        /// Constructor for ProgressEventArgs
        /// </summary>
        /// <param name="fileName">the srcML file being processed.</param>
        /// <param name="message">message indicating what has been done.</param>
        public ProgressEventArgs(string fileName, string message)
        {
            this.FileName = fileName;
            this.Message = message;
        }
    }
}
