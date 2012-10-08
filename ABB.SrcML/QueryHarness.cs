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
using System.Reflection;
using System.Globalization;

namespace ABB.SrcML
{
    /// <summary>
    /// QueryHarness is a test harness for methods with the <see cref="QueryAttribute"/>. The test takes a type and a method to be tested.
    /// </summary>
    public class QueryHarness : ITransform
    {
        private Type _type;
        private MethodInfo _method;
        private ConstructorInfo _constructor;

        /// <summary>
        /// Instantiates a new QueryFunctionTestObject with <paramref name="type"/> and <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The type to make a query function for</param>
        /// <param name="methodName">the method in <paramref name="type"/> to test</param>
        public QueryHarness(Type type, string methodName) : this(type, GetMethod(type, methodName))
        {

        }

        /// <summary>
        /// Instantiates a new QueryFunctionTestObject with <paramref name="type"/> and <paramref name="method"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="method"></param>
        public QueryHarness(Type type, MethodInfo method)
        {
            if (null == type)
                throw new ArgumentNullException("type", "type cannot be null");
            if (null == method)
                throw new ArgumentNullException("method", "method cannot be null");

            CheckArguments(type, method);
            this._type = type;
            _constructor = type.GetConstructor(Type.EmptyTypes);
            this._method = method;
        }

        private QueryHarness()
        {

        }

        private static void CheckArguments(Type type, MethodInfo method)
        {   
            var defaultConstructors = from constructor in type.GetConstructors()
                                      where constructor.IsPublic
                                      where 0 == constructor.GetParameters().Length
                                      select constructor;
            if (1 != defaultConstructors.Count() && !method.IsStatic)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "{0} must have a public default constructor if {1} is not static", type.FullName, method.Name), "type");
        }

        private static MethodInfo GetMethod(Type type, string methodName)
        {
            if (null == type)
                throw new ArgumentNullException("type");
            if (null == methodName)
                throw new ArgumentNullException("methodName");

            MethodInfo method = type.GetMethod(methodName, new Type[] { typeof(XElement) });

            if (null == method)
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "{0} was not found in {1}", methodName, type.FullName), "methodName");
            return method;
        }

        /// <summary>
        /// Generates a QueryHarness object for each function in <paramref name="type"/> that has the <see cref="QueryAttribute"/>
        /// and matches the <see cref="ITransform.Query"/> signature.
        /// </summary>
        /// <param name="type">The type to find queries in.</param>
        /// <returns>An IEnumerable of QueryHarness objects</returns>
        public static IEnumerable<ITransform> CreateFromType(Type type)
        {
            if (null == type)
                throw new ArgumentNullException("type");

            var tests = from method in type.GetMethods()
                          let attributes = method.GetCustomAttributes(typeof(QueryAttribute), true)
                          where null != attributes && 0 < attributes.Length
                          where typeof(IEnumerable<XElement>) == method.ReturnType
                          let parameters = method.GetParameters()
                          where 1 == parameters.Length
                          where typeof(XElement) == parameters[0].ParameterType
                          select new QueryHarness(type, method) as ITransform;
            return tests;
        }

        /// <summary>
        /// Gives the full signature of the function being tested.
        /// </summary>
        /// <returns>The full signature &lt;return type&gt; &lt;type&gt;.&lt;function name&gt;(&lt;parameter list&gt;)</returns>
        public override string ToString()
        {
            var parameterTypeNames = from param in _method.GetParameters()
                                     select param.ParameterType.Name;
            var parameters = String.Join(",", parameterTypeNames.ToArray<string>());
            
            return String.Format(CultureInfo.InvariantCulture, "<QueryHarness {0} {1}.{2}({3})>", _method.ReturnType.Name, _type.FullName, _method.Name, parameters);
        }
        #region ITransform Members
        /// <summary>
        /// The query function takes the given type and executes its Query function.
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public IEnumerable<XElement> Query(XElement element)
        {
            var instance = (this._method.IsStatic ? null : this._constructor.Invoke(new object[] { }));
            var results = this._method.Invoke(instance, new object[] { element });
            return results as IEnumerable<XElement>;
        }

        /// <summary>
        /// This just returns the input <paramref name="element"/>
        /// </summary>
        /// <param name="element">The XElement to transform</param>
        /// <returns><paramref name="element"/></returns>
        public XElement Transform(XElement element)
        {
            return element;
        }

        #endregion
    }
}
