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
using System.Collections;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Class to represent declaration info in the database.
    /// </summary>
    partial class Declaration
    {
        private readonly static HashSet<XName> _validNames = new HashSet<XName>()
        {
            SRC.ClassDeclaration,
            SRC.ConstructorDeclaration,
            SRC.Declaration,
            SRC.DestructorDeclaration,
            SRC.FunctionDeclaration,
            SRC.StructDeclaration
        };

        /// <summary>
        /// Valid XNames for declarations
        /// </summary>
        public static new HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }
        /// <summary>
        /// Creates a declaration object from the given element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>the declaration object</returns>
        public static IEnumerable<Declaration> CreateFromElement(XElement element, string fileName)
        {
            if (VariableDeclaration.ValidNames.Contains(element.Name))
            {
                yield return new VariableDeclaration(element, fileName);
            }
            else if (MethodDeclaration.ValidNames.Contains(element.Name))
            {
                yield return new MethodDeclaration(element, fileName);
            }
            else if (TypeDeclaration.ValidNames.Contains(element.Name))
            {
                yield return new TypeDeclaration(element, fileName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Declaration"/> class.
        /// </summary>
        /// <param name="declaration">The declaration.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected Declaration(XElement declaration, string fileName)
            : base(declaration, fileName)
        {
            if (null == declaration)
                throw new ArgumentNullException("declaration");
            if (null == fileName)
                throw new ArgumentNullException("fileName");

            var variableName = declaration.Element(SRC.Name);
            if (null != variableName)
            {
                this.DeclarationName = variableName.Value;
            }
            this.IsGlobal = (declaration.Parent.Name == SRC.DeclarationStatement &&
                             declaration.Parent.Parent.Name == SRC.Unit)
                            ||
                            (declaration.Parent.Name == SRC.DeclarationStatement && 
                             declaration.Parent.Parent.Name == SRC.Block &&
                             declaration.Parent.Parent.Parent.Name == SRC.Extern);
        }
    }

    /// <summary>
    /// Class to represent variable declarations in the database
    /// </summary>
    partial class VariableDeclaration
    {
        private static readonly HashSet<XName> _validNames = new HashSet<XName>() { SRC.Declaration };
        
        /// <summary>
        /// Valid XNames for variable declarations
        /// </summary>
        public new static HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableDeclaration"/> class.
        /// </summary>
        /// <param name="declaration">The declaration.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public VariableDeclaration(XElement declaration, string fileName)
            : base(declaration, fileName)
        {
            if (null == declaration)
                throw new ArgumentNullException("declaration");
            if (null == fileName)
                throw new ArgumentNullException("fileName");
            SrcMLHelper.ThrowExceptionOnInvalidName(declaration, ValidNames);

            var typeElement = declaration.Element(SRC.Type);
            if (null != typeElement && typeElement.Elements(SRC.Name).Any())
            {
                this.VariableTypeName = typeElement.Elements(SRC.Name).Last().Value;
            }
        }
    }

    /// <summary>
    /// Class to represent method declarations in the database
    /// </summary>
    partial class MethodDeclaration
    {
        /// <summary>
        /// Valid XNames for method declarations
        /// </summary>
        public new static HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }
        
        private static readonly HashSet<XName> _validNames = new HashSet<XName>()
        {
            SRC.ConstructorDeclaration,
            SRC.DestructorDeclaration,
            SRC.FunctionDeclaration,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodDeclaration"/> class.
        /// </summary>
        /// <param name="declaration">The declaration.</param>
        /// <param name="fileName">Name of the file.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public MethodDeclaration(XElement declaration, string fileName)
            : base(declaration, fileName)
        {
            if (null == declaration)
                throw new ArgumentNullException("declaration");
            if (null == fileName)
                throw new ArgumentNullException("fileName");
            SrcMLHelper.ThrowExceptionOnInvalidName(declaration, ValidNames);

            var className = MethodDefinition.GetParentTypeName(declaration);
            if (null != className)
            {
                this.DeclarationClassName = className;
            }

            var returnTypeElement = declaration.Element(SRC.Type);
            if (null != returnTypeElement && returnTypeElement.Elements(SRC.Name).Any())
            {
                this.DeclarationReturnTypeName = returnTypeElement.Elements(SRC.Name).Last().Value;
            }

            this.DeclarationNumberOfParameters= 0;
            this.DeclarationNumberOfParametersWithDefaults= 0;

            var parameterList = declaration.Element(SRC.ParameterList);
            if (null != parameterList)
            {
                this.DeclarationNumberOfParameters = MethodDefinition.CountParameters(parameterList.Elements(SRC.Parameter));
                this.DeclarationNumberOfParametersWithDefaults = MethodDefinition.CountParametersWithDefaults(this.DeclarationNumberOfParameters ?? 0, parameterList.Elements(SRC.Parameter));
            }
        }
    }

    /// <summary>
    /// Class to represent type declarations in the database
    /// </summary>
    partial class TypeDeclaration
    {
        /// <summary>
        /// Valid XNames for type declarations
        /// </summary>
        public new static HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }
        private static readonly HashSet<XName> _validNames = new HashSet<XName>()
        {
            SRC.ClassDeclaration,
            SRC.StructDeclaration
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDeclaration"/> class.
        /// </summary>
        /// <param name="declaration">The declaration.</param>
        /// <param name="fileName">Name of the file.</param>
        public TypeDeclaration(XElement declaration, string fileName)
            : base(declaration, fileName)
        {

        }
    }
}
