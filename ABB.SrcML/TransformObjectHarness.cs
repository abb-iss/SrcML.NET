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
using System.Reflection;
using System.Xml.Linq;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// TransformObjectHarness is a test harness for testing objects that implement the <see cref="ITransform"/> interface.
    /// </summary>
    public class TransformObjectHarness : ITransform
    {
        private Type _type;

        /// <summary>
        /// Instantiates a new QueryFunctionTestObject for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to create a Test object for.</param>
        public TransformObjectHarness(Type type)
        {
            CheckTypeIsValid(type);
            this._type = type;
        }

        private TransformObjectHarness()
        {

        }

        private static void CheckTypeIsValid(Type type)
        {
            
            if (null == type)
                throw new ArgumentNullException("type", "Cannot pass null to TransformObjectHarness");
            if (null == type.GetInterface("ABB.SrcML.ITransform"))
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "{0} must implement ABB.SrcML.ITransform", type.FullName), "type");
            
            var defaultConstructors = from constructor in type.GetConstructors()
                                      where constructor.IsPublic
                                      where 0 == constructor.GetParameters().Length
                                      select constructor;
            if (1 != defaultConstructors.Count())
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "{0} must have a public default constructor", type.FullName), "type");
        }
        /// <summary>
        /// Returns the type being tested.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture, "<TransformObjectHarness {0}>", _type.FullName);
        }
        #region ITransform Members

        /// <summary>
        /// Instantiates the object under test and then executes its query function.
        /// </summary>
        /// <param name="element">The XElement to query</param>
        /// <returns>an IEnumerable of the results</returns>
        public IEnumerable<XElement> Query(XElement element)
        {
            ITransform transform = Activator.CreateInstance(this._type) as ITransform;
            var results = transform.Query(element);
            return results as IEnumerable<XElement>;
        }

        /// <summary>
        /// Instantiates the object under test and then executes its transform function.
        /// </summary>
        /// <param name="element">The XElement to transform</param>
        /// <returns>The transformed version of <paramref name="element"/></returns>
        public XElement Transform(XElement element)
        {
            ITransform transform = Activator.CreateInstance(this._type) as ITransform;
            XElement transformedElement = transform.Transform(element);
            return transformedElement;
        }

        #endregion
    }
}
