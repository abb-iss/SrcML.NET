/******************************************************************************
 * Copyright (c) 2011 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test
{
    [TestFixture]
    [Category("Data")]
    public class SrcMLDataContextTests
    {
        [Test]
        public void TestGetDeclarationForVariableName()
        {
            SrcMLFile src = new SrcMLFile(@"..\..\TestInputs\foo.xml");
            SrcMLDataContext srcDb = SrcMLDataContext.CreateDatabaseConnection("SrcMLDataContextTests", true);
            srcDb.Load(src.FileName);

            var file = src.FileUnits.First();
            var varNames = (from name in file.Descendants(SRC.Name)
                            where name.Parent.Name == SRC.Expression
                            select name).ToList();

            Assert.AreEqual(3, varNames.Count, "Wrong number of variable usages found");
            var varDecl = srcDb.GetDeclarationForVariableName(varNames[0]);
            Assert.AreEqual(1, varDecl.LineNumber);

            varDecl = srcDb.GetDeclarationForVariableName(varNames[1]);
            Assert.AreEqual(5, varDecl.LineNumber);

            varDecl = srcDb.GetDeclarationForVariableName(varNames[2]);
            Assert.AreEqual(5, varDecl.LineNumber);
        }

        [Test]
        public void TestGetDeclarationForVariableName_NestedScopes()
        {
            SrcMLFile src = new SrcMLFile(@"..\..\TestInputs\nested_scopes.xml");
            SrcMLDataContext srcDb = SrcMLDataContext.CreateDatabaseConnection("SrcMLDataContextTests", true);
            srcDb.Load(src.FileName);

            var file = src.FileUnits.First();
            var varNames = (from name in file.Descendants(SRC.Name)
                            where name.Parent.Name == SRC.Expression
                            select name).ToList();

            int[] declLineNumbers = new int[] { 1, 6, 5, 6, 9, 4 };

            Assert.AreEqual(declLineNumbers.Length, varNames.Count, "Wrong number of variable usages found");

            for(int i = 0; i < varNames.Count; i++)
            {
                var decl = srcDb.GetDeclarationForVariableName(varNames[i]);
                Assert.IsNotNull(decl, string.Format("No declaration found for {0}", varNames[i]));
                Assert.AreEqual(declLineNumbers[i], decl.LineNumber);
            }
        }

        [Test]
        public void TestGetDefinitionForMethodCall_function()
        {
            SrcMLFile src = new SrcMLFile(@"..\..\TestInputs\function_def.xml");
            SrcMLDataContext srcDb = SrcMLDataContext.CreateDatabaseConnection("SrcMLDataContextTests", true);
            srcDb.Load(src.FileName);

            var file = src.FileUnits.First();
            var calls = file.Descendants(SRC.Call).ToList();
            Assert.AreEqual(1, calls.Count);

            MethodDefinition md = srcDb.GetDefinitionForMethodCall(calls[0]);
            Console.WriteLine("Method call: {0}", calls[0].Value);
            Console.WriteLine("Matched defintion on line {0}: {1}", md.LineNumber, md.MethodSignature);
            Assert.AreEqual(3, md.LineNumber);
        }

        [Test]
        public void TestGetDefinitionForMethodCall_methods()
        {
            SrcMLFile src = new SrcMLFile(@"..\..\TestInputs\method_def.xml");
            SrcMLDataContext srcDb = SrcMLDataContext.CreateDatabaseConnection("SrcMLDataContextTests", true);
            srcDb.Load(src.FileName);

            var file = src.FileUnits.First();
            var calls = file.Descendants(SRC.Call).ToList();
            Assert.AreEqual(4, calls.Count);

            int[] declLineNumbers = new int[] { 10, 21, 17, 21 };
            for(int i = 0; i < declLineNumbers.Length; i++)
            {
                MethodDefinition md = srcDb.GetDefinitionForMethodCall(calls[i]);
                Console.WriteLine("Method call on line {0}: {1}", calls[i].GetSrcLineNumber(), calls[i].Value);
                Console.WriteLine("Matched defintion on line {0}: {1}", md.LineNumber, md.MethodSignature);
                Assert.AreEqual(declLineNumbers[i], md.LineNumber);
            }
        }
    }
}
