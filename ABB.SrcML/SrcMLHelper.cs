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
using System.IO;

namespace ABB.SrcML
{
    /// <summary>
    /// Collection of helper functions for working with srcML elements
    /// </summary>
    public static class SrcMLHelper
    {
        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Throws a SrcMLRequiredNameException if <paramref name="name"/> does not match <paramref name="requiredName"/>.</exception>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="requiredName">Name of the required.</param>
        public static void ThrowExceptionOnInvalidName(XName name, XName requiredName)
        {
            if (name != requiredName)
                throw new SrcMLRequiredNameException(requiredName);
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Throws a SrcMLRequiredNameException if <paramref name="name"/> is not in the list of <paramref name="validNames">valid names</paramref>.</exception>
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="validNames">The valid names.</param>
        public static void ThrowExceptionOnInvalidName(XName name, IEnumerable<XName> validNames)
        {
            if (validNames.All(validName => validName != name))
                throw new SrcMLRequiredNameException(validNames.ToList());
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Thrown if the given <paramref name="element"/> does not have <paramref name="requiredName"/> as it's Name.</exception>
        /// </summary>
        /// <param name="element">The element to check the name for</param>
        /// <param name="requiredName">The name required</param>
        public static void ThrowExceptionOnInvalidName(XElement element, XName requiredName)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            ThrowExceptionOnInvalidName(element.Name, requiredName);
        }

        /// <summary>
        /// <exception cref="SrcMLRequiredNameException">Thrown if the given <paramref name="element"/> does not have a Name from the list of <paramref name="validNames"/></exception>
        /// </summary>
        /// <param name="element">The element to check the name for</param>
        /// <param name="validNames">The collection of valid names</param>
        public static void ThrowExceptionOnInvalidName(XElement element, IEnumerable<XName> validNames)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            ThrowExceptionOnInvalidName(element.Name, validNames);
        }
        /// <summary>
        /// <para>Gets the function name for the given method.</para>
        /// <para>If the function is an implementation of a class method, it has two parts: the class name and the method name. This function returns just the method name if both are present</para>
        /// </summary>
        /// <param name="method">The method to get the name for</param>
        /// <returns>The name of the method</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static XElement GetNameForMethod(XElement method)
        {
            if (null == method)
                throw new ArgumentNullException("method");

            ThrowExceptionOnInvalidName(method, new List<XName>() { SRC.Constructor, SRC.Destructor, SRC.Function,
                                                                    SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration,
                                                                    SRC.Call });

            var name = method.Element(SRC.Name);

            if (null == name)
            {
                return null;
            }
            if (name.Elements(SRC.Name).Any())
                return name.Elements(SRC.Name).Last();
            else
                return name;
        }

        /// <summary>
        /// Gets the class name for method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>the class name if found. Otherwise, null</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static XElement GetClassNameForMethod(XElement method)
        {
            if (null == method)
                throw new ArgumentNullException("method");

            ThrowExceptionOnInvalidName(method, new List<XName>() { SRC.Constructor, SRC.Destructor, SRC.Function,
                                                                    SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration,
                                                                    SRC.Call });

            var name = method.Element(SRC.Name);
            if (null == name)
            {
                return null;
            }

            var nameCount = name.Elements(SRC.Name).Count();

            if (nameCount > 1)
            {
                var className = name.Elements(SRC.Name).Skip(nameCount - 2).FirstOrDefault();
                return className;
            }

            return null;
        }
        /// <summary>
        /// <para>Gets all the calls contained in a function element.Function elements can either be of type <c>SRC.Function</c> or <c>SRC.Constructor</c>.</para>
        /// <exception cref="ABB.SrcML.SrcMLRequiredNameException">thrown if <c>function.Name</c> is not <c>SRC.Constructor</c> or <c>SRC.Function</c></exception>
        /// </summary>
        /// <param name="function">the function to find calls in</param>
        /// <returns>all method calls and constructor uses</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static IEnumerable<XElement> GetCallsFromFunction(XElement function)
        {
            if (null == function)
                throw new ArgumentNullException("function");

            SrcMLHelper.ThrowExceptionOnInvalidName(function, new List<XName>() { SRC.Function, SRC.Constructor, SRC.Destructor });

            var calls = from call in function.Descendants(SRC.Call)
                                  select call;
            var constructorCalls = from decl in function.Descendants(SRC.Declaration)
                                   where decl.Element(SRC.ArgumentList) != null
                                   select decl;
            var allCalls = calls.Concat(constructorCalls).InDocumentOrder();

            return allCalls;
        }

        /// <summary>
        /// Gets the default srcML binary directory. It checks the following conditions:
        /// 1. If the SRCMLBINDIR environment variable is set, then that is used.
        /// 2. If c:\Program Files (x86)\SrcML\bin directory exists (should only exist on 64-bit systems), then that is used.
        /// 3. If c:\Program Files\SrcML\bin directory exists, then that is used.
        /// 4. If none of the above is true, then the current directory is used.
        /// 
        /// This function does not check that any of the paths actually contains the srcML executables.
        /// </summary>
        /// <returns>The default srcML binary directory.</returns>
        public static string GetSrcMLDefaultDirectory()
         {
            var srcmlDir = Environment.GetEnvironmentVariable("SRCMLBINDIR");
            if (null == srcmlDir)
            {
                var programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                if (null == programFilesDir)
                    programFilesDir = Environment.GetEnvironmentVariable("ProgramFiles");
                srcmlDir = Path.Combine(programFilesDir, Path.Combine("SrcML", "bin"));
            }
             
            if (!Directory.Exists(srcmlDir))
                return Directory.GetCurrentDirectory();
            return srcmlDir;
        }

        /// <summary>
        /// Returns the default srcML binary directory.
        /// </summary>
        /// <param name="extensionDirectory"></param>
        /// <returns></returns>
        public static string GetSrcMLDefaultDirectory(string extensionDirectory)
        {
            return Path.Combine(extensionDirectory, "SrcML");
        }
    }
}
