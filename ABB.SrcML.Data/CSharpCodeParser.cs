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
            this.AliasElementName = SRC.Using;
            //TODO: what else needs to be set here?
        }

        /// <summary>
        /// Returns <c>Language.CSharp</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.CSharp; }
        }

        /// <summary>
        /// Parses the given typeElement and returns a TypeDefinition object.
        /// </summary>
        /// <param name="typeElement">the type XML element.</param>
        /// <param name="fileUnit">The containing file unit</param>
        /// <returns>A new TypeDefinition object</returns>
        public override TypeDefinition CreateTypeDefinition(XElement typeElement, XElement fileUnit) {
            var typeDef = base.CreateTypeDefinition(typeElement, fileUnit);
            var partials = from specifiers in typeElement.Elements(SRC.Specifier)
                           where specifiers.Value == "partial"
                           select specifiers;
            typeDef.IsPartial = partials.Any();
            return typeDef;
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
                //specifiers might include non-access keywords like "partial"
                //get first specifier that is in the access modifier map
                result = accessModifierMap[specifiers.First(spec => accessModifierMap.ContainsKey(spec.Value)).Value];
            }
            return result;
        }

        /// <summary>
        /// Gets the parent types for this type, from the "super" element.
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <param name="fileUnit">The file unit that contains this type</param>
        /// <param name="typeDefinition">The TypeDefinition object for the type element.</param>
        /// <returns>A collection of type uses that represent the parent classes</returns>
        public override Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit, TypeDefinition typeDefinition) {
            var parents = new Collection<TypeUse>();
            var superElement = typeElement.Element(SRC.Super);
            if(superElement != null) {
                var parentNameElements = superElement.Elements(SRC.Name);
                foreach(var parentName in parentNameElements) {
                    parents.Add(CreateTypeUse(parentName, fileUnit, typeDefinition));
                }
            }
            return parents;
        }

        public override bool AliasIsNamespaceImport(XElement aliasStatement) {
            // TODO handle "using A = B.C"
            return true;
        }

        public override string GetTypeForBooleanLiteral(string literalValue) {
            return "bool";
        }

        public override string GetTypeForCharacterLiteral(string literalValue) {
            return "char";
        }

        public override string GetTypeForNumberLiteral(string literalValue) {
            //rules taken from C# 4.0 in a Nutshell by Joseph Albahari and Ben Albahari, page 22.
            bool isHex = literalValue.StartsWith("0x");
            string suffix;
            if(literalValue.EndsWith("UL") || literalValue.EndsWith("LU")) {
                suffix = literalValue.Substring(literalValue.Length - 2);
            } else {
                suffix = literalValue.Substring(literalValue.Length - 1);
            }
            //process suffix
            if(string.Compare(suffix, "F", StringComparison.InvariantCultureIgnoreCase) == 0 && !isHex) {
                return "float";
            }
            if(string.Compare(suffix, "D", StringComparison.InvariantCultureIgnoreCase) == 0) {
                return "double";
            }
            if(string.Compare(suffix, "M", StringComparison.InvariantCultureIgnoreCase) == 0) {
                return "decimal";
            }
            if(string.Compare(suffix, "U", StringComparison.InvariantCultureIgnoreCase) == 0) {
                return "uint";
            }
            if(string.Compare(suffix, "L", StringComparison.InvariantCultureIgnoreCase) == 0) {
                return "long";
            }
            if(string.Compare(suffix, "UL", StringComparison.InvariantCultureIgnoreCase) == 0 ||
               string.Compare(suffix, "LU", StringComparison.InvariantCultureIgnoreCase) == 0) {
                return "ulong";
            }
            //no (valid) suffix, infer type
            if(literalValue.Contains('.') || literalValue.Contains('E')) {
                return "double";
            }
            //TODO: determine proper integral type based on size of literal, i.e. int, uint, long, or ulong
            return "int";
        }

        public override string GetTypeForStringLiteral(string literalValue) {
            return "string";
        }

        //TODO: implement support for using blocks, once SrcML has been fixed to parse them correctly
        ///// <summary>
        ///// Gets all of the variable declarations from a container
        ///// </summary>
        ///// <param name="container">the container</param>
        ///// <param name="fileUnit">the containing file unit</param>
        ///// <returns>An enumerable of variable declarations</returns>
        //public override IEnumerable<VariableDeclaration> GetVariableDeclarationsFromContainer(XElement container, XElement fileUnit, Scope parentScope) {
        //    if(null == container) return Enumerable.Empty<VariableDeclaration>();

        //    if(container.Name != SRC.Using) {
        //        return base.GetVariableDeclarationsFromContainer(container, fileUnit, parentScope);
        //    }
        //    //parse using element

        //}


        #region Private methods
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
        #endregion

    }
}
