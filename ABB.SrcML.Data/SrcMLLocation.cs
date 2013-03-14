using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a location in a SrcML document. 
    /// This extends SourceLocation to include an XPath, and other relevant properties.
    /// </summary>
    public class SrcMLLocation : SourceLocation {
        /// <summary>
        /// The XPath query that identifies this scope
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// True if this location is a reference; false if it is a definition
        /// </summary>
        public bool IsReference { get; set; }

        /// <summary>
        /// Creates a new srcML location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        public SrcMLLocation(XElement element, string fileName) : this(element, fileName, false) { }

        /// <summary>
        /// Creates a new srcML location object based on the given <see cref="System.Xml.Linq.XElement">XML element</see> and <see cref="ABB.SrcML.SRC.Unit">file unit</see>
        /// </summary>
        /// <param name="element">The element (should contain <see cref="ABB.SrcML.POS"/> attributes</param>
        /// <param name="fileUnit">The file unit (must be a <see cref="ABB.SrcML.SRC.Unit"/>)</param>
        public SrcMLLocation(XElement element, XElement fileUnit) : this(element, fileUnit, false) { }

        /// <summary>
        /// Creates a new srcML location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileUnit">The file unit that contains <paramref name="element"/></param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SrcMLLocation(XElement element, XElement fileUnit, bool isReferenceLocation) {
            if(element == null) throw new ArgumentNullException("element");
            if(fileUnit == null) throw new ArgumentNullException("fileUnit");
            this.SourceFileName = SrcMLElement.GetFileNameForUnit(fileUnit);
            this.StartingLineNumber = element.GetSrcLineNumber();
            this.StartingColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
            SetEndingLocation(element);
        }

        /// <summary>
        /// Creates a new srcML location object
        /// </summary>
        /// <param name="element">The srcML element that this location refers to</param>
        /// <param name="fileName">The filename</param>
        /// <param name="isReferenceLocation">true if this is a reference location; false otherwise</param>
        public SrcMLLocation(XElement element, string fileName, bool isReferenceLocation) {
            if(element == null) throw new ArgumentNullException("element");
            this.SourceFileName = fileName;
            this.StartingLineNumber = element.GetSrcLineNumber();
            this.StartingColumnNumber = element.GetSrcLinePosition();
            this.XPath = element.GetXPath(false);
            this.IsReference = isReferenceLocation;
            SetEndingLocation(element);
        }

        /// <summary>
        /// Determines whether the given source location occurs within this location.
        /// This will be determined using the XPath, if set.
        /// </summary>
        /// <param name="otherLoc">The SourceLocation to test</param>
        /// <returns>True if this location subsumes the given location, False otherwise.</returns>
        public override bool Contains(SourceLocation otherLoc) {
            if(otherLoc == null) throw new ArgumentNullException("otherLoc");

            var otherSrcMLLoc = otherLoc as SrcMLLocation;
            if(otherSrcMLLoc != null && !string.IsNullOrWhiteSpace(XPath) && !string.IsNullOrWhiteSpace(otherSrcMLLoc.XPath)) {
                //return XPath.StartsWith(otherSrcMLLoc.XPath);
                return otherSrcMLLoc.XPath.StartsWith(this.XPath);
            }
            return base.Contains(otherLoc);
        }


        private void SetEndingLocation(XElement element) {
            var nextSibling = element.ElementsAfterSelf().FirstOrDefault();
            if(null != nextSibling) {
                this.EndingLineNumber = nextSibling.GetSrcLineNumber();
                this.EndingColumnNumber = nextSibling.GetSrcLinePosition();
            } else {
                this.EndingLineNumber = int.MaxValue;
                this.EndingColumnNumber = int.MaxValue;
            }
        }
    }
}
