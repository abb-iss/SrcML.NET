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
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Provides parsing facilities for the C# language
    /// </summary>
    public class CSharpCodeParser : AbstractCodeParser {

        /// <summary>
        /// Constructs a C# code parser
        /// </summary>
        public CSharpCodeParser()
            : base() {
            this.TypeElementNames = new HashSet<XName> { SRC.Class, SRC.Enum, SRC.Struct }; //SRC.Interface?
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
        /// Checks if the using statement is a namespace import
        /// </summary>
        /// <param name="aliasStatement"></param>
        /// <returns></returns>
        protected override bool AliasIsNamespaceImport(XElement aliasStatement) {
            // TODO handle "using A = B.C"
            return true;
        }

        /// <summary>
        /// Tests whether this container is a reference or whether it includes a definition.
        /// </summary>
        /// <param name="element">The element to test</param>
        /// <returns>True if this is a reference element; false otherwise</returns>
        protected override bool ContainerIsReference(XElement element) {
            if(element == null) {
                throw new ArgumentNullException("typeUseElement");
            }

            var functionNames = new[] { SRC.Function, SRC.Constructor, SRC.Destructor };
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

        /// <summary>
        /// Gets the parent type elements for a type element
        /// </summary>
        /// <param name="typeElement">The type element to parse</param>
        /// <returns>The type use elements</returns>
        protected override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
            var superElement = typeElement.Element(SRC.Super);
            if(superElement != null) {
                return superElement.Elements(SRC.Name);
            }
            return Enumerable.Empty<XElement>();
        }

        /// <summary>
        /// Parses a C# boolean literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>returns "bool"</returns>
        protected override string GetTypeForBooleanLiteral(string literalValue) {
            return "bool";
        }

        /// <summary>
        /// Parses a C# character literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>returns "char"</returns>
        protected override string GetTypeForCharacterLiteral(string literalValue) {
            return "char";
        }

        /// <summary>
        /// Parses a C# number literal based on C# 4.0 in a Nutshell by Joseph Albahari and Ben
        /// Albahari, page 22.
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>returns the appropriate numeric type</returns>
        protected override string GetTypeForNumberLiteral(string literalValue) {
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

        /// <summary>
        /// Parses a C# string literal
        /// </summary>
        /// <param name="literalValue">The literal value</param>
        /// <returns>Returns "string"</returns>
        protected override string GetTypeForStringLiteral(string literalValue) {
            return "string";
        }

        /// <summary>
        /// Parses a C# namespace block
        /// </summary>
        /// <param name="namespaceElement">the namespace element to parse</param>
        /// <param name="context">the parser context</param>
        protected override NamespaceDefinition ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            if(namespaceElement == null)
                throw new ArgumentNullException("namespaceElement");
            if(!NamespaceElementNames.Contains(namespaceElement.Name))
                throw new ArgumentException(string.Format("Not a valid namespace element: {0}", namespaceElement.Name), "namespaceElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var nameElement = namespaceElement.Element(SRC.Name);
            if(nameElement == null) {
                throw new ParseException(context.FileName, namespaceElement.GetSrcLineNumber(), namespaceElement.GetSrcLinePosition(), this,
                                         "No SRC.Name element found in namespace.", null);
            }

            //parse the name and create a NamespaceDefinition for each component
            NamespaceDefinition topNS = null;
            NamespaceDefinition lastNS = null;
            foreach(var name in NameHelper.GetNameElementsFromName(nameElement)) {
                var newNS = new NamespaceDefinition {
                    Name = name.Value,
                    ProgrammingLanguage = ParserLanguage
                };
                newNS.AddLocation(context.CreateLocation(name));
                if(topNS == null) { topNS = newNS; }
                if(lastNS != null) {
                    lastNS.AddChildStatement(newNS);
                }
                lastNS = newNS;
            }

            //add body of namespace to lastNS
            var blockElement = namespaceElement.Element(SRC.Block);
            if(blockElement != null) {
                foreach(var child in blockElement.Elements()) {
                    lastNS.AddChildStatement(ParseStatement(child, context));
                }
            }

            return topNS;
        }

        /// <summary>
        /// Parses the given <paramref name="aliasElement"/> and creates an ImportStatement or AliasStatement from it.
        /// </summary>
        /// <param name="aliasElement">The alias element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>An ImportStatement if the element is an import, or an AliasStatement if it is an alias.</returns>
        protected override Statement ParseAliasElement(XElement aliasElement, ParserContext context) {
            if(aliasElement == null)
                throw new ArgumentNullException("aliasElement");
            if(aliasElement.Name != AliasElementName)
                throw new ArgumentException(string.Format("Must be a SRC.{0} element", AliasElementName.LocalName), "aliasElement");
            if(context == null)
                throw new ArgumentNullException("context");
            
            Statement stmt = null;
            if(GetTextNodes(aliasElement).Any(n => n.Value.Contains("("))) {
                //using block
                stmt = ParseUsingBlockElement(aliasElement, context);
            } else if(aliasElement.Element(SRC.Init) != null) {
                //alias
                var alias = new AliasStatement() {ProgrammingLanguage = ParserLanguage};
                alias.AddLocation(context.CreateLocation(aliasElement));

                var nameElement = aliasElement.Element(SRC.Name);
                if(nameElement != null) {
                    alias.AliasName = nameElement.Value;
                }

                var initElement = aliasElement.Element(SRC.Init);
                alias.Target = ParseExpression(GetFirstChildExpression(initElement), context) as NameUse;
                
                stmt = alias;
            } else {
                //import
                var import = new ImportStatement() {ProgrammingLanguage = ParserLanguage};
                import.AddLocation(context.CreateLocation(aliasElement));

                var nameElement = aliasElement.Element(SRC.Name);
                if(nameElement != null) {
                    import.ImportedNamespace = ParseNamespaceUse(nameElement, context);
                }

                stmt = import;
            }
            
            return stmt;
        }

        protected virtual UsingBlockStatement ParseUsingBlockElement(XElement usingElement, ParserContext context) {
            if(usingElement == null)
                throw new ArgumentNullException("usingElement");
            if(usingElement.Name != SRC.Using)
                throw new ArgumentException("Must be a SRC.Using element", "usingElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var usingStmt = new UsingBlockStatement() {ProgrammingLanguage = ParserLanguage};
            usingStmt.AddLocation(context.CreateLocation(usingElement));

            foreach(var child in usingElement.Elements()) {
                if(child.Name == SRC.Init) {
                    //TODO: waiting for update to srcml
                    usingStmt.Initializer = ParseExpression(GetChildExpressions(child), context);
                    //TODO: update this to handle cases where it's not a declaration. Could be an expression
                }
                else if(child.Name == SRC.Block) {
                    var blockStatements = child.Elements().Select(e => ParseStatement(e, context));
                    usingStmt.AddChildStatements(blockStatements);
                } else {
                    usingStmt.AddChildStatement(ParseStatement(child, context));
                }
            }

            return usingStmt;
        }


        protected override MethodDefinition ParseMethodElement(XElement methodElement, ParserContext context) {
            var methodDefinition = base.ParseMethodElement(methodElement, context);
            
            var typeElement = methodElement.Elements(SRC.Type).FirstOrDefault();
            methodDefinition.IsPartial = ElementHasPartialKeyword(null == typeElement ? methodElement : typeElement);

            return methodDefinition;
        }
        protected override TypeDefinition ParseTypeElement(XElement typeElement, ParserContext context) {
            var typeDefinition = base.ParseTypeElement(typeElement, context);
            typeDefinition.IsPartial = ElementHasPartialKeyword(typeElement);
            return typeDefinition;
        }

        private bool ElementHasPartialKeyword(XElement element) {
            var partials = from specifier in element.Elements(SRC.Specifier)
                           where specifier.Value == "partial"
                           select specifier;
            return partials.Any();
        }

        protected override Statement ParseDeclarationStatementElement(XElement stmtElement, ParserContext context) {
            if(stmtElement == null)
                throw new ArgumentNullException("stmtElement");
            if(stmtElement.Name != SRC.DeclarationStatement)
                throw new ArgumentException("Must be a SRC.DeclarationStatement element", "stmtElement");
            if(context == null)
                throw new ArgumentNullException("context");
            
            //first check if this is a property and parse accordingly
            var declElement = stmtElement.Element(SRC.Declaration);
            if(declElement != null) {
                var blockElement = declElement.Element(SRC.Block);
                if(blockElement != null) {
                    //this is a property
                    return ParsePropertyDeclarationElement(declElement, context);
                }
            }

            //otherwise, parse as base:
            return base.ParseDeclarationStatementElement(stmtElement, context);
        }

        protected virtual PropertyDefinition ParsePropertyDeclarationElement(XElement declElement, ParserContext context) {
            if(declElement == null)
                throw new ArgumentNullException("declElement");
            if(declElement.Name != SRC.Declaration)
                throw new ArgumentException("Must be a SRC.Declaration element", "declElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var propertyDef = new PropertyDefinition {ProgrammingLanguage = ParserLanguage};
            propertyDef.AddLocation(context.CreateLocation(declElement));

            foreach(var child in declElement.Elements()) {
                if(child.Name == SRC.Type) {
                    propertyDef.Accessibility = GetAccessModifierFromTypeUseElement(child);
                    propertyDef.ReturnType = ParseTypeUseElement(child, context);
                }
                else if(child.Name == SRC.Name) {
                    propertyDef.Name = child.Value;
                }
                else if(child.Name == SRC.Block) {
                    //add children from block. This should be the getter/setter methods
                    var blockStatements = child.Elements().Select(e => ParseStatement(e, context));
                    propertyDef.AddChildStatements(blockStatements);
                } else {
                    propertyDef.AddChildStatement(ParseStatement(child, context));
                }
            }

            return propertyDef;
        }

        //TODO: implement C# type use parsing
        ///// <summary>
        ///// Parses the given typeUseElement and returns a TypeUse object. This handles the "var"
        ///// keyword for C# if used
        ///// </summary>
        ///// <param name="typeUseElement">The XML type use element</param>
        ///// <param name="context">The parser context</param>
        ///// <returns>A new TypeUse object</returns>
        //protected override TypeUse ParseTypeUseElement(XElement typeUseElement, ParserContext context) {
        //    if(typeUseElement == null)
        //        throw new ArgumentNullException("typeUseElement");

        //    XElement typeElement;
        //    XElement typeNameElement;

        //    // validate the type use typeUseElement (must be a SRC.Name or SRC.Type)
        //    if(typeUseElement.Name == SRC.Type) {
        //        typeElement = typeUseElement;
        //        typeNameElement = typeUseElement.Elements(SRC.Name).LastOrDefault();
        //    } else if(typeUseElement.Name == SRC.Name) {
        //        typeElement = typeUseElement.Ancestors(SRC.Type).FirstOrDefault();
        //        typeNameElement = typeUseElement;
        //    } else {
        //        throw new ArgumentException("typeUseElement should be of type type or name", "typeUseElement");
        //    }

        //    if(typeNameElement.Value == "var") {
        //        var initElement = typeElement.ElementsAfterSelf(SRC.Init).FirstOrDefault();
        //        var expressionElement = (null == initElement ? null : initElement.Element(SRC.Expression));
        //        var callElement = (null == expressionElement ? null : expressionElement.Element(SRC.Call));

        //        IResolvesToType initializer = (null == callElement ? null : ParseCallElement(callElement, context));
        //        var typeUse = new CSharpVarTypeUse() {
        //            Name = typeNameElement.Value,
        //            Initializer = initializer,
        //            ParentScope = context.CurrentStatement,
        //            Location = context.CreateLocation(typeNameElement),
        //            ProgrammingLanguage = this.ParserLanguage,
        //        };
        //        return typeUse;
        //    } else {
        //        return base.ParseTypeUseElement(typeUseElement, context);
        //    }
        //}

        //TODO: implement support for using blocks, once SrcML has been fixed to parse them correctly
        ///// <summary>
        ///// Gets all of the variable declarations from a container
        ///// </summary>
        ///// <param name="container">the container</param>
        ///// <param name="fileUnit">the containing file unit</param>
        ///// <returns>An enumerable of variable declarations</returns>
        //public override IEnumerable<VariableDeclaration> GetVariableDeclarationsFromContainer(XElement container, XElement fileUnit, Scope parentScope) {
        //    if(null == container) return Enumerable.Empty<VariableDeclaration>();

        // if(container.Name != SRC.Using) { return
        // base.GetVariableDeclarationsFromContainer(container, fileUnit, parentScope); } //parse
        // using typeUseElement

        //}

        #region Private methods

        private NamespaceUse CreateNamespaceUsePrefix(XElement nameElement, ParserContext context) {
            IEnumerable<XElement> parentNameElements = Enumerable.Empty<XElement>();

            parentNameElements = NameHelper.GetNameElementsExceptLast(nameElement);
            NamespaceUse current = null, root = null;

            if(parentNameElements.Any()) {
                foreach(var element in parentNameElements) {
                    var namespaceUse = new NamespaceUse {
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

        #endregion Private methods
    }
}