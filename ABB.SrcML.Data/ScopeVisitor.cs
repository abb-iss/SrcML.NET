using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class ScopeVisitor {
        AbstractCodeParser Parser;

        public XElement FileUnit;
        public VariableScope CurrentScope { get { return ScopeStack.Peek(); } }
        public Stack<VariableScope> ScopeStack;
        
        public ScopeVisitor(AbstractCodeParser parser, XElement fileUnit) {
            this.Parser = parser;
            this.FileUnit = fileUnit;
            this.ScopeStack = new Stack<VariableScope>();
            
        }

        public IEnumerable<VariableScope> Visit(XElement element) {
            var scopeForElement = CreateScope(element);
            if(ScopeStack.Count > 0) {
                CurrentScope.AddChildScope(scopeForElement);
            }
            ScopeStack.Push(scopeForElement);

            foreach(var variable in Parser.GetVariableDeclarationsFromContainer(element, FileUnit)) {
                CurrentScope.AddDeclaredVariable(variable);
            }

            foreach(var child in Parser.GetChildContainers(element)) {
                foreach(var childScope in Visit(child)) {
                    yield return childScope;
                }
            }

            yield return ScopeStack.Pop();
        }

        public VariableScope CreateScope(XElement element) {
            VariableScope scope;
            if(element.Name == SRC.Unit) {
                scope = VisitFile(element);
            } else if(Parser.TypeElementNames.Contains(element.Name)) {
                scope = VisitType(element);
            } else if(Parser.NamespaceElementNames.Contains(element.Name)) {
                scope = VisitNamespace(element);
            } else if(Parser.MethodElementNames.Contains(element.Name)) {
                scope = VisitMethod(element);
            } else {
                scope = VisitScope(element);
            }
            scope.XPath = element.GetXPath(false);
            return scope;
        }

        public VariableScope VisitFile(XElement fileUnit) {
            FileUnit = fileUnit;
            return Parser.CreateScopeFromFile(fileUnit);
        }

        TypeDefinition VisitType(XElement element) {
            return Parser.CreateTypeDefinition(element, FileUnit);
        }

        MethodDefinition VisitMethod(XElement element) {
            return Parser.CreateMethodDefinition(element, FileUnit);
        }

        NamespaceDefinition VisitNamespace(XElement element) {
            return Parser.CreateNamespaceDefinition(element, FileUnit);
        }
        VariableScope VisitScope(XElement element) {
            return Parser.CreateScopeFromContainer(element, FileUnit);
        }
    }
}
