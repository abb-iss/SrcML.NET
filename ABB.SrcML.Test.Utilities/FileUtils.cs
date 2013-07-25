/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Threading.Tasks;

namespace ABB.SrcML.Test.Utilities
{
    public class FileUtils
    {
        public static void CopyDirectory(string sourcePath, string destinationPath) {
            foreach(var fileTemplate in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories)) {
                var fileName = fileTemplate.Replace(sourcePath, destinationPath);
                var directoryName = Path.GetDirectoryName(fileName);
                if(!Directory.Exists(directoryName)) {
                    Directory.CreateDirectory(directoryName);
                }
                File.Copy(fileTemplate, fileName);
            }
        }

        public static string GetSolutionDirectory(string solutionRelativeToRoot) {
            var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
            while(currentDirectory != null && !File.Exists(Path.Combine(currentDirectory.FullName, solutionRelativeToRoot))) {
                currentDirectory = currentDirectory.Parent;
            }
            return currentDirectory.FullName;
        }
    }
}
