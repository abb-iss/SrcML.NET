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
using ABB.SrcML;
using ABB.SrcML.Data;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test
{
    /// <summary>
    /// Summary description for MethodCallTester
    /// </summary>
    public class MethodCallTester
    {
        public static void TestMethodCalls(string xmlFilePath, List<string> testNames, int testCountPerName = 10)
        {
            Helper<MethodDefinition>.RunOnMap(xmlFilePath, Verify, FindCallsTo, FindMethodsInArchive, testNames, testCountPerName);
        }

        private static IEnumerable<XElement> FindCallsTo(XElement unit, string name)
        {
            var results = from call in unit.Descendants(SRC.Call)
                          where call.Element(SRC.Name).Value == name
                          select call;
            return results;
        }

        private static IEnumerable<MethodDefinition> FindMethodsInArchive(SrcMLDataContext db, Archive archive, XElement element)
        {
            return archive.GetMethodForCall(db, element);
        }

        private static bool Verify(SrcMLDataContext db, MethodDefinition def, XElement use)
        {
            var methodNameElement = SrcMLHelper.GetNameForMethod(use);
            bool namesMatch = def.MethodName == methodNameElement.Value;

            var numberOfArguments = use.Element(SRC.ArgumentList).Elements(SRC.Argument).Count();
            var mininumNumberOfArguments = def.NumberOfMethodParameters - def.NumberOfMethodParametersWithDefaults;

            bool argCountMatches = numberOfArguments >= mininumNumberOfArguments && numberOfArguments <= def.NumberOfMethodParameters;

            return namesMatch && argCountMatches;
        }

        private void MethodDeclarationsWithMultipleMatches(string xmlFilePath)
        {
            var dbName = SrcMLDataContext.MakeDBName(xmlFilePath);

            using (var db = SrcMLDataContext.CreateDatabaseConnection(dbName))
            {
                db.ObjectTrackingEnabled = false;
                db.CommandTimeout = 300;
                
                var archive = (from a in db.Archives
                               where a.Path == xmlFilePath
                               select a).First();


                var declarations = from unit in archive.Document.FileUnits
                                   from declaration in unit.Descendants()
                                   where ContainerNames.MethodDeclarations.Contains(declaration.Name)
                                   select declaration;

                int numDeclarations = 0;
                int declarationsWithSingleMatch = 0;
                int declarationsWithNoMatch = 0;
                int declarationsWithMultipleMatches = 0;
                foreach (var declaration in declarations)
                {
                    numDeclarations++;
                    var methodNameElement = SrcMLHelper.GetNameForMethod(declaration);
                    var classNameElement = SrcMLHelper.GetClassNameForMethod(declaration);

                    string className = null;
                    string methodName = null;

                    if (null != methodNameElement)
                        methodName = methodNameElement.Value;
                    if (null != classNameElement)
                        className = classNameElement.Value;

                    
                    int numParameters = 0;
                    var parameterList = declaration.Element(SRC.ParameterList);
                    if(null != parameterList)
                        numParameters = parameterList.Elements(SRC.Parameter).Count();

                    
                    var matches = from method in db.Definitions.OfType<MethodDefinition>()
                                  where method.MethodName == methodName
                                  where (methodName == null ? true : methodName == method.MethodClassName)
                                  where method.NumberOfMethodParameters == numParameters
                                  select method;
                    int numMatches = matches.Count();

                    if (numMatches == 0)
                        declarationsWithNoMatch++;
                    else if (numMatches == 1)
                        declarationsWithSingleMatch++;
                    else if (numMatches > 1)
                        declarationsWithMultipleMatches++;
                }
                Console.WriteLine("{0} has {1} method declarations", xmlFilePath, numDeclarations);
                Console.WriteLine("\t{0} with no match", declarationsWithNoMatch);
                Console.WriteLine("\t{0} with a single match", declarationsWithSingleMatch);
                Console.WriteLine("\t{0} with multiple matches", declarationsWithMultipleMatches);
            }
        }
    }
}