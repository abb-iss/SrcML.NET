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
        private const string DefaultInputName = "serializationtest_input.xml";
        private const string DefaultOutputName = "serializationtest_output";
        [SetUp]
        public void TestSetup() {
            File.Delete(DefaultInputName);
            File.Delete(DefaultOutputName);
            File.Delete(Path.ChangeExtension(DefaultOutputName, XmlSerialization.DEFAULT_EXTENSION));
            File.Delete(Path.ChangeExtension(DefaultOutputName, XmlSerialization.DEFAULT_COMPRESSED_EXTENSION));
        }

        [Test]
        [TestCase("A.cpp", true)]
        [TestCase("A.cpp", false)]
        [TestCase("A.h", true)]
        [TestCase("A.h", false)]
        public void TestRoundTrip(string sourceFileName, bool compressOutput) {
            var sourceFilePath = Path.Combine(TestInputPath, sourceFileName);

            var srcMLGenerator = new SrcMLGenerator("SrcML");
            var dataGenerator = new DataGenerator();

            Assert.That(srcMLGenerator.Generate(sourceFilePath, DefaultInputName));
            var fileUnit = SrcMLElement.Load(DefaultInputName);
            var nsd = dataGenerator.Parse(fileUnit) as NamespaceDefinition;
            XmlSerialization.WriteElement(nsd, DefaultOutputName, compressOutput);
            var nsdFromFile = XmlSerialization.Load(DefaultOutputName, compressOutput) as NamespaceDefinition;
            DataAssert.StatementsAreEqual(nsd, nsdFromFile);
        }

        [Test]
        [TestCase("A.cpp", true)]
        [TestCase("A.cpp", false)]
        [TestCase("A.h", true)]
        [TestCase("A.h", false)]
        public void TestRoundTripWithDefaultExtension(string sourceFileName, bool useCompression) {
            var sourceFilePath = Path.Combine(TestInputPath, sourceFileName);

            var srcMLGenerator = new SrcMLGenerator("SrcML");
            var dataGenerator = new DataGenerator();

            Assert.That(srcMLGenerator.Generate(sourceFilePath, DefaultInputName));
            var fileUnit = SrcMLElement.Load(DefaultInputName);
            var nsd = dataGenerator.Parse(fileUnit) as NamespaceDefinition;
            
            string outputFileName = Path.ChangeExtension(DefaultOutputName, useCompression ? XmlSerialization.DEFAULT_COMPRESSED_EXTENSION : XmlSerialization.DEFAULT_EXTENSION);
            XmlSerialization.WriteElement(nsd, outputFileName);
            var nsdFromFile = XmlSerialization.Load(outputFileName) as NamespaceDefinition;
            DataAssert.StatementsAreEqual(nsd, nsdFromFile);
        }
    }
}
