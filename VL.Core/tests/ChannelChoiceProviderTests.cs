using NUnit.Framework;
using System;
using System.Linq;
using VL.Core.Reactive;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ChannelChoiceProviderTests
    {
        // An order of null leaves the channel without one
        static IChannel<object> Create(string path, int? order = null)
        {
            var channel = new Channel<object>();
            channel.SetPath(path);
            if (order is { } o)
                channel.AttributesChannel.Value = Spread.Create<Attribute>(new EditorAttributes.OrderAttribute(o));
            return channel;
        }

        static string[] Sort(params IChannel<object>[] channels)
            => ChannelChoiceProvider.SortByPath(channels).Select(c => c.Path).ToArray();

        [Test]
        public void ChannelsWithoutAnOrderGoByName()
            => Assert.That(Sort(Create("b"), Create("C"), Create("a")), Is.EqualTo(new[] { "a", "b", "C" }));

        [Test]
        public void OrderComesBeforeName()
            => Assert.That(Sort(Create("a", 2), Create("b"), Create("c", -1)), Is.EqualTo(new[] { "c", "b", "a" }));

        [Test]
        public void ParentComesBeforeItsChildren()
            => Assert.That(Sort(Create("A.Y"), Create("A.X"), Create("A")), Is.EqualTo(new[] { "A", "A.X", "A.Y" }));

        [Test]
        public void OrderOnlyRanksAmongSiblings()
        {
            // Z has the highest order of its siblings, which doesn't move it ahead of or behind the channels of other parents
            var sorted = Sort(Create("B.Z", 5), Create("B.A", 9), Create("B.M", 1), Create("A.Z", 100), Create("A.A", -100));
            Assert.That(sorted, Is.EqualTo(new[] { "A.A", "A.Z", "B.M", "B.Z", "B.A" }));
        }

        [Test]
        public void ParentOrderRanksTheWholeGroup()
            => Assert.That(Sort(Create("A.X"), Create("A", 1), Create("B.X"), Create("B")), Is.EqualTo(new[] { "B", "B.X", "A", "A.X" }));

        [Test]
        public void GroupWithoutAChannelOfItsOwnHasNoOrder()
            => Assert.That(Sort(Create("P.X"), Create("Q", -1), Create("R", 1)), Is.EqualTo(new[] { "Q", "P.X", "R" }));
    }
}
