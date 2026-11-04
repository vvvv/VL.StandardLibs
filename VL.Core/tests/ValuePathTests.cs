using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using VL.Lib.Collections;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ValuePathTests
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
            public ImmutableDictionary<string, Item> Lookup { get; init; } = ImmutableDictionary<string, Item>.Empty.Add("say \"hi\"", new Item { Name = "quoted" }).Add("plain", new Item { Name = "p" });
            public List<int> Numbers { get; init; } = [10, 20, 30];
        }

        static IEnumerable<TestCaseData> ValidPaths()
        {
            yield return new TestCaseData("", new ValuePathSegment[0]).SetName("Empty");
            yield return new TestCaseData("A", new[] { ValuePathSegment.ForProperty("A") }).SetName("Property");
            yield return new TestCaseData(".A", new[] { ValuePathSegment.ForProperty("A") }).SetName("Property with leading dot");
            yield return new TestCaseData("My Prop.B", new[] { ValuePathSegment.ForProperty("My Prop"), ValuePathSegment.ForProperty("B") }).SetName("Property with space");
            yield return new TestCaseData("[2]", new[] { ValuePathSegment.ForIndex(2) }).SetName("Index");
            yield return new TestCaseData("Items[2].Position.X", new[] { ValuePathSegment.ForProperty("Items"), ValuePathSegment.ForIndex(2), ValuePathSegment.ForProperty("Position"), ValuePathSegment.ForProperty("X") }).SetName("Mixed");
            yield return new TestCaseData("Lookup[\"a\"].Name", new[] { ValuePathSegment.ForProperty("Lookup"), ValuePathSegment.ForKey("a"), ValuePathSegment.ForProperty("Name") }).SetName("Key");
            yield return new TestCaseData("[\"\"]", new[] { ValuePathSegment.ForKey("") }).SetName("Empty key");
            yield return new TestCaseData("[\"a.b[0]\"]", new[] { ValuePathSegment.ForKey("a.b[0]") }).SetName("Key with separators");
            yield return new TestCaseData("[\"say \"\"hi\"\"\"]", new[] { ValuePathSegment.ForKey("say \"hi\"") }).SetName("Key with quotes");
            yield return new TestCaseData("[1][2]", new[] { ValuePathSegment.ForIndex(1), ValuePathSegment.ForIndex(2) }).SetName("Nested indices");
        }

        [TestCaseSource(nameof(ValidPaths))]
        public void ParsesValidPaths(string text, ValuePathSegment[] expected)
        {
            var path = ValuePath.Parse(text);
            Assert.That(path.IsValid, Is.True);
            Assert.That(path.Segments, Is.EqualTo(expected));
            Assert.That(ValuePath.TryParse(text, out _), Is.True);
        }

        [TestCase("Items[2].Position.X")]
        [TestCase("[3]")]
        [TestCase("Lookup[\"say \"\"hi\"\"\"].Name")]
        [TestCase("[\"a\"][\"b\"]")]
        public void FormatRoundTrips(string text)
        {
            var path = ValuePath.Parse(text);
            Assert.That(path.ToString(), Is.EqualTo(text));
            Assert.That(ValuePath.Parse(path.ToString()).Segments, Is.EqualTo(path.Segments));
        }

        [Test]
        public void FormatDropsLeadingDot()
            => Assert.That(ValuePath.Parse(".A.B[1]").ToString(), Is.EqualTo("A.B[1]"));

        [TestCase("Items[", "Items", "[", 1)]
        [TestCase("Items.", "Items", ".", 1)]
        [TestCase("A..B", "A", "..B", 1)]
        [TestCase("A[x]", "A", "[x]", 1)]
        [TestCase("A[]", "A", "[]", 1)]
        [TestCase("A[\"b]", "A", "[\"b]", 1)]
        [TestCase("A[\"b\"c\"]", "A", "[\"b\"c\"]", 1)]
        [TestCase("A.B[1].", "A.B[1]", ".", 3)]
        public void ReportsWhereItFailed(string text, string parsed, string remainder, int failedAtSegment)
        {
            var path = ValuePath.Parse(text);
            Assert.That(path.IsValid, Is.False);
            Assert.That(path.ToString(), Is.EqualTo(parsed));
            Assert.That(path.Remainder, Is.EqualTo(remainder));
            Assert.That(path.Count, Is.EqualTo(failedAtSegment));
            Assert.That(ValuePath.TryParse(text, out _), Is.False);
        }

        [Test]
        public void AppendToPrefixesProperties()
        {
            Assert.That(ValuePathSegment.ForProperty("A").AppendTo(""), Is.EqualTo(".A"));
            Assert.That(ValuePathSegment.ForIndex(1).AppendTo(".A"), Is.EqualTo(".A[1]"));
            Assert.That(ValuePathSegment.ForKey("k\"").AppendTo(".A"), Is.EqualTo(".A[\"k\"\"\"]"));
        }

        [TestCase("A", true)]
        [TestCase("My Prop", true)]
        [TestCase("", false)]
        [TestCase("A.B", false)]
        [TestCase("A[0]", false)]
        public void PropertyNamesMustSurviveParsing(string name, bool canBeWritten)
            => Assert.That(ValuePathSegment.CanBeWrittenAsProperty(name), Is.EqualTo(canBeWritten));

        [TestCase("Items[1].Name", "b")]
        [TestCase(".Items[0].Name", "a")]
        [TestCase("Items[1].Position.X", null)]
        [TestCase("Lookup[\"plain\"].Name", "p")]
        [TestCase("Lookup[\"say \"\"hi\"\"\"].Name", "quoted")]
        public void WalkersFindValuesByPath(string path, string expectedName)
        {
            var root = new Root();
            if (expectedName is null)
            {
                Assert.That(root.TryGetValueByPath<float>(path, -1f, out var x, out var exists), Is.True);
                Assert.That(x, Is.EqualTo(1f));
                Assert.That(exists, Is.True);
            }
            else
            {
                Assert.That(root.TryGetValueByPath<string>(path, null, out var name, out var exists), Is.True);
                Assert.That(name, Is.EqualTo(expectedName));
                Assert.That(exists, Is.True);
            }
        }

        [Test]
        public void WalkersReadLists()
            => Assert.That(new Root().TryGetValueByPath<int>("Numbers[2]", 0, out var value, out _) && value == 30, Is.True);

        [TestCase("Items[5].Name")]
        [TestCase("Items[x].Name")]
        [TestCase("Lookup[\"missing\"].Name")]
        [TestCase("Nothing")]
        public void WalkersFailForUnknownPaths(string path)
            => Assert.That(new Root().TryGetValueByPath<string>(path, null, out _, out _), Is.False);

        [Test]
        public void ObjectGraphNodesHaveEscapedPaths()
        {
            var node = new Root().TryGetObjectGraphNodeByPath("Lookup[\"say \"\"hi\"\"\"].Name");
            Assert.That(node.HasValue, Is.True);
            Assert.That(node.Value.Value, Is.EqualTo("quoted"));
            Assert.That(node.Value.Path, Is.EqualTo(".Lookup[\"say \"\"hi\"\"\"].Name"));
        }
    }
}
