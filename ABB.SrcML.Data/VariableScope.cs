using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class VariableScope {
        protected Collection<VariableScope> ChildScopeCollection;
        protected Dictionary<string, VariableDeclaration> DeclaredVariablesDictionary;


        public VariableScope ParentScope { get; set; }
        public IEnumerable<VariableScope> ChildScopes { get { return this.ChildScopeCollection.AsEnumerable(); } }
        public IEnumerable<VariableDeclaration> DeclaredVariables { get { return this.DeclaredVariablesDictionary.Values.AsEnumerable(); } }
        
        
        public string XPath { get; set; }

        public VariableScope() {
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>();
            ChildScopeCollection = new Collection<VariableScope>();
        }

        public void AddChildScope(VariableScope childScope) {
            ChildScopeCollection.Add(childScope);
            childScope.ParentScope = this;
        }

        public void AddDeclaredVariable(VariableDeclaration declaration) {
            DeclaredVariablesDictionary[declaration.Name] = declaration;
            declaration.Scope = this;
        }

        public bool IsScopeFor(XElement element) {
            return IsScopeFor(element.GetXPath(false));
        }

        public bool IsScopeFor(string xpath) {
            return xpath.StartsWith(this.XPath);
        }

        public IEnumerable<VariableScope> GetScopesForPath(string xpath) {
            if(IsScopeFor(xpath)) {
                yield return this;

                foreach(var child in this.ChildScopes) {
                    foreach(var matchingScope in child.GetScopesForPath(xpath)) {
                        yield return matchingScope;
                    }
                }
            }
        }

        public IEnumerable<VariableDeclaration> GetDeclarationsForVariableName(string variableName, string xpath) {
            foreach(var scope in GetScopesForPath(xpath)) {
                VariableDeclaration declaration;
                if(scope.DeclaredVariablesDictionary.TryGetValue(variableName, out declaration)) {
                    yield return declaration;
                }
            }
        }

        private static HashSet<XName> _containerNames = new HashSet<XName>(new XName[] {
            SRC.Block, SRC.Catch, SRC.Class, SRC.Constructor, SRC.Destructor,
            SRC.Do, SRC.Else, SRC.Enum, SRC.Extern, SRC.For, SRC.Function, SRC.If,
            SRC.Namespace, SRC.Struct, SRC.Switch, SRC.Template, SRC.Then, SRC.Try,
            SRC.Typedef, SRC.Union, SRC.Unit, SRC.While
        });

        private static HashSet<XName> _typeContainerNames = new HashSet<XName>(new XName[] {
            SRC.Class, SRC.Enum, SRC.Struct, SRC.Union
        });

        private static HashSet<XName> _specifierContainerNames = new HashSet<XName>(new XName[] {
            SRC.Private, SRC.Protected, SRC.Public
        });

        public static HashSet<XName> Containers { get { return _containerNames; } }
        public static HashSet<XName> TypeContainers { get { return _typeContainerNames; } }
        public static HashSet<XName> SpecifierContainers { get { return _specifierContainerNames; } }
    }
}
