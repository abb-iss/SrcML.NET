using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    [TestFixture]
    [Category("Build")]
    public class SerializationTests {
        [SetUp]
        public void Setup() {
            if(Directory.Exists("DataArchiveTests")) {
                Directory.Delete("DataArchiveTests", true);
            }
        }
        
        [Test]
        public void TestRoundtrip() {
            var archive = new SrcMLArchive("DataArchiveTests");
            archive.AddOrUpdateFile(@"..\..\TestInputs\A.h");
            archive.AddOrUpdateFile(@"..\..\TestInputs\A.cpp");
            var data = new DataArchive(archive);
            data.Save(@"DataArchiveTests\saved.dar");

            var newData = new DataArchive(archive, @"DataArchiveTests\saved.dar");

            Assert.IsTrue(TestHelper.ScopesAreEqual(data.GlobalScope, newData.GlobalScope));
        }

        [Test]
        [Category("LongRunning")]
        public void TestRoundtrip_Self() {
            var archive = new SrcMLArchive("DataArchiveTests");
            foreach(var csFile in Directory.GetFiles(@"..\..\ABB.SrcML", "*.cs", SearchOption.AllDirectories)) {
                archive.AddOrUpdateFile(csFile);
            }
            var data = new DataArchive(archive);
            data.Save(@"DataArchiveTests\saved.dar");

            var newData = new DataArchive(archive, @"DataArchiveTests\saved.dar");

            Assert.IsTrue(TestHelper.ScopesAreEqual(data.GlobalScope, newData.GlobalScope));
        }

        [Test]
        [ExpectedException("System.Runtime.Serialization.SerializationException")]
        public void TestLoad_BadFile() {
            var archive = new SrcMLArchive("DataArchiveTests");
            var data = new DataArchive(archive, @"..\..\TestInputs\bad_file.dar");
        }
    }
}
