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
using System.Xml.Linq;
using System.IO;
using System.Globalization;

namespace ABB.SrcML.Utilities
{
    /// <summary>
    /// Reader class for reading Visual Studo project files
    /// </summary>
    public sealed class VisualStudioProjectReader
    {
        private delegate string[] readDelegate(string filename);
        private readonly static Dictionary<string, readDelegate> mapping = new Dictionary<string, readDelegate>()
        {
            { ".vcproj", ReadVCProject },
            { ".csproj", ReadCSProject }
        };

        private VisualStudioProjectReader()
        {

        }

        /// <summary>
        /// read the source files from the given Visual Studio project
        /// </summary>
        /// <param name="fileName">the filename for the Visual Studio project</param>
        /// <returns>an array of source files</returns>
        public static string[] ReadProjectFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToUpperInvariant();
            return mapping[ext](fileName);
        }

        /// <summary>
        /// Parse a C# project file
        /// </summary>
        /// <param name="fileName">the path to the C# project file</param>
        /// <returns>an array of source files</returns>
        public static string[] ReadCSProject(string fileName)
        {
            XDocument doc = XDocument.Load(fileName, LoadOptions.None);
            IEnumerable<string> sourcefiles = from srcfile in doc.Root.Descendants((XNamespace)"http://schemas.microsoft.com/developer/msbuild/2003" + "Compile")
                                              select (string)srcfile.Attribute("Include");

            string dir = Path.GetDirectoryName(fileName);
            string[] results = sourcefiles.ToArray<string>();

            for (int i = 0; i < results.Length; i++)
            {
                if (!Path.IsPathRooted(results[i]))
                    results[i] = Path.Combine(dir, results[i]);
            }
            return results;
        }

        /// <summary>
        /// Parse a Visual C++ projecct file
        /// </summary>
        /// <param name="fileName">the path to the VC++ project file</param>
        /// <returns>an array of source files</returns>
        public static string[] ReadVCProject(string fileName)
        {
            XDocument doc = XDocument.Load(fileName, LoadOptions.None);
            IEnumerable<string> sourcefiles = from srcfile in doc.Root.Descendants("File")
                                              where ((string)srcfile.Parent.Attribute("Name") == "Source Files" || 
                                                     (string)srcfile.Parent.Attribute("Name") == "Header Files")
                                              select (string)srcfile.Attribute("RelativePath");

            string dir = Path.GetDirectoryName(fileName);
            string[] results = sourcefiles.ToArray<string>();

            for (int i = 0; i < results.Length; i++)
            {
                if (!Path.IsPathRooted(results[i]))
                    results[i] = Path.Combine(dir, results[i]);
            }
            return results;
        }
    }
}
