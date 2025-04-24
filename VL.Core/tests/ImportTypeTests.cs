using NUnit.Framework;
using VL.Core.Import;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ImportTypeTests
    {
        [Test]
        public void NamespaceIsUsedAsCategory()
        {
            var a = new ImportTypeAttribute(typeof(ImportTypeTests));
            Assert.AreEqual(typeof(ImportTypeTests).Namespace, a.GetCategory(typeof(ImportTypeTests).Namespace));
        }

        [Test]
        public void CategoryIsUsedAsCategory()
        {
            var a = new ImportTypeAttribute(typeof(ImportTypeTests)) { Category = "Foo" };
            Assert.AreEqual("Foo", a.GetCategory(typeof(ImportTypeTests).Namespace));
        }

        [Test]
        public void NamespaceIsStrippedAndUsedAsCategory()
        {
            var a = new ImportTypeAttribute(typeof(ImportTypeTests)) { NamespacePrefixToStrip = "VL.Core" };
            Assert.AreEqual("Tests", a.GetCategory(typeof(ImportTypeTests).Namespace));
        }

        [Test]
        public void NamespaceIsStrippedAndPlacedBelowCategory()
        {
            var a = new ImportTypeAttribute(typeof(ImportTypeTests)) { NamespacePrefixToStrip = "VL.Core", Category = "Foo" };
            Assert.AreEqual("Foo.Tests", a.GetCategory(typeof(ImportTypeTests).Namespace));
        }
    }
}
