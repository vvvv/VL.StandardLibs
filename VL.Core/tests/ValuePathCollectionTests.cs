using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Core.Tests
{
    /// <summary>
    /// The resolver, which lists what can follow a path, and the nodes that follow a path must agree on what a list is.
    /// </summary>
    [TestFixture]
    public class ValuePathCollectionTests
    {
        class Item
        {
            public string Name { get; init; } = "";
        }

        // Implements the generic interfaces only, like a class that wraps a view model collection would
        sealed class GenericOnlyList(params Item[] items) : IReadOnlyList<Item>, IList<Item>
        {
            readonly List<Item> list = [.. items];

            public Item this[int index] { get => list[index]; set => list[index] = value; }
            public int Count => list.Count;
            public bool IsReadOnly => false;
            public void Add(Item item) => list.Add(item);
            public void Clear() => list.Clear();
            public bool Contains(Item item) => list.Contains(item);
            public void CopyTo(Item[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
            public IEnumerator<Item> GetEnumerator() => list.GetEnumerator();
            public int IndexOf(Item item) => list.IndexOf(item);
            public void Insert(int index, Item item) => list.Insert(index, item);
            public bool Remove(Item item) => list.Remove(item);
            public void RemoveAt(int index) => list.RemoveAt(index);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        sealed class ReadOnlyOnlyList(params Item[] items) : IReadOnlyList<Item>
        {
            public Item this[int index] => items[index];
            public int Count => items.Length;
            public IEnumerator<Item> GetEnumerator() => ((IEnumerable<Item>)items).GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        class Root
        {
            static Item[] Two => [new Item { Name = "a" }, new Item { Name = "b" }];

            public Spread<Item> AsSpread { get; init; } = Spread.Create(Two);
            public Item[] Array { get; init; } = Two;
            public List<Item> List { get; init; } = [.. Two];
            public ImmutableList<Item> ImmutableList { get; init; } = [.. Two];
            public IReadOnlyList<Item> ReadOnlyInterface { get; init; } = Two;
            public GenericOnlyList GenericOnly { get; init; } = new(Two);
            public ReadOnlyOnlyList ReadOnlyOnly { get; init; } = new(Two);
            public GenericOnlyList EmptyGenericOnly { get; init; } = new();
        }

        static readonly string[] Collections = [nameof(Root.AsSpread), nameof(Root.Array), nameof(Root.List), nameof(Root.ImmutableList), nameof(Root.ReadOnlyInterface), nameof(Root.GenericOnly), nameof(Root.ReadOnlyOnly)];

        [TestCaseSource(nameof(Collections))]
        public void ResolverListsTheItemsAndFollowsIntoThem(string property)
        {
            var root = new Root();

            var items = ValuePathResolver.GetChildren(root, null, ValuePath.Parse(property), null);
            Assert.That(items.Children.Select(c => c.Segment.ToString()), Is.EqualTo(new[] { "[0]", "[1]" }));
            Assert.That(items.Children.All(c => c.HasChildren && !c.IsTypeOnly && c.Type == typeof(Item)), Is.True);

            // Items are live, so the second step knows the value and not just the type
            var item = ValuePathResolver.GetChildren(root, null, ValuePath.Parse($"{property}[1]"), null);
            Assert.That(item.ResolvedSegmentCount, Is.EqualTo(2));
            Assert.That(item.Children.Select(c => c.Segment.ToString()), Is.EqualTo(new[] { nameof(Item.Name) }));
            Assert.That(item.Children.Single().IsTypeOnly, Is.False);
        }

        [TestCaseSource(nameof(Collections))]
        public void NodesFollowWhatTheResolverLists(string property)
        {
            var root = new Root();

            // Everything the resolver offers can be read by the nodes. The names prove it's the live item that was reached.
            Assert.That(root.TryGetValueByPath<string>($"{property}[1].Name", null, out var name, out var exists), Is.True);
            Assert.That(name, Is.EqualTo("b"));
            Assert.That(exists, Is.True);
            Assert.That(root.TryGetValueByPath<Item>($"{property}[0]", null, out var item, out _), Is.True);
            Assert.That(item.Name, Is.EqualTo("a"));
            Assert.That(root.TryGetValueByPath<Item>($"{property}[2]", null, out _, out _), Is.False);

            var node = root.TryGetObjectGraphNodeByPath($"{property}[1].Name");
            Assert.That(node.HasValue && "b".Equals(node.Value.Value), Is.True);
        }

        [Test]
        public void EmptyListsContinueOnTheType()
        {
            var children = ValuePathResolver.GetChildren(new Root(), null, ValuePath.Parse(nameof(Root.EmptyGenericOnly)), null);
            Assert.That(children.Children.Select(c => c.Segment.ToString()), Is.EqualTo(new[] { "[0]" }));
            Assert.That(children.Children.Single().IsTypeOnly, Is.True);
        }

        [Test]
        public void ChoicesOfTheProviderListTheItems()
        {
            var choices = ValuePathChoiceProvider.GetChoices(null, new Root(), nameof(Root.GenericOnly));
            Assert.That(choices.Where(c => c.Continues && c.Label != ValuePathChoiceProvider.UpLabel).Select(c => c.Value), Is.EqualTo(new[] { "GenericOnly[0]", "GenericOnly[1]" }));
        }

        [Test]
        public void NoExceptionsAreThrownInternally()
        {
            var thrown = new List<string>();
            void Handler(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e) => thrown.Add(e.Exception.ToString());
            var root = new Root();
            System.AppDomain.CurrentDomain.FirstChanceException += Handler;
            try
            {
                foreach (var property in Collections.Append(nameof(Root.EmptyGenericOnly)))
                {
                    ValuePathResolver.GetChildren(root, null, ValuePath.Parse($"{property}[1].Name"), null);
                    root.TryGetValueByPath<string>($"{property}[1].Name", null, out _, out _);
                    root.TryGetObjectGraphNodeByPath($"{property}[1].Name");
                    ValuePathChoiceProvider.GetChoices(null, root, property);
                }
            }
            finally
            {
                System.AppDomain.CurrentDomain.FirstChanceException -= Handler;
            }
            Assert.That(thrown, Is.Empty);
        }    }
}