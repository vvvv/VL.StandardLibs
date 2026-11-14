using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using VL.Lib.Text;

namespace VL.Core.Tests
{
    [TestFixture]
    public class FontFileCatalogTests
    {
        static string[] GetFolders()
        {
            if (OperatingSystem.IsWindows())
                return new[] { Environment.GetFolderPath(Environment.SpecialFolder.Fonts) };
            return FontFileCatalog.GetSystemFontFolders().ToArray();
        }

        static FontFileCatalog CreateCatalog()
        {
            var catalog = new FontFileCatalog(GetFolders());
            if (catalog.Families.Count == 0)
                Assert.Ignore("No fonts found on this machine");
            return catalog;
        }

        [Test]
        public void FindsFamiliesAndFaces()
        {
            var catalog = CreateCatalog();
            var family = OperatingSystem.IsWindows() ? "Arial" : catalog.Families[0];
            Assert.That(catalog.Families.Any(f => string.Equals(f, family, StringComparison.OrdinalIgnoreCase)));

            var regular = catalog.Find(family, FontStyle.Regular);
            Assert.That(regular, Is.Not.Null);
            Assert.That(File.Exists(regular.Value.Path));
            Assert.That(regular.Value.Index, Is.GreaterThanOrEqualTo(0));
            Assert.That(catalog.Find(family.ToUpperInvariant(), FontStyle.Regular), Is.Not.Null);
            Assert.That(catalog.Find("No such font family", FontStyle.Regular), Is.Null);
        }

        [Test]
        public void RegularAndBoldResolveToDifferentFaces()
        {
            var catalog = CreateCatalog();
            var family = OperatingSystem.IsWindows()
                ? "Arial"
                : catalog.Families.FirstOrDefault(f => catalog.Find(f, FontStyle.Bold)?.IsBold == true);
            if (family is null)
                Assert.Ignore("No family with a bold face found on this machine");

            var regular = catalog.Find(family, FontStyle.Regular).Value;
            var bold = catalog.Find(family, FontStyle.Bold).Value;
            Assert.That(regular.IsBold, Is.False);
            Assert.That(bold.IsBold, Is.True);
            // A different file, or the same collection (.ttc: most system fonts of macOS) with another face in it
            Assert.That((bold.Path, bold.Index), Is.Not.EqualTo((regular.Path, regular.Index)), "Regular and bold are the same face of the same file.");
            Assert.That(File.Exists(bold.Path));
        }

        [Test]
        public void FontCollectionYieldsItsFamilies()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Ignore("Needs a .ttc file of a known family");

            var catalog = CreateCatalog();
            var collectionFaces = catalog.Faces
                .Where(f => f.Path.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (collectionFaces.Count == 0)
                Assert.Ignore("No .ttc files found on this machine");
            Assert.That(collectionFaces.Select(f => f.Family).Distinct().Count(), Is.GreaterThan(1));
            Assert.That(collectionFaces.Any(f => f.Index > 0));
        }

        [Test]
        public void TruncatedAndBrokenFilesAreSkipped()
        {
            var catalog = CreateCatalog();
            var source = catalog.Faces[0].Path;
            var bytes = File.ReadAllBytes(source);

            var folder = Path.Combine(Path.GetTempPath(), "VL.Core.Tests.Fonts." + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllBytes(Path.Combine(folder, "truncated.ttf"), bytes.AsSpan(0, Math.Min(bytes.Length, 100)).ToArray());
                File.WriteAllBytes(Path.Combine(folder, "garbage.otf"), Enumerable.Range(0, 500).Select(i => (byte)i).ToArray());
                File.WriteAllBytes(Path.Combine(folder, "empty.ttc"), Array.Empty<byte>());
                File.WriteAllBytes(Path.Combine(folder, "notafont.txt"), bytes);

                var broken = new FontFileCatalog(new[] { folder, Path.Combine(folder, "missing") });
                Assert.That(broken.Families, Is.Empty);

                // A valid file next to the broken ones is still found
                File.Copy(source, Path.Combine(folder, "valid" + Path.GetExtension(source)));
                var mixed = new FontFileCatalog(new[] { folder });
                Assert.That(mixed.Families, Is.Not.Empty);
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
