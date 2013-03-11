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
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Source locations indicate where in the original source code a <see cref="Scope"/> is located
    /// It stores the file name, line number, &amp; startingPosition
    /// </summary>
    public class SourceLocation {
        /// <summary>
        /// The file name for this location
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// The starting line number for this location
        /// </summary>
        public int StartingLineNumber { get; set; }

        /// <summary>
        /// The starting position within the line for this location
        /// </summary>
        public int StartingColumnNumber { get; set; }

        /// <summary>
        /// The ending line number for this location &mdash; this is the starting line number of this element's sibling.
        /// </summary>
        public int EndingLineNumber { get; set; }

        /// <summary>
        /// The ending column number for this location &mdash; this is the starting starting position of this element's sibling.
        /// </summary>
        public int EndingColumnNumber { get; set; }

        /// <summary>
        /// The XPath query that identifies this scope
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// True if this location is a reference; false if it is a definition
        /// </summary>
        public bool IsReference { get; set; }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="startingLineNumber">the starting line number</param>
        /// <param name="startingPosition">The starting position within <paramref name="startingLineNumber"/></param>
        /// <param name="endingLineNumber">the ending line number</param>
        /// <param name="endingPosition">The ending position with <paramref name="endingLineNumber"/></param>
        /// <param name="xpath">The XPath that identifies this location</param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(string fileName, int startingLineNumber, int startingPosition, int endingLineNumber, int endingPosition, string xpath, bool isReferenceLocation) {
            this.SourceFileName = fileName;
            this.StartingLineNumber = startingLineNumber;
            this.StartingColumnNumber = startingPosition;
            this.EndingLineNumber = endingLineNumber;
            this.EndingColumnNumber = endingPosition;
            this.XPath = xpath;
            this.IsReference = isReferenceLocation;
        }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        public SourceLocation(XElement element, string fileName) : this(element, fileName, false) { }

        /// <summary>
        /// Creates a new source location object based on the given <see cref="System.Xml.Linq.XElement">XML element</see> and <see cref="ABB.SrcML.SRC.Unit">file unit</see>
        /// </summary>
        /// <param name="element">The element (should contain <see cref="ABB.SrcML.POS"/> attributes</param>
        /// <param name="fileUnit">The file unit (must be a <see cref="ABB.SrcML.SRC.Unit"/>)</param>
        public SourceLocation(XElement element, XElement fileUnit) : this(element, fileUnit, false) { }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileUnit">The file unit that contains <paramref name="element"/></param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(XElement element, XElement fileUnit, bool isReferenceLocation) {
            this.SourceFileName = SrcMLElement.GetFileNameForUnit(fileUnit);
            this.StartingLineNumber = element.GetSrcLineNumber();
            this.StartingColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
            SetEndingLocation(element);
        }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(XElement element, string fileName, bool isReferenceLocation) {
            this.SourceFileName = fileName;
            this.StartingLineNumber = element.GetSrcLineNumber();
            this.StartingColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
            SetEndingLocation(element);
        }

        /// <summary>
        /// Returns a string representation of the SourceLocation.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}: start({0},{1}) end({2},{3})", SourceFileName, StartingLineNumber, StartingColumnNumber, EndingLineNumber, EndingColumnNumber);
        }

        private void SetEndingLocation(XElement element) {
            var nextSibling = element.ElementsAfterSelf().FirstOrDefault();
            if(null != nextSibling) {
                this.EndingLineNumber = nextSibling.GetSrcLineNumber();
                this.EndingColumnNumber = nextSibling.GetSrcLinePosition();
            } else {
                this.EndingLineNumber = Int32.MaxValue;
                this.EndingColumnNumber = Int32.MaxValue;
            }
        }
    }
}
