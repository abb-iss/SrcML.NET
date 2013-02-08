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
        }

        /// <summary>
        /// Returns <c>Language.Java</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.Java; }
        }

        /// <summary>
        /// Parses the java package statement from a file
        /// </summary>
        /// <param name="element">The element to find the namespace for</param>
        /// <param name="fileUnit">The file unit that contains <paramref name="element"/></param>
        /// <returns>The namespace definition for the given element.</returns>
        public override NamespaceDefinition CreateNamespaceDefinition(XElement element, XElement fileUnit) {
            var javaPackage = fileUnit.Descendants(SRC.Package).FirstOrDefault();

            var definition = new NamespaceDefinition();
            if(null != javaPackage) {
                var namespaceNames = from name in javaPackage.Elements(SRC.Name)
                                     select name.Value;
                var namespaceName = string.Join(".", namespaceNames);
                definition.Name = namespaceName;
                
            }
            return definition;
        }

        /// <summary>
        /// Creates a scope object from a file unit element. For java, it checks for a <see cref="ABB.SrcML.SRC.Package"/> tag at the top of the file.
        /// If it finds one, it returns a namespace definition for that package. Otherwise, it just returns a global namespace object.
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public override VariableScope CreateScopeFromFile(XElement fileUnit) {
            var namespaceForFile = CreateNamespaceDefinition(fileUnit, fileUnit);
            return namespaceForFile;
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
            return modifiers.FirstOrDefault();
        }

        /// <summary>
        /// Gets the parent types for this type. It parses the java "implements" statement.
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <param name="fileUnit">The file unit that contains this type</param>
        /// <returns>A collection of type uses that represent the parent classes</returns>
        public override Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit) {
            Collection<TypeUse> parents = new Collection<TypeUse>();
            var superTag = typeElement.Element(SRC.Super);

            if(null != superTag) {
                var implementsTag = superTag.Element(SRC.Implements);
                if(null != implementsTag) {
                    var aliases = CreateAliasesForFile(fileUnit);
                    var parentElements = implementsTag.Elements(SRC.Name);
                    foreach(var parentElement in parentElements) {
                        parents.Add(CreateTypeUse(parentElement, fileUnit, aliases));
                    }
                }
                
            }
            return parents;
        }

        /// <summary>
        /// Creates a list of aliases for the <paramref name="fileUnit"/>. For java, this parses the import statements (marked up by the <see cref="ABB.SrcML.SRC.Import"/>).
        /// </summary>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>an enumerable of alias objects</returns>
        public override IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit) {
            var aliases = from statement in fileUnit.Descendants(SRC.Import)
                          select CreateAliasFromImportStatement(statement, fileUnit);
            return aliases;
        }

        /// <summary>
        /// Creates an alias for the given import XElement.
        /// </summary>
        /// <param name="importStatement">an import element (<c>importStatement.Name</c> must be <see cref="ABB.SrcML.SRC.Import"/></param>
        /// <returns>An alias representing the import statement</returns>
        public Alias CreateAliasFromImportStatement(XElement importStatement, XElement fileUnit) {
            if(null == importStatement)
                throw new ArgumentNullException("importStatement");
            if(importStatement.Name != SRC.Import)
                throw new ArgumentException("must be an import statement", "importStatement");

            Alias alias = new Alias() {
                Location = new SourceLocation(importStatement, fileUnit),
            };

            var lastName = importStatement.Elements(SRC.Name).LastOrDefault();
            
            var textContainsAsterisk = (from textNode in GetTextNodes(importStatement)
                                        where textNode.IsAfter(lastName)
                                        where textNode.Value.Contains("*")
                                        select textNode).Any();
            if(textContainsAsterisk) {
                // if text contains asterisk, this is a namespace import
                var names = from name in importStatement.Elements(SRC.Name)
                            select name.Value;

                alias.NamespaceName = String.Join(".", names);
            } else {
                // if the text does not contain an asterisk this is a class import
                // the last <name> element is the imported class name
                // and the rest of the 
                var names = from name in importStatement.Elements(SRC.Name)
                            where name.IsBefore(lastName)
                            select name.Value;
                alias.NamespaceName = String.Join(".", names);
                alias.Name = lastName.Value;
            }

            return alias;
        }

        /// <summary>
        /// Generates a list of possible names for a type use
        /// </summary>
        /// <param name="typeUse">The type use</param>
        /// <returns>An enumerable of all the valid combinations of aliases with the name of the type use</returns>
        public override IEnumerable<string> GeneratePossibleNamesForTypeUse(TypeUse typeUse) {
            // a single name 
            yield return typeUse.CurrentNamespace.MakeQualifiedName(typeUse.Name);

            var aliases = from alias in typeUse.Aliases
                          where alias.IsAliasFor(typeUse)
                          select alias.MakeQualifiedName(typeUse);

            foreach(var alias in aliases) {
                yield return alias;
            }
            if(!typeUse.CurrentNamespace.IsGlobal)
                yield return typeUse.Name;
        }
    }
}
