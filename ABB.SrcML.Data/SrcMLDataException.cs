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
using System.Runtime.Serialization;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// exception for errors in SrcMLData
    /// </summary>
    [Serializable]
    public class SrcMLDataException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLDataException"/> class.
        /// </summary>
        public SrcMLDataException()
            : base()
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLDataException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public SrcMLDataException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLDataException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SrcMLDataException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLDataException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        ///   
        /// <exception cref="T:System.Runtime.Serialization.SerializationException">The class name is null or <see cref="P:System.Exception.HResult"/> is zero (0). </exception>
        protected SrcMLDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }
}
