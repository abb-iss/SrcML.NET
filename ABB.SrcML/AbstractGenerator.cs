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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    /// <summary>
    /// The abstract generator file takes an input file and creates an output file.
    /// </summary>
    public abstract class AbstractGenerator {
        private TextWriter _synchronizedErrorLog;

        public bool IsLoggingErrors { get; set; }

        public TextWriter ErrorLog {
            get { return _synchronizedErrorLog; }
            set { _synchronizedErrorLog = (value != null ? TextWriter.Synchronized(value) : null); }
        }

        /// <summary>
        /// Default constructor for the abstract generator
        /// </summary>
        protected AbstractGenerator() {
            IsLoggingErrors = false;
            ErrorLog = Console.Error;
        }

        /// <summary>
        /// A list of extensions supported by this generator
        /// </summary>
        public abstract ICollection<string> SupportedExtensions { get; }

        /// <summary>
        /// Generates <paramref name="outputFileName"/> from <paramref name="inputFileName"/>
        /// </summary>
        /// <param name="inputFileName">The input file</param>
        /// <param name="outputFileName">the output file</param>
        public bool Generate(string inputFileName, string outputFileName) {
            try {
                return GenerateImpl(inputFileName, outputFileName);
            } catch(Exception e) {
                if(IsLoggingErrors) {
                    LogError(e);
                    return false;
                } else {
                    throw;
                }
            }
        }

        protected virtual void LogError(Exception e) {
            if(null != ErrorLog) {
                ErrorLog.WriteLine(e);
            }
        }

        /// <summary>
        /// Generates <paramref name="outputFileName"/> from <paramref name="inputFileName"/>
        /// </summary>
        /// <param name="inputFileName">The input file</param>
        /// <param name="outputFileName">the output file</param>
        protected abstract bool GenerateImpl(string inputFileName, string outputFileName);
    }
}
