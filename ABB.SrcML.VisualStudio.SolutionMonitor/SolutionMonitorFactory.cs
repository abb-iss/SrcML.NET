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
    class SolutionMonitorFactory
    {
        ////private const string Lucene = "\\lucene";
        ////public static string LuceneDirectory { get; set; }

        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// TODO: isIndexRecreationRequired: specific for Sando
        /// </summary>
        /// <param name="isIndexRecreationRequired"></param>
        /// <returns></returns>
        public static SolutionMonitor CreateMonitor(bool isIndexRecreationRequired)
        {
            ////var openSolution = UIPackage.GetOpenSolution();     //// Use my own GetOpenSolution()
            var openSolution = GetOpenSolution();
            return CreateMonitor(openSolution, isIndexRecreationRequired);
        }

        /// <summary>
        /// Constructor of SolutionMonitorFactory
        /// TODO: isIndexRecreationRequired: specific for Sando
        /// </summary>
        /// <param name="openSolution"></param>
        /// <param name="isIndexRecreationRequired"></param>
        /// <returns></returns>
        private static SolutionMonitor CreateMonitor(Solution openSolution, bool isIndexRecreationRequired)
        {
            Contract.Requires(openSolution != null, "A solution must be open");

            ////TODO if solution is reopen - the guid should be read from file - future change
            ////SolutionKey solutionKey = new SolutionKey(Guid.NewGuid(), openSolution.FileName, GetLuceneDirectoryForSolution(openSolution));
            SolutionKey solutionKey = new SolutionKey(Guid.NewGuid(), openSolution.FileName);   //// Remove indexer parts
            ////var currentIndexer = DocumentIndexerFactory.CreateIndexer(solutionKey, AnalyzerType.Snowball);
            ////if (isIndexRecreationRequired)
            ////{
            ////    currentIndexer.DeleteDocuments("*");
            ////    currentIndexer.CommitChanges();
            ////}
            ////var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution), solutionKey, currentIndexer, isIndexRecreationRequired);
            var currentMonitor = new SolutionMonitor(SolutionWrapper.Create(openSolution), solutionKey);    //// Remove indexer parts
            return currentMonitor;
        }

        /* //// Remove index part
        private static string CreateLuceneFolder()
        {
            Contract.Requires(LuceneDirectory != null, "Please set the LuceneDirectory before calling this method");
            return CreateFolder(Lucene, LuceneDirectory);
        }
        */

        //// Used by create lucene folder
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

        //// Used by create lucene folder
        private static string GetName(Solution openSolution)
        {
            var fullName = openSolution.FullName;
            var split = fullName.Split('\\');
            return split[split.Length - 1] + fullName.GetHashCode();
        }

        /* //// Remove index part
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