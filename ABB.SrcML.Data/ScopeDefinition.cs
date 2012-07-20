/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace ABB.SrcML.Data
{
    partial class ScopeDefinition
    {
        
        private readonly static HashSet<XName> _validNames = new HashSet<XName>(ContainerNames.All);

        /// <summary>
        /// Valid XNames for scope definitions
        /// </summary>
        public static new HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }
        /// <summary>
        /// Creates scope definitions from the given element
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>collection of scope definitions for each scope found</returns>
        public static IEnumerable<ScopeDefinition> CreateFromElement(XElement element, string fileName)
        {
            if (MethodDefinition.ValidNames.Contains(element.Name))
            {
                yield return new MethodDefinition(element, fileName);
            }
            else if (TypeDefinition.ValidNames.Contains(element.Name))
            {
                if (element.Elements(SRC.Name).Count() > 0)
                {
                    foreach (var nameElement in element.Elements(SRC.Name))
                    {
                        yield return new TypeDefinition(element, nameElement.Value, fileName);
                    }
                }
                else
                {
                    yield return new TypeDefinition(element, fileName);
                }
            }
            else
            {
                yield return new ScopeDefinition(element, fileName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeDefinition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file the element belongs to</param>
        public ScopeDefinition(XElement element, string fileName)
            : base(element, fileName)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScopeDefinition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        public ScopeDefinition(XElement element)
            : base(element, GetFileName(element))
        {

        }

        /// <summary>
        /// Gets the elements.
        /// </summary>
        /// <param name="fileUnit">The file unit.</param>
        /// <param name="db">The db.</param>
        /// <returns></returns>
        internal static IEnumerable<Tuple<string, string>> GetElements(XElement fileUnit, SrcMLDataContext db)
        {
            Dictionary<string, Stack<XElement>> currentDeclarations = new Dictionary<string, Stack<XElement>>();
            foreach (var scopePairs in GetDeclarationScopesFromContainer(fileUnit, currentDeclarations, db))
            {
                yield return scopePairs;
            }
        }

        private static string GetXPath(XElement element)
        {
            var parentDefinition = element.Annotation<TypeDefinition>();
            var xpath = element.GetXPath(false);
            if (null == parentDefinition || xpath.StartsWith("/src:unit[@filename=", StringComparison.OrdinalIgnoreCase))
                return xpath;

            var parts = new List<string>(xpath.Substring(1).Split('/'));
            parts[0] = parentDefinition.XPath;

            return string.Join("/", parts);
        }
        #region walk container tree

        private static IEnumerable<Tuple<string, string>> GetDeclarationScopesFromContainer(XElement element, Dictionary<string, Stack<XElement>> currentDeclarations, SrcMLDataContext db)
        {
            var allContainerNames = new HashSet<XName>(ContainerNames.All);
            var declarations = GetDeclarationsFromElement(element, db);

            // add each variable definition to the appropriate definition stack
            foreach (var declaration in declarations)
            {
                Stack<XElement> stackForName;
                var name = declaration.Element(SRC.Name).Value;
                if (currentDeclarations.TryGetValue(name, out stackForName))
                {
                    stackForName.Push(declaration);
                }
                else
                {
                    stackForName = new Stack<XElement>();
                    stackForName.Push(declaration);
                    currentDeclarations[name] = stackForName;
                }
            }

            // add all current variable definitions to the current scope
            foreach (var definitionStack in currentDeclarations.Values)
            {
                yield return new Tuple<string, string>(GetXPath(element), GetXPath(definitionStack.Peek()));
            }

            // get all of the child containers
            var childContainers = from child in element.Elements()
                                  where allContainerNames.Contains(child.Name)//ContainerNames.All.Any(c => c == child.ElementXName)
                                  select child;

            // run GetDeclarationScopesFromContainer on each child container
            foreach (var container in childContainers)
            {
                foreach (var childScope in GetDeclarationScopesFromContainer(container, currentDeclarations, db))
                {
                    yield return childScope;
                }
            }

            // remove the declarations from their stacks
            foreach (var declaration in declarations)
            {
                var name = declaration.Element(SRC.Name).Value;
                currentDeclarations[name].Pop();
                if (0 == currentDeclarations[name].Count)
                    currentDeclarations.Remove(name);
            }
        }

        private static IEnumerable<XElement> GetDeclarationsFromElement(XElement container, SrcMLDataContext db)
        {
            var name = container.Name;
            IEnumerable<XElement> declarations;

            var containersWithoutDeclarations = new HashSet<XName>()
            {
                SRC.Class, SRC.Do, SRC.Else, SRC.Enum, SRC.Extern, SRC.If,
                SRC.Namespace, SRC.Struct, SRC.Switch, SRC.Template, SRC.Then,
                SRC.Try, SRC.Typedef, SRC.Union, SRC.Unit, SRC.While
            };
            var containersLikeBlocks = new HashSet<XName>()
            {
                SRC.Block, SRC.Private, SRC.Protected, SRC.Public
            };
            var methodContainers = new HashSet<XName>(ContainerNames.MethodDefinitions);

            if (SRC.Catch == name)
            {
                declarations = GetDeclarationsFromCatch(container, db);
            }
            else if (SRC.For == name)
            {
                declarations = GetDeclarationsFromFor(container, db);
            }
            else if (methodContainers.Contains(name))
            {
                declarations = GetDeclarationsFromFunction(container, db);
            }
            else if (containersLikeBlocks.Contains(name))
            {
                declarations = GetDeclarationsFromBlock(container, db);
            }
            else if (containersWithoutDeclarations.Contains(name))
            {
                declarations = Enumerable.Empty<XElement>();
            }
            else
            {
                throw new SrcMLRequiredNameException(ContainerNames.All);
            }

            var validDeclarations = from decl in declarations
                                    where decl.Elements(SRC.Name).Any()
                                    select decl;
            return validDeclarations;
        }

        private static string GetFileName(XElement element)
        {
            return element.AncestorsAndSelf(SRC.Unit).First().Attribute("filename").Value;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "db")]
        private static IEnumerable<XElement> GetDeclarationsFromBlock(XElement container, SrcMLDataContext db)
        {
            IEnumerable<XElement> declarations = from declaration in container.Elements(SRC.DeclarationStatement)
                                                 select declaration.Element(SRC.Declaration);
            return declarations;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "db")]
        private static IEnumerable<XElement> GetDeclarationsFromCatch(XElement container, SrcMLDataContext db)
        {
            IEnumerable<XElement> declarations = from parameter in container.Elements(SRC.Parameter)
                                                 select parameter.Element(SRC.Declaration);
            return declarations;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "db")]
        private static IEnumerable<XElement> GetDeclarationsFromFor(XElement container, SrcMLDataContext db)
        {
            IEnumerable<XElement> declarations = container.Element(SRC.Init).Elements(SRC.Declaration);
            return declarations;
        }

        private static IEnumerable<XElement> GetDeclarationsFromFunction(XElement container, SrcMLDataContext db)
        {
            IEnumerable<XElement> declarations = from param in container.Element(SRC.ParameterList).Elements(SRC.Parameter)
                                                 select param.Elements().FirstOrDefault();

            foreach (var declaration in declarations)
            {
                yield return declaration;
            }
            var classNameElement = SrcMLHelper.GetClassNameForMethod(container);
            if (null != classNameElement)
            {
                var className = classNameElement.Value;
                var classDefinition = (from t in db.Definitions.OfType<TypeDefinition>()
                                       where t.TypeName == className
                                       select t).FirstOrDefault();
                if (null != classDefinition)
                {
                    var block = classDefinition.Xml.Element(SRC.Block);
                    if (null != block)
                    {
                        foreach (var declaration in GetDeclarationsFromBlock(block, db))
                        {
                            declaration.AddAnnotation(classDefinition);
                            yield return declaration;
                        }
                        
                        foreach (var name in ContainerNames.ClassSections)
                        {
                            foreach (var child in block.Elements(name))
                            {
                                foreach (var declaration in GetDeclarationsFromElement(child, db))
                                {
                                    declaration.AddAnnotation(classDefinition);
                                    yield return declaration;
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Class to represent method definitions in the database
    /// </summary>
    partial class MethodDefinition
    {
        private readonly static HashSet<XName> _validNames = new HashSet<XName>()
        {
            SRC.Constructor,
            SRC.Destructor,
            SRC.Function
        };
        /// <summary>
        /// Valid XNames for method definitions
        /// </summary>
        public static new HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodDefinition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MethodDefinition(XElement element, string fileName)
            : base(element, fileName)
        {
            if (null == element)
                throw new ArgumentNullException("element");
            if (null == fileName)
                throw new ArgumentNullException("fileName");
            SrcMLHelper.ThrowExceptionOnInvalidName(element, ValidNames);

            this.MethodName = SrcMLHelper.GetNameForMethod(element).Value;
            var className = GetParentTypeName(element);
            
            if (null != className)
                this.MethodClassName = className;

            if (element.Elements(SRC.Type).Any())
                this.MethodReturnTypeName = element.Element(SRC.Type).Elements(SRC.Name).Last().Value;

            var parameterList = element.Element(SRC.ParameterList);
            this.NumberOfMethodParameters = 0;
            this.NumberOfMethodParametersWithDefaults = 0;
            if (null != parameterList)
            {
                this.NumberOfMethodParameters = CountParameters(parameterList.Elements(SRC.Parameter));
                this.NumberOfMethodParametersWithDefaults = CountParametersWithDefaults(this.NumberOfMethodParameters ?? 0, parameterList.Elements(SRC.Parameter));
            }
            this.MethodSignature = MakeSignature(element.Name, this.MethodClassName, this.MethodName, this.NumberOfMethodParameters ?? 0, element.GetSrcLineNumber());
        }

        /// <summary>
        /// Counts the parameters. This function is used to handle the special case of <c>foo(void)</c>, which has 0 parameters.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public static int CountParameters(IEnumerable<XElement> parameters)
        {
            if (null == parameters)
                throw new ArgumentNullException("parameters");
            int count = parameters.Count();
            if (1 == count && parameters.First().Value == "void")
            {
                return 0;
            }
            return count;
        }

        internal static int CountParametersWithDefaults(int numberOfParametersWithDefaults, IEnumerable<XElement> parameters)
        {
            if (numberOfParametersWithDefaults > 0)
            {
                return (from p in parameters
                        where p.Elements().First().Elements(SRC.Init).Any()
                        select p).Count();
            }
            return 0;
        }
        /// <summary>
        /// Makes the signature.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="numberOfParameters">The number of parameters.</param>
        /// <param name="lineNumber">The line number.</param>
        /// <returns>a method signature</returns>
        public static string MakeSignature(XName elementType, string className, string methodName, int numberOfParameters, int lineNumber)
        {
            string methodType = String.Empty;
            if (elementType == SRC.Constructor)
                methodType = "Constructor";
            else if (elementType == SRC.Destructor)
                methodType = "Destructor";
            else if (elementType == SRC.Function)
                methodType = "Function";

            if (string.IsNullOrEmpty(className))
                className = String.Empty;
            
            var nameSeparator = (string.IsNullOrEmpty(className) ? String.Empty : "::");
            return String.Format(CultureInfo.InvariantCulture,
                                 "{0}Signature({1}{2}{3},{4}):{5}", methodType, className,
                                 nameSeparator, methodName, numberOfParameters, lineNumber);
        }
        internal static string GetParentTypeName(XElement element)
        {
            var typeNameElement = SrcMLHelper.GetClassNameForMethod(element);
            if (null == typeNameElement)
            {
                typeNameElement = (from p in element.Ancestors()
                                   where TypeDefinition.ValidNames.Contains(p.Name)
                                   where p.Elements(SRC.Name).Any()
                                   select p.Element(SRC.Name)).FirstOrDefault();

            }

            if (null == typeNameElement)
                return null;
            return typeNameElement.Value;
        }
    }

    /// <summary>
    /// class to represent various type definitions
    /// </summary>
    partial class TypeDefinition
    {
        private readonly static HashSet<XName> _validNames = new HashSet<XName>()
        {
            SRC.Class,
            SRC.Struct,
            SRC.Typedef
        };

        /// <summary>
        /// Valid XNames for type definitions
        /// </summary>
        public static new HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDefinition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The name.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TypeDefinition(XElement element, string name, string fileName)
            : base(element, fileName)
        {
            if (null == element)
                throw new ArgumentNullException("element");
            if (null == name)
                throw new ArgumentNullException("name");
            if (null == fileName)
                throw new ArgumentNullException("fileName");
            SrcMLHelper.ThrowExceptionOnInvalidName(element, ValidNames);

            this.TypeName = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDefinition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TypeDefinition(XElement element, string fileName)
            : base(element, fileName)
        {
            if (null == element)
                throw new ArgumentNullException("element");
            if (null == fileName)
                throw new ArgumentNullException("fileName");
            SrcMLHelper.ThrowExceptionOnInvalidName(element, ValidNames);

            var nameElement = element.Element(SRC.Name);
            if (null != nameElement)
            {
                this.TypeName = nameElement.Value;
            }
        }
    }
}
