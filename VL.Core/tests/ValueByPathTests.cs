using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using VL.Lib.Collections;

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

        [Test]
        public void SetArrayItem()
        {
            var array = new[] { 1, 2, 3 };
            bool pathExists;
            var mutated = array.WithValueByPath("[1]", 4, out _);
            Assert.AreEqual(4, mutated[1]);
            Assert.AreEqual(array, mutated); 
            Assert.AreNotEqual(2, array[1]);
        }

        [Test]
        public void SetImmutableListItem()
        {
            var list = ImmutableList.Create(1, 2, 3);
            var changed = list.WithValueByPath("[1]", 4, out _);
            Assert.AreEqual(4, changed[1]);
            Assert.AreNotEqual(list, changed);
            Assert.AreEqual(2, list[1]);
        }

        [Test]
        public void ListSetIndexBoundsChecked()
        {
            var list = new List<int>() { 1, 2, 3 };
            bool pathExists;
            Assert.DoesNotThrow(() => { 
                var indexOverflow = list.WithValueByPath("[3]", 4, out pathExists);
                Assert.AreEqual(list, indexOverflow);
                Assert.IsFalse(pathExists);

            });
            Assert.DoesNotThrow(() => { 
                var indexUnderflow = list.WithValueByPath("[-3]", 4, out pathExists);
                Assert.AreEqual(list, indexUnderflow);
                Assert.IsFalse(pathExists);
            });
        }

        [Test]
        public void IListGetItem()
        {
            var list = new List<int>() { 1, 2, 3 };
            int listValue;

            Assert.IsTrue(
                list.TryGetValueByPath("[1]", 0, out listValue, out _)
            );
            Assert.AreEqual(2, listValue);

            var array = new[] { 1, 2, 3 };
            int arrayValue;
            Assert.IsTrue(
                array.TryGetValueByPath("[1]", 0, out arrayValue, out _)
            );
            Assert.AreEqual(2, arrayValue);
        }

        [Test]
        public void ListGetIndexBoundsChecked()
        {
            var list = new List<int>() { 1, 2, 3 };
            int value = -1;
            int defaultValue = 0;
            bool pathExists;
            Assert.DoesNotThrow(() => {
                var lookupSucceeded = list.TryGetValueByPath("[3]", defaultValue, out value, out pathExists);
                Assert.IsFalse(lookupSucceeded);
                Assert.AreEqual(defaultValue, value);
                Assert.IsFalse(pathExists);

            });
            Assert.DoesNotThrow(() => {
                var lookupSucceeded = list.TryGetValueByPath("[-3]", defaultValue, out value, out pathExists);
                Assert.IsFalse(lookupSucceeded);
                Assert.AreEqual(defaultValue, value);
                Assert.IsFalse(pathExists);
            });
        }

        [Test]
        public void SpreadGetIndexBoundsChecked()
        {
            var list = Spread.Create(1, 2, 3);
            int value = -1;
            int defaultValue = 0;
            bool pathExists;
            Assert.DoesNotThrow(() => {
                var lookupSucceeded = list.TryGetValueByPath("[3]", defaultValue, out value, out pathExists);
                Assert.IsFalse(lookupSucceeded);
                Assert.AreEqual(defaultValue, value);
                Assert.IsFalse(pathExists);

            });
            Assert.DoesNotThrow(() => {
                var lookupSucceeded = list.TryGetValueByPath("[-3]", defaultValue, out value, out pathExists);
                Assert.IsFalse(lookupSucceeded);
                Assert.AreEqual(defaultValue, value);
                Assert.IsFalse(pathExists);
            });
        }

        [Test]
        public void SpreadSetIndexBoundsChecked()
        {
            var list = Spread.Create(1, 2, 3);
            bool pathExists;
            Assert.DoesNotThrow(() => {
                var indexOverflow = list.WithValueByPath("[3]", 4, out pathExists);
                Assert.AreEqual(list, indexOverflow);
                Assert.IsFalse(pathExists);

            });
            Assert.DoesNotThrow(() => {
                var indexUnderflow = list.WithValueByPath("[-3]", 4, out pathExists);
                Assert.AreEqual(list, indexUnderflow);
                Assert.IsFalse(pathExists);
            });
        }
    }
}
