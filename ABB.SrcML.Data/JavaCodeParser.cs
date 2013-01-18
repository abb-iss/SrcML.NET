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

            if(null != javaPackage) {
                var namespaceNames = from name in javaPackage.Elements(SRC.Name)
                                     select name.Value;
                var namespaceName = string.Join(".", namespaceNames);
                var definition = new NamespaceDefinition() {
                    Name = namespaceName,
                };
                return definition;
            }
            return null;
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
    }
}
