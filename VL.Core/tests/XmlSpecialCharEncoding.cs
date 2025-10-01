using NUnit.Framework;
using static VL.Core.Serialization;

namespace VL.Core.Tests
{
    [TestFixture]
    internal class XmlSpecialCharEncoding
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("▫")]
        [TestCase("\0")]
        [TestCase("A string with\0 some \x1234 illegal characters")]
        [TestCase("\xFEFF")]
        [TestCase("\xFEFF﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿\xFEFF\xFEFF")]
        [TestCase("\xFEFF﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿\0")]
        [TestCase("BOM in \xFEFF﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿\xFEFF between")]
        public static void RoundTrip(string s)
        {
            Assert.AreEqual(s, Decode(Encode(s)));
        }

        [Test]
        public static void RoundTrip0000ToFFFF()
        {
            for (int i = 0; i < char.MaxValue; i++)
            {
                var c = (char)i;
                var s = c.ToString();
                Assert.AreEqual(s, Decode(Encode(s)));
            }
        }

        [Test]
        public static void RoundTrip0000ToFFFFDouble()
        {
            for (int i = 0; i < char.MaxValue; i++)
            {
                var c = (char)i;
                var s = new string(c, 2);
                Assert.AreEqual(s, Decode(Encode(s)));
            }
        }

        [Test]
        public static void RoundTrip0000ToFFFFTriple()
        {
            for (int i = 0; i < char.MaxValue; i++)
            {
                var c = (char)i;
                var s = new string(c, 3);
                Assert.AreEqual(s, Decode(Encode(s)));
            }
        }

        [Test]
        public static void ShouldNotGetEncoded()
        {
            var s = "^°<>|!`\"§$%&/()[]{}\\=?`´*+~'#-_.:,;µ²³üöä";
            Assert.AreEqual(s, Encode(s));
        }
    }
}
