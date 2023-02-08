using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;

namespace VL.Core.Tests.Collections
{
    [TestFixture]
    public class SpreadTests
    {
        [Test]
        public static void SetItemOfSameValueDoesNotChangeSpread()
        {
            var spread = Spread<int>.Empty.Add(1);
            var sameSpread = spread.SetItem(0, 1);
            Assert.AreEqual(spread, sameSpread);
        }

        [Test]
        public static void SetSliceOfSameValueDoesNotChangeSpread()
        {
            var spread = Spread<int>.Empty.Add(1);
            var sameSpread = spread.SetSlice(1, 0);
            Assert.AreEqual(spread, sameSpread);
        }

        [Test]
        public static void SpreadBuilderToReadOnlyMemory()
        {
            var builder = new SpreadBuilder<int>();
            builder.Add(1);
            Assert.IsTrue(builder.TryGetMemory(out ReadOnlyMemory<int> memory) && !memory.IsEmpty);
        }

        [Test]
        public static void SpreadBuilderToMemory()
        {
            var builder = new SpreadBuilder<int>();
            builder.Add(1);
            Assert.IsTrue(builder.TryGetMemory(out Memory<int> memory) && !memory.IsEmpty);
        }

        [Test]
        public static void SpreadToReadOnlyMemory()
        {
            var spread = Spread<int>.Empty.Add(1);
            Assert.IsTrue(spread.TryGetMemory(out ReadOnlyMemory<int> memory) && !memory.IsEmpty);
        }

        [Test]
        public static void Spread_NOT_ToMemory()
        {
            var spread = Spread<int>.Empty.Add(1);
            Assert.IsFalse(spread.TryGetMemory(out Memory<int> memory));
        }
    }
}
