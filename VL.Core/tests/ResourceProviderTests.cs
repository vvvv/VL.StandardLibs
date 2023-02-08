using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Basics.Resources;

namespace VL.Core.Tests
{
    [TestFixture]
    public class ResourceProviderTests
    {
        [Test]
        public void Return_ThrowsObjectDisposedException()
        {
            var myResource = Disposable.Create(() => { });
            var provider = ResourceProvider.Return(myResource, r => r.Dispose());

            // Someone takes explicit ownership and cleans up
            { 
                using var handle1 = provider.GetHandle();
                using var handle2 = provider.GetHandle();
            }

            // Someone else triess to access the already cleaned up resource
            Assert.Throws<ObjectDisposedException>(() => provider.GetHandle());
        }
    }
}
