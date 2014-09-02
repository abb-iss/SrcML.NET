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
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Provides parsing facilities for the Java language
    /// </summary>
    public class JavaCodeParser : AbstractCodeParser {

        /// <summary>
        /// Creates a new java code parser object
        /// </summary>
        public JavaCodeParser() {
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class, SRC.Enum });
            this.NamespaceElementNames = new HashSet<XName>();
            this.AliasElementName = SRC.Import;
        }

        /// <summary>
        /// Returns <c>Language.Java</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.Java; }
        }


        /// <summary>
        /// Gets the parent type from a java type
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The parent type elements for the class</returns>
        protected override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
            var superTag = typeElement.Element(SRC.Super);

            var parentElements = Enumerable.Empty<XElement>();

            if(null != superTag) {
                parentElements = from element in superTag.Elements()
                                 where element.Name == SRC.Extends || element.Name == SRC.Implements
                                 from name in element.Elements(SRC.Name)
                                 select name;
            }
            return parentElements;
        }

        /// <summary>
        /// Parses a java boolean literal
        /// </summary>
        /// <param name="literalValue">the literal value</param>
        /// <returns>not implemented</returns>
        protected override string GetTypeForBooleanLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses a java character literal
        /// </summary>
        /// <param name="literalValue">the literal value</param>
        /// <returns>not implemented</returns>
        protected override string GetTypeForCharacterLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses a java number literal
        /// </summary>
        /// <param name="literalValue">the literal value</param>
        /// <returns>not implemented</returns>
        protected override string GetTypeForNumberLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Parses a java string
        /// </summary>
        /// <param name="literalValue">the literal value</param>
        /// <returns>Not implemented</returns>
        protected override string GetTypeForStringLiteral(string literalValue) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a NamespaceDefinition object from the given Java package element.
        /// This will create a NamespaceDefinition for each component of the name, e.g. com.java.foo.bar, and link them as children of each other.
        /// This will not add any child statements to the bottom namespace.
        /// </summary>
        /// <param name="packageElement">The SRC.Package element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A NamespaceDefinition corresponding to <paramref name="packageElement"/>.</returns>
        protected override NamespaceDefinition ParseNamespaceElement(XElement packageElement, ParserContext context) {
            if(packageElement == null)
                throw new ArgumentNullException("packageElement");
            if(packageElement.Name != SRC.Package)
                throw new ArgumentException("must be a SRC.Package", "packageElement");
            if(context == null)
                throw new ArgumentNullException("context");

            var nameElement = packageElement.Element(SRC.Name);
            if(nameElement == null) {
                throw new ParseException(context.FileName, packageElement.GetSrcLineNumber(), packageElement.GetSrcLinePosition(), this,
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

            return topNS;
        }

        /// <summary>
        /// Parses a java file unit. This handles the "package" directive by calling
        /// <see cref="ParseNamespaceElement"/>
        /// </summary>
        /// <param name="unitElement">The file unit to parse.</param>
        /// <param name="context">The parser context to use.</param>
        protected override NamespaceDefinition ParseUnitElement(XElement unitElement, ParserContext context) {
            if(null == unitElement)
                throw new ArgumentNullException("unitElement");
            if(SRC.Unit != unitElement.Name)
                throw new ArgumentException("should be a SRC.Unit", "unitElement");
            if(context == null)
                throw new ArgumentNullException("context");
            context.FileUnit = unitElement;
            //var aliases = from aliasStatement in GetAliasElementsForFile(unitElement)
            //              select ParseAliasElement(aliasStatement, context);

            //context.Aliases = new Collection<Alias>(aliases.ToList());

            //create a global namespace for the file unit
            var namespaceForUnit = new NamespaceDefinition() {ProgrammingLanguage = ParserLanguage};
            namespaceForUnit.AddLocation(context.CreateLocation(unitElement));
            NamespaceDefinition bottomNamespace = namespaceForUnit;

            //create a namespace for the package, and attach to global namespace
            var packageElement = unitElement.Element(SRC.Package);
            if(packageElement != null) {
                var namespaceForPackage = ParseNamespaceElement(packageElement, context);
                namespaceForUnit.AddChildStatement(namespaceForPackage);
                bottomNamespace = namespaceForPackage.GetDescendantsAndSelf<NamespaceDefinition>().Last();
            }

            //add children to bottom namespace
            foreach(var child in unitElement.Elements()) {
                var childStmt = ParseStatement(child, context);
                if(childStmt != null) {
                    bottomNamespace.AddChildStatement(childStmt);
                }
            }

            return namespaceForUnit;
        }

        /// <summary>
        /// Creates a ForStatement or ForeachStatement from the given element.
        /// </summary>
        /// <param name="forElement">The SRC.For element to parse.</param>
        /// <param name="context">The parser context to use.</param>
        /// <returns>A ForStatement or ForeachStatement corresponding to forElement.</returns>
        protected override ConditionBlockStatement ParseForElement(XElement forElement, ParserContext context) {
            if(forElement == null)
                throw new ArgumentNullException("forElement");
            if(forElement.Name != SRC.For)
                throw new ArgumentException("Must be a SRC.For element", "forElement");
            if(context == null)
                throw new ArgumentNullException("context");

            if(forElement.Element(SRC.Condition) != null) {
                //this is a standard for-loop, use the base processing
                return base.ParseForElement(forElement, context);
            }

            //else, this is a Java-style foreach loop
            var foreachStmt = new ForeachStatement() {ProgrammingLanguage = ParserLanguage};
            foreachStmt.AddLocation(context.CreateLocation(forElement));

            foreach(var child in forElement.Elements()) {
                if(child.Name == SRC.Init) {
                    //fill in condition/initializer
                    var expElement = GetFirstChildExpression(child);
                    if(expElement != null) {
                        foreachStmt.Condition = ParseExpression(expElement, context);
                    }
                }
                else if(child.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = child.Elements().Select(e => ParseStatement(e, context));
                    foreachStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    foreachStmt.AddChildStatement(ParseStatement(child, context));
                }
            }

            return foreachStmt;
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

            var isNamespaceImport = GetTextNodes(aliasElement).Any(n => n.Value.Contains("*"));

            Statement stmt = null;
            if(isNamespaceImport) {
                //namespace import
                var import = new ImportStatement() {ProgrammingLanguage = ParserLanguage};
                import.AddLocation(context.CreateLocation(aliasElement));
                var nameElement = aliasElement.Element(SRC.Name);
                if(nameElement != null) {
                    import.ImportedNamespace = ParseNameUseElement<NamespaceUse>(nameElement, context);
                    //TODO: fix to handle the trailing operator tag
                }
                stmt = import;
            } else {
                //importing a single member, i.e. an alias
                var alias = new AliasStatement() {ProgrammingLanguage = ParserLanguage};
                alias.AddLocation(context.CreateLocation(aliasElement));
                var nameElement = aliasElement.Element(SRC.Name);
                if(nameElement != null) {
                    alias.Target = ParseExpression(nameElement, context);
                    alias.AliasName = NameHelper.GetLastName(nameElement);
                }
                stmt = alias;
            }

            return stmt;
        }
    }
}