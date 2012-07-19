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
using System.Collections.ObjectModel;

namespace ABB.SrcML.Utilities
{
    /// <summary>
    /// Wrapper class for running executables with given command line arguments
    /// </summary>
    public class ExecutableRunner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutableRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        /// <param name="executableName">Name of the executable.</param>
        public ExecutableRunner(string applicationDirectory, string executableName)
        {
            this.ApplicationDirectory = applicationDirectory;
            this.ExecutableName = executableName;
        }

        /// <summary>
        /// Gets or sets the name of the executable.
        /// </summary>
        /// <value>
        /// The name of the executable.
        /// </value>
        public string ExecutableName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the application directory.
        /// </summary>
        /// <value>
        /// The application directory.
        /// </value>
        public string ApplicationDirectory
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the executable path.
        /// </summary>
        public string ExecutablePath
        {
            get
            {
                return Path.Combine(this.ApplicationDirectory, this.ExecutableName);
            }
        }

        /// <summary>
        /// Runs this executable with the specified arguments.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        public void Run(Collection<string> arguments)
        {
            var argumentString = KsuAdapter.MakeArgumentString(arguments);

            this.Run(argumentString);
        }

        /// <summary>
        /// Runs this executable with the specified argument text.
        /// </summary>
        /// <param name="argumentText">The argument text.</param>
        public void Run(string argumentText)
        {
            try
            {
                KsuAdapter.RunExecutable(this.ExecutablePath, argumentText);
            }
            catch (SrcMLRuntimeException)
            {
                throw;
            }
        }

        /// <summary>
        /// Runs this executable with the specified arguments and additional input passed in on standard input.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        /// <param name="standardInput">The standard input.</param>
        /// <returns></returns>
        public string Run(Collection<string> arguments, string standardInput)
        {
            var argumentString = KsuAdapter.MakeArgumentString(arguments);

            return this.Run(argumentString, standardInput);
        }

        /// <summary>
        /// Runs this executable with the specified argument text and additional input passed in on standard input.
        /// </summary>
        /// <param name="argumentText">The argument text.</param>
        /// <param name="standardInput">The standard input.</param>
        /// <returns></returns>
        public string Run(string argumentText, string standardInput)
        {
            try
            {
                var output = KsuAdapter.RunExecutable(this.ExecutablePath, argumentText, standardInput);
                return output;
            }
            catch (SrcMLRuntimeException)
            {
                throw;
            }
        }
    }
}
