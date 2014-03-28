/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Provides parsing facilities for the C++ language
    /// </summary>
    public class CPlusPlusCodeParser : AbstractCodeParser {

        /// <summary>
        /// Creates a new C++ code parser object
        /// </summary>
        public CPlusPlusCodeParser() {
            this.SpecifierContainerNames = new HashSet<XName>(new XName[] { SRC.Private, SRC.Protected, SRC.Public });
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class, SRC.Enum, SRC.Struct, SRC.Union });
            this.VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement, SRC.FunctionDeclaration });
            this.AliasElementName = SRC.Using;
        }

        /// <summary>
        /// Returns <c>Language.CPlusPlus</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.CPlusPlus; }
        }

        /// <summary>
        /// Returns the list of specifier containers (<see cref="ABB.SrcML.SRC.Private"/>,
        /// <see cref="ABB.SrcML.SRC.Protected"/>, and <see cref="ABB.SrcML.SRC.Public"/>
        /// </summary>
        public HashSet<XName> SpecifierContainerNames { get; set; }

        /// <summary>
        /// Checks if this alias statement represents a namespace import or something more specific
        /// (such as a method or class alias). In C++, namespace aliases contain the "namespace"
        /// keyword (for instance, <c>using namespace std;</c>).
        /// </summary>
        /// <param name="aliasStatement">The statement to parse. Should be of type see
        /// cref="AbstractCodeParser.AliasElementName"/></param>
        /// <returns>True if this is a namespace import; false otherwise</returns>
        public override bool AliasIsNamespaceImport(XElement aliasStatement) {
            if(null == aliasStatement)
                throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName)
                throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");
            var containsNamespaceKeyword = (from textNode in GetTextNodes(aliasStatement)
                                            where textNode.Value.Contains("namespace")
                                            select textNode).Any();
            return containsNamespaceKeyword;
        }

        /// <summary>
        /// Gets the access modifiers for this method. In C++, methods are contained within
        /// "specifier" blocks
        /// </summary>
        /// <param name="methodElement">The method typeUseElement</param>
        /// <returns>The access modifier for this method; if none, it returns see
        /// cref="AccessModifier.None"/></returns>
        public override AccessModifier GetAccessModifierForMethod(XElement methodElement) {
            Dictionary<XName, AccessModifier> accessModifierMap = new Dictionary<XName, AccessModifier>() {
                { SRC.Public, AccessModifier.Public },
                { SRC.Private, AccessModifier.Private },
                { SRC.Protected, AccessModifier.Protected },
            };

            var specifiers = from container in methodElement.Ancestors()
                             where SpecifierContainerNames.Contains(container.Name)
                             select accessModifierMap[container.Name];

            return (specifiers.Any() ? specifiers.First() : AccessModifier.None);
        }

        /// <summary>
        /// Gets the access modifier for this type. In C++, all types are public, so this always
        /// returns "public"
        /// </summary>
        /// <param name="typeElement">The type</param>
        /// <returns>the access modifier for this type.</returns>
        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            return AccessModifier.None;
        }

        /// <summary>
        /// Gets the child containers for a C++ type typeUseElement. This iterates over the public,
        /// private, and protected blocks that appear in C++ classes in srcML.
        /// </summary>
        /// <param name="container">the type typeUseElement</param>
        /// <returns>the child elements of this C++ type</returns>
        public override IEnumerable<XElement> GetChildContainersFromType(XElement container) {
            foreach(var child in base.GetChildContainersFromType(container)) {
                yield return child;
            }

            var block = container.Element(SRC.Block);
            var specifierBlocks = from child in block.Elements()
                                  where SpecifierContainerNames.Contains(child.Name)
                                  select child;

            foreach(var specifierBlock in specifierBlocks) {
                foreach(var child in GetChildContainers(specifierBlock)) {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Gets the variables declared in this C++ type typeUseElement. This iterates over the
        /// public, private, and protected blocks that appear in C++ classes in srcML.
        /// </summary>
        /// <param name="container">the type typeUseElement</param>
        /// <returns>The decl elements for this type typeUseElement</returns>
        public override IEnumerable<XElement> GetDeclarationsFromTypeElement(XElement container) {
            foreach(var decl in base.GetDeclarationsFromTypeElement(container)) {
                yield return decl;
            }

            var block = container.Element(SRC.Block);
            var specifierElements = from child in block.Elements()
                                    where SpecifierContainerNames.Contains(child.Name)
                                    select child;

            foreach(var specifierElement in specifierElements) {
                foreach(var declElement in GetDeclarationsFromBlockElement(specifierElement)) {
                    yield return declElement;
                }
            }
        }

        /// <summary>
        /// Gets the name for a method. This is the unqualified name, not any class names that might
        /// be prepended to it.
        /// </summary>
        /// <param name="methodElement">The method typeUseElement</param>
        /// <returns>a string with the method name</returns>
        public override string GetNameForMethod(XElement methodElement) {
            var nameElement = methodElement.Element(SRC.Name);

            if(null == nameElement)
                return string.Empty;
            return NameHelper.GetLastName(nameElement);
        }

        /// <summary>
        /// Checks if the method element has only one parameter "void" (which is really zero
        /// parameters in C/C++). If not, it just calls
        /// <see cref="AbstractCodeParser.GetParametersFromMethodElement(XElement)"/>
        /// </summary>
        /// <param name="method">The method to get parameter elements for</param>
        /// <returns>An enumerable of method parameter elements</returns>
        public override IEnumerable<XElement> GetParametersFromMethodElement(XElement method) {
            bool singleVoidParameter = false;
            if(method.Element(SRC.ParameterList).Elements(SRC.Parameter).Count() == 1) {
                singleVoidParameter = method.Element(SRC.ParameterList).Element(SRC.Parameter).Value == "void";
            }

            if(singleVoidParameter) {
                return Enumerable.Empty<XElement>();
            }
            return base.GetParametersFromMethodElement(method);
        }

        /// <summary>
        /// Gets the parent types for this type. It parses the C++ ":" operator that appears in type
        /// definitions.
        /// </summary>
        /// <param name="typeElement">The type typeUseElement</param>
        /// <returns>A collection of type use elements that represent the parent classes</returns>
        public override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
            var superTag = typeElement.Element(SRC.Super);

            if(null != superTag) {
                return superTag.Elements(SRC.Name);
            }
            return Enumerable.Empty<XElement>();
        }

        /// <summary>
        /// Parses a C++ boolean literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Returns "bool"</returns>
        public override string GetTypeForBooleanLiteral(string literalValue) {
            return "bool";
        }

        /// <summary>
        /// Parses a C++ character literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Returns "char"</returns>
        public override string GetTypeForCharacterLiteral(string literalValue) {
            return "char";
        }

        /// <summary>
        /// Parses a C++ number literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Uses <see href="http://www.cplusplus.com/doc/tutorial/constants/">C++ number
        /// rules</see> to determine the proper type</returns>
        public override string GetTypeForNumberLiteral(string literalValue) {
            // rules taken from: http://www.cplusplus.com/doc/tutorial/constants/ double rules:
            // contains '.', 'e', 'E' long double also ends in 'L'
            // float: ends in 'f' or 'F'
            //if(literalValue.cont
            // otherwise it's an integer
            // ends with 'u' indicates "unsigned int"
            // ends with 'l' indicates "long"
            // ends with 'ul' indicates "unsigned long"
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses a C++ string literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Returns "char*"</returns>
        public override string GetTypeForStringLiteral(string literalValue) {
            return "char*";
        }

        /// <summary>
        /// Creates a method definition object from
        /// <paramref name="methodElement"/>. For C++, it looks for <code>int A::B::Foo(){ }</code>
        /// and adds "A->B" to <see cref="INamedScope.ParentScopeCandidates"/>
        /// </summary>
        /// <param name="methodElement">The method typeUseElement</param>
        /// <param name="context">The parser context</param>
        /// <returns>the method definition object for
        /// <paramref name="methodElement"/></returns>
        public override void ParseMethodElement(XElement methodElement, ParserContext context) {
            var nameElement = methodElement.Element(SRC.Name);

            base.ParseMethodElement(methodElement, context);

            var prefix = ParseNamedScopeUsePrefix(nameElement, context);
            if(null != prefix) {
                (context.CurrentStatement as INamedScope).ParentScopeCandidates.Add(prefix);
            }
        }

        /// <summary>
        /// Creates a NamespaceDefinition object for the given namespace typeUseElement. This must
        /// be one of the typeUseElement types defined in NamespaceElementNames.
        /// </summary>
        /// <param name="namespaceElement">the namespace element</param>
        /// <param name="context">The parser context</param>
        /// <returns>a new NamespaceDefinition object</returns>
        public override void ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            if(namespaceElement == null)
                throw new ArgumentNullException("namespaceElement");
            if(!NamespaceElementNames.Contains(namespaceElement.Name))
                throw new ArgumentException(string.Format("Not a valid namespace typeUseElement: {0}", namespaceElement.Name), "namespaceElement");

            var nameElement = namespaceElement.Element(SRC.Name);
            var namespaceName = nameElement != null ? nameElement.Value : string.Empty;

            var namespaceDefinition = new NamespaceDefinition { Name = namespaceName };
            context.Push(namespaceDefinition);
        }
    }
}