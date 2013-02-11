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
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// <para>AbstractCodeParser is used to parse SrcML files and extract useful info from the elements. Implementations of this class provide language-specific functions to extract useful data from the class.</para>
    /// <para>It contains two methods that wrap the language specific methods: <see cref="CreateTypeDefinition(XElement,XElement)"/> and <see cref="CreateTypeUse(XElement,XElement,IEnumerable{Alias})"/></para>
    /// </summary>
    public abstract class AbstractCodeParser {
        /// <summary>
        /// Creates a new abstract code parser object. Should only be called by child classes.
        /// </summary>
        protected AbstractCodeParser() {
            ContainerElementNames = new HashSet<XName>(new XName[] {
                SRC.Block, SRC.Catch, SRC.Class, SRC.Constructor, SRC.ConstructorDeclaration, SRC.Destructor,  SRC.DestructorDeclaration, SRC.Do,
                SRC.Else, SRC.Enum, SRC.Extern, SRC.For, SRC.Function, SRC.FunctionDeclaration, SRC.If, SRC.Namespace, SRC.Struct, SRC.Switch,
                SRC.Template, SRC.Then, SRC.Try, SRC.Typedef, SRC.Union, SRC.Unit, SRC.While,
            });
            MethodElementNames = new HashSet<XName>(new XName[] { SRC.Function, SRC.Constructor, SRC.Destructor,
                                                                  SRC.FunctionDeclaration, SRC.ConstructorDeclaration, SRC.DestructorDeclaration });
            NamespaceElementNames = new HashSet<XName>(new XName[] { SRC.Namespace });
            VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement, SRC.Parameter });
        }

        /// <summary>
        /// Returns the Language that this parser supports
        /// </summary>
        public abstract Language ParserLanguage { get; }

        /// <summary>
        /// Returns the XNames that represent containers for this language
        /// </summary>
        public HashSet<XName> ContainerElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> MethodElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent namespaces for this language
        /// </summary>
        public HashSet<XName> NamespaceElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary>
        /// Returns the XNames that represent variable declarations for this language
        /// </summary>
        public HashSet<XName> VariableDeclarationElementNames { get; protected set; }

        /// <summary>
        /// Looks at the name of the element and then creates a variablescope depending on the <see cref="System.Xml.Linq.XName"/>.
        /// </summary>
        /// <param name="element">The element to create a scope for</param>
        /// <param name="fileUnit">The file unit that contains this element</param>
        /// <returns>A variable scope for the element</returns>
        public VariableScope CreateScope(XElement element, XElement fileUnit) {
            VariableScope scope;

            if(element.Name == SRC.Unit) {
                scope = CreateScopeFromFile(element);
                fileUnit = element;
            } else if(TypeElementNames.Contains(element.Name)) {
                scope = CreateTypeDefinition(element, fileUnit);
            } else if(NamespaceElementNames.Contains(element.Name)) {
                scope = CreateNamespaceDefinition(element, fileUnit);
            } else if(MethodElementNames.Contains(element.Name)) {
                scope = CreateMethodDefinition(element, fileUnit);
            } else {
                scope = CreateScopeFromContainer(element, fileUnit);
            }
            scope.Location = new SourceLocation(element, fileUnit);
            return scope;
        }

        /// <summary>
        /// Creates a MethodDefinition object for the given element.
        /// </summary>
        /// <param name="methodElement">The method element. <c>methodElement.Name</c> must belong to <c>Parser.MethodElementNames</c></param>
        /// <param name="fileUnit">The file unit that contains <paramref name="methodElement"/>. It must be a <see cref="ABB.SrcML.SRC.Unit"/></param>
        /// <returns>A method definition that represents <paramref name="methodElement"/></returns>
        public virtual MethodDefinition CreateMethodDefinition(XElement methodElement, XElement fileUnit) {
            if(null == methodElement) throw new ArgumentNullException("methodElement");
            if(null == fileUnit) throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit) throw new ArgumentException("must be a SRC.unit", "fileUnit");
            if(!MethodElementNames.Contains(methodElement.Name)) throw new ArgumentException("must be a method element", "fileUnit");

            var methodDefinition = new MethodDefinition() {
                Name = GetNameForMethod(methodElement),
                IsConstructor = (methodElement.Name == SRC.Constructor || methodElement.Name == SRC.ConstructorDeclaration),
                IsDestructor = (methodElement.Name == SRC.Destructor || methodElement.Name == SRC.DestructorDeclaration),
            };

            var parameters = from paramElement in GetParametersFromMethod(methodElement)
                             select CreateVariableDeclaration(paramElement, fileUnit, methodDefinition);
            methodDefinition.Parameters = new Collection<VariableDeclaration>(parameters.ToList());
            return methodDefinition;
        }

        /// <summary>
        /// Creates a NamespaceDefinition object for the given element. This function looks for the namespace that contains <paramref name="element"/> and creates a definition based on that.
        /// </summary>
        /// <param name="element">the element</param>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>a new NamespaceDefinition object</returns>
        public abstract NamespaceDefinition CreateNamespaceDefinition(XElement element, XElement fileUnit);

        /// <summary>
        /// Creates a variable scope object for the given container. It adds all of the variables declared at this scope.
        /// </summary>
        /// <param name="container">The variable scope</param>
        /// <param name="fileUnit">the file unit that contains this <paramref name="container"/></param>
        /// <returns>A variable scope that represents <paramref name="container"/></returns>
        public virtual VariableScope CreateScopeFromContainer(XElement container, XElement fileUnit) {
            var currentScope = new VariableScope();

            // get the variables declared at this scope
            var declaredVariables = GetVariableDeclarationsFromContainer(container, fileUnit, currentScope);
            foreach(var declaration in declaredVariables) {
                currentScope.AddDeclaredVariable(declaration);
            }

            return currentScope;
        }

        /// <summary>
        /// Creates a variable scope for the given file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>A variable scope that represents the file.</returns>
        public abstract VariableScope CreateScopeFromFile(XElement fileUnit);

        /// <summary>
        /// Parses the given typeElement and returns a TypeDefinition object.
        /// </summary>
        /// <param name="typeElement">the type XML element.</param>
        /// <param name="fileUnit">The containing file unit</param>
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
            };
            typeDefinition.Parents = GetParentTypeUses(typeElement, fileUnit, typeDefinition);

            var fileName = GetFileNameForUnit(fileUnit);
            if(fileName.Length > 0) {
                typeDefinition.Filenames.Add(fileName);
            }

            return typeDefinition;
        }

        /// <summary>
        /// Parses the type use and returns a TypeUse object
        /// </summary>
        /// <param name="element">An element naming the type. Must be a <see cref="ABB.SrcML.SRC.Type"/>or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="fileUnit">The file unit that contains the typeElement</param>
        /// <param name="aliases">The aliases that apply to this type element (usually created from <paramref name="fileUnit"/>)</param>
        /// <returns>A new TypeUse object</returns>
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit, VariableScope parentScope, IEnumerable<Alias> aliases) {
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
                ParentScope = parentScope,
                Prefix = new Collection<string>(prefixes.ToList()),
                Location = new SourceLocation(element, fileUnit),
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
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit, VariableScope parentScope) {
            var aliases = CreateAliasesForFile(fileUnit);
            return CreateTypeUse(element, fileUnit, parentScope, aliases);
        }

        /// <summary>
        /// Gets the name for the type element
        /// </summary>
        /// <param name="typeElement">The type element to get the name for</param>
        /// <returns>The name of the type</returns>
        public virtual string GetNameForType(XElement typeElement) {
            var name = typeElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
        }

        /// <summary>
        /// Gets the name for the method element
        /// </summary>
        /// <param name="methodElement">the method element to get the name for</param>
        /// <returns>The name of the method</returns>
        public virtual string GetNameForMethod(XElement methodElement) {
            var name = methodElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
        }

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
        public abstract Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit, TypeDefinition typeDefinition);

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
        /// <returns>An enumerable of full names for this type use.</returns>
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

        /// <summary>
        /// This helper function returns all of the names from a name element. If a name element has no children, it just yields the name element back.
        /// However, if the name element has child elements, it yields all of the child name elements.
        /// </summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>An enumerable of either all the child names, or the root if there are none.</returns>
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
        /// <summary>
        /// Gets all of the child containers for the given container
        /// </summary>
        /// <param name="container">The container</param>
        /// <returns>An enumerable of all the children</returns>
        public virtual IEnumerable<XElement> GetChildContainers(XElement container) {
            if(null == container) return Enumerable.Empty<XElement>();
            IEnumerable<XElement> children;

            if(TypeElementNames.Contains(container.Name)) {
                children = GetChildContainersFromType(container);
            } else if(MethodElementNames.Contains(container.Name)) {
                children = GetChildContainersFromMethod(container);
            } else if(NamespaceElementNames.Contains(container.Name)) {
                children = GetChildContainersFromNamespace(container);
            } else {
                children = from child in container.Elements()
                           where ContainerElementNames.Contains(child.Name)
                           select child;
            }
            return children;
        }

        /// <summary>
        /// Gets all of the child containers for a namespace. It calls <see cref="GetChildContainers(XElement)"/> on the child block.
        /// </summary>
        /// <param name="container">The namespace container</param>
        /// <returns>All of the child containers</returns>
        public virtual IEnumerable<XElement> GetChildContainersFromNamespace(XElement container) {
            var block = container.Element(SRC.Block);
            return GetChildContainers(block);
        }

        /// <summary>
        /// Gets all of the child containers for a method. It calls <see cref="GetChildContainers(XElement)"/> on the child block.
        /// </summary>
        /// <param name="container">The method container</param>
        /// <returns>All of the child containers</returns>
        public virtual IEnumerable<XElement> GetChildContainersFromMethod(XElement container) {

            var block = container.Element(SRC.Block);
            return GetChildContainers(block);
        }

        /// <summary>
        /// Gets all of the child containers for a type. It calls <see cref="GetChildContainers(XElement)"/> on the child block.
        /// </summary>
        /// <param name="container">The namespace type</param>
        /// <returns>All of the child containers</returns>
        public virtual IEnumerable<XElement> GetChildContainersFromType(XElement container) {
            var block = container.Element(SRC.Block);
            return GetChildContainers(block);
        }

        #endregion get child containers
        #region create variable declarations
        /// <summary>
        /// Generates a variable declaration for the given declaration
        /// </summary>
        /// <param name="declaration">The declaration XElement. Can be of type <see cref="ABB.SrcML.SRC.Declaration"/>, <see cref="ABB.SrcML.SRC.DeclarationStatement"/>, or <see cref="ABB.SrcML.SRC.Parameter"/></param>
        /// <param name="fileUnit">The containing file unit</param>
        /// <returns>A variable declaration object</returns>
        public virtual VariableDeclaration CreateVariableDeclaration(XElement declaration, XElement fileUnit, VariableScope parentScope) {
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
                VariableType = CreateTypeUse(declElement.Element(SRC.Type), fileUnit, parentScope),
                Name = declElement.Element(SRC.Name).Value,
                Location = new SourceLocation(declaration, fileUnit),
            };
            return variableDeclaration;
        }
        /// <summary>
        /// Gets all of the variable declarations from a container
        /// </summary>
        /// <param name="container">the container</param>
        /// <param name="fileUnit">the containing file unit</param>
        /// <returns>An enumerable of variable declarations</returns>
        public virtual IEnumerable<VariableDeclaration> GetVariableDeclarationsFromContainer(XElement container, XElement fileUnit, VariableScope parentScope) {
            if(null == container) return Enumerable.Empty<VariableDeclaration>();

            IEnumerable<XElement> declarationElements;
            if(SRC.Block == container.Name) {
                declarationElements = GetDeclarationsFromBlock(container);
            } else if(SRC.Catch == container.Name) {
                declarationElements = GetDeclarationsFromCatch(container);
            } else if(SRC.For == container.Name) {
                declarationElements = GetDeclarationsFromFor(container);
            } else if(MethodElementNames.Contains(container.Name)) {
                declarationElements = GetParametersFromMethod(container);
                declarationElements = declarationElements.Concat(GetDeclarationsFromMethod(container));
            } else if(TypeElementNames.Contains(container.Name)) {
                declarationElements = GetDeclarationsFromType(container);
            }else {
                declarationElements = Enumerable.Empty<XElement>();
            }

            var declarations = from decl in declarationElements
                               select CreateVariableDeclaration(decl, fileUnit, parentScope);
            return declarations;
        }

        /// <summary>
        /// Gets all of the variable declarations for this catch block. It finds the variable declarations in <see cref="ABB.SrcML.SRC.ParameterList"/>.
        /// </summary>
        /// <param name="container">The catch container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromCatch(XElement container) {
            var declarations = from parameter in container.Elements(SRC.Parameter)
                               let declElement = parameter.Element(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        /// <summary>
        /// Gets all of the variable declarations for this block.
        /// </summary>
        /// <param name="container">The type container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromBlock(XElement container) {
            if(null == container) return Enumerable.Empty<XElement>();
            var declarations = from stmtElement in container.Elements(SRC.DeclarationStatement)
                               let declElement = stmtElement.Element(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        /// <summary>
        /// Gets all of the variable declarations for this for loop. It finds the variable declaration in the <see cref="ABB.SrcML.SRC.Init"/> statement.
        /// </summary>
        /// <param name="container">The type container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromFor(XElement container) {
            var declarations = from declElement in container.Element(SRC.Init).Elements(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        /// <summary>
        /// Gets all of the parameters for this method. It finds the variable declarations in parameter list.
        /// </summary>
        /// <param name="method">The method container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetParametersFromMethod(XElement method) {
            var parameters = from parameter in method.Element(SRC.ParameterList).Elements(SRC.Parameter)
                             let declElement = parameter.Element(SRC.Declaration)
                             select declElement;
            return parameters;
        }

        /// <summary>
        /// Gets all of the variable declarations for this method. It finds the variable declarations in the child block.
        /// </summary>
        /// <param name="container">The method container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromMethod(XElement container) {
            var block = container.Element(SRC.Block);
            return GetDeclarationsFromBlock(block);
        }

        /// <summary>
        /// Gets all of the variable declarations for this type. It finds the variable declarations in the child block.
        /// </summary>
        /// <param name="container">The type container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromType(XElement container) {
            var block = container.Element(SRC.Block);
            foreach(var declElement in GetDeclarationsFromBlock(block)) {
                yield return declElement;
            }
        }

        #endregion create variable declarations
    }
}
