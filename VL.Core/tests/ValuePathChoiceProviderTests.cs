using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Core.Import;
using VL.Lib.Collections;
using VL.Lib.Reactive;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ValuePathChoiceProviderTests
    {
        record Position(float X, float Y);

        class Item
        {
            public string Name { get; init; } = "";
            public Position Position { get; init; } = new(1, 2);
        }

        class Root
        {
            public Spread<Item> Items { get; init; } = Spread.Create(new Item { Name = "a" }, new Item { Name = "b" });
            public Spread<Item> NoItems { get; init; } = Spread<Item>.Empty;
            public int[] Many { get; init; } = Enumerable.Range(0, 80).ToArray();
        }

        static string[] Values(object input, string text)
            => ValuePathChoiceProvider.GetChoices(null, input, text).Select(c => $"{c.Value}{(c.Continues ? "+" : "")}{(c.TypeOnly ? "?" : "")}").ToArray();

        [Test]
        public void OffersPropertiesAtTheRoot()
            => Assert.That(Values(new Root(), ""), Is.EqualTo(new[] { "Items+", "NoItems+", "Many+" }));

        [Test]
        public void OffersWhatFollowsTheText()
        {
            // The path itself ends the selection, up (a step) goes back
            Assert.That(Values(new Root(), "Items"), Is.EqualTo(new[] { "Items", "+", "Items[0]+", "Items[1]+" }));
            Assert.That(Values(new Root(), "Items[1]"), Is.EqualTo(new[] { "Items[1]", "Items+", "Items[1].Name", "Items[1].Position+" }));
        }

        [Test]
        public void OffersSiblingsAtALeaf()
            => Assert.That(Values(new Root(), "Items[1].Position.X"), Is.EqualTo(new[] { "Items[1].Position", "Items[1]+", "Items[1].Position.X", "Items[1].Position.Y" }));

        [Test]
        public void IgnoresWhatDoesntLeadAnywhere()
        {
            Assert.That(Values(new Root(), "Items[1].Pos"), Is.EqualTo(Values(new Root(), "Items[1]")));
            Assert.That(Values(new Root(), "Items[1]."), Is.EqualTo(Values(new Root(), "Items[1]")));
            Assert.That(Values(new Root(), "!"), Is.EqualTo(Values(new Root(), "")));
        }

        [Test]
        public void MarksChoicesThatComeFromTypes()
            => Assert.That(Values(new Root(), "NoItems"), Is.EqualTo(new[] { "NoItems", "+", "NoItems[0]+?" }));

        [Test]
        public void EntriesCarryTheTypeTheyPointTo()
        {
            var choices = ValuePathChoiceProvider.GetChoices(null, new Root(), "Items[1]");
            Assert.That(choices.Single(c => c.Value == "Items[1].Name").TypeArguments, Is.EqualTo(new[] { typeof(string) }));
            Assert.That(choices.Single(c => c.Value == "Items[1]").TypeArguments, Is.EqualTo(new[] { typeof(Item) }));
            Assert.That(choices.Single(c => c.Label == ValuePathChoiceProvider.UpLabel).TypeArguments, Is.Null);
        }

        [Test]
        public void ExplainsWhenItemsAreLeftOut()
        {
            var choices = ValuePathChoiceProvider.GetChoices(null, new Root(), "Many");
            Assert.That(choices.Count, Is.EqualTo(2 + 50 + 1));
            Assert.That(choices.Last().Value, Is.Null);
            Assert.That(choices.Last().Label, Does.Contain("30"));
        }

        [Test]
        public void StartsAtTheObjectOfAChannel()
        {
            var channel = Channel.Create(new Item());
            Assert.That(Values(channel, ""), Is.EqualTo(new[] { "Name", "Position+" }));
        }

        [ChoiceProviderRequires("Main", "Other")]
        sealed class OtherRootProvider : ValuePathChoiceProvider
        {
            public OtherRootProvider() : base("Main")
            {
            }
        }

        [Test]
        public void SaysWhichInputsItReads()
        {
            Assert.That(ChoiceProviderRequiresAttribute.GetInputPins(typeof(ValuePathChoiceProvider)), Is.EqualTo(new[] { "Input" }));
            Assert.That(ChoiceProviderRequiresAttribute.GetInputPins(typeof(OtherRootProvider)), Is.EqualTo(new[] { "Main", "Other" }));
            Assert.That(ChoiceProviderRequiresAttribute.GetInputPins(typeof(string)), Is.Empty);
        }

        [Test]
        public void HasNothingToOfferWithoutAType()
            => Assert.That(ValuePathChoiceProvider.GetChoices(null, null, ""), Is.Null);
    }
}