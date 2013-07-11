/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using System.Xml;
using System.IO;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// Extensions for working with SrcML documents
    /// </summary>
    public static class Extensions
    {
        #region UNUSED
        /// <summary>
        /// Checks whether the given container contains a call to the specified function.
        /// </summary>
        /// <param name="container">The container to test.</param>
        /// <param name="functionName">The function name to look for.</param>
        /// <returns>True if the call exists, false if not.</returns>
        public static bool ContainsCallTo(this XContainer container, string functionName)
        {
            if (null == container)
                throw new ArgumentNullException("container");

            return container.Descendants(SRC.Call).Where(c => c.Element(SRC.Name).Value == functionName).Any();
        }

        /// <summary>
        /// Checks whether the element is a declaration statement for a variable of the specified type.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="typeName">The typename to look for.</param>
        /// <returns>True if this is a declaration for the given type; false if not.</returns>
        public static bool IsDeclOfType(this XElement element, string typeName)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            return element.Name == SRC.DeclarationStatement &&
                   (from decl in element.Descendants(SRC.Declaration)
                    where decl.Elements(SRC.Type).Any()
                    where decl.Element(SRC.Type).Value == typeName
                    select decl).Any();
        }

        /// <summary>
        /// Gets the local declaration corresponding to the given name.
        /// </summary>
        /// <param name="name">A <see cref="SRC"/> element.</param>
        /// <returns>The corresponding declaration, null if not found.</returns>
        public static XElement GetLocalDecl(this XElement name)
        {
            if (null == name)
                throw new ArgumentNullException("name");

            SrcMLElement.ThrowExceptionOnInvalidName(name, SRC.Name);

            var decls = from d in name.Ancestors(SRC.Function).First().Descendants(SRC.Declaration)
                        where d.Elements(SRC.Name).Any()
                        where d.IsBefore(name) && d.Element(SRC.Name).Value == name.Value
                        select d;
            return decls.Last();
        }
        #endregion
    }
}
