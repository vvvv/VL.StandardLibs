using NUnit.Framework;

namespace VL.Core.Tests
{
    [TestFixture]
    internal class ValueByPathTests
    {
        [Test]
        public void RecordsAreCloned()
        {
            var foo = new Foo(1);
            var fooClone = foo.WithValueByPath(nameof(Foo.X), 42, out _);
            Assert.AreEqual(1, foo.X); // Not mutated
            Assert.AreEqual(42, fooClone.X);
            Assert.AreNotEqual(foo, fooClone);
        }

        record Foo(int X);
    }
}
