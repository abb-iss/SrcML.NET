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
        public JavaCodeParser() {
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class });
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
        public override NamespaceDefinition GetNamespaceDefinition(XElement element, XElement fileUnit) {
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
        /// Gets the type name for the given type element.
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The name of the type</returns>
        public override string GetNameForType(XElement typeElement) {
            var name = typeElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
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
                    var parentElements = implementsTag.Elements(SRC.Name);
                    foreach(var parentElement in parentElements) {
                        parents.Add(CreateTypeUse(parentElement, fileUnit));
                    }
                }
                
            }
            return parents;
        }

        public override IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit) {
            var aliases = from statement in fileUnit.Descendants(SRC.Import)
                          select CreateAliasFromImportStatement(statement);
            return aliases;
        }

        public Alias CreateAliasFromImportStatement(XElement importStatement) {
            if(null == importStatement)
                throw new ArgumentNullException("importStatement");
            if(importStatement.Name != SRC.Import)
                throw new ArgumentException("must be an import statement", "importStatement");

            Alias alias = new Alias();

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
    }
}
