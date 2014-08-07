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
using System.Xml;
using System.Xml.Serialization;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Source locations indicate where in the original source code a <see cref="AbstractProgramElement">program element</see> 
    /// is located. It stores the file name, line number, &amp; startingPosition.
    /// </summary>
    public class SourceLocation : IXmlElement {
        /// <summary>XML name for serialization</summary>
        public const string XmlName = "Location";

        /// <summary>XML name for the file attribute</summary>
        public const string XmlFileAttributeName = "file";

        /// <summary>XML name for the starting line number</summary>
        public const string XmlStartingLineAttributeName = "sl";

        /// <summary>XML name for the starting column number</summary>
        public const string XmlStartingColumnAttributeName = "sc";

        /// <summary>XML name for the ending line number</summary>
        public const string XmlEndingLineAttributeName = "el";

        /// <summary>XML name for the ending column number</summary>
        public const string XmlEndingColumnAttributeName = "ec";

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="startingLineNumber">The starting line number.</param>
        /// <param name="startingColumnNumber">The starting column within
        /// <paramref name="startingLineNumber"/></param>
        public SourceLocation(string fileName, int startingLineNumber, int startingColumnNumber)
            : this(fileName, startingLineNumber, startingColumnNumber, startingLineNumber, startingColumnNumber) { }

        /// <summary>
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="startingLineNumber">the starting line number</param>
        /// <param name="startingPosition">The starting position within
        /// <paramref name="startingLineNumber"/></param>
        /// <param name="endingLineNumber">the ending line number</param>
        /// <param name="endingPosition">The ending position with
        /// <paramref name="endingLineNumber"/></param>
        public SourceLocation(string fileName, int startingLineNumber, int startingPosition, int endingLineNumber, int endingPosition) {
            this.SourceFileName = fileName;
            this.StartingLineNumber = startingLineNumber;
            this.StartingColumnNumber = startingPosition;
            this.EndingLineNumber = endingLineNumber;
            this.EndingColumnNumber = endingPosition;
        }

        /// <summary>
        /// Creates a new empty SourceLocation
        /// </summary>
        public SourceLocation() {
        }

        /// <summary>
        /// The ending column number for this location -- this is the starting starting position of
        /// this element's sibling.
        /// </summary>
        public int EndingColumnNumber { get; set; }

        /// <summary>
        /// The ending line number for this location -- this is the starting line number of this
        /// element's sibling.
        /// </summary>
        public int EndingLineNumber { get; set; }

        /// <summary>
        /// The file name for this location
        /// </summary>
        public string SourceFileName { get; set; }

        /// <summary>
        /// The starting position within the line for this location
        /// </summary>
        public int StartingColumnNumber { get; set; }

        /// <summary>
        /// The starting line number for this location
        /// </summary>
        public int StartingLineNumber { get; set; }

        /// <summary>
        /// Determines whether the given source location occurs within this location.
        /// </summary>
        /// <param name="otherLoc">The SourceLocation to test</param>
        /// <returns>True if this location subsumes the given location, False otherwise.</returns>
        public virtual bool Contains(SourceLocation otherLoc) {
            if(otherLoc == null)
                throw new ArgumentNullException("otherLoc");

            if(string.Compare(this.SourceFileName, otherLoc.SourceFileName, StringComparison.OrdinalIgnoreCase) != 0) {
                //files not equal
                return false;
            }
            if(EndingLineNumber < otherLoc.StartingLineNumber ||
                (EndingLineNumber == otherLoc.StartingLineNumber && EndingColumnNumber <= otherLoc.StartingColumnNumber)) {
                //otherLoc starts after this location ends
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

        /// <summary> Returns the XML schema for this object. </summary>
        public System.Xml.Schema.XmlSchema GetSchema() {
            return null;
        }

        /// <summary> Returns the XML name for this object. </summary>
        public virtual string GetXmlName() { return SourceLocation.XmlName; }

        /// <summary>
        /// Read the current XML element into this object
        /// </summary>
        /// <param name="reader">The XML reader</param>
        public void ReadXml(XmlReader reader) {
            bool isEmpty = reader.IsEmptyElement;

            ReadXmlAttributes(reader);
            reader.ReadStartElement();
            
            if(!isEmpty) {
                reader.ReadEndElement();
            }
        }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected virtual void ReadXmlAttributes(XmlReader reader) {
            string attr = reader.GetAttribute(XmlFileAttributeName);
            SourceFileName = attr;

            attr = reader.GetAttribute(XmlStartingLineAttributeName);
            StartingLineNumber = (String.IsNullOrEmpty(attr) ? 1 : Int32.Parse(attr));

            attr = reader.GetAttribute(XmlStartingColumnAttributeName);
            StartingColumnNumber = (String.IsNullOrEmpty(attr) ? 1 : Int32.Parse(attr));

            attr = reader.GetAttribute(XmlEndingLineAttributeName);
            EndingLineNumber = (String.IsNullOrEmpty(attr) ? 1 : Int32.Parse(attr));

            attr = reader.GetAttribute(XmlEndingColumnAttributeName);
            EndingColumnNumber = (String.IsNullOrEmpty(attr) ? 1 : Int32.Parse(attr));
        }

        /// <summary>
        /// Writes all of the data to be serialized to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer</param>
        public virtual void WriteXml(XmlWriter writer) {
            writer.WriteAttributeString(XmlFileAttributeName, SourceFileName);
            writer.WriteAttributeString(XmlStartingLineAttributeName, StartingLineNumber.ToString());
            writer.WriteAttributeString(XmlStartingColumnAttributeName, StartingColumnNumber.ToString());
            writer.WriteAttributeString(XmlEndingLineAttributeName, EndingLineNumber.ToString());
            writer.WriteAttributeString(XmlEndingColumnAttributeName, EndingColumnNumber.ToString());
        }
    }
}