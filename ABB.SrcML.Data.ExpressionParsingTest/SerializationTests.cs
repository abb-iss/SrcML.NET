/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    [TestFixture(Category="Build")]
    public class SerializationTests {
        private const string TestInputPath = @"..\..\TestInputs";

        [Test, TestCase("A.cpp"), TestCase("A.h")]
        public void TestRoundTrip(string sourceFileName) {
            var sourceFilePath = Path.Combine(TestInputPath, sourceFileName);

            var srcMLGenerator = new SrcMLGenerator("SrcML");
            var dataGenerator = new DataGenerator();

            Assert.That(srcMLGenerator.Generate(sourceFilePath, "test.xml"));
            var fileUnit = SrcMLElement.Load("test.xml");
            var nsd = dataGenerator.Parse(fileUnit) as NamespaceDefinition;
            XmlSerialization.WriteElement(nsd, "test_data.xml");
            var nsdFromFile = XmlSerialization.Load("test_data.xml") as NamespaceDefinition;
            DataAssert.StatementsAreEqual(nsd, nsdFromFile);
        }
    }
}
