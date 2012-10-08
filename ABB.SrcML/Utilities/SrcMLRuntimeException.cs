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
using System.IO;
using System.Runtime.Serialization;
using System.Globalization;

namespace ABB.SrcML.Utilities
{
    [Serializable]
    class SrcMLRuntimeException : SrcMLException
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        private string _pathToExecutable;
        private string _argumentString;
        private ExecutableReturnValue _returnValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRuntimeException"/> class.
        /// </summary>
        public SrcMLRuntimeException()
            : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRuntimeException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SrcMLRuntimeException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRuntimeException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public SrcMLRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRuntimeException"/> class.
        /// </summary>
        /// <param name="pathToExecutable">The path to executable.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="returnValue">The return value.</param>
        public SrcMLRuntimeException(string pathToExecutable, string arguments, ExecutableReturnValue returnValue)
            : base(FormatMessage(pathToExecutable, arguments, returnValue))
        {
            this._pathToExecutable = pathToExecutable;
            this._argumentString = arguments;
            this._returnValue = returnValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRuntimeException"/> class.
        /// </summary>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="context">The context.</param>
        protected SrcMLRuntimeException(SerializationInfo serializationInfo, StreamingContext context)
            : base(serializationInfo, context)
        {

        }

        /// <summary>
        /// Gets the executable return value
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ExecutableReturnValue ReturnValue
        {
            get { return this._returnValue; }
        }

        /// <summary>
        /// Gets the error message that corresponds to <see cref="ExecutableReturnValue"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ErrorMessage
        {
            get
            {
                return KsuAdapter.GetErrorMessageFromReturnCode(this._returnValue);
            }
        }
        /// <summary>
        /// Gets the argument string passed to the srcML executable
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string ArgumentString
        {
            get
            {
                return this._argumentString;
            }
        }

        internal static string FormatMessage(string pathToExecutable, string arguments, ExecutableReturnValue returnValue)
        {
            string executableName = Path.GetFileName(pathToExecutable);
            string message = KsuAdapter.GetErrorMessageFromReturnCode(returnValue);

            return String.Format(CultureInfo.CurrentCulture, "{0} failed: {1}\n\t{2} {3}", executableName, message, pathToExecutable, arguments);
        }
    }
}
