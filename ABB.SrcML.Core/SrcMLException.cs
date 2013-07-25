/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using System.Runtime.Serialization;

namespace ABB.SrcML
{
    /// <summary>
    /// The base SrcML Exception
    /// </summary>
    [Serializable]
    public class SrcMLException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLException"/> class.
        /// </summary>
        public SrcMLException()
            : this("An error occurred with srcML")
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SrcMLException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        /// <summary>
        /// Create a SrcMLException with the given message.
        /// </summary>
        /// <param name="message"></param>
        public SrcMLException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="context">The context.</param>
        protected SrcMLException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {

        }
    }
}
