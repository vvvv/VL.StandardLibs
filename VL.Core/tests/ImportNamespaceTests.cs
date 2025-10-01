using NUnit.Framework;
using VL.Core.Import;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ImportNamespaceTests
    {
        [Test]
        public void NamespaceIsUsedAsCategory()
        {
            var a = new ImportNamespaceAttribute("Foo");
            Assert.AreEqual("", a.GetCategory("Foo"));
            Assert.AreEqual("Bar", a.GetCategory("Foo.Bar"));
        }

        [Test]
        public void EverythingIsPlacedBelowCategory()
        {
            var a = new ImportNamespaceAttribute("") { Category = "Dst" };
            Assert.AreEqual("Dst", a.GetCategory(""));
            Assert.AreEqual("Dst.Foo", a.GetCategory("Foo"));
            Assert.AreEqual("Dst.Foo.Bar", a.GetCategory("Foo.Bar"));
        }

        [Test]
        public void NamespaceIsPlacedBelowCategory()
        {
            var a = new ImportNamespaceAttribute("Src") { Category = "Dst"};
            Assert.AreEqual("Dst", a.GetCategory("Src"));
            Assert.AreEqual("Dst.Bar", a.GetCategory("Src.Bar"));
        }

        [Test]
        public void NamespaceIsStrippedFromCategory()
        {
            var a = new ImportNamespaceAttribute("VL");
            Assert.AreEqual("", a.GetCategory("VL"));
            Assert.AreEqual("Video", a.GetCategory("VL.Video"));
            Assert.AreEqual("Video.Src", a.GetCategory("VL.Video.Src"));
        }

        [Test]
        public void CategoryIsNullForSymbolsNotInNamespace()
        {
            var a = new ImportNamespaceAttribute("VL");
            Assert.AreEqual(null, a.GetCategory(null));
            Assert.AreEqual(null, a.GetCategory(""));
            Assert.AreEqual(null, a.GetCategory("Foo"));
        }
    }
}
