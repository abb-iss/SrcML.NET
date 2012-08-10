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
using System.Collections.ObjectModel;
using System.IO;
using ABB.SrcML.Utilities;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// Utility class for running srcml2src.exe
    /// </summary>
    public class SrcML2SrcRunner : ExecutableRunner
    {
        /// <summary>
        /// The srcml2src executable name
        /// </summary>
        public const string SrcML2SrcExecutableName = "srcml2src.exe";

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SrcML2SrcRunner"/> class.
        /// </summary>
        public SrcML2SrcRunner()
            : this(SrcMLHelper.GetSrcMLDefaultDirectory())
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcML2SrcRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        public SrcML2SrcRunner(string applicationDirectory)
            : base(applicationDirectory, SrcML2SrcExecutableName)
        {

        }
        #endregion

        /// <summary>
        /// Extracts the source.
        /// </summary>
        /// <param name="xmlFileName">Name of the XML file.</param>
        /// <param name="outputFileName">Name of the output file.</param>
        /// <param name="unitIndex">Index of the unit.</param>
        public void ExtractSource(string xmlFileName, string outputFileName, int unitIndex)
        {
            var arguments = String.Format(CultureInfo.InvariantCulture, "--unit={0} --output=\"{1}\" \"{2}\"", unitIndex, outputFileName, xmlFileName);
            base.Run(arguments);
        }
    }
}
