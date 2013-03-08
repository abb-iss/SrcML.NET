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
        /// <summary>
        /// Creates a new parser context
        /// </summary>
        public ParserContext() {
            ScopeStack = new Stack<Scope>();
        }

        /// <summary>
        /// The aliases for this context. This should be set by a call to <see cref="AbstractCodeParser.ParseUnitElement"/>.
        /// </summary>
        public Collection<Alias> Aliases { get; set; }

        /// <summary>
        /// The file unit for this context. This should be set by a call to <see cref="AbstractCodeParser.ParseUnitElement"/>.
        /// Alternatively, this can be set manually for calls to other Parse methods in <see cref="AbstractCodeParser"/>.
        /// </summary>
        public XElement FileUnit { get; set; }

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
        
    }
}
