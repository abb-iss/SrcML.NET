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
        /// Checks if this java import statement is a wild card (<c>import java.lang.*</c>) or for a
        /// specific class (<c>import java.lang.String</c>)
        /// </summary>
        /// <param name="aliasStatement">The alias statement to check. Must be of type see
        /// cref="AbstractCodeParser.AliasElementName"/></param>
        /// <returns>True if this import statement ends with an asterisk; false otherwise</returns>
        protected override bool AliasIsNamespaceImport(XElement aliasStatement) {
            if(null == aliasStatement)
                throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName)
                throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

            var lastName = aliasStatement.Elements(SRC.Name).LastOrDefault();
            var textContainsAsterisk = (from textNode in GetTextNodes(aliasStatement)
                                        where textNode.IsAfter(lastName)
                                        where textNode.Value.Contains("*")
                                        select textNode).Any();
            return textContainsAsterisk;
        }

        /// <summary>
        /// Gets all of the names for this alias
        /// </summary>
        /// <param name="aliasStatement">The alias statement. Must be of type see
        /// cref="AbstractCodeParser.AliasElementName"/></param>
        /// <returns>An enumerable of all the <see cref="ABB.SrcML.SRC.Name">name elements</see> for
        /// this statement</returns>
        protected override IEnumerable<XElement> GetNamesFromAlias(XElement aliasStatement) {
            if(null == aliasStatement)
                throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName)
                throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

            var nameElements = from name in aliasStatement.Elements(SRC.Name)
                               select name;
            return nameElements;
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
        /// Parses a Java package directive
        /// </summary>
        /// <param name="namespaceElement">A file unit</param>
        /// <param name="context">The parser context</param>
        protected override NamespaceDefinition ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            throw new NotImplementedException();

            //var javaPackage = context.FileUnit.Elements(SRC.Package).FirstOrDefault();

            //// Add a global namespace definition
            //var globalNamespace = new NamespaceDefinition();
            //context.Push(globalNamespace);

            //if(null != javaPackage) {
            //    var namespaceElements = from name in javaPackage.Elements(SRC.Name)
            //                            select name;
            //    foreach(var name in namespaceElements) {
            //        var namespaceForName = new NamespaceDefinition() {
            //            Name = name.Value,
            //            ProgrammingLanguage = ParserLanguage,
            //        };
            //        namespaceForName.AddSourceLocation(context.CreateLocation(name));
            //        context.Push(namespaceForName, globalNamespace);
            //    }
            //}
        }

        //TODO: implement Java unit element parsing
        ///// <summary>
        ///// Parses a java file unit. This handles the "package" directive by calling
        ///// <see cref="ParseNamespaceElement"/>
        ///// </summary>
        ///// <param name="unitElement">The file unit to parse</param>
        ///// <param name="context">The parser context to place the global scope in</param>
        //protected override NamespaceDefinition ParseUnitElement(XElement unitElement, ParserContext context) {
        //    if(null == unitElement)
        //        throw new ArgumentNullException("unitElement");
        //    if(unitElement.Name != SRC.Unit)
        //        throw new ArgumentException("should be a unit", "unitElement");

        //    context.FileUnit = unitElement;
        //    var aliases = from aliasStatement in GetAliasElementsForFile(unitElement)
        //                  select ParseAliasElement(aliasStatement, context);

        //    context.Aliases = new Collection<IAlias>(aliases.ToList());

        //    ParseNamespaceElement(unitElement, context);
        //}

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
            var foreachStmt = new ForeachStatement() {
                Location = context.CreateLocation(forElement),
                ProgrammingLanguage = ParserLanguage
            };

            foreach(var child in forElement.Elements()) {
                if(child.Name == SRC.Init) {
                    //fill in condition/initializer
                    var expElement = GetChildExpression(child);
                    if(expElement != null) {
                        foreachStmt.Condition = ParseExpression(expElement, context);
                    }
                }
                else if(child.Name == SRC.Block) {
                    //add children from block
                    var blockStatements = child.Elements().Select(e => ParseElement(e, context));
                    foreachStmt.AddChildStatements(blockStatements);
                } else {
                    //add child
                    foreachStmt.AddChildStatement(ParseElement(child, context));
                }
            }

            return foreachStmt;
        }
    }
}