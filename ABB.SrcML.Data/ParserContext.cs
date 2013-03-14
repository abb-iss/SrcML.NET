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
            ParentScopeStack = new Stack<Scope>();
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
        /// the parent scope stack stores the parent of the scope being parsed. This is only used in specific cases such as the following C# example:
        /// <code language="C#">
        /// namespace A.B.C { }
        /// </code>
        /// In this example, we want the tree to be <c>A->B->C</c>. What <see cref="AbstractCodeParser.ParseNamespaceElement(XElement,ParserContext)"/> does
        /// in this case is create three namespaces: <c>A</c>, <c>B</c>, and <c>C</c> and puts them all on <see cref="ScopeStack"/>. Because we have created
        /// three elements, we need a way to track how many need to be popped off. the <c>A</c> namespace will be put placed on <see cref="ParentScopeStack"/>.
        /// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> will see that ParentScopeStack and <see cref="ScopeStack"/> are not equal and 
        /// it will <see cref="Stack.Pop()"/> elements off until they are.
        /// </summary>
        private Stack<Scope> ParentScopeStack { get; set; }

        /// <summary>
        /// The scope stack stores all of the scopes being parsed. When <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/>
        /// creates a scope it pushes it onto the stack. Once it has finished creating the scope (including calling
        /// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> on all of its children), it removes it from the stack.
        /// </summary>
        private Stack<Scope> ScopeStack { get; set; }

        /// <summary>
        /// The current scope on <see cref="ParentScopeStack"/>. If the stack is empty, it returns null.
        /// </summary>
        public Scope CurrentParentScope {
            get {
                if(ParentScopeStack.Count > 0)
                    return ParentScopeStack.Peek();
                return null;
            }
        }

        /// <summary>
        /// The current scope on <see cref="ScopeStack"/>. If the stack is empty, it returns null.
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
        public SrcMLLocation CreateLocation(XElement element, bool isReference) {
            var location = new SrcMLLocation(element, this.FileName, isReference);
            return location;
        }

        /// <summary>
        /// Creates a location object for the given <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to create a location for</param>
        /// <returns>The new location object. The <see cref="SourceLocation.SourceFileName"/> will be set to <see cref="FileName"/></returns>
        public SrcMLLocation CreateLocation(XElement element) {
            var location = new SrcMLLocation(element, this.FileName);
            return location;
        }

        /// <summary>
        /// Removes the most recent scope from the scope stack and returns it. If intermediate scopes were inserted, it calls <see cref="RevertToNextParent()"/>.
        /// </summary>
        /// <returns>the most recent scope.</returns>
        public Scope Pop() {
            RevertToNextParent();
            ParentScopeStack.Pop();
            return ScopeStack.Pop();
        }

        /// <summary>
        /// adds <paramref name="scope"/> to this scope stack. This simply calls <see cref="Push(Scope,Scope)"/> with both arguments set to <paramref name="scope"/>
        /// </summary>
        /// <param name="scope">the scope to add.</param>
        public void Push(Scope scope) {
            Push(scope, scope);
        }

        /// <summary>
        /// Adds <paramref name="scope"/> and <paramref name="parent">it's parent</paramref>. If <see cref="CurrentParentScope"/> is equal to <paramref name="parent"/>
        /// then parent is not added.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="parent"></param>
        public void Push(Scope scope, Scope parent) {
            ScopeStack.Push(scope);
            if(parent != CurrentParentScope) {
                ParentScopeStack.Push(parent);
            }
        }

        /// <summary>
        /// Removes scopes until <c>CurrentScope == CurrentParentScope</c>. As each scope is removed, it is added as a child to its predecessor.
        /// </summary>
        public void RevertToNextParent() {
            while(CurrentScope != CurrentParentScope) {
                var scope = ScopeStack.Pop();
                CurrentScope.AddChildScope(scope);
            }
        }

        
    }
}
