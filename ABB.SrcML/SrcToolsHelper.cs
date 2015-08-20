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
namespace ABB.SrcML
{
    /// <summary>
    /// Collection of helper functions for working with srcML elements
    /// </summary>
    public static class SrcToolsHelper
    {
        /// <summary>
        /// Gets the default srcTools directory. It checks the following conditions:
        /// 1. If SRCTOOLSBINDIR environment variable is set, then that is used.
        /// 2. If c:\Program Files (x86)\SrcML\bin directory exists (should only exist on 64-bit systems), then that is used.
        /// 3. If c:\Program Files\SrcML\bin directory exists, then that is used.
        /// 4. If none of the above is true, then the current directory is used.
        /// </summary>
        /// <param name="toolName">The name of the tool (since there are multiple) that the user is looking for.</param>
        /// Does not check to see if the directory contains the executables
        /// <returns>Path to the proper directory for the given tool</returns>
        public static string GetSrcMLToolDefaultDirectory(String toolName)
        {
            toolName = toolName.ToLower(); //tool names are all lowercase
            var srcmlToolsDir = Environment.GetEnvironmentVariable("SRCTOOLSBINDIR");
            if (null == srcmlToolsDir)
            {
                var programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (null == programFilesDir)
                    programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles");
                srcmlToolsDir = Path.Combine(programFilesDir, Path.Combine("SrcTools", String.Format("{0}\\bin", toolName)));
            }

            if (!Directory.Exists(srcmlToolsDir))
                return Directory.GetCurrentDirectory();
            return srcmlToolsDir;
        }

        /// <summary>
        /// Returns the default srcML binary directory.
        /// </summary>
        /// <param name="extensionDirectory"></param>
        /// <returns></returns>
        public static string GetSrcMLDefaultDirectory(string toolName, string extensionDirectory)
        {
            if (!Directory.Exists(Path.Combine(extensionDirectory, "SrcTools")))
            {
                return SrcToolsHelper.GetSrcMLToolDefaultDirectory(toolName);
            }
            else
            {
                return Path.Combine(extensionDirectory, "srcTools");
            }
        }
    }
}
