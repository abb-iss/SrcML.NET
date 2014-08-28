/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - implementation and documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    class DataGeneratorTests {
        public const string SOURCE_DIRECTORY = "datageneratortests";
        public const string OUTPUT_DIRECTORY = "datagenerator_output";

        [TestFixtureSetUp]
        public static void TestClassSetup() {
            TestClassCleanup();
            Directory.CreateDirectory(SOURCE_DIRECTORY);
            Directory.CreateDirectory(OUTPUT_DIRECTORY);
        }

        [TestFixtureTearDown]
        public static void TestClassCleanup() {
            if(Directory.Exists(SOURCE_DIRECTORY)) {
                Directory.Delete(SOURCE_DIRECTORY, true);
            }
            if(Directory.Exists(OUTPUT_DIRECTORY)) {
                Directory.Delete(OUTPUT_DIRECTORY, true);
            }
        }

        [TearDown]
        public void TestSetup() {
            foreach(var fileName in Directory.GetFiles(SOURCE_DIRECTORY).Concat(Directory.GetFiles(OUTPUT_DIRECTORY))) {
                File.Delete(fileName);
            }
        }

        [Test]
        public void TestBadEncoding() {
            string testCode = @"void Foo()";
            var fileName = @"BadPath™.cpp";
            var sourceFilePath = Path.Combine(SOURCE_DIRECTORY, fileName);
            var xmlFilePath = Path.Combine(OUTPUT_DIRECTORY, Path.ChangeExtension(fileName, "xml"));
            var dataFilePath = Path.ChangeExtension(xmlFilePath, XmlSerialization.DEFAULT_EXTENSION);

            File.WriteAllText(sourceFilePath, testCode);
            SrcMLGenerator generator = new SrcMLGenerator("SrcML");

            generator.GenerateSrcMLFromFile(sourceFilePath, xmlFilePath, Language.C);

            var dataGenerator = new DataGenerator();
            dataGenerator.Generate(xmlFilePath, dataFilePath);
        }

        [Test]
        public void TestMissingSrcMLFile() {
            var fileName = "missing.cpp";
            var xmlFilePath = Path.Combine(OUTPUT_DIRECTORY, Path.ChangeExtension(fileName, "xml"));
            var dataFilePath = Path.ChangeExtension(xmlFilePath, XmlSerialization.DEFAULT_EXTENSION);

            var dataGenerator = new DataGenerator();
            dataGenerator.Generate(xmlFilePath, dataFilePath);
        }
    }
}
