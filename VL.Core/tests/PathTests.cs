using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.IO;

namespace VL.Core.Tests
{
    [TestFixture]
    public class PathTests
    {
        [Test]
        public static void MakePathRelativeTest()
        {
            var basePath = @"C:\Some\Folder\";
            TestRelativeToAbsoluteRoundTrip(basePath, @"C:\Some\Folder\");
            TestRelativeToAbsoluteRoundTrip(basePath, @"C:\Some\Folder\Sub\File.txt");
            TestRelativeToAbsoluteRoundTrip(basePath, @"C:\Some\Folder\Sub\");
            TestRelativeToAbsoluteRoundTrip(basePath, @"C:\Some\");
        }

        static void TestRelativeToAbsoluteRoundTrip(string from, string to)
        {
            TestRelativeToAbsoluteRoundTrip(new Path(from), new Path(to));
        }

        static void TestRelativeToAbsoluteRoundTrip(Path from, Path to)
        {
            var relative = to.MakeRelative(from);
            var absolute = relative.MakeAbsolute(from);
            Assert.AreEqual(to, absolute);
        }
    }
}
