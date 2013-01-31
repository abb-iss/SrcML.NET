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

        /// <summary>
        /// Returns the list of specifier containers (<see cref="ABB.SrcML.SRC.Private"/>, <see cref="ABB.SrcML.SRC.Protected"/>, and <see cref="ABB.SrcML.SRC.Public"/>
        /// </summary>
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

        /// <summary>
        /// Creates a scope from the file. For C++, this means returning a global namespace object
        /// </summary>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>a global namespace object.</returns>
        public override VariableScope CreateScopeFromFile(XElement fileUnit) {
            var namespaceForFile = new NamespaceDefinition();

            return namespaceForFile;
        }

        /// <summary>
        /// Gets the name for a method. This is the unqualified name, not any class names that might be prepended to it.
        /// </summary>
        /// <param name="methodElement">The method element</param>
        /// <returns>a string with the method name</returns>
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

        /// <summary>
        /// Creates aliases for the files. For C++, this means interpreting using statements (both <c>using A::B;</c> and <c>using namespace std;</c>).
        /// </summary>
        /// <param name="fileUnit">The file unit to find aliases in</param>
        /// <returns>The aliases for this file</returns>
        public override IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit) {
            var aliases = from usingStatement in fileUnit.Descendants(SRC.Using)
                          select CreateAliasFromUsingStatement(usingStatement);
            return aliases;
        }

        /// <summary>
        /// Creates an alias for a C++ using statement
        /// </summary>
        /// <param name="usingStatement">The using statement (<c>usingStatement.Name</c> must be <see cref="ABB.SrcML.SRC.Using"/></param>
        /// <returns>An alias for this using statement</returns>
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

        /// <summary>
        /// Gets the child containers for a C++ type element. This iterates over the public, private, and protected blocks that appear in C++ classes in srcML.
        /// </summary>
        /// <param name="container">the type element</param>
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
        /// Gets the variables declared in this C++ type element. This iterates over the public, private, and protected blocks that appear in C++ classes in srcML.
        /// </summary>
        /// <param name="container">the type element</param>
        /// <returns>The decl elements for this type element</returns>
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
