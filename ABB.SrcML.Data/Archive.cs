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
using System.Data.Linq;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Class representing a SrcML archive from the database
    /// </summary>
    partial class Archive
    {
        private static HashSet<XName> ScopeContainers = new HashSet<XName>(ContainerNames.All);

        private SrcMLFile _document;

        /// <summary>
        /// Gets the document.
        /// </summary>
        public SrcMLFile Document
        {
            get
            {
                if (null == this._document)
                    this._document = new SrcMLFile(this.Path);
                return this._document;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Archive"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Archive(SrcMLFile document)
            : this()
        {
            if (null == document)
                throw new ArgumentNullException("document");

            this.Path = document.FileName;
            this.LastUpdated = System.IO.File.GetLastWriteTime(this.Path);
        }

        /// <summary>
        /// Gets the definitions from archive.
        /// </summary>
        /// <returns>all the definitions associated with this archive</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public IEnumerable<Definition> GetDefinitionsFromArchive()
        {
            var definitions = from unit in this.Document.FileUnits
                              from definition in GetDefinitionsFromFileUnit(unit)
                              select definition;
            return definitions;
        }

        /// <summary>
        /// Creates definitions from this file unit
        /// </summary>
        /// <param name="fileUnit">The file unit.</param>
        /// <returns>an enumerable of definition objects to be inserted into the database</returns>
        public IEnumerable<Definition> GetDefinitionsFromFileUnit(XElement fileUnit)
        {
            if (fileUnit == null)
                throw new ArgumentNullException("fileUnit");

            var fileName = fileUnit.Attribute("filename").Value;
            var definitions = from child in fileUnit.DescendantsAndSelf()
                              where Definition.ValidNames.Contains(child.Name)
                              let defs = Definition.CreateFromElement(child, fileName, this.Id)
                              from def in defs
                              select def;
            return definitions;
        }

        /// <summary>
        /// Locates possible variable declarations that match the given name element.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="element">The element.</param>
        /// <returns>A collection of declarations that match this element</returns>
        public IEnumerable<VariableDeclaration> GetDeclarationForVariable(SrcMLDataContext db, XElement element)
        {
            if (null == db)
                throw new ArgumentNullException("db");
            if (null == element)
                throw new ArgumentNullException("element");
            //SrcMLHelper.ThrowExceptionOnInvalidName(element, SRC.Name);

            var parentContainer = (from container in element.Ancestors()
                                   where ScopeContainers.Contains(container.Name)
                                   select container).FirstOrDefault();
            if (null != parentContainer)
            {
                var parentXPath = parentContainer.GetXPath(false);
                return GetDeclarationForVariable(db, element, parentXPath);
            }
            return null;
        }

        /// <summary>
        /// Locates possible variable declarations that match the given name element.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="element">The element.</param>
        /// <param name="containerPath">The container path.</param>
        /// <returns>A collection of declarations that match this element</returns>
        public IEnumerable<VariableDeclaration> GetDeclarationForVariable(SrcMLDataContext db, XElement element, string containerPath)
        {
            if (null == db)
                throw new ArgumentNullException("db");
            var declarations = from declaration in db.Definitions.OfType<VariableDeclaration>()
                               where declaration.ArchiveId == this.Id
                               where declaration.DeclarationName == element.Value
                               where declaration.ValidScopes.Any(vs => vs.XPath == containerPath)
                               select declaration;
            
            if (!declarations.Any())
            {
                declarations = from declaration in db.Definitions.OfType<VariableDeclaration>()
                               where declaration.Archive == this
                               where declaration.DeclarationName == element.Value
                               where declaration.IsGlobal ?? false
                               select declaration;
            }

            return declarations;
        }

        /// <summary>
        /// Locates the ty
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="typeElement">The type element.</param>
        /// <returns>a collection of type definitions that match the given element.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public IEnumerable<TypeDefinition> GetTypeForVariableName(SrcMLDataContext db, XElement typeElement)
        {
            if (null == db)
                throw new ArgumentNullException("db");
            if (null == typeElement)
                throw new ArgumentNullException("typeElement");
            SrcMLHelper.ThrowExceptionOnInvalidName(typeElement, SRC.Type);

            var typeName = typeElement.Elements(SRC.Name).Last().Value;

            var typeCandidates = from typeDefinition in db.Definitions.OfType<TypeDefinition>()
                                 where typeDefinition.Archive == this
                                 where typeDefinition.TypeName == typeName
                                 select typeDefinition;
            
            return typeCandidates;
        }

        /// <summary>
        /// Gets a collection of method definitions that match the given call element.
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="call">The call.</param>
        /// <returns>A collection of method definitions that match</returns>
        public IEnumerable<MethodDefinition> GetMethodForCall(SrcMLDataContext db, XElement call)
        {
            if (null == db)
                throw new ArgumentNullException("db");
            if (null == call)
                throw new ArgumentNullException("call");
            SrcMLHelper.ThrowExceptionOnInvalidName(call, SRC.Call);

            var containingFunction = (from parent in call.Ancestors()
                                      where ContainerNames.MethodDefinitions.Any(n => n == parent.Name)
                                      select parent).First();
            
            var className = SrcMLHelper.GetClassNameForMethod(containingFunction);
            TypeDefinition classDef = null;
            if (null != className)
            {
                var candidates = from ce in db.Definitions.OfType<TypeDefinition>()
                                 where ce.TypeName == className.Value
                                 select ce;
                if (candidates.Any())
                {
                    classDef = candidates.First();
                }
            }

            var precedingElements = call.ElementsBeforeSelf();
            if (precedingElements.Any())
            {
                var last = precedingElements.Last();
                var count = precedingElements.Count();

                if (last.Name == OP.Operator && count > 1 && (last.Value == "." || last.Value == "->"))
                {
                    var callingObject = precedingElements.Take(count - 1).Last();
                    if("this" != callingObject.Value)
                    {
                        var candidateDeclarations = this.GetDeclarationForVariable(db, callingObject);
                        if (candidateDeclarations.Any())
                        {
                            var declaration = candidateDeclarations.First();
                            var possibleTypes = from ce in db.Definitions.OfType<TypeDefinition>()
                                                where ce.TypeName == declaration.VariableTypeName
                                                select ce;
                            if (possibleTypes.Any())
                            {
                                classDef = possibleTypes.First();
                            }
                        }
                    }
                }
            }

            var numArguments = call.Element(SRC.ArgumentList).Elements(SRC.Argument).Count();
            string typeName = null;
            if (null != classDef)
                typeName = classDef.TypeName;

            var methods = from method in db.Definitions.OfType<MethodDefinition>()
                          where method.MethodName == call.Element(SRC.Name).Value
                          where (typeName == null ? true : typeName == method.MethodClassName)
                          let minNumberOfParameters = method.NumberOfMethodParameters - method.NumberOfMethodParametersWithDefaults
                          where numArguments >= minNumberOfParameters && numArguments <= method.NumberOfMethodParameters
                          select method;
            return methods;
        }
    }
}

