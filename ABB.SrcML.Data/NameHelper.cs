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
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// <para>NameHelper provides a collection of static methods that aid in parsing elements of
    /// with a node type of <see cref="ABB.SrcML.SRC.Name"/>.</para> <para>The functions are
    /// targetted at helping to parse the common srcML idiom of nesting
    /// <see cref="ABB.SrcML.SRC.Name"/> elements within other <see cref="ABB.SrcML.SRC.Name">name
    /// elements.</see></para>
    /// </summary>
    public static class NameHelper {

        /// <summary>
        /// Gets the string value for the <see cref="GetLastNameElement(XElement)">last name
        /// element</see> of
        /// <paramref name="nameElement"/></summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>the string value for the last name in
        /// <paramref name="nameElement"/></returns>
        public static string GetLastName(XElement nameElement) {
            var lastNameElement = GetLastNameElement(nameElement);
            return (null == lastNameElement ? String.Empty : lastNameElement.Value);
        }

        /// <summary>
        /// Gets the last name from
        /// <paramref name="nameElement"/>. If
        /// <paramref name="nameElement"/> has no children of type <see cref="ABB.SrcML.SRC.Name"/>,
        /// it just returns
        /// <paramref name="nameElement"/>.
        /// </summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>The last <see cref="ABB.SrcML.SRC.Name">name element</see> in
        /// <paramref name="nameElement"/>. If there are none, it returns
        /// <paramref name="nameElement"/></returns>
        public static XElement GetLastNameElement(XElement nameElement) {
            var names = GetNameElementsFromName(nameElement);
            var lastName = names.Last();
            return lastName;
        }

        /// <summary>
        /// Gets all of the name elements from <paramref name="nameElement"/> except for the last one. If
        /// <paramref name="nameElement"/>has no children of type <see cref="ABB.SrcML.SRC.Name"/>,
        /// returns an empty enumerable.
        /// </summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>An enumerable of <see cref="ABB.SrcML.SRC.Name">name elements</see> in
        /// <paramref name="nameElement"/>except for the <see cref="GetLastNameElement">last
        /// </see></returns>
        public static IEnumerable<XElement> GetNameElementsExceptLast(XElement nameElement) {
            var last = GetLastNameElement(nameElement);
            return GetNameElementsFromName(nameElement).TakeWhile(e => e != last);
        }

        /// <summary>
        /// This helper function returns all of the names from a name element. If a name element has
        /// no children, it just yields the name element back. However, if the name element has
        /// child elements, it yields all of the child name elements.
        /// </summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>An enumerable of either all the child names, or the root if there are
        /// none.</returns>
        public static IEnumerable<XElement> GetNameElementsFromName(XElement nameElement) {
            if(nameElement == null)
                throw new ArgumentNullException("nameElement");
            if(nameElement.Name != SRC.Name)
                throw new ArgumentException("should be a SRC.Name", "nameElement");

            if(!nameElement.Elements(SRC.Name).Any()) {
                yield return nameElement;
            } else {
                foreach(var name in nameElement.Elements(SRC.Name)) {
                    yield return name;
                }
            }
        }

        /// <summary>
        /// Gets the string values for all of the name elements in
        /// <paramref name="nameElement"/><see cref="GetNameElementsExceptLast(XElement)">except for
        /// the last one</see>.
        /// </summary>
        /// <param name="nameElement">The name element</param>
        /// <returns>An enumerable of strings of all the name elements in
        /// <paramref name="nameElement"/>except for the last one. If there are no child elements,
        /// it returns an empty enumerable.</returns>
        public static IEnumerable<string> GetNamesExceptLast(XElement nameElement) {
            var names = from name in GetNameElementsExceptLast(nameElement)
                        select name.Value;
            return names;
        }
    }
}