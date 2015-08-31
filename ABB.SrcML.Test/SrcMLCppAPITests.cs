using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ABB.SrcML;
namespace ABB.SrcML.Test {
    [TestFixture]
    [Category("Build")]
    class SrcMLCppAPITests {
        [Test]
        public void DumbTest() {
            List<String> l = new List<string>();
            l.Add("input.cpp");
            l.Add("input2.cpp");
            SrcMLCppAPI.SrcmlCreateArchiveFromFilename(l.ToArray(), l.Count(), "output.cpp.xml");
        }
    }
}
