using VL.Lib.Collections;

namespace VL.Serialization.Raw.Tests
{
    public class RawTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SpreadOfByte()
        {
            Spread<byte> source = Spread.Create(new byte[2] { 1, 2 });
            var sink = RawSerialization.Deserialize<Spread<byte>>(RawSerialization.Serialize(source));
            Assert.AreEqual(source, sink);
        }

        [Test]
        public void ReadOnlyMemoryOfByte()
        {
            ReadOnlyMemory<byte> source = new byte[2] { 1, 2 };
            var sink = RawSerialization.Deserialize<ReadOnlyMemory<byte>>(RawSerialization.Serialize(source));
            Assert.AreEqual(source, sink);
        }

        [Test]
        public void ReadOnlyMemoryOfFloat()
        {
            ReadOnlyMemory<float> source = new float[2] { 1, 2 };
            var sink = RawSerialization.Deserialize<ReadOnlyMemory<float>>(RawSerialization.Serialize(source));
            Assert.AreEqual(source, sink);
        }

        [Test]
        public void ArraySegmentOfByte()
        {
            ArraySegment<byte> source = new byte[2] { 1, 2 };
            var sink = RawSerialization.Deserialize<ArraySegment<byte>>(RawSerialization.Serialize(source));
            Assert.AreEqual(source, sink);
        }

        [Test]
        public void ArraySegmentOfFloat()
        {
            ArraySegment<float> source = new float[2] { 1, 2 };
            var sink = RawSerialization.Deserialize<ArraySegment<float>>(RawSerialization.Serialize(source));
            Assert.IsTrue(source.SequenceEqual(sink));
        }
    }
}