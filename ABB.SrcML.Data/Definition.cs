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

namespace ABB.SrcML.Data
{
    /// <summary>
    /// base class for definitions
    /// </summary>
    partial class Definition
    {
        private static readonly HashSet<XName> _validNames = new HashSet<XName>(ScopeDefinition.ValidNames.Concat(Declaration.ValidNames));

        /// <summary>
        /// Valid XNames for definitions
        /// </summary>
        public static HashSet<XName> ValidNames
        {
            get
            {
                return _validNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Definition"/> class.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file the element belongs to</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected Definition(XElement element, string fileName)
            : this()
        {
            if (null == element)
                throw new ArgumentNullException("element");
            this.FileName = fileName;
            this.LineNumber = element.GetSrcLineNumber();
            this.XPath = element.GetXPath(false);
            this.Xml = element;
            this.ElementXName = element.Name.ToString();
        }

        /// <summary>
        /// Create a definition from the given elementXName
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="fileName">Name of the file that contains the element</param>
        /// <param name="archiveId">The archive id that the file belongs to</param>
        /// <returns></returns>
        public static IEnumerable<Definition> CreateFromElement(XElement element, string fileName, int archiveId)
        {
            IEnumerable<Definition> definitions = Enumerable.Empty<Definition>();
            if (Declaration.ValidNames.Contains(element.Name))
            {
                definitions = Declaration.CreateFromElement(element, fileName);
            }
            else if (ScopeDefinition.ValidNames.Contains(element.Name))
            {
                definitions = ScopeDefinition.CreateFromElement(element, fileName);
            }
            
            foreach (var definition in definitions)
            {
                definition.ArchiveId = archiveId;
                yield return definition;
            }
        }
    }
}
