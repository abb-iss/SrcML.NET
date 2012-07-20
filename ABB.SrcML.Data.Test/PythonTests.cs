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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ABB.SrcML.Data.Test
{
    [TestClass]
    public class PythonTests
    {
        public static string PythonXmlPath = @"..\..\..\ABB.SrcML.Data.Test\XmlInput\python-2.6.5.xml";

        [ClassInitialize()]
        public static void Init(TestContext context)
        {
            DbHelper.AddArchiveToDb(PythonXmlPath);
        }

        [TestMethod]
        public void PrintPythonStatsTest()
        {
            DbHelper.GetStatsFromDb(PythonXmlPath);
        }

        [TestMethod]
        public void PythonTypeTest()
        {
            List<string> testTypeNames = new List<string>() { "Noddy", "asdl_seq", "PyGetSetDef" };
            TypeTester.TestTypeUse(PythonXmlPath, testTypeNames);
        }

        [TestMethod]
        public void PythonLocalDeclarationTest()
        {
            List<string> useNames = new List<string>() { "tok", "fp", "len" };
            DeclarationTester.TestLocalVariables(PythonXmlPath, useNames);
        }

        [TestMethod]
        public void PythonGlobalDeclarationTest()
        {
            List<string> useNames = new List<string>() { "PyFPE_jbuf" };
            DeclarationTester.TestGlobalVariables(PythonXmlPath, useNames);
        }

        [TestMethod]
        public void PythonCallTest()
        {
            List<string> testCalls = new List<string>() { "PyErr_SetString", "PyObject_GetAttrString", "PyString_FromString" };
            MethodCallTester.TestMethodCalls(PythonXmlPath, testCalls);
        }

        [TestMethod]
        public void PythonCallGraphTest()
        {
            var testData = new List<Tuple<string, string, bool>>();

            CallGraphTester.TestCallGraph(PythonXmlPath, testData);
        }
    }
}
