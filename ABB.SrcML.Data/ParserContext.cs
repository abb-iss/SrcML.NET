using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Parser context objects store the current state of the <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> method.
    /// </summary>
    public class ParserContext {
        private XElement fileUnitBeingParsed;

        /// <summary>
        /// Creates a new parser context
        /// </summary>
        public ParserContext() : this(null) { }

        /// <summary>
        /// Creates a new parser context
        /// </summary>
        /// <param name="fileUnit">The file unit for this context</param>
        public ParserContext(XElement fileUnit) {
            this.FileUnit = fileUnit;
            ScopeStack = new Stack<Scope>();
        }
        /// <summary>
        /// The aliases for this context. This should be set by a call to <see cref="AbstractCodeParser.ParseUnitElement"/>.
        /// </summary>
        public Collection<Alias> Aliases { get; set; }

        /// <summary>
        /// The file name from <see cref="FileUnit"/>
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The file unit for this context. This should be set by a call to <see cref="AbstractCodeParser.ParseUnitElement"/>.
        /// Alternatively, this can be set manually for calls to other Parse methods in <see cref="AbstractCodeParser"/>.
        /// </summary>
        public XElement FileUnit {
            get { return this.fileUnitBeingParsed; }
            set {
                if(null != value) {
                    if(value.Name != SRC.Unit) throw new ArgumentException("must be a SRC.Unit", "value");
                    this.FileName = SrcMLElement.GetFileNameForUnit(value);
                } else {
                    this.FileName = string.Empty;
                }
                this.fileUnitBeingParsed = value;
            }
        }

        /// <summary>
        /// The scope stack stores all of the scopes being parsed. When <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/>
        /// creates a scope it pushes it onto the stack. Once it has finished creating the scope (including calling
        /// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> on all of its children), it removes it from the stack.
        /// </summary>
        public Stack<Scope> ScopeStack { get; set; }

        /// <summary>
        /// The current scope on <see cref="ScopeStack"/>. If the scope is empty, it returns null.
        /// </summary>
        public Scope CurrentScope {
            get {
                if(ScopeStack.Count > 0)
                    return ScopeStack.Peek();
                return null;
            }
        }

        /// <summary>
        /// Creates a location object for the given <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to create a location for</param>
        /// <param name="isReference">whether or not this is a reference location</param>
        /// <returns>The new location object. The <see cref="SourceLocation.SourceFileName"/> will be set to <see cref="FileName"/></returns>
        public SourceLocation CreateLocation(XElement element, bool isReference) {
            var location = new SourceLocation(element, this.FileName, isReference);
            return location;
        }

        /// <summary>
        /// Creates a location object for the given <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to create a location for</param>
        /// <returns>The new location object. The <see cref="SourceLocation.SourceFileName"/> will be set to <see cref="FileName"/></returns>
        public SourceLocation CreateLocation(XElement element) {
            var location = new SourceLocation(element, this.FileName);
            return location;
        }
    }
}
