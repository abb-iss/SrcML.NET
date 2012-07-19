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

namespace ABB.SrcML
{
    /// <summary>
    /// This class is a wrapper for querying and transforming a document using an ITransform.
    /// </summary>
    public sealed class TransformRunner
    {
        private TransformRunner()
        {

        }

        /// <summary>
        /// Run the query against the given document.
        /// </summary>
        /// <param name="document">The document to query.</param>
        /// <param name="transform">The transform containing the Query.</param>
        /// <returns>The list of matching nodes.</returns>
        public static IEnumerable<XElement> RunQuery(SrcMLFile document, ITransform transform)
        {
            if (null == document)
                throw new ArgumentNullException("document");

            return document.QueryEachUnit(transform);
        }

        /// <summary>
        /// Runs the transform against the list of elements.
        /// </summary>
        /// <param name="elements">The elements to transform.</param>
        /// <param name="transform">The transform containing the <see cref="ITransform.Transform"/>.</param>
        /// <returns>The list of transformed nodes.</returns>
        public static IEnumerable<XElement> RunTransform(IEnumerable<XElement> elements, ITransform transform)
        {
            foreach (var e in elements)
                yield return transform.Transform(new XElement(e));
        }
    }
}
