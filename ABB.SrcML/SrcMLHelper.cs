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
using System.Xml.Linq;
using System.IO;

namespace ABB.SrcML
{
    /// <summary>
    /// Collection of helper functions for working with srcML elements
    /// </summary>
    public static class SrcMLHelper
    {
        

        /// <summary>
        /// Gets the default srcML binary directory. It checks the following conditions:
        /// 1. If the SRCMLBINDIR environment variable is set, then that is used.
        /// 2. If c:\Program Files (x86)\SrcML\bin directory exists (should only exist on 64-bit systems), then that is used.
        /// 3. If c:\Program Files\SrcML\bin directory exists, then that is used.
        /// 4. If none of the above is true, then the current directory is used.
        /// 
        /// This function does not check that any of the paths actually contains the srcML executables.
        /// </summary>
        /// <returns>The default srcML binary directory.</returns>
        public static string GetSrcMLDefaultDirectory()
         {
            var srcmlDir = Environment.GetEnvironmentVariable("SRCMLBINDIR");
            if (null == srcmlDir)
            {
                var programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (null == programFilesDir)
                    programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles");
                srcmlDir = Path.Combine(programFilesDir, Path.Combine("SrcML", "bin"));
            }
             
            if (!Directory.Exists(srcmlDir))
                return Directory.GetCurrentDirectory();
            return srcmlDir;
        }

        /// <summary>
        /// Returns the default srcML binary directory.
        /// </summary>
        /// <param name="extensionDirectory"></param>
        /// <returns></returns>
        public static string GetSrcMLDefaultDirectory(string extensionDirectory)
        {
            if(!Directory.Exists(Path.Combine(extensionDirectory, "SrcML"))) {
                return SrcMLHelper.GetSrcMLDefaultDirectory();
            } else {
                return Path.Combine(extensionDirectory, "SrcML");
            }
        }
    }
}
