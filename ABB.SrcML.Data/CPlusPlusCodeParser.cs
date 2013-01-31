using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using KsuAdapter = ABB.SrcML.Utilities.KsuAdapter;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Provides parsing facilities for the C++ language
    /// </summary>
    public class CPlusPlusCodeParser : AbstractCodeParser {
        public CPlusPlusCodeParser() {
            this.SpecifierContainerNames = new HashSet<XName>(new XName[] { SRC.Private, SRC.Protected, SRC.Public });
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class, SRC.Enum, SRC.Struct, SRC.Union });
        }
        /// <summary>
        /// Returns <c>Language.CPlusPlus</c>
        /// </summary>
        public override Language ParserLanguage {
            get { return Language.CPlusPlus; }
        }

        public HashSet<XName> SpecifierContainerNames { get; set; }

        /// <summary>
        /// Finds all of the namespace blocks that wrap this <paramref name="element"/>. It then creates the namespace name.
        /// </summary>
        /// <param name="element">The element to find the namespace for</param>
        /// <param name="fileUnit">The file unit that contains <paramref name="element"/></param>
        /// <returns>The namespace definition for the given element.</returns>
        public override NamespaceDefinition CreateNamespaceDefinition(XElement element, XElement fileUnit) {
            var names = from namespaceElement in element.Ancestors(SRC.Namespace)
                        let name = namespaceElement.Element(SRC.Name)
                        select name.Value;

            var namespaceName = String.Join(".", names.Reverse());
            NamespaceDefinition definition = new NamespaceDefinition();
            if(namespaceName.Length > 0) {
                definition.Name = namespaceName;
            }
            return definition;
        }

        public override VariableScope CreateScopeFromFile(XElement fileUnit) {
            var namespaceForFile = new NamespaceDefinition();
            return namespaceForFile;
        }

        public override string GetNameForMethod(XElement methodElement) {
            var nameElement = methodElement.Element(SRC.Name);

            if(null == nameElement)
                return string.Empty;

            var names = GetNameElementsFromName(nameElement);
            return names.Last().Value;
        }
        /// <summary>
        /// Gets the access modifier for this type. In C++, all types are public, so this always returns "public"
        /// </summary>
        /// <param name="typeElement">The type</param>
        /// <returns>the access modifier for this type.</returns>
        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            return AccessModifier.Public;
        }

        /// <summary>
        /// Gets the parent types for this type. It parses the C++ ":" operator that appears in type definitions.
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <param name="fileUnit">The file unit that contains this type</param>
        /// <returns>A collection of type uses that represent the parent classes</returns>
        public override Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit) {
            Collection<TypeUse> parents = new Collection<TypeUse>();
            var superTag = typeElement.Element(SRC.Super);

            if(null != superTag) {
                var parentElements = superTag.Elements(SRC.Name);
                var aliases = CreateAliasesForFile(fileUnit);
                foreach(var parentElement in parentElements) {
                    parents.Add(CreateTypeUse(parentElement, fileUnit, aliases));
                }
            }
            return parents;
        }

        public override IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit) {
            var aliases = from usingStatement in fileUnit.Descendants(SRC.Using)
                          select CreateAliasFromUsingStatement(usingStatement);
            return aliases;
        }

        public Alias CreateAliasFromUsingStatement(XElement usingStatement) {
            if(null == usingStatement)
                throw new ArgumentNullException("usingStatement");
            if(usingStatement.Name != SRC.Using)
                throw new ArgumentException("must be an using statement", "usingStatement");

            var alias = new Alias();
            var nameElement = usingStatement.Element(SRC.Name);
            var names = GetNameElementsFromName(nameElement);

            var containsNamespaceKeyword = (from textNode in GetTextNodes(usingStatement)
                                            where textNode.Value.Contains("namespace")
                                            select textNode).Any();
            if(containsNamespaceKeyword) {
                // if the using declaration contains the namespace keyword then this is a namespace import
                alias.NamespaceName = String.Join(".", from name in names select name.Value);
            } else {
                // if the namespace keyword isn't present then the using declaration is importing a specific type or variable
                var lastName = names.LastOrDefault();
                var namespaceNames = from name in names
                                     where name.IsBefore(lastName)
                                     select name.Value;

                alias.NamespaceName = String.Join(".", namespaceNames);
                alias.Name = lastName.Value;
            }
            return alias;
        }

        public override IEnumerable<string> GeneratePossibleNamesForTypeUse(TypeUse typeUse) {
            throw new NotImplementedException();
        }

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

        public override IEnumerable<XElement> GetDeclarationsFromType(XElement container) {
            foreach(var decl in base.GetDeclarationsFromType(container)) {
                yield return decl;
            }

            var block = container.Element(SRC.Block);
            var specifierElements = from child in container.Elements()
                                    where SpecifierContainerNames.Contains(child.Name)
                                    select child;

            foreach(var specifierElement in specifierElements) {
                foreach(var declElement in GetDeclarationsFromBlock(specifierElement)) {
                    yield return declElement;
                }
            }
        }
    }
}
