using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Utilities {

    /// <summary>
    /// FileHelper contains numerous static methods for working with files
    /// </summary>
    public static class FileHelper {

        /// <summary>
        /// GetCommonPath finds the longest common path for all of the files in
        /// <paramref name="filePaths"/>that is
        /// <paramref name="startingPoint"/>or a parent of it.
        /// </summary>
        /// <param name="startingPoint">The starting point to start searching from</param>
        /// <param name="filePaths">The enumerable of file paths</param>
        /// <returns>The longest path that is common to all
        /// <paramref name="filePaths"/>. If it cannot find one, null is returned.</returns>
        public static string GetCommonPath(string startingPoint, IEnumerable<string> filePaths) {
            if(null == startingPoint)
                throw new ArgumentNullException("startingPoint");
            if(null == filePaths)
                throw new ArgumentNullException("filePaths");
            bool commonPathFound = false;
            string commonPath = startingPoint;
            while(!commonPathFound) {
                commonPathFound = filePaths.All(f => f.StartsWith(commonPath, StringComparison.OrdinalIgnoreCase));
                if(commonPathFound) {
                    break;
                }
                commonPath = Path.GetDirectoryName(commonPath);
                if(null == commonPath) {
                    break;
                }
            }
            return commonPath;
        }

        /// <summary>
        /// Finds the longest common path for all of the files in
        /// <paramref name="filePaths"/></summary>
        /// <remarks>
        /// This calls <see cref="GetCommonPath(string,IEnumerable{string})"/> where the first file
        /// in
        /// <paramref name="filePaths"/>is used as the starting point
        /// </remarks>
        /// <param name="filePaths">The enumerable of file paths</param>
        /// <returns>The longest path that is common to all
        /// <paramref name="filePaths"/>. If it cannot find one, null is returned.</returns>
        public static string GetCommonPath(IEnumerable<string> filePaths) {
            string shortest = null;
            try {
                shortest = filePaths.First();
            } catch(InvalidOperationException) {
                shortest = null;
            }

            return GetCommonPath(shortest, filePaths);
        }


        /// <summary>
        /// Finds a list of common paths from the files in
        /// <paramref name="filePaths"/></summary>
        /// <param name="filePaths">The enumerable of file paths</param>
        /// <returns>A list of common paths for all the files
        /// <paramref name="filePaths"/>. If it cannot find any paths, an empty list is returned.</returns>
        public static List<string> GetCommonPathList(IEnumerable<string> filePaths)
        {
            if (null == filePaths)
                throw new ArgumentNullException("filePaths");
           
            var filePathsList = filePaths.Select(f => Path.GetDirectoryName(f)).OrderBy(d => d.Length).ToList();
            var commonPathList = new List<string>();
            while (filePathsList.Any())
            {
                var shortestPath = filePathsList.First();
                commonPathList.Add(shortestPath);
                filePathsList.RemoveAll(f => f.StartsWith(shortestPath, StringComparison.Ordinal));
            }
            return commonPathList;
        }
    }
}