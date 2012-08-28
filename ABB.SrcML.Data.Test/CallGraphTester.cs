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
using System.Diagnostics;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test
{
    public class CallGraphTester
    {
        public static void TestCallGraph(string xmlArchive, List<Tuple<string,string,bool>> testData)
        {
            CallGraph callGraph;
            var dbName = SrcMLDataContext.MakeDBName(xmlArchive);
            SrcMLDataContext.DropUserInstanceDatabase(dbName);

            var sw = Stopwatch.StartNew();
            
            using (var db = SrcMLDataContext.CreateDatabaseConnection(dbName))
            {
                callGraph = new CallGraph(xmlArchive, db);
            }
            sw.Stop();
            Assert.IsNotNull(callGraph);
            Console.WriteLine("{0} to build the call graph", sw.Elapsed);

            List<bool> statuses = new List<bool>();
            foreach (var test in testData)
            {
                var caller = test.Item1;
                var callee = test.Item2;
                var expected = test.Item3;
                var should = test.Item3 ? "should" : "shouldn't";

                Console.Write("{0} {1} call {2}: ", caller, should, callee);
                var status = (expected == callGraph.ContainsRelationship(caller, callee));
                statuses.Add(status);
                Console.WriteLine(status ? "PASS" : "FAIL");

                Console.Write("{0} {1} be in the caller list for {2}: ", callee, should, callee);
                status = (expected == callGraph.GetCallers(callee).Any(c => (c.CallerDefinition as MethodDefinition).MethodSignature == caller));
                statuses.Add(status);
                Console.WriteLine(status ? "PASS" : "FAIL");

                Console.Write("{0} {1} be in the callee list for {2}: ", callee, should, caller);
                status = (expected == callGraph.GetCallees(caller).Any(c => (c.CalleeDefinition as MethodDefinition).MethodSignature == callee));
                statuses.Add(status);
                Console.WriteLine(status ? "PASS" : "FAIL");
                Console.WriteLine();
            }
            Assert.IsTrue(statuses.All(s => s));
        }
    }
}
