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
    /// It stores the file name, line number, &amp; position
    /// </summary>
    public class SourceLocation {
        /// <summary>
        /// The file name for this location
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// The line number for this location
        /// </summary>
        public int SourceLineNumber { get; set; }

        /// <summary>
        /// The position within the line for this location
        /// </summary>
        public int SourceColumnNumber { get; set; }

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
        /// <param name="lineNumber">the line number</param>
        /// <param name="position">The position within the line</param>
        /// <param name="xpath">The XPath that identifies this location</param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(string fileName, int lineNumber, int position, string xpath, bool isReferenceLocation) {
            this.SourceFileName = fileName;
            this.SourceLineNumber = lineNumber;
            this.SourceColumnNumber = position;
            this.XPath = xpath;
            this.IsReference = isReferenceLocation;
        }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(XElement element, string fileName, bool isReferenceLocation) {
            this.SourceFileName = fileName;
            this.SourceLineNumber = element.GetSrcLineNumber();
            this.SourceColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
        }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        public SourceLocation(XElement element, string fileName) : this(element, fileName, false) { }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileUnit">The file unit that contains <paramref name="element"/></param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SourceLocation(XElement element, XElement fileUnit, bool isReferenceLocation) {
            this.SourceFileName = SrcMLElement.GetFileNameForUnit(fileUnit);
            this.SourceLineNumber = element.GetSrcLineNumber();
            this.SourceColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
        }

        /// <summary>
        /// Creates a new source location object based on the given <see cref="System.Xml.Linq.XElement">XML element</see> and <see cref="ABB.SrcML.SRC.Unit">file unit</see>
        /// </summary>
        /// <param name="element">The element (should contain <see cref="ABB.SrcML.POS"/> attributes</param>
        /// <param name="fileUnit">The file unit (must be a <see cref="ABB.SrcML.SRC.Unit"/>)</param>
        public SourceLocation(XElement element, XElement fileUnit) : this(element, fileUnit, false) { }

        /// <summary>
        /// Returns a string representation of the SourceLocation.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}: line {1}, column {2}", SourceFileName, SourceLineNumber, SourceColumnNumber);
        }
    }
}
