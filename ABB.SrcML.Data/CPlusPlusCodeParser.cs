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
            this.TypeElementNames = new HashSet<XName>(new XName[] {
                SRC.Class, SRC.Enum, SRC.Struct, SRC.Union,
                SRC.ClassDeclaration, SRC.StructDeclaration, SRC.UnionDeclaration
            });
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
        /// Gets the access modifiers for this method. In C++, methods are contained within
        /// "specifier" blocks
        /// </summary>
        /// <param name="methodElement">The method typeUseElement</param>
        /// <returns>The access modifier for this method; if none, it returns see
        /// cref="AccessModifier.None"/></returns>
        protected override AccessModifier GetAccessModifierForMethod(XElement methodElement) {
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
        protected override AccessModifier GetAccessModifierForType(XElement typeElement) {
            return AccessModifier.None;
        }

        /// <summary>
        /// Gets the name for a method. This is the unqualified name, not any class names that might
        /// be prepended to it.
        /// </summary>
        /// <param name="methodElement">The method typeUseElement</param>
        /// <returns>a string with the method name</returns>
        protected override string GetNameForMethod(XElement methodElement) {
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
        /// <param name="methodElement">The method to get parameter elements for</param>
        /// <returns>An enumerable of parameter elements</returns>
        protected override IEnumerable<XElement> GetParametersFromMethodElement(XElement methodElement) {
            var paramElements = methodElement.Element(SRC.ParameterList).Elements(SRC.Parameter).ToList();
            if(paramElements.Count() == 1 && paramElements.First().Value == "void") {
                //there's only a single "void" parameter, which actually means there are no parameters
                return Enumerable.Empty<XElement>();
            } else {
                return base.GetParametersFromMethodElement(methodElement);
            }
        }

        /// <summary>
        /// Gets the parent types for this type. It parses the C++ ":" operator that appears in type
        /// definitions.
        /// </summary>
        /// <param name="typeElement">The type typeUseElement</param>
        /// <returns>A collection of type use elements that represent the parent classes</returns>
        protected override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
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
        protected override string GetTypeForBooleanLiteral(string literalValue) {
            return "bool";
        }

        /// <summary>
        /// Parses a C++ character literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Returns "char"</returns>
        protected override string GetTypeForCharacterLiteral(string literalValue) {
            return "char";
        }

        /// <summary>
        /// Parses a C++ number literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Uses <see href="http://www.cplusplus.com/doc/tutorial/constants/">C++ number
        /// rules</see> to determine the proper type</returns>
        protected override string GetTypeForNumberLiteral(string literalValue) {
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
        protected override string GetTypeForStringLiteral(string literalValue) {
            return "char*";
        }

        /// <summary>
        /// Creates a method definition object from
        /// <paramref name="methodElement"/>. For C++, it looks for something like <code>int A::B::Foo(){ }</code>
        /// and adds "A::B" as the NamePrefix.
        /// </summary>
        /// <param name="methodElement">The method element to parse. This must be one of the elements contained in MethodElementNames.</param>
        /// <param name="context">The parser context</param>
        /// <returns>The method definition object for <paramref name="methodElement"/></returns>
        protected override MethodDefinition ParseMethodElement(XElement methodElement, ParserContext context) {
            var md = base.ParseMethodElement(methodElement, context);
            var nameElement = methodElement.Element(SRC.Name);
            if(nameElement != null) {
                md.Prefix = ParseNamePrefix(nameElement, context);
            }
            return md;
        }

        /// <summary>
        /// Creates a NamespaceDefinition object for the given namespace typeUseElement. This must
        /// be one of the typeUseElement types defined in NamespaceElementNames.
        /// </summary>
        /// <param name="namespaceElement">the namespace element</param>
        /// <param name="context">The parser context</param>
        /// <returns>a new NamespaceDefinition object</returns>
        protected override NamespaceDefinition ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            if(namespaceElement == null)
                throw new ArgumentNullException("namespaceElement");
            if(!NamespaceElementNames.Contains(namespaceElement.Name))
                throw new ArgumentException(string.Format("Not a valid namespace element: {0}", namespaceElement.Name), "namespaceElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var nameElement = namespaceElement.Element(SRC.Name);
            var namespaceName = nameElement != null ? nameElement.Value : string.Empty;

            var nd = new NamespaceDefinition {
                Name = namespaceName,
                ProgrammingLanguage = ParserLanguage,
            };
            nd.AddLocation(context.CreateLocation(namespaceElement));

            //add children
            var blockElement = namespaceElement.Element(SRC.Block);
            if(blockElement != null) {
                foreach(var child in blockElement.Elements()) {
                    nd.AddChildStatement(ParseStatement(child, context));
                }
            }

            return nd;
        }

        /// <summary>
        /// Parses an element corresponding to a type definition and creates a TypeDefinition object 
        /// </summary>
        /// <param name="typeElement">The type element to parse. This must be one of the elements contained in TypeElementNames.</param>
        /// <param name="context">The parser context</param>
        /// <returns>A TypeDefinition parsed from the element</returns>
        protected override TypeDefinition ParseTypeElement(XElement typeElement, ParserContext context) {
            if(null == typeElement)
                throw new ArgumentNullException("typeElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var typeDefinition = new TypeDefinition() {
                Accessibility = GetAccessModifierForType(typeElement),
                Kind = XNameMaps.GetKindForXElement(typeElement),
                Name = GetNameForType(typeElement),
                ProgrammingLanguage = ParserLanguage
            };
            typeDefinition.AddLocation(context.CreateLocation(typeElement, ContainerIsReference(typeElement)));

            foreach(var parentTypeElement in GetParentTypeUseElements(typeElement)) {
                var parentTypeUse = ParseTypeUseElement(parentTypeElement, context);
                typeDefinition.AddParentType(parentTypeUse);
            }

            var typeBlock = typeElement.Element(SRC.Block);
            if(typeBlock != null) {
                foreach(var child in typeBlock.Elements()) {
                    if(child.Name == SRC.Private) {
                        typeDefinition.AddChildStatements(ParseClassChildren(child, context, AccessModifier.Private));
                    } else if(child.Name == SRC.Protected) {
                        typeDefinition.AddChildStatements(ParseClassChildren(child, context, AccessModifier.Protected));
                    } else if(child.Name == SRC.Public) {
                        typeDefinition.AddChildStatements(ParseClassChildren(child, context, AccessModifier.Public));
                    } else {
                        typeDefinition.AddChildStatement(ParseStatement(child, context));
                    }
                }
            }


            return typeDefinition;
        }

        /// <summary>
        /// Parses the given <paramref name="aliasElement"/> and creates an ImportStatement or AliasStatement from it.
        /// </summary>
        /// <param name="aliasElement">The alias element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An ImportStatement if the element is an import, or an AliasStatement if it is an alias.</returns>
        protected override Statement ParseAliasElement(XElement aliasElement, ParserContext context) {
            if(null == aliasElement)
                throw new ArgumentNullException("aliasElement");
            if(aliasElement.Name != AliasElementName)
                throw new ArgumentException(string.Format("Must be a SRC.{0} element", AliasElementName.LocalName), "aliasElement");
            if(context == null)
                throw new ArgumentNullException("context");

            Statement stmt = null;
            bool containsNamespaceKeyword = (from textNode in GetTextNodes(aliasElement)
                                             where textNode.Value.Contains("namespace")
                                             select textNode).Any();
            if(containsNamespaceKeyword) {
                //import statement
                var import = new ImportStatement() {ProgrammingLanguage = ParserLanguage};
                import.AddLocation(context.CreateLocation(aliasElement));

                var nameElement = aliasElement.Element(SRC.Name);
                if(nameElement != null) {
                    import.ImportedNamespace = ParseNameUseElement<NamespaceUse>(nameElement, context);
                }

                stmt = import;
            } else {
                //alias statement
                var alias = new AliasStatement() {ProgrammingLanguage = ParserLanguage};
                alias.AddLocation(context.CreateLocation(aliasElement));

                var nameElement = aliasElement.Element(SRC.Name);
                var initElement = aliasElement.Element(SRC.Init);
                if(initElement != null) {
                    //example: using foo = std::bar;
                    if(nameElement != null) {
                        alias.AliasName = nameElement.Value;
                    }
                    //TODO check this once srcml is updated to see if it's accurate
                    alias.Target = ParseExpression(GetFirstChildExpression(initElement), context);
                } else {
                    //example: using std::cout;
                    if(nameElement != null) {
                        alias.Target = ParseTypeUseElement(nameElement, context);
                        alias.AliasName = NameHelper.GetLastName(nameElement);
                    }
                }

                stmt = alias;
            }

            return stmt;
        }

        #region Private methods
        /// <summary>
        /// This method parses and returns the children within the public/protected/private block under a C++ class, 
        /// and sets the specified access modifier on the children that support it.
        /// </summary>
        private IEnumerable<Statement> ParseClassChildren(XElement accessBlockElement, ParserContext context, AccessModifier accessModifier) {
            if(accessBlockElement == null)
                throw new ArgumentNullException("accessBlockElement");
            if(!(new[] {SRC.Public, SRC.Protected, SRC.Private}.Contains(accessBlockElement.Name)))
                throw new ArgumentException("Not a valid accessibility block element", "accessBlockElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var children = accessBlockElement.Elements().Select(e => ParseStatement(e, context)).ToList();
            foreach(var ne in children.OfType<INamedEntity>()) {
                ne.Accessibility = accessModifier;
            }
            return children;
        }

        #endregion Private methods
    }
}