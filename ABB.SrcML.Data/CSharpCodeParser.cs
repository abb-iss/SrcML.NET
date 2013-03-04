/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Provides parsing facilities for the C# language
    /// </summary>
    public class CSharpCodeParser : AbstractCodeParser {

        public CSharpCodeParser() :base() {
            this.TypeElementNames = new HashSet<XName> {SRC.Class, SRC.Enum, SRC.Struct}; //SRC.Interface?
            //TODO: what else needs to be set here?
        }

        /// <summary>
        /// Returns <c>Language.CSharp</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.CSharp; }
        }

        /// <summary>
        /// Creates a NamespaceDefinition object for the given namespace element. This must be one of the element types defined in NamespaceElementNames.
        /// </summary>
        /// <param name="namespaceElement">The namespace element</param>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>A new NamespaceDefinition object</returns>
        public override NamespaceDefinition CreateNamespaceDefinition(XElement namespaceElement, XElement fileUnit) {
            if(namespaceElement == null)
                throw new ArgumentNullException("namespaceElement");
            if(!NamespaceElementNames.Contains(namespaceElement.Name))
                throw new ArgumentException(string.Format("Not a valid namespace element: {0}", namespaceElement.Name), "namespaceElement");

            var nameElement = namespaceElement.Element(SRC.Name);
            string namespaceName;
            if(nameElement == null) {
                namespaceName = string.Empty;
            } else {
                namespaceName = NameHelper.GetLastName(nameElement);
            }
            var namespaceDef = new NamespaceDefinition { Name = namespaceName };

            var prefix = CreateNamespaceUsePrefix(nameElement, fileUnit);
            if(prefix != null) {
                namespaceDef.ParentScopeCandidates.Add(prefix);
            }
            return namespaceDef;
        }

        
        /// <summary>
        /// Creates a global NamespaceDefinition for the file.
        /// </summary>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>A global NamespaceDefinition object</returns>
        public override Scope CreateScopeFromFile(XElement fileUnit) {
            return new NamespaceDefinition();
        }

        /// <summary>
        /// Gets the access modifier for this type element
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The access modifier for the given type element.</returns>
        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            if(typeElement == null)
                throw new ArgumentNullException("typeElement");
            if(!TypeElementNames.Contains(typeElement.Name))
                throw new ArgumentException(string.Format("Not a valid type element: {0}", typeElement.Name), "typeElement");

            var accessModifierMap = new Dictionary<string, AccessModifier>()
                                    {
                                        {"public", AccessModifier.Public},
                                        {"private", AccessModifier.Private},
                                        {"protected", AccessModifier.Protected},
                                        {"internal", AccessModifier.Internal}
                                    };
            var specifiers = typeElement.Elements(SRC.Specifier).ToList();
            AccessModifier result;
            if(!specifiers.Any()) {
                result = AccessModifier.None;
            } else if(specifiers.Count == 2 && specifiers[0].Value == "protected" && specifiers[1].Value == "internal") {
                result = AccessModifier.ProtectedInternal;
            } else {
                if(specifiers.Count > 1) {
                    Debug.WriteLine("Found too many access modifiers on type: " + typeElement);
                }
                result = accessModifierMap[specifiers.First().Value];
            }
            return result;
        }

        public override Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit, TypeDefinition typeDefinition) {
            //TODO: implement
            return new Collection<TypeUse>();
        }

        public override IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit) {
            //TODO: implement
            return Enumerable.Empty<Alias>();
        }

        private NamespaceUse CreateNamespaceUsePrefix(XElement nameElement, XElement fileUnit) {
            IEnumerable<XElement> parentNameElements = Enumerable.Empty<XElement>();

            parentNameElements = NameHelper.GetNameElementsExceptLast(nameElement);
            NamespaceUse current = null, root = null;

            if(parentNameElements.Any()) {
                foreach(var element in parentNameElements) {
                    var namespaceUse = new NamespaceUse
                                       {
                                           Name = element.Value,
                                           Location = new SourceLocation(element, fileUnit, false),
                                           ProgrammingLanguage = this.ParserLanguage,
                                       };
                    if(null == root) {
                        root = namespaceUse;
                    }
                    if(current != null) {
                        current.ChildScopeUse = namespaceUse;
                    }
                    current = namespaceUse;
                }
            }
            return root;
        }

        public override string GetTypeForBooleanLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        public override string GetTypeForCharacterLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        public override string GetTypeForNumberLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        public override string GetTypeForStringLiteral(string literalValue) {
            throw new NotImplementedException();
        }
    }
}
