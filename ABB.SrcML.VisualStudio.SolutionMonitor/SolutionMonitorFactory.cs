/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Jiang Zheng (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.Diagnostics.Contracts;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
////using Sando.Core;
////using Sando.Indexer;

namespace ABB.SrcML.VisualStudio.SolutionMonitor
{
    /// <summary>
    /// This class was from Sando.
    /// Now most likely this class would not be needed any more in SrcML.NET. Sando would maintain its own SolutionMonitorFactory class.
    /// </summary>
    public class SolutionMonitorFactory
    {
        ////private const string Lucene = "\\lucene";
        ////public static string LuceneDirectory { get; set; }

        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// </summary>
        /// <returns></returns>
        ////public static SolutionMonitor CreateMonitor(bool isIndexRecreationRequired)
        public static SolutionMonitor CreateMonitor()
        {
            ////var openSolution = UIPackage.GetOpenSolution();
            var openSolution = GetOpenSolution();   // Use my own GetOpenSolution()
            ////return CreateMonitor(openSolution, isIndexRecreationRequired);
            return CreateMonitor(openSolution);
        }

        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// </summary>
        /// <param name="openSolution"></param>
        /// <returns></returns>
        ////private static SolutionMonitor CreateMonitor(Solution openSolution, bool isIndexRecreationRequired)
        private static SolutionMonitor CreateMonitor(Solution openSolution)
        {
            Contract.Requires(openSolution != null, "A solution must be open");

            ////TODO if solution is reopen - the guid should be read from file - future change
            ////SolutionKey solutionKey = new SolutionKey(Guid.NewGuid(), openSolution.FileName, GetLuceneDirectoryForSolution(openSolution));
            //SolutionKey solutionKey = new SolutionKey(Guid.NewGuid(), openSolution.FileName);
            ////var currentIndexer = DocumentIndexerFactory.CreateIndexer(solutionKey, AnalyzerType.Snowball);
            ////if (isIndexRecreationRequired)
            ////{
            ////    currentIndexer.DeleteDocuments("*");
            ////    currentIndexer.CommitChanges();
            ////}
            ////var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution), solutionKey, currentIndexer, isIndexRecreationRequired);
            var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution));    // Remove code about index
            return currentMonitor;
        }

        /* //// Remove code about index
        private static string CreateLuceneFolder()
        {
            Contract.Requires(LuceneDirectory != null, "Please set the LuceneDirectory before calling this method");
            return CreateFolder(Lucene, LuceneDirectory);
        }

        private static string CreateFolder(string folderName, string parentDirectory)
        {
            if (!File.Exists(parentDirectory + folderName))
            {
                var directoryInfo = Directory.CreateDirectory(parentDirectory + folderName);
                return directoryInfo.FullName;
            }
            else
            {
                return parentDirectory + folderName;
            }
        }

        private static string GetName(Solution openSolution)
        {
            var fullName = openSolution.FullName;
            var split = fullName.Split('\\');
            return split[split.Length - 1] + fullName.GetHashCode();
        }

        private static string GetLuceneDirectoryForSolution(Solution openSolution)
        {
            var luceneFolder = CreateLuceneFolder();
            CreateFolder(GetName(openSolution), luceneFolder + "\\");
            return luceneFolder + "\\" + GetName(openSolution);
        }
        */

        /// <summary>
        /// Get the open solution.
        /// Copied from UIPackage
        /// </summary>
        /// <returns></returns>
        public static Solution GetOpenSolution()
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            if (dte != null)
            {
                var openSolution = dte.Solution;
                return openSolution;
            }
            else
            {
                return null;
            }
        }
    }
}