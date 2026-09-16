using NUnit.Framework;
using VL.Core;
using VL.TestFramework;
using Path = VL.Lib.IO.Path;

namespace VL.Serialization.FSPickler.Tests
{
    [TestFixture]
    public class FSPicklerTests
    {
        private TestAppHost appHost;

        [SetUp]
        public void Setup()
        {
            appHost = new TestAppHost();
            appHost.MakeCurrent().DisposeBy(appHost);
        }

        [TearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void PathBinarySerialization()
        {
            var path = new Path("Foo");
            var content = FSPicklerSerialization.SerializeBinary(path);
            var result = FSPicklerSerialization.DeserializeBinary<Path>(content);
            Assert.AreEqual(path, result);
        }

        [Test]
        public void PathJsonSerialization()
        {
            var path = new Path("Foo");
            var content = FSPicklerSerialization.SerializeJson(path);
            var result = FSPicklerSerialization.DeserializeJson<Path>(content);
            Assert.AreEqual(path, result);
        }
    }
}
