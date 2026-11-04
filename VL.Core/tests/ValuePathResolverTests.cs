using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ValuePathResolverTests
    {
        record Position(float X, float Y);

        class Item
        {
            public string Name { get; init; } = "";
            public Position Position { get; init; } = new(1, 2);
            public Item Next { get; init; }
        }

        class Root
        {
            public Spread<Item> Items { get; init; } = Spread.Create(new Item { Name = "a" }, new Item { Name = "b" });
            public Spread<Item> NoItems { get; init; } = Spread<Item>.Empty;
            public ImmutableDictionary<string, Item> Lookup { get; init; } = ImmutableDictionary<string, Item>.Empty.Add("a", new Item()).Add("say \"hi\"", new Item());
            public List<int> Numbers { get; init; } = [10, 20, 30];
            public Item Missing { get; init; }
            public int this[int i] => i;
            public int Count => 3;
        }

        static ValuePathChildren Get(object root, string path, Type rootType = null, int maxItems = ValuePathResolver.DefaultMaxItems)
            => ValuePathResolver.GetChildren(root, rootType, ValuePath.Parse(path), null, maxItems);

        static string[] Segments(ValuePathChildren result)
            => result.Children.Select(c => c.Segment.ToString() + (c.HasChildren ? "+" : "") + (c.IsTypeOnly ? "?" : "")).ToArray();

        [Test]
        public void ListsPropertiesOfTheRoot()
        {
            var result = Get(new Root(), "");
            Assert.That(result.ResolvedSegmentCount, Is.EqualTo(0));
            Assert.That(result.Type, Is.EqualTo(typeof(Root)));
            // No indexer, but the Count is there
            Assert.That(Segments(result), Is.EqualTo(new[] { "Items+", "NoItems+", "Lookup+", "Numbers+", "Missing+", "Count" }));
        }

        [Test]
        public void ListsItemsOfSpreadsAndListsWithTheirType()
        {
            var items = Get(new Root(), "Items");
            Assert.That(Segments(items), Is.EqualTo(new[] { "[0]+", "[1]+" }));
            Assert.That(items.Children[0].Type, Is.EqualTo(typeof(Item)));
            Assert.That(Segments(Get(new Root(), "Numbers")), Is.EqualTo(new[] { "[0]", "[1]", "[2]" }));
            Assert.That(Get(new Root(), "Numbers").Children[0].Type, Is.EqualTo(typeof(int)));
        }

        [Test]
        public void CapsItems()
        {
            var result = Get(new { Many = Enumerable.Range(0, 120).ToArray() }, "Many", maxItems: 50);
            Assert.That(result.Children.Count, Is.EqualTo(50));
            Assert.That(result.OmittedCount, Is.EqualTo(70));
            Assert.That(Get(new Root(), "Numbers").OmittedCount, Is.EqualTo(0));
        }

        [Test]
        public void ListsKeysOfLiveDictionariesAndFollowsEscapedKeys()
        {
            Assert.That(Segments(Get(new Root(), "Lookup")), Is.EquivalentTo(new[] { "[\"a\"]+", "[\"say \"\"hi\"\"\"]+" }));
            var result = Get(new Root(), "Lookup[\"say \"\"hi\"\"\"]");
            Assert.That(result.ResolvedSegmentCount, Is.EqualTo(2));
            Assert.That(result.Type, Is.EqualTo(typeof(Item)));
        }

        [Test]
        public void FollowsPropertiesAndStopsAtLeaves()
        {
            var result = Get(new Root(), "Items[1].Position");
            Assert.That(result.ResolvedSegmentCount, Is.EqualTo(3));
            Assert.That(Segments(result), Is.EqualTo(new[] { "X", "Y" }));
            Assert.That(result.Children.All(c => c.Type == typeof(float) && !c.IsTypeOnly), Is.True);

            var leaf = Get(new Root(), "Items[1].Name");
            Assert.That(leaf.ResolvedSegmentCount, Is.EqualTo(3));
            Assert.That(leaf.Type, Is.EqualTo(typeof(string)));
            Assert.That(leaf.Children, Is.Empty);
        }

        [Test]
        public void ContinuesWithTypesWhereTheValueEnds()
        {
            // Null property
            var result = Get(new Root(), "Missing");
            Assert.That(Segments(result), Is.EqualTo(new[] { "Name?", "Position+?", "Next+?" }));

            // Empty spread
            var empty = Get(new Root(), "NoItems");
            Assert.That(Segments(empty), Is.EqualTo(new[] { "[0]+?" }));
            Assert.That(Get(new Root(), "NoItems[0]").Type, Is.EqualTo(typeof(Item)));
            Assert.That(Segments(Get(new Root(), "NoItems[0].Position")), Is.EqualTo(new[] { "X?", "Y?" }));

            // Index beyond the end
            Assert.That(Get(new Root(), "Items[7].Position").ResolvedSegmentCount, Is.EqualTo(3));

            // Recursion needs no value
            Assert.That(Get(new Root(), "Missing.Next.Next.Next.Name").ResolvedSegmentCount, Is.EqualTo(5));
        }

        [Test]
        public void ContinuesWithTheRootTypeIfThereIsNoValue()
        {
            var result = Get(null, "Items[3].Name", typeof(Root));
            Assert.That(result.ResolvedSegmentCount, Is.EqualTo(3));
            Assert.That(Segments(Get(null, "", typeof(Root))), Does.Contain("Items+?"));
            // Dictionary keys need a value
            Assert.That(Get(null, "Lookup", typeof(Root)).Children, Is.Empty);
            Assert.That(Get(null, "Lookup[\"x\"]", typeof(Root)).Type, Is.EqualTo(typeof(Item)));
        }

        [Test]
        public void ReportsHowFarThePathGot()
        {
            var result = Get(new Root(), "Items[0].Nope.Name");
            Assert.That(result.ResolvedSegmentCount, Is.EqualTo(2));
            Assert.That(result.Type, Is.EqualTo(typeof(Item)));
            Assert.That(Segments(result), Is.EqualTo(new[] { "Name", "Position+", "Next+" }));

            Assert.That(Get(new Root(), "Items.Name").ResolvedSegmentCount, Is.EqualTo(1));
            Assert.That(Get(new Root(), "Name[0]").ResolvedSegmentCount, Is.EqualTo(0));
            Assert.That(Get(null, "A", null).Children, Is.Empty);
        }

        [Test]
        public void FrameworkTypesAreLeaves()
        {
            Assert.That(Get("text", "").Children, Is.Empty);
            Assert.That(Get(new DateTime(2000, 1, 1), "").Children, Is.Empty);
        }
    }
}