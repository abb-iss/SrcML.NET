using NUnit.Framework;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class SourceLocationTests {

        [Test]
        public void TestContains_DifferentLines() {
            var outer = new SourceLocation("Foo.cpp", 3, 5, 10, 60);
            var inner = new SourceLocation("Foo.cpp", 4, 1, 5, 1);
            Assert.IsTrue(outer.Contains(inner));
            Assert.IsFalse(inner.Contains(outer));
        }

        [Test]
        public void TestContains_Overlapping() {
            var outer = new SourceLocation("Foo.cpp", 3, 1, 10, 60);
            var inner = new SourceLocation("Foo.cpp", 4, 1, 11, 1);
            Assert.IsFalse(outer.Contains(inner));
            Assert.IsFalse(inner.Contains(outer));
        }

        [Test]
        public void TestContains_Point() {
            var outer = new SourceLocation("Foo.cpp", 3, 1, 10, 60);
            var inner = new SourceLocation("Foo.cpp", 4, 1);
            Assert.IsTrue(outer.Contains(inner));
            Assert.IsFalse(inner.Contains(outer));
        }

        [Test]
        public void TestContains_SameLine() {
            var outer = new SourceLocation("Foo.cpp", 3, 1, 10, 60);
            var inner = new SourceLocation("Foo.cpp", 3, 3, 3, 5);
            Assert.IsTrue(outer.Contains(inner));
            Assert.IsFalse(inner.Contains(outer));
        }
    }
}