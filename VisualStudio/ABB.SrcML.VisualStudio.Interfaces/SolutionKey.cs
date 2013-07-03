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

namespace ABB.SrcML.VisualStudio
{
    /// <summary>
    /// Now most likely this class would not be needed any more in SrcML.NET. Sando would maintain its own SolutionKey class.
    /// </summary>
    public class SolutionKey
    {
        /// <summary>
        /// New constructor.
        /// Removed the indexPath.
        /// </summary>
        /// <param name="solutionId"></param>
        /// <param name="solutionPath"></param>
        public SolutionKey(Guid solutionId, string solutionPath)
        {
            Contract.Requires(solutionId != null, "SolutionKey:Constructor - solution id cannot be null!");
            Contract.Requires(solutionId != Guid.Empty, "SolutionKey:Constructor - solution id cannot be an empty guid!");
            Contract.Requires(!String.IsNullOrWhiteSpace(solutionPath), "SolutionKey:Constructor - solution path cannot be null or an empty string!");

            this.solutionId = solutionId;
            this.solutionPath = solutionPath;
        }

        /* //// Original implementation
        public SolutionKey(Guid solutionId, string solutionPath, string indexPath)
        {
            Contract.Requires(solutionId != null, "SolutionKey:Constructor - solution id cannot be null!");
            Contract.Requires(solutionId != Guid.Empty, "SolutionKey:Constructor - solution id cannot be an empty guid!");
            Contract.Requires(!String.IsNullOrWhiteSpace(solutionPath), "SolutionKey:Constructor - solution path cannot be null or an empty string!");
            Contract.Requires(!String.IsNullOrWhiteSpace(indexPath), "SolutionKey:Constructor - index path cannot be null or an empty string!");

            this.solutionId = solutionId;
            this.solutionPath = solutionPath;
            this.indexPath = indexPath;
        }
        */

        /// <summary>
        /// Return the solution ID.
        /// </summary>
        /// <returns></returns>
        public Guid GetSolutionId()
        {
            return this.solutionId;
        }

        /* //// Remove code about index
        public string GetIndexPath()
        {
            return indexPath;
        }
        */

        /// <summary>
        /// Return the solution path
        /// </summary>
        /// <returns></returns>
        public string GetSolutionPath()
        {
            return solutionPath;
        }

        private Guid solutionId;
        private string solutionPath;
        /* //// Remove code about index
        private string indexPath;
        */
    }
}
