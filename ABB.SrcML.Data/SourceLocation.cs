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
    [Serializable]
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
        /// The ending line number for this location -- this is the starting line number of this element's sibling.
        /// </summary>
        public int EndingLineNumber { get; set; }

        /// <summary>
        /// The ending column number for this location -- this is the starting starting position of this element's sibling.
        /// </summary>
        public int EndingColumnNumber { get; set; }

        /// <summary>
        /// Creates a new empty SourceLocation
        /// </summary>
        protected SourceLocation() {}

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="startingLineNumber">The starting line number.</param>
        /// <param name="startingColumnNumber">The starting column within <paramref name="startingLineNumber"/></param>
        public SourceLocation(string fileName, int startingLineNumber, int startingColumnNumber)
            : this(fileName, startingLineNumber, startingColumnNumber, startingLineNumber, startingColumnNumber) {}

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="startingLineNumber">the starting line number</param>
        /// <param name="startingPosition">The starting position within <paramref name="startingLineNumber"/></param>
        /// <param name="endingLineNumber">the ending line number</param>
        /// <param name="endingPosition">The ending position with <paramref name="endingLineNumber"/></param>
        public SourceLocation(string fileName, int startingLineNumber, int startingPosition, int endingLineNumber, int endingPosition) {
            this.SourceFileName = fileName;
            this.StartingLineNumber = startingLineNumber;
            this.StartingColumnNumber = startingPosition;
            this.EndingLineNumber = endingLineNumber;
            this.EndingColumnNumber = endingPosition;
        }

        /// <summary>
        /// Determines whether the given source location occurs within this location.
        /// </summary>
        /// <param name="otherLoc">The SourceLocation to test</param>
        /// <returns>True if this location subsumes the given location, False otherwise.</returns>
        public virtual bool Contains(SourceLocation otherLoc) {
            if(otherLoc == null) throw new ArgumentNullException("otherLoc");

            if(string.Compare(this.SourceFileName, otherLoc.SourceFileName, StringComparison.InvariantCultureIgnoreCase) != 0) {
                //files not equal
                return false;
            }
            if((StartingLineNumber < otherLoc.StartingLineNumber ||
                (StartingLineNumber == otherLoc.StartingLineNumber && StartingColumnNumber <= otherLoc.StartingColumnNumber))
               &&
               (otherLoc.EndingLineNumber < EndingLineNumber ||
                (otherLoc.EndingLineNumber == EndingLineNumber && otherLoc.EndingColumnNumber <= EndingColumnNumber))) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a string representation of the SourceLocation.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}: start({1},{2}) end({3},{4})", SourceFileName, StartingLineNumber, StartingColumnNumber, EndingLineNumber, EndingColumnNumber);
        }

    }
}
