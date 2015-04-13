/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Utilities {
    /// <summary>
    /// A helper class for recovering the before/after version of a SrcML file from SrcDiff output.
    /// </summary>
    public static class SrcDiffFilter {

        /// <summary>
        /// Recovers the "before" SrcML from a SrcDiff representation.
        /// </summary>
        /// <param name="xml">The root element of the SrcDiff XML to filter.</param>
        /// <returns>A copy of the input XML, with the diff elements removed, representing the original file.</returns>
        public static XElement GetBeforeVersion(XElement xml) {
            if(xml == null) { throw new ArgumentNullException("xml"); }

            var root = new XElement(xml);

            //trim any nodes that were added (but keeping any common elements that might be within)
            foreach(var addElement in root.Descendants(DIFF.Insert).ToList()) {
                var commonElements = addElement.Descendants(DIFF.Common).ToList();
                if(commonElements.Count > 0) {
                    addElement.ReplaceWith(commonElements.SelectMany(ce => ce.Nodes()).ToList());
                } else {
                    addElement.Remove();
                }
            }

            //add back any nodes that were removed
            foreach(var deleteElement in root.Descendants(DIFF.Delete).ToList()) {
                deleteElement.ReplaceWith(deleteElement.Nodes());
            }

            //remove the diff tags from any remaining common elements
            foreach(var commonElement in root.Descendants(DIFF.Common).ToList()) {
                commonElement.ReplaceWith(commonElement.Nodes());
            }

            return root;
        }

        /// <summary>
        /// Recovers the "after" SrcML from a SrcDiff representation.
        /// </summary>
        /// <param name="xml">The root element of the SrcDiff XML to filter.</param>
        /// <returns>A copy of the input XML, with the diff elements removed, representing the modified file.</returns>
        public static XElement GetAfterVersion(XElement xml) {
            if(xml == null) { throw new ArgumentNullException("xml"); }

            var root = new XElement(xml);

            //trim any nodes that were deleted (but keeping any common elements that might be within)
            foreach(var deleteElement in root.Descendants(DIFF.Delete).ToList()) {
                var commonElements = deleteElement.Descendants(DIFF.Common).ToList();
                if(commonElements.Count > 0) {
                    deleteElement.ReplaceWith(commonElements.SelectMany(ce => ce.Nodes()).ToList());
                } else {
                    deleteElement.Remove();
                }
            }

            //add the nodes that were added
            foreach(var addElement in root.Descendants(DIFF.Insert).ToList()) {
                addElement.ReplaceWith(addElement.Nodes());
            }

            //remove the diff tags from any remaining common elements
            foreach(var commonElement in root.Descendants(DIFF.Common).ToList()) {
                commonElement.ReplaceWith(commonElement.Nodes());
            }

            return root;
        }
    }
}
