﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// <para>AbstractCodeParser is used to parse SrcML files and extract useful info from the elements. Implementations of this class provide language-specific functions to extract useful data from the class.</para>
    /// <para>It contains two methods that wrap the language specific methods: <see cref="CreateTypeDefinition"/> and <see cref="CreateTypeUse"/></para>
    /// </summary>
    public abstract class AbstractCodeParser {
        protected AbstractCodeParser() {
            VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement, SRC.Parameter });
        }

        /// <summary>
        /// Returns the Language that this parser supports
        /// </summary>
        public abstract Language ParserLanguage { get; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent variable declarations for this language
        /// </summary>
        public HashSet<XName> VariableDeclarationElementNames { get; protected set; }

        /// <summary>
        /// Creates all of the type definitions from a file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit to search. <c>XElement.Name</c> must be SRC.Unit</param>
        /// <returns>An enumerable of TypeDefinition objects (one per type)</returns>
        public virtual IEnumerable<TypeDefinition> CreateTypeDefinitions(XElement fileUnit) {
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a a <unit> element", "fileUnit");

            Language language = SrcMLElement.GetLanguageForUnit(fileUnit);
            var fileName = GetFileNameForUnit(fileUnit);

            var typeElements = from typeElement in fileUnit.Descendants()
                               where TypeElementNames.Contains(typeElement.Name)
                               select CreateTypeDefinition(typeElement, fileUnit);
            return typeElements;
        }

        /// <summary>
        /// Parses the given typeElement and returns a TypeDefinition object.
        /// </summary>
        /// <param name="typeElement">the type XML element.</param>
        /// <returns>A new TypeDefinition object</returns>
        public virtual TypeDefinition CreateTypeDefinition(XElement typeElement, XElement fileUnit) {
            if(null == typeElement)
                throw new ArgumentNullException("typeElement");
            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a SRC.unit", "fileUnit");

            var typeDefinition = new TypeDefinition() {
               Accessibility = GetAccessModifierForType(typeElement),
               Filenames = new Collection<string>(),
               Kind = XNameMaps.GetKindForXElement(typeElement),
               Language = this.ParserLanguage,
               Name = GetNameForType(typeElement),
               Namespace = GetNamespaceDefinition(typeElement, fileUnit),
               Parents = GetParentTypeUses(typeElement, fileUnit),
               XPath = typeElement.GetXPath(false),
            };

            var fileName = GetFileNameForUnit(fileUnit);
            if(fileName.Length > 0) {
                typeDefinition.Filenames.Add(fileName);
            }

            typeDefinition.Namespace.Types.Add(typeDefinition);
            return typeDefinition;
        }

        /// <summary>
        /// Parses the type use and returns a TypeUse object
        /// </summary>
        /// <param name="element">An element naming the type. Must be a <see cref="ABB.SrcML.SRC.Type"/>or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="fileUnit">The file unit that contains the typeElement</param>
        /// <param name="aliases">The aliases that apply to this type element (usually created from <paramref name="fileUnit"/>)</param>
        /// <returns>A new TypeUse object</returns>
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit, IEnumerable<Alias> aliases) {
            XElement typeNameElement;

            if(element == null)
                throw new ArgumentNullException("element");
            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a SRC.unit", "fileUnit");

            // validate the type use element (must be a SRC.Name or SRC.Type            
            if(element.Name == SRC.Type) {
                typeNameElement = element.Element(SRC.Name);
            } else if(element.Name == SRC.Name) {
                typeNameElement = element;
            } else {
                throw new ArgumentException("element should be of type type or name", "element");
            }

            var nameElements = GetNameElementsFromName(typeNameElement);

            var lastName = nameElements.Last();
            var prefixes = from name in nameElements.TakeWhile(e => e.IsBefore(lastName))
                           select name.Value;

            var typeUse = new TypeUse() {
                Name = lastName.Value,
                Prefix = new Collection<string>(prefixes.ToList()),
                CurrentNamespace = GetNamespaceDefinition(element, fileUnit),
                Parser = this,
                Aliases = new Collection<Alias>(aliases.ToList<Alias>()),
            };

            return typeUse;
        }
        /// <summary>
        /// Parses the type use and returns a TypeUse object
        /// </summary>
        /// <param name="element">An element naming the type. Must be a <see cref="ABB.SrcML.SRC.Type"/>or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="fileUnit">The file unit that contains the typeElement</param>
        /// <returns>A new TypeUse object</returns>
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit) {
            var aliases = CreateAliasesForFile(fileUnit);
            return CreateTypeUse(element, fileUnit, aliases);
        }

        /// <summary>
        /// Creates a NamespaceDefinition object for the given element. This function looks for the namespace that contains <paramref name="element"/> and creates a definition based on that.
        /// </summary>
        /// <param name="element">the element</param>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>a new NamespaceDefinition object</returns>
        public abstract NamespaceDefinition GetNamespaceDefinition(XElement element, XElement fileUnit);

        /// <summary>
        /// Gets the name for the type element
        /// </summary>
        /// <param name="typeElement">The type element to get the name for</param>
        /// <returns>The name of the type</returns>
        public abstract string GetNameForType(XElement typeElement);

        /// <summary>
        /// Gets the access modifier for the given type
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The access modifier for the type.</returns>
        public abstract AccessModifier GetAccessModifierForType(XElement typeElement);

        /// <summary>
        /// Gets the parents for the given type.
        /// </summary>
        /// <param name="typeElement">the type element to get the parents for</param>
        /// <param name="fileUnit">the file unit that contains <paramref name="typeElement"/></param>
        /// <returns>A collection of TypeUses that represent the parent classes of <paramref name="typeElement"/></returns>
        public abstract Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit);

        /// <summary>
        /// Get type aliases for the given file
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public abstract IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit);

        /// <summary>
        /// Generates the possible names for this type use based on the aliases and the use data.
        /// </summary>
        /// <param name="typeUse">The type use to create</param>
        /// <returns>An enumerable of fully</returns>
        public abstract IEnumerable<string> GeneratePossibleNamesForTypeUse(TypeUse typeUse);

        /// <summary>
        /// Gets the filename for the given file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit. <c>fileUnit.Name</c> must be <c>SRC.Unit</c></param>
        /// <returns>The file path represented by this <paramref name="fileUnit"/></returns>
        public virtual string GetFileNameForUnit(XElement fileUnit) {
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("element must be a unit", "fileUnit");

            var fileNameAttribute = fileUnit.Attribute("filename");

            if(null != fileNameAttribute)
                return fileNameAttribute.Value;
            return String.Empty;
        }

        /// <summary>
        /// Gets all of the text nodes that are children of the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>An enumerable of the XText elements for <paramref name="element"/></returns>
        public IEnumerable<XText> GetTextNodes(XElement element) {
            var textNodes = from node in element.Nodes()
                where node.NodeType == XmlNodeType.Text
                let text = node as XText
                select text;
            return textNodes;
        }

        public IEnumerable<XElement> GetNameElementsFromName(XElement nameElement) {
            if(nameElement.Elements(SRC.Name).Any()) {
                foreach(var name in nameElement.Elements(SRC.Name)) {
                    yield return name;
                }
            } else {
                yield return nameElement;
            }
        }

        #region scope definition

        public virtual VariableScope CreateScopeFromContainer(XElement container, XElement fileUnit) {
            var currentScope = new VariableScope();
            currentScope.XPath = container.GetXPath(false);

            // get the variables declared at this scope
            var declaredVariables = GetVariableDeclarationsFromContainer(container, fileUnit);
            foreach(var declaration in declaredVariables) {
                currentScope.AddDeclaredVariable(declaration);
            }
            

            // create the child scopes and connect them to the parent scope
            foreach(var child in GetChildContainers(container)) {
                var childScope = CreateScopeFromContainer(child, fileUnit);
                currentScope.AddChildScope(childScope);
            }

            return currentScope;
        }

        public virtual IEnumerable<XElement> GetChildContainers(XElement container) {
            IEnumerable<XElement> children;

            if(VariableScope.TypeContainers.Contains(container.Name)) {
                children = GetChildContainersFromType(container);
            } else {
                children = from child in container.Elements()
                           where VariableScope.Containers.Contains(child.Name)
                           select child;
            }
            return children;
        }

        public virtual IEnumerable<XElement> GetChildContainersFromType(XElement container) {
            var block = container.Element(SRC.Block);

            foreach(var child in GetChildContainers(block)) {
                yield return child;
            }

            var specifierBlocks = from child in block.Elements()
                                  where VariableScope.SpecifierContainers.Contains(child.Name)
                                  select child;

            foreach(var specifierBlock in specifierBlocks) {
                foreach(var child in GetChildContainers(specifierBlock)) {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// Generates a variable declaration for the given declaration
        /// </summary>
        /// <param name="declaration">The declaration XElement. Can be of type <see cref="ABB.SrcML.SRC.Declaration"/>, <see cref="ABB.SrcML.SRC.DeclarationStatement"/>, or <see cref="ABB.SrcML.SRC.Parameter"/></param>
        /// <returns>A variable declaration object</returns>
        public virtual VariableDeclaration CreateVariableDeclaration(XElement declaration, XElement fileUnit) {
            if(declaration == null)
                throw new ArgumentNullException("declaration");
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(!VariableDeclarationElementNames.Contains(declaration.Name))
                throw new ArgumentException("XElement.Name must be in VariableDeclarationElementNames");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be of type SRC.Unit", "fileUnit");

            XElement declElement;
            if(declaration.Name == SRC.Declaration) {
                declElement = declaration;
            } else {
                declElement = declaration.Element(SRC.Declaration);
            }

            var variableDeclaration = new VariableDeclaration() {
                VariableType = CreateTypeUse(declElement.Element(SRC.Type), fileUnit),
                Name = declElement.Element(SRC.Name).Value,
            };
            return variableDeclaration;
        }

        public virtual IEnumerable<VariableDeclaration> GetVariableDeclarationsFromContainer(XElement container, XElement fileUnit) {
            var containersLikeBlocks = new HashSet<XName>() {
                SRC.Block, SRC.Private, SRC.Protected, SRC.Public
            };

            var containersLikeFunctions = new HashSet<XName>() {
                SRC.Function, SRC.Constructor, SRC.Destructor
            };

            IEnumerable<XElement> declarationElements;
            if(SRC.Catch == container.Name) {
                declarationElements = GetDeclarationsFromCatch(container);
            } else if(SRC.For == container.Name) {
                declarationElements = GetDeclarationsFromFor(container);
            } else if(containersLikeBlocks.Contains(container.Name)) {
                declarationElements = GetDeclarationsFromBlock(container);
            } else if(containersLikeFunctions.Contains(container.Name)) {
                declarationElements = GetDeclarationsFromMethod(container);
            } else if(VariableScope.TypeContainers.Contains(container.Name)) {
                declarationElements = GetDeclarationsFromType(container);
            }else {
                declarationElements = Enumerable.Empty<XElement>();
            }

            var declarations = from decl in declarationElements
                               select CreateVariableDeclaration(decl, fileUnit);
            return declarations;
        }

        public virtual IEnumerable<XElement> GetDeclarationsFromCatch(XElement container) {
            var declarations = from parameter in container.Elements(SRC.Parameter)
                               let declElement = parameter.Element(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        public virtual IEnumerable<XElement> GetDeclarationsFromBlock(XElement container) {
            var declarations = from stmtElement in container.Elements(SRC.DeclarationStatement)
                               let declElement = stmtElement.Element(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        public virtual IEnumerable<XElement> GetDeclarationsFromFor(XElement container) {
            var declarations = from declElement in container.Element(SRC.Init).Elements(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        public virtual IEnumerable<XElement> GetDeclarationsFromMethod(XElement container) {
            var declarations = from parameter in container.Element(SRC.ParameterList).Elements(SRC.Parameter)
                               let declElement = parameter.Element(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        public virtual IEnumerable<XElement> GetDeclarationsFromType(XElement container) {
            var block = container.Element(SRC.Block);
            foreach(var declElement in GetDeclarationsFromBlock(block)) {
                yield return declElement;
            }

            var specifierElements = from child in container.Elements()
                                    where VariableScope.SpecifierContainers.Contains(child.Name)
                                    select child;

            foreach(var specifierElement in specifierElements) {
                foreach(var declElement in GetDeclarationsFromBlock(specifierElement)) {
                    yield return declElement;
                }
            }
        }

        #endregion scope definition
    }
}