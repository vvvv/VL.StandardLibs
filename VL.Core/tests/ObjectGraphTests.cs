using NUnit.Framework;
using System;
using System.Reactive.Disposables;
using VL.Lib.Collections;
using VL.AppServices;
using static VL.Core.VLObjectExtensions;
using VL.TestFramework;

namespace VL.Core.Tests
{
    interface IIII: IVLObject
    {
        string Name => this.ToString();
    }

    class CCCC : FactoryBasedVLNode, IIII
    {
        public CCCC() : base(NodeContext.Default)
        {
        }
    }

    class A: CCCC
    {
        public Spread<IIII> Children { get; set; }
    }

    class B : CCCC
    {
        public A A1 { get; set; }
        public A A2 { get; set; }
    }

    [TestFixture]
    public class ObjectGraphTests
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
        public static void CrawlOnTypeLevel()
        {
            var x = new A() { Children = Spread.Create<IIII>(
                new B(),
                new B()
                {
                    A1 = new A() { },
                    A2 = new A() { }
                }
                ) };

            Spread<ObjectGraphNode> nodes;
            
            // let's say we only know the type is IIII. and want to crawl on type level
            nodes = CrawlObjectGraph(x, "", new TypeBasedFilter(), true, typeof(IIII));
            Assert.IsTrue(nodes.Count == 1);

            // on object level
            nodes = CrawlObjectGraph(x, "", new FilterThatCrawlsAllProperties(), true, typeof(IIII));
            Assert.IsTrue(nodes.Count == 6);

            // on type level with root known to be always A
            nodes = CrawlObjectGraph(x, "", new TypeBasedFilter(), true, typeof(A));
            Assert.IsTrue(nodes.Count == 4);
        }
    }

    class FilterThatCrawlsAllProperties: ICrawlObjectGraphFilter
    {
        public bool CrawlAllProperties => true;
    }

    class TypeBasedFilter : ICrawlObjectGraphFilter
    {
        public bool CrawlAllProperties => true;
        public bool CrawlOnTypeLevel => true;
    }

}
