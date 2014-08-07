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
using System.IO;
using System.Linq;
using System.Text;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Collections;

namespace ABB.SrcML
{
    /// <summary>
    /// Abstract class for controlling srcml executables (src2srcml, srcml2src, and srcdiff).
    /// </summary>
    public class SrcMLRunner : ExecutableRunner
    {
        private readonly DefaultsDictionary<string, Language> _extensionMapping = new DefaultsDictionary<string, Language>(new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase) {
                    { ".c" , Language.C },
                    { ".h", Language.C },
                    { ".cpp", Language.CPlusPlus },
                    { ".java", Language.Java }
        });

        private Collection<string> _namespaceArguments;
        /// <summary>
        /// Mapping of source extensions to their languages.
        /// </summary>
        public DefaultsDictionary<string, Language> ExtensionMapping
        {
            get
            {
                return _extensionMapping;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to treat headers (.h files) as C plus plus].
        /// If this property is set to <c>false</c>, headers will be treated as C regardless of the language assigned to it
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [treat headers as C plus plus]; otherwise, <c>false</c>.
        /// </value>
        public bool TreatHeadersAsCPlusPlus
        {
            get
            {
                return ExtensionMapping[".h"] == Language.CPlusPlus;
            }

            set
            {
                if (value)
                    ExtensionMapping[".h"] = Language.CPlusPlus;
                else
                    ExtensionMapping[".h"] = Language.C;
            }
        }

        /// <summary>
        /// Gets or sets the list of common namespace arguments
        /// </summary>
        /// <value>
        /// The namespace arguments.
        /// </value>
        public Collection<string> NamespaceArguments
        {
            get
            {
                var arguments = from arg in this._namespaceArguments
                                select arg;

                return new Collection<string>(arguments.ToList());
            }
            private set
            {
                this._namespaceArguments = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        /// <param name="executableName">Name of the executable.</param>
        /// <param name="namespaceArguments">The namespace arguments.</param>
        public SrcMLRunner(string applicationDirectory, string executableName, IEnumerable<string> namespaceArguments)
            : base(applicationDirectory, executableName) {
            this.NamespaceArguments = new Collection<string>(namespaceArguments.ToList());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcMLRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        /// <param name="executableName">Name of the executable.</param>
        public SrcMLRunner(string applicationDirectory, string executableName)
            : this(applicationDirectory, executableName, new[] { LIT.ArgumentLabel, OP.ArgumentLabel, TYPE.ArgumentLabel })
        {

        }

        /// <summary>
        /// Runs this executable and places the output in the specified output file.
        /// This executable is run with the following string <c>[this.ExecutablePath] --register-ext [ExtensionMapping] --output=[outputfile] [addititionlArguments]</c>
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        /// <param name="additionalArguments">The additional arguments.</param>
        public void Run(string outputFile, Collection<string> additionalArguments)
        {
            if (null == additionalArguments)
                throw new ArgumentNullException("additionalArguments");

            var arguments = new Collection<string>();
            foreach (var argument in this.NamespaceArguments)
                arguments.Add(argument);

            if (ExtensionMapping.NonDefaultValueCount > 0)
            {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--register-ext {0}", KsuAdapter.ConvertMappingToString(ExtensionMapping)));
            }

            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--output=\"{0}\"", outputFile));

            foreach(var arg in additionalArguments)
                arguments.Add(arg);

            try
            {
                base.Run(arguments);
            }
            catch (SrcMLRuntimeException e)
            {
                throw new SrcMLException(String.Format(CultureInfo.CurrentCulture, "{0} encountered an error: {1}", this.ExecutablePath, e.Message), e);
            }
        }

        /// <summary>
        /// Runs this executable and places the output in the specified output file. The inputs are written to a temporary file that is deleted when finished.
        /// This executable is run with the following string <c>[this.ExecutablePath] --register-ext [ExtensionMapping] --output=[outputfile] --files-from=[input file] [addititionlArguments]</c>
        /// </summary>
        /// <param name="outputFile">The output file.</param>
        /// <param name="additionalArguments">The additional arguments.</param>
        /// <param name="inputs">The inputs.</param>
        public void Run(string outputFile, Collection<string> additionalArguments, Collection<string> inputs)
        {
            if (null == inputs)
                throw new ArgumentNullException("inputs");

            var arguments = new Collection<string>(additionalArguments);

            var tempFileListing = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(tempFileListing))
            {
                foreach (var input in inputs)
                {
                    writer.WriteLine(input);
                }
            }
            arguments.Insert(0, String.Format(CultureInfo.InvariantCulture, "--files-from=\"{0}\"", tempFileListing));

            try
            {
                this.Run(outputFile, arguments);
            }
            catch (SrcMLException)
            {
                throw;
            }
            finally
            {
                File.Delete(tempFileListing);
            }
        }
    }
}
