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
using System.Text;
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
        /// Parses a java file unit and returns a global namespace object. Because Java uses a package directive at the top of a source file
        /// this function inserts a global namespace object as the root of the parse tree if necessary
        /// </summary>
        /// <param name="fileUnit">the java file unit to parse</param>
        /// <returns>A global namespace object for <paramref name="fileUnit"/></returns>
        public override NamespaceDefinition ParseFileUnit(XElement fileUnit) {
            if(null == fileUnit) throw new ArgumentNullException("fileUnit");
            if(SRC.Unit != fileUnit.Name) throw new ArgumentException("should be a SRC.Unit", "fileUnit");

            var namespaceForFile = ParseElement(fileUnit, new ParserContext()) as NamespaceDefinition;

            if(namespaceForFile.IsGlobal)
                return namespaceForFile;
            var globalNamespace = new NamespaceDefinition();
            globalNamespace.AddSourceLocation(new SourceLocation(fileUnit, fileUnit));
            globalNamespace.AddChildScope(namespaceForFile);
            return globalNamespace;
        }

        public override void ParseUnitElement(XElement unitElement, ParserContext context) {
            if(null == unitElement) throw new ArgumentNullException("unitElement");
            if(unitElement.Name != SRC.Unit) throw new ArgumentException("should be a unit", "unitElement");

            context.FileUnit = unitElement;
            var aliases = from aliasStatement in GetAliasElementsForFile(unitElement)
                          select ParseAliasElement(aliasStatement, context);

            context.Aliases = new Collection<Alias>(aliases.ToList());

            ParseNamespaceElement(unitElement, context);
        }

        public override void ParseNamespaceElement(XElement namespaceElement, ParserContext context) {
            var javaPackage = context.FileUnit.Elements(SRC.Package).FirstOrDefault();
            
            var definition = new NamespaceDefinition();
            if(null != javaPackage) {
                var namespaceNames = from name in javaPackage.Elements(SRC.Name)
                                     select name.Value;
                var namespaceName = string.Join(".", namespaceNames);
                definition.Name = namespaceName;
            }
            context.Push(definition);
        }

        /// <summary>
        /// Gets the access modifier for this type
        /// </summary>
        /// <param name="typeElement">The type</param>
        /// <returns>the access modifier for this type.</returns>
        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            Dictionary<string, AccessModifier> accessModifierMap = new Dictionary<string, AccessModifier>() {
                { "public", AccessModifier.Public },
                { "private", AccessModifier.Private },
                { "protected", AccessModifier.Protected },
            };

            var modifiers = from specifier in typeElement.Elements(SRC.Specifier)
                            where accessModifierMap.ContainsKey(specifier.Value)
                            select accessModifierMap[specifier.Value];
            return (modifiers.Any() ? modifiers.First() : AccessModifier.None);
        }

        public override IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement) {
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

        /// <summary>
        /// Checks if this java import statement is a wild card (<c>import java.lang.*</c>) or for a specific class (<c>import java.lang.String</c>)
        /// </summary>
        /// <param name="aliasStatement">The alias statement to check. Must be of type <see cref="AliasElementName"/></param>
        /// <returns>True if this import statement ends with an asterisk; false otherwise</returns>
        public override bool AliasIsNamespaceImport(XElement aliasStatement) {
            if(null == aliasStatement) throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName) throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

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
        /// <param name="aliasStatement">The alias statement. Must be of type <see cref="AliasElementName"/></param>
        /// <returns>An enumerable of all the <see cref="ABB.SrcML.SRC.Name">name elements</see> for this statement</returns>
        public override IEnumerable<XElement> GetNamesFromAlias(XElement aliasStatement) {
            if(null == aliasStatement) throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName) throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

            var nameElements = from name in aliasStatement.Elements(SRC.Name)
                               select name;
            return nameElements;
        }
    }
}
