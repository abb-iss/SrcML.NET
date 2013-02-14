using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Source locations indicate where in the original source code a <see cref="VariableScope"/> is located
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
        /// Creates a new source location object
        /// </summary>
        /// <param name="fileName">The filename</param>
        /// <param name="lineNumber">the line number</param>
        /// <param name="position">The position within the line</param>
        /// <param name="xpath">The XPath that identifies this location</param>
        public SourceLocation(string fileName, int lineNumber, int position, string xpath) {
            this.SourceFileName = fileName;
            this.SourceLineNumber = lineNumber;
            this.SourceColumnNumber = position;
            this.XPath = xpath;
        }

        /// <summary>
        /// Creates a new source location object based on the given <see cref="System.Xml.Linq.XElement">XML element</see> and <see cref="ABB.SrcML.SRC.Unit">file unit</see>
        /// </summary>
        /// <param name="element">The element (should contain <see cref="ABB.SrcML.POS"/> attributes</param>
        /// <param name="fileUnit">The file unit (must be a <see cref="ABB.SrcML.SRC.Unit"/>)</param>
        public SourceLocation(XElement element, XElement fileUnit) {
            this.SourceFileName = SrcMLElement.GetFileNameForUnit(fileUnit);
            this.SourceLineNumber = element.GetSrcLineNumber();
            this.SourceColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
        }

        /// <summary>
        /// Returns a string representation of the SourceLocation.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}: line {1}, column {2}", SourceFileName, SourceLineNumber, SourceColumnNumber);
        }
    }
}
