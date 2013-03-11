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
        /// <param name="typeElement">the type XML typeUseElement.</param>
        /// <param name="context">the parser context</param>
        /// <returns>A new TypeDefinition object</returns>
        public override void ParseTypeElement(XElement typeElement, ParserContext context) {
            base.ParseTypeElement(typeElement, context);

            var partials = from specifiers in typeElement.Elements(SRC.Specifier)
                           where specifiers.Value == "partial"
                           select specifiers;
            (context.CurrentScope as TypeDefinition).IsPartial = partials.Any();
        }

        /// <summary>
        /// Parses a C# namespace block
        /// </summary>
        /// <param name="namespaceElement">the namespace element to parse</param>
        /// <param name="context">the parser context</param>
        public override void ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            if(namespaceElement == null) throw new ArgumentNullException("namespaceElement");
            if(!NamespaceElementNames.Contains(namespaceElement.Name)) throw new ArgumentException(string.Format("Not a valid namespace element: {0}", namespaceElement.Name), "namespaceElement");

            var nameElement = namespaceElement.Element(SRC.Name);
            string namespaceName;
            if(nameElement == null) {
                namespaceName = string.Empty;
            } else {
                NamespaceDefinition root = null;
                foreach(var name in NameHelper.GetNameElementsFromName(nameElement)) {
                    var namespaceForName = new NamespaceDefinition() {
                        Name = name.Value,
                        ProgrammingLanguage = ParserLanguage,
                    };
                    if(root == null) {
                        root = namespaceForName;
                    } else {
                        namespaceForName.AddSourceLocation(context.CreateLocation(name));
                    }
                    context.Push(namespaceForName, root);
                }
            }
        }

        /// <summary>
        /// Gets the access modifier for this type typeUseElement
        /// </summary>
        /// <param name="typeElement">The type typeUseElement</param>
        /// <returns>The access modifier for the given type typeUseElement.</returns>
        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            if(typeElement == null)
                throw new ArgumentNullException("typeElement");
            if(!TypeElementNames.Contains(typeElement.Name))
                throw new ArgumentException(string.Format("Not a valid type typeUseElement: {0}", typeElement.Name), "typeElement");

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

        public override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
            var superElement = typeElement.Element(SRC.Super);
            if(superElement != null) {
                return superElement.Elements(SRC.Name);
            }
            return Enumerable.Empty<XElement>();
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
        //    //parse using typeUseElement

        //}

        /// <summary>
        /// Tests whether this container is a reference or whether it includes a definition.
        /// </summary>
        /// <param name="typeUseElement"></param>
        /// <returns></returns>
        public override bool ContainerIsReference(XElement element) {
            if(element == null) {
                throw new ArgumentNullException("typeUseElement");
            }

            var functionNames = new[] {SRC.Function, SRC.Constructor, SRC.Destructor};
            bool isReference = false;
            if(functionNames.Contains(element.Name)) {
                var typeElement = element.Element(SRC.Type);
                if(typeElement != null && typeElement.Elements(SRC.Specifier).Any(spec => spec.Value == "partial")) {
                    //partial method
                    if(element.Element(SRC.Block) == null) {
                        isReference = true;
                    }
                }
            }
            return isReference || base.ContainerIsReference(element);
        }

        #region Private methods
        private NamespaceUse CreateNamespaceUsePrefix(XElement nameElement, ParserContext context) {
            IEnumerable<XElement> parentNameElements = Enumerable.Empty<XElement>();

            parentNameElements = NameHelper.GetNameElementsExceptLast(nameElement);
            NamespaceUse current = null, root = null;

            if(parentNameElements.Any()) {
                foreach(var element in parentNameElements) {
                    var namespaceUse = new NamespaceUse
                                       {
                                           Name = element.Value,
                                           Location = context.CreateLocation(element, false),
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
