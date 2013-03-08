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
            VariableDeclarationElementNames = new HashSet<XName>(new XName[] { SRC.Declaration, SRC.DeclarationStatement });
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
        /// Returns the XName that represents an import statement
        /// </summary>
        public XName AliasElementName { get; protected set; }

        /// <summary>
        /// Parses a file unit and returns a <see cref="NamespaceDefinition.IsGlobal">global</see> <see cref="NamespaceDefinition">namespace definition</see> object
        /// </summary>
        /// <param name="fileUnit">The file unit to parse</param>
        /// <returns>a global namespace definition for <paramref name="fileUnit"/></returns>
        public virtual NamespaceDefinition ParseFileUnit(XElement fileUnit) {
            if(null == fileUnit) throw new ArgumentNullException("fileUnit");
            if(SRC.Unit != fileUnit.Name) throw new ArgumentException("should be a SRC.Unit", "fileUnit");

            var globalScope = ParseElement(fileUnit, new ParserContext()) as NamespaceDefinition;
            return globalScope;
        }

        public virtual Scope ParseElement(XElement element, ParserContext context) {
            Scope scope;

            if(element.Name == SRC.Unit) {
                scope = ParseUnitElement(element, context);
            } else if(TypeElementNames.Contains(element.Name)) {
                scope = ParseTypeElement(element, context);
            } else if(NamespaceElementNames.Contains(element.Name)) {
                scope = ParseNamespaceElement(element, context);
            } else if(MethodElementNames.Contains(element.Name)) {
                scope = ParseMethodElement(element, context);
            } else {
                scope = ParseContainerElement(element, context);
            }

            context.ScopeStack.Push(scope);
            foreach(var declarationElement in GetDeclarationsFromElement(element)) {
                var declaration = ParseDeclarationElement(declarationElement, context);
                scope.AddDeclaredVariable(declaration);
            }
            foreach(var methodCallElement in GetMethodCallsFromElement(element)) {
                var methodCall = ParseCallElement(methodCallElement, context);
                scope.AddMethodCall(methodCall);
            }

            foreach(var childElement in GetChildContainers(element)) {
                var childScope = ParseElement(childElement, context);
                context.CurrentScope.AddChildScope(childScope);
            }
            scope.AddSourceLocation(context.CreateLocation(element, ContainerIsReference(element)));
            scope.ProgrammingLanguage = this.ParserLanguage;

            return context.ScopeStack.Pop();
        }

        public virtual Scope ParseContainerElement(XElement element, ParserContext context) {
            var scope = new Scope();
            return scope;
        }

        public virtual MethodDefinition ParseMethodElement(XElement methodElement, ParserContext context) {
            if(null == methodElement) throw new ArgumentNullException("methodElement");
            if(!MethodElementNames.Contains(methodElement.Name)) throw new ArgumentException("must be a method typeUseElement", "fileUnit");

            var methodDefinition = new MethodDefinition() {
                Name = GetNameForMethod(methodElement),
                IsConstructor = (methodElement.Name == SRC.Constructor || methodElement.Name == SRC.ConstructorDeclaration),
                IsDestructor = (methodElement.Name == SRC.Destructor || methodElement.Name == SRC.DestructorDeclaration),
                Accessibility = GetAccessModifierForMethod(methodElement),
            };
            var parameters = from paramElement in GetParametersFromMethodElement(methodElement)
                             select ParseMethodParameterElement(paramElement, context);
            foreach(var parameter in parameters) {
                methodDefinition.Parameters.Add(parameter);
            }
            return methodDefinition;
        }

        public abstract NamespaceDefinition ParseNamespaceElement(XElement namespaceElement, ParserContext context);

        public virtual TypeDefinition ParseTypeElement(XElement typeElement, ParserContext context) {
            if(null == typeElement) throw new ArgumentNullException("typeElement");

            var typeDefinition = new TypeDefinition() {
                Accessibility = GetAccessModifierForType(typeElement),
                Kind = XNameMaps.GetKindForXElement(typeElement),
                Name = GetNameForType(typeElement),
            };
            foreach(var parentTypeElement in GetParentTypeUseElements(typeElement)) {
                var parentTypeUse = ParseTypeUseElement(parentTypeElement, context);
                typeDefinition.AddParentType(parentTypeUse);
            }
            return typeDefinition;
        }

        public virtual NamespaceDefinition ParseUnitElement(XElement unitElement, ParserContext context) {
            if(null == unitElement) throw new ArgumentNullException("unitElement");
            if(SRC.Unit != unitElement.Name) throw new ArgumentException("should be a SRC.Unit", "unitElement");
            context.FileUnit = unitElement;
            var aliases = from aliasStatement in GetAliasElementsForFile(unitElement)
                          select ParseAliasElement(aliasStatement, context);

            context.Aliases = new Collection<Alias>(aliases.ToList());

            var namespaceForUnit = new NamespaceDefinition();
            
            return namespaceForUnit;
        }

        /// <summary>
        /// Creates an <see cref="Alias"/> object from a using import (such as using in C++ and C# and import in Java).
        /// </summary>
        /// <param name="aliasStatement">The statement to parse. Should be of type <see cref="AliasElementName"/></param>
        /// <param name="fileUnit">The file unit that contains this element</param>
        /// <returns>a new alias object that represents this alias statement</returns>
        public Alias ParseAliasElement(XElement aliasStatement, ParserContext context) {
            if(null == aliasStatement) throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName) throw new ArgumentException(String.Format("must be a {0} statement", AliasElementName), "usingStatement");

            var alias = new Alias() {
                Location = new SourceLocation(aliasStatement, context.FileUnit),
                ProgrammingLanguage = ParserLanguage,
            };

            IEnumerable<XElement> namespaceNames = GetNamesFromAlias(aliasStatement);

            if(!AliasIsNamespaceImport(aliasStatement)) {
                var lastNameElement = namespaceNames.LastOrDefault();
                namespaceNames = from name in namespaceNames
                                 where name.IsBefore(lastNameElement)
                                 select name;

                alias.ImportedNamedScope = new NamedScopeUse() {
                    Name = lastNameElement.Value,
                    Location = context.CreateLocation(lastNameElement),
                    ProgrammingLanguage = ParserLanguage,
                };
            }

            NamespaceUse current = null;
            foreach(var namespaceName in namespaceNames) {
                var use = new NamespaceUse() {
                    Name = namespaceName.Value,
                    Location = context.CreateLocation(namespaceName),
                    ProgrammingLanguage = ParserLanguage,
                };

                if(alias.ImportedNamespace == null) {
                    alias.ImportedNamespace = use;
                    current = use;
                } else {
                    current.ChildScopeUse = use;
                    current = use;
                }
            }

            return alias;
        }

        public virtual MethodCall ParseCallElement(XElement callElement, ParserContext context) {
            string name = String.Empty;
            bool isConstructor = false;
            bool isDestructor = false;
            IEnumerable<XElement> callingObjectNames = Enumerable.Empty<XElement>();

            var nameElement = callElement.Element(SRC.Name);
            if(null != nameElement) {
                name = NameHelper.GetLastName(nameElement);
                callingObjectNames = NameHelper.GetNameElementsExceptLast(nameElement);
            }

            var precedingElements = callElement.ElementsBeforeSelf();

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
                ParentScope = context.CurrentScope,
                Location = context.CreateLocation(callElement),
            };

            var arguments = from argument in callElement.Element(SRC.ArgumentList).Elements(SRC.Argument)
                            select CreateResolvableUse(argument, context);
            methodCall.Arguments = new Collection<IResolvesToType>(arguments.ToList<IResolvesToType>());

            IResolvesToType current = methodCall;
            foreach(var callingObjectName in callingObjectNames.Reverse()) {
                var callingObject = this.CreateVariableUse(callingObjectName, context);
                current.CallingObject = callingObject;
                current = callingObject;
            }

            var elementsBeforeCall = callElement.ElementsBeforeSelf().ToArray();
            int i = elementsBeforeCall.Length - 1;

            while(i > 0 && elementsBeforeCall[i].Name == OP.Operator &&
                  (elementsBeforeCall[i].Value == "." || elementsBeforeCall[i].Value == "->")) {
                i--;
                if(i >= 0 && elementsBeforeCall[i].Name == SRC.Name) {
                    var callingObject = CreateVariableUse(elementsBeforeCall[i], context);
                    current.CallingObject = callingObject;
                    current = callingObject;
                }
            }
            return methodCall;
        }

        public virtual VariableDeclaration ParseDeclarationElement(XElement declarationElement, ParserContext context) {
            if(declarationElement == null) throw new ArgumentNullException("declaration");
            if(!VariableDeclarationElementNames.Contains(declarationElement.Name)) throw new ArgumentException("XElement.Name must be in VariableDeclarationElementNames");

            XElement declElement;
            if(declarationElement.Name == SRC.Declaration || declarationElement.Name == SRC.FunctionDeclaration) {
                declElement = declarationElement;
            } else {
                declElement = declarationElement.Element(SRC.Declaration);
            }

            var typeElement = declElement.Element(SRC.Type);
            var nameElement = declElement.Element(SRC.Name);
            var name = (nameElement == null ? String.Empty : nameElement.Value);

            var variableDeclaration = new VariableDeclaration() {
                VariableType = ParseTypeUseElement(typeElement, context),
                Name = name,
                Location = context.CreateLocation(declarationElement),
                Scope = context.CurrentScope,
            };
            return variableDeclaration;
        }

        /// <summary>
        /// Generates a parameter declaration for the given declaration
        /// </summary>
        /// <param name="declElement">The declaration XElement from within the parameter element.</param>
        /// <param name="fileUnit">The containing file unit</param>
        /// <param name="method">The method that this parameter is part of.</param>
        /// <returns>A parameter declaration object</returns>
        public virtual ParameterDeclaration ParseMethodParameterElement(XElement declElement, ParserContext context) {
            if(declElement == null) throw new ArgumentNullException("declElement");
            if(declElement.Name != SRC.Declaration && declElement.Name != SRC.FunctionDeclaration) throw new ArgumentException("must be of element type SRC.Declaration or SRC.FunctionDeclaration", "declElement");

            var typeElement = declElement.Element(SRC.Type);
            var nameElement = declElement.Element(SRC.Name);
            var name = (nameElement == null ? String.Empty : nameElement.Value);

            var parameterDeclaration = new ParameterDeclaration {
                VariableType = ParseTypeUseElement(typeElement, context),
                Name = name,
                Method = context.CurrentScope as MethodDefinition
            };
            parameterDeclaration.Locations.Add(context.CreateLocation(declElement));
            return parameterDeclaration;
        }

        public virtual TypeUse ParseTypeUseElement(XElement typeUseElement, ParserContext context) {
            if(typeUseElement == null) throw new ArgumentNullException("typeUseElement");

            XElement typeNameElement;

            // validate the type use typeUseElement (must be a SRC.Name or SRC.Type)
            if(typeUseElement.Name == SRC.Type) {
                typeNameElement = typeUseElement.Element(SRC.Name);
            } else if(typeUseElement.Name == SRC.Name) {
                typeNameElement = typeUseElement;
            } else {
                throw new ArgumentException("typeUseElement should be of type type or name", "typeUseElement");
            }

            XElement lastNameElement = null;
            NamedScopeUse prefix = null;

            if(typeNameElement != null) {
                lastNameElement = NameHelper.GetLastNameElement(typeNameElement);
                prefix = ParseNamedScopeUsePrefix(typeNameElement, context);
            }

            var typeUse = new TypeUse() {
                Name = (lastNameElement != null ? lastNameElement.Value : String.Empty),
                ParentScope = context.CurrentScope,
                Location = context.CreateLocation(lastNameElement != null ? lastNameElement : typeUseElement),
                Prefix = prefix,
                ProgrammingLanguage = this.ParserLanguage,
            };

            return typeUse;
        }

        public abstract IEnumerable<XElement> GetParentTypeUseElements(XElement typeElement);

        public NamedScopeUse ParseNamedScopeUsePrefix(XElement nameElement, ParserContext context) {
            IEnumerable<XElement> parentNameElements = Enumerable.Empty<XElement>();

            parentNameElements = NameHelper.GetNameElementsExceptLast(nameElement);
            NamedScopeUse current = null, root = null;

            if(parentNameElements.Any()) {
                foreach(var element in parentNameElements) {
                    var scopeUse = new NamedScopeUse() {
                        Name = element.Value,
                        Location = context.CreateLocation(element, true),
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
        /// Gets all of the parameters for this method. It finds the variable declarations in parameter list.
        /// </summary>
        /// <param name="method">The method container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetParametersFromMethodElement(XElement method) {
            var parameters = from parameter in method.Element(SRC.ParameterList).Elements(SRC.Parameter)
                             let declElement = parameter.Elements().First()
                             select declElement;
            return parameters;
        }

        // TODO make this fit in with the rest of the parse methods
        public virtual IResolvesToType CreateResolvableUse(XElement element, ParserContext context) {
            var use = new VariableUse() {
                Location = context.CreateLocation(element, true),
                ParentScope = context.CurrentScope,
                ProgrammingLanguage = ParserLanguage,
            };
            return use;
        }
        
        // TODO make this fit in with the rest of the parse methods
        public virtual VariableUse CreateVariableUse(XElement element, ParserContext context) {
            XElement nameElement;
            if(element.Name == SRC.Name) {
                nameElement = element;
            } else if(element.Name == SRC.Expression) {
                nameElement = element.Element(SRC.Name);
            } else if(element.Name == SRC.ExpressionStatement || element.Name == SRC.Argument) {
                nameElement = element.Element(SRC.Expression).Element(SRC.Name);
            } else {
                throw new ArgumentException("element should be an expression, expression statement, argument, or name", "element");
            }

            var lastNameElement = NameHelper.GetLastNameElement(nameElement);

            var variableUse = new VariableUse() {
                Location = context.CreateLocation(lastNameElement, true),
                Name = lastNameElement.Value,
                ParentScope = context.CurrentScope,
                ProgrammingLanguage = ParserLanguage,
            };
            return variableUse;
        }

        #region aliases
        /// <summary>
        /// Checks if this alias statement is a namespace import or something more specific (such as a type or method)
        /// </summary>
        /// <param name="aliasStatement">The alias statement to check. Must be of type <see cref="AliasElementName"/></param>
        /// <returns>True if this is a namespace import; false otherwise</returns>
        public abstract bool AliasIsNamespaceImport(XElement aliasStatement);

        /// <summary>
        /// Gets all of the names for this alias
        /// </summary>
        /// <param name="aliasStatement">The alias statement. Must be of type <see cref="AliasElementName"/></param>
        /// <returns>An enumerable of all the <see cref="ABB.SrcML.SRC.Name">name elements</see> for this statement</returns>
        public virtual IEnumerable<XElement> GetNamesFromAlias(XElement aliasStatement) {
            if(null == aliasStatement) throw new ArgumentNullException("aliasStatement");
            if(aliasStatement.Name != AliasElementName) throw new ArgumentException(String.Format("should be an {0} statement", AliasElementName), "aliasStatement");

            var nameElement = aliasStatement.Element(SRC.Name);
            if(null != nameElement)
                return NameHelper.GetNameElementsFromName(nameElement);
            return Enumerable.Empty<XElement>();
        }
        #endregion

        public virtual IEnumerable<XElement> GetAliasElementsForFile(XElement fileUnit) {
            if(null == fileUnit) throw new ArgumentNullException("fileUnit");
            if(SRC.Unit != fileUnit.Name) throw new ArgumentException("must be a unit element", "fileUnit");

            return fileUnit.Elements(AliasElementName);
        }
        
        #region get child containers from scope
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
        #endregion

        #region get method calls from scope
        public virtual IEnumerable<XElement> GetMethodCallsFromElement(XElement element) {
            if(MethodElementNames.Contains(element.Name) ||
               NamespaceElementNames.Contains(element.Name) ||
               TypeElementNames.Contains(element.Name)) {
                return GetCallsFromBlockParent(element);
            }
            return GetMethodCallsFromBlockElement(element);
        }

        private IEnumerable<XElement> GetCallsFromBlockParent(XElement container) {
            var block = container.Element(SRC.Block);
            if(null == block)
                return Enumerable.Empty<XElement>();
            return GetMethodCallsFromBlockElement(block);
        }

        private IEnumerable<XElement> GetMethodCallsFromBlockElement(XElement container) {
            var methodCalls = from child in container.Elements()
                              where !ContainerElementNames.Contains(child.Name)
                              from call in child.Descendants(SRC.Call)
                              select call;
            return methodCalls;
        }

        #endregion get method calls from scope

        #region get declarations from scope
        public virtual IEnumerable<XElement> GetDeclarationsFromElement(XElement element) {
            if(null == element) return Enumerable.Empty<XElement>();

            IEnumerable<XElement> declarationElements;
            if(SRC.Block == element.Name) {
                declarationElements = GetDeclarationsFromBlockElement(element);
            } else if(SRC.Catch == element.Name) {
                declarationElements = GetDeclarationsFromCatchElement(element);
            } else if(SRC.For == element.Name) {
                declarationElements = GetDeclarationsFromForElement(element);
            } else if(MethodElementNames.Contains(element.Name)) {
                declarationElements = GetDeclarationsFromMethodElement(element);
            } else if(TypeElementNames.Contains(element.Name)) {
                declarationElements = GetDeclarationsFromTypeElement(element);
            }else {
                declarationElements = Enumerable.Empty<XElement>();
            }

            return declarationElements;
        }

        /// <summary>
        /// Gets all of the variable declarations for this catch block. It finds the variable declarations in <see cref="ABB.SrcML.SRC.ParameterList"/>.
        /// </summary>
        /// <param name="container">The catch container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromCatchElement(XElement container) {
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
        public virtual IEnumerable<XElement> GetDeclarationsFromBlockElement(XElement container) {
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
        public virtual IEnumerable<XElement> GetDeclarationsFromForElement(XElement container) {
            var declarations = from declElement in container.Element(SRC.Init).Elements(SRC.Declaration)
                               select declElement;
            return declarations;
        }

        /// <summary>
        /// Gets all of the variable declarations for this method. It finds the variable declarations in the child block.
        /// </summary>
        /// <param name="container">The method container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromMethodElement(XElement container) {
            var block = container.Element(SRC.Block);
            return GetDeclarationsFromBlockElement(block);
        }

        /// <summary>
        /// Gets all of the variable declarations for this type. It finds the variable declarations in the child block.
        /// </summary>
        /// <param name="container">The type container</param>
        /// <returns>An enumerable of all the declaration XElements.</returns>
        public virtual IEnumerable<XElement> GetDeclarationsFromTypeElement(XElement container) {
            var block = container.Element(SRC.Block);
            foreach(var declElement in GetDeclarationsFromBlockElement(block)) {
                yield return declElement;
            }
        }
        #endregion get declarations from scope

        #region access modifiers
        /// <summary>
        /// Gets the access modifier for this method. For Java & C#, a "specifier" tag is placed in either
        /// the method callElement, or the type callElement in the method.
        /// </summary>
        /// <param name="methodElement">The method callElement</param>
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
        /// <param name="typeElement">The type callElement</param>
        /// <returns>The access modifier for the type.</returns>
        public abstract AccessModifier GetAccessModifierForType(XElement typeElement);
        #endregion access modifiers

        #region parse literal types
        public virtual LiteralUse ParseLiteralElement(XElement literalElement, ParserContext context) {
            if(literalElement == null) throw new ArgumentNullException("literalElement");
            if(literalElement.Name != LIT.Literal) throw new ArgumentException("should be a literal", "literalElement");

            var kind = LiteralUse.GetLiteralKind(literalElement);
            string typeName = string.Empty;


            var use = new LiteralUse() {
                Kind = kind,
                Location = context.CreateLocation(literalElement),
                Name = GetTypeForLiteralValue(kind, literalElement.Value),
                ParentScope = context.CurrentScope,
            };

            return use;
        }

        public virtual string GetTypeForLiteralValue(LiteralKind kind, string literalValue) {
            switch(kind) {
                case LiteralKind.Boolean:
                    return GetTypeForBooleanLiteral(literalValue);
                case LiteralKind.Character:
                    return GetTypeForCharacterLiteral(literalValue);
                case LiteralKind.Number:
                    return GetTypeForNumberLiteral(literalValue);
                case LiteralKind.String:
                    return GetTypeForStringLiteral(literalValue);
            }
            return String.Empty;
        }

        public abstract string GetTypeForBooleanLiteral(string literalValue);
        public abstract string GetTypeForCharacterLiteral(string literalValue);
        public abstract string GetTypeForNumberLiteral(string literalValue);
        public abstract string GetTypeForStringLiteral(string literalValue);
        #endregion

        #region utilities
        /// <summary>
        /// Checks to see if this callElement is a reference container
        /// </summary>
        /// <param name="callElement">The callElement to check</param>
        /// <returns>True if this is a reference container; false otherwise</returns>
        public virtual bool ContainerIsReference(XElement element) {
            return (element != null && ContainerReferenceElementNames.Contains(element.Name));
        }

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
        /// Gets the name for the method callElement
        /// </summary>
        /// <param name="methodElement">the method callElement to get the name for</param>
        /// <returns>The name of the method</returns>
        public virtual string GetNameForMethod(XElement methodElement) {
            var name = methodElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
        }

        /// <summary>
        /// Gets the name for the type callElement
        /// </summary>
        /// <param name="typeElement">The type callElement to get the name for</param>
        /// <returns>The name of the type</returns>
        public virtual string GetNameForType(XElement typeElement) {
            var name = typeElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
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

        #endregion utilities
    }
}
