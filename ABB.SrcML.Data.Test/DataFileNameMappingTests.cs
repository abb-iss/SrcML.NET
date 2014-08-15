/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
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
    public class DataFileNameMappingTests {
        public const string TEST_DIRECTORY = "dataMappingTest";
        [TestFixtureSetUp]
        public void FixtureSetup() {
            if(!Directory.Exists(TEST_DIRECTORY)) {
                Directory.CreateDirectory(TEST_DIRECTORY);
            }
        }

        [TestFixtureTearDown]
        public void FixtureTeardown() {
            if(Directory.Exists(TEST_DIRECTORY)) {
                Directory.Delete(TEST_DIRECTORY, true);
            }
        }

        [Test,
         TestCase(true),
         TestCase(false)]
        public void TestDataFileNameMap(bool compressionEnabled) {
            var generator = new DataGenerator();
            var mapping = new DataFileNameMapping(TEST_DIRECTORY, compressionEnabled);
            var sourcePath = @"..\..\TestInputs\function_def.cpp";
            var srcmlPath = @"..\..\TestInputs\function_def.xml";

            var mappedPath = mapping.GetTargetPath(sourcePath);
            
            var actualExtension = Path.GetExtension(mappedPath);
            var expectedExtension = (compressionEnabled ? XmlSerialization.DEFAULT_COMPRESSED_EXTENSION : XmlSerialization.DEFAULT_EXTENSION);
            StringAssert.AreEqualIgnoringCase(expectedExtension, actualExtension);

            generator.Generate(srcmlPath, mappedPath);
            Assert.That(File.Exists(mappedPath), String.Format("Could not generate {0}", mappedPath));

            var data = XmlSerialization.Load(mappedPath, compressionEnabled);
            Assert.IsNotNull(data, String.Format("Could not load data from {0}. It should {1}be compressed", mappedPath, compressionEnabled ? String.Empty : "not "));
        }
    }
}
