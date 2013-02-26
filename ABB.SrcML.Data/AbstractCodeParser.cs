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
            ContainerReferenceElementNames = new HashSet<XName>(new XName[] { SRC.ClassDeclaration, SRC.StructDeclaration, SRC.UnionDeclaration,
                                                                                SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration });
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
        /// Returns the XNames that represent reference elements (such as function_decl and class_decl)
        /// </summary>
        public HashSet<XName> ContainerReferenceElementNames { get; protected set; }

        /// <summary>
        /// Parses a file unit and returns a <see cref="NamespaceDefinition.IsGlobal">global</see> <see cref="NamespaceDefinition">namespace definition</see> object
        /// </summary>
        /// <param name="fileUnit">The file unit to parse</param>
        /// <returns>a global namespace definition for <paramref name="fileUnit"/></returns>
        public virtual NamespaceDefinition ParseFileUnit(XElement fileUnit) {
            if(null == fileUnit) throw new ArgumentNullException("fileUnit");
            if(SRC.Unit != fileUnit.Name) throw new ArgumentException("should be a SRC.Unit", "fileUnit");

            var globalScope = SrcMLElementVisitor.Visit(fileUnit, this) as NamespaceDefinition;
            return globalScope;
        }

        /// <summary>
        /// Looks at the name of the element and then creates a variablescope depending on the <see cref="System.Xml.Linq.XName"/>.
        /// </summary>
        /// <param name="element">The element to create a scope for</param>
        /// <param name="fileUnit">The file unit that contains this element</param>
        /// <returns>A variable scope for the element</returns>
        public Scope CreateScope(XElement element, XElement fileUnit) {
            Scope scope;

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
            scope.AddSourceLocation(new SourceLocation(element, fileUnit, ContainerIsReference(element)));
            scope.ProgrammingLanguage = this.ParserLanguage;
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
                Accessibility = GetAccessModifierForMethod(methodElement),
            };

            var parameters = from paramElement in GetParametersFromMethod(methodElement)
                             select CreateParameterDeclaration(paramElement, fileUnit, methodDefinition);
            methodDefinition.Parameters = new Collection<ParameterDeclaration>(parameters.ToList());
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
        public virtual Scope CreateScopeFromContainer(XElement container, XElement fileUnit) {
            var currentScope = new Scope();
            return currentScope;
        }

        /// <summary>
        /// Creates a variable scope for the given file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>A variable scope that represents the file.</returns>
        public abstract Scope CreateScopeFromFile(XElement fileUnit);

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
               Kind = XNameMaps.GetKindForXElement(typeElement),
               Name = GetNameForType(typeElement),
            };
            typeDefinition.ParentTypes = GetParentTypeUses(typeElement, fileUnit, typeDefinition);
            return typeDefinition;
        }

        /// <summary>
        /// Parses the type use and returns a TypeUse object
        /// </summary>
        /// <param name="element">An element naming the type. Must be a <see cref="ABB.SrcML.SRC.Type"/>or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="fileUnit">The file unit that contains the typeElement</param>
        /// <param name="aliases">The aliases that apply to this type element (usually created from <paramref name="fileUnit"/>)</param>
        /// <returns>A new TypeUse object</returns>
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit, Scope parentScope) {
            XElement typeNameElement;

            if(element == null)
                throw new ArgumentNullException("element");
            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a SRC.unit", "fileUnit");

            // validate the type use element (must be a SRC.Name or SRC.Type)
            if(element.Name == SRC.Type) {
                typeNameElement = element.Element(SRC.Name);
            } else if(element.Name == SRC.Name) {
                typeNameElement = element;
            } else {
                throw new ArgumentException("element should be of type type or name", "element");
            }

            XElement lastNameElement = null;
            NamedScopeUse prefix = null;
            
            if(typeNameElement != null) {
                lastNameElement = NameHelper.GetLastNameElement(typeNameElement);
                prefix = CreateNamedScopeUsePrefix(typeNameElement, fileUnit);
            }

            var typeUse = new TypeUse() {
                Name = lastNameElement.Value,
                ParentScope = parentScope,
                Location = new SourceLocation(lastNameElement, fileUnit),
                Prefix = prefix,
                ProgrammingLanguage = this.ParserLanguage,
            };

            return typeUse;
        }

        public NamedScopeUse CreateNamedScopeUsePrefix(XElement nameElement, XElement fileUnit) {
            IEnumerable<XElement> parentNameElements = Enumerable.Empty<XElement>();

            parentNameElements = NameHelper.GetNameElementsExceptLast(nameElement);
            NamedScopeUse current = null, root = null;

            if(parentNameElements.Any()) {
                foreach(var element in parentNameElements) {
                    var scopeUse = new NamedScopeUse() {
                        Name = element.Value,
                        Location = new SourceLocation(element, fileUnit, true),
                        ProgrammingLanguage = this.ParserLanguage,
                    };
                    if(null == root) {
                        root = scopeUse;
                    }
                    if(current != null) {
                        current.ChildScopeUse = scopeUse;
                    }
                    current = scopeUse;
                }
            }
            return root;
        }

        /// <summary>
        /// Checks to see if this element is a reference container
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <returns>True if this is a reference container; false otherwise</returns>
        public virtual bool ContainerIsReference(XElement element) {
            return (element != null && ContainerReferenceElementNames.Contains(element.Name));
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
        /// Gets the access modifier for this method. For Java & C#, a "specifier" tag is placed in either
        /// the method element, or the type element in the method.
        /// </summary>
        /// <param name="methodElement">The method element</param>
        /// <returns>The first specifier encountered. If none, it returns <see cref="AccessModifier.None"/></returns>
        public virtual AccessModifier GetAccessModifierForMethod(XElement methodElement) {
            Dictionary<string, AccessModifier> accessModifierMap = new Dictionary<string, AccessModifier>() {
                { "public", AccessModifier.Public },
                { "private", AccessModifier.Private },
                { "protected", AccessModifier.Protected },
                { "internal", AccessModifier.Internal },
            };

            var specifierContainer = methodElement.Element(SRC.Type);
            if(null == specifierContainer) {
                specifierContainer = methodElement;
            }

            var specifiers = from specifier in specifierContainer.Elements(SRC.Specifier)
                             where accessModifierMap.ContainsKey(specifier.Value)
                             select accessModifierMap[specifier.Value];

            return (specifiers.Any() ? specifiers.First() : AccessModifier.None);
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

        #region create method calls
        public virtual MethodCall CreateMethodCall(XElement element, XElement fileUnit, Scope parentScope) {
            string name = String.Empty;
            bool isConstructor = false;
            bool isDestructor = false;

            var nameElement = element.Element(SRC.Name);
            if(null != nameElement) {
                name = NameHelper.GetLastName(nameElement);
            }

            var precedingElements = element.ElementsBeforeSelf();

            foreach(var pe in precedingElements) {
                if(pe.Name == OP.Operator && pe.Value == "new") {
                    isConstructor = true;
                } else if(pe.Name == OP.Operator && pe.Value == "~") {
                    isDestructor = true;
                }
            }


            var methodCall = new MethodCall() {
                Name = name,
                IsConstructor = isConstructor,
                IsDestructor = isDestructor,
                ParentScope = parentScope,
                Location = new SourceLocation(element, fileUnit),
            };
            var arguments = from argument in element.Element(SRC.ArgumentList).Elements(SRC.Argument)
                            select CreateVariableUse(argument, fileUnit, parentScope);
            methodCall.Arguments = new Collection<VariableUse>(arguments.ToList<VariableUse>());
            return methodCall;
        }

        public virtual VariableUse CreateVariableUse(XElement element, XElement fileUnit, Scope parentScope) {
            XElement expression;
            if(element.Name == SRC.Expression) {
                expression = element;
            } else if(element.Name == SRC.ExpressionStatement || element.Name == SRC.Argument) {
                expression = element.Element(SRC.Expression);
            } else {
                throw new ArgumentException("element should be an expression, expression statement, or argument", "element");
            }

            var nameElement = NameHelper.GetLastNameElement(expression.Element(SRC.Name));

            var variableUse = new VariableUse() {
                Location = new SourceLocation(nameElement, fileUnit, true),
                Name = nameElement.Value,
                ParentScope = parentScope,
                ProgrammingLanguage = ParserLanguage,
            };
            return variableUse;
        }

        public IEnumerable<MethodCall> GetMethodCallsFromContainer(XElement container, XElement fileUnit, Scope parentScope) {
            if(null == container) return Enumerable.Empty<MethodCall>();

            IEnumerable<XElement> methodCallElements;

            if(MethodElementNames.Contains(container.Name) ||
               NamespaceElementNames.Contains(container.Name) ||
               TypeElementNames.Contains(container.Name)) {
                methodCallElements = GetMethodCallsFromBlockParent(container);
            } else {
                methodCallElements = GetMethodCallsFromBlock(container);
            }

            var methodCalls = from call in methodCallElements
                              select CreateMethodCall(call, fileUnit, parentScope);
            return methodCalls;
        }

        private IEnumerable<XElement> GetMethodCallsFromBlockParent(XElement container)
        {
 	        var block = container.Element(SRC.Block);
            if(null == block)
                return Enumerable.Empty<XElement>();
            return GetMethodCallsFromBlock(block);
        }   

        private IEnumerable<XElement> GetMethodCallsFromBlock(XElement container) {
            var methodCalls = from child in container.Elements()
                              where !ContainerElementNames.Contains(child.Name)
                              from call in child.Descendants(SRC.Call)
                              select call;
            return methodCalls;
        }


        #endregion create method calls

        #region create variable declarations
        /// <summary>
        /// Generates a variable declaration for the given declaration
        /// </summary>
        /// <param name="declaration">The declaration XElement. Can be of type <see cref="ABB.SrcML.SRC.Declaration"/>, <see cref="ABB.SrcML.SRC.DeclarationStatement"/>, or <see cref="ABB.SrcML.SRC.Parameter"/></param>
        /// <param name="fileUnit">The containing file unit</param>
        /// <returns>A variable declaration object</returns>
        public virtual VariableDeclaration CreateVariableDeclaration(XElement declaration, XElement fileUnit, Scope parentScope) {
            if(declaration == null)
                throw new ArgumentNullException("declaration");
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(!VariableDeclarationElementNames.Contains(declaration.Name))
                throw new ArgumentException("XElement.Name must be in VariableDeclarationElementNames");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be of type SRC.Unit", "fileUnit");

            XElement declElement;
            if(declaration.Name == SRC.Declaration || declaration.Name == SRC.FunctionDeclaration) {
                declElement = declaration;
            } else {
                declElement = declaration.Element(SRC.Declaration);
            }

            var typeElement = declElement.Element(SRC.Type);
            var nameElement = declElement.Element(SRC.Name);
            var name = (nameElement == null ? String.Empty : nameElement.Value);

            var variableDeclaration = new VariableDeclaration() {
                VariableType = CreateTypeUse(typeElement, fileUnit, parentScope),
                Name = name,
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
        public virtual IEnumerable<VariableDeclaration> GetVariableDeclarationsFromContainer(XElement container, XElement fileUnit, Scope parentScope) {
            if(null == container) return Enumerable.Empty<VariableDeclaration>();

            IEnumerable<XElement> declarationElements;
            if(SRC.Block == container.Name) {
                declarationElements = GetDeclarationsFromBlock(container);
            } else if(SRC.Catch == container.Name) {
                declarationElements = GetDeclarationsFromCatch(container);
            } else if(SRC.For == container.Name) {
                declarationElements = GetDeclarationsFromFor(container);
            } else if(MethodElementNames.Contains(container.Name)) {
                //declarationElements = GetParametersFromMethod(container);
                //declarationElements = declarationElements.Concat(GetDeclarationsFromMethod(container));
                declarationElements = GetDeclarationsFromMethod(container);
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
                               let typeElement = declElement.Element(SRC.Type)
                               where typeElement != null
                               where !typeElement.Elements(TYPE.Modifier).Any()
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
                             let declElement = parameter.Elements().First()
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

        /// <summary>
        /// Generates a parameter declaration for the given declaration
        /// </summary>
        /// <param name="declElement">The declaration XElement from within the parameter element.</param>
        /// <param name="fileUnit">The containing file unit</param>
        /// <param name="method">The method that this parameter is part of.</param>
        /// <returns>A parameter declaration object</returns>
        public virtual ParameterDeclaration CreateParameterDeclaration(XElement declElement, XElement fileUnit, MethodDefinition method) {
            if(declElement == null)
                throw new ArgumentNullException("declElement");
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(method == null)
                throw new ArgumentNullException("method");
            if(declElement.Name != SRC.Declaration)
                throw new ArgumentException("must be of element type SRC.Declaration", "declElement");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be of type SRC.Unit", "fileUnit");

            var typeElement = declElement.Element(SRC.Type);
            var nameElement = declElement.Element(SRC.Name);
            var name = (nameElement == null ? String.Empty : nameElement.Value);

            var parameterDeclaration = new ParameterDeclaration
                                       {
                                           VariableType = CreateTypeUse(typeElement, fileUnit, method),
                                           Name = name,
                                           Method = method
                                       };
            parameterDeclaration.Locations.Add(new SourceLocation(declElement, fileUnit));

            return parameterDeclaration;
        }
        #endregion create variable declarations
    }
}
