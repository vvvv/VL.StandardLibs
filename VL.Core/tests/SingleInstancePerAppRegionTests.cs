#nullable enable
using NUnit.Framework;
using System;
using System.Collections.Generic;
using VL.Core.PublicAPI;
using VL.Lib.Animation;
using VL.Lib.Basics.Resources;
using VL.Lib.Experimental;

namespace VL.Core.Tests
{
    [TestFixture]
    public class SingleInstancePerAppRegionTests
    {
        private readonly List<IDisposable> _regions = new();
        private readonly List<CountingInlay> _createdInlays = new();
        private readonly RefCountedPool<SingleInstancePerAppRegion<int>.SharedInstance> _pool = new();
        private FrameClock _clock = null!;

        [SetUp]
        public void SetUp()
        {
            _clock = new FrameClock();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var region in _regions)
                region.Dispose();
            _regions.Clear();
            _createdInlays.Clear();
        }

        [Test]
        public void Update_EachFrame_ExecutesInlayAgain()
        {
            var region = CreateRegion(NewNodeId(), CompiledVersion1());

            var values = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                NextFrame();
                region.Update(out var value);
                values.Add(value);
            }

            Assert.That(values, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Update_TwoRegionsWithSameNodeIdInSameFrame_ExecuteSharedInlayOnce()
        {
            var nodeId = NewNodeId();
            var first = CreateRegion(nodeId, CompiledVersion1());
            var second = CreateRegion(nodeId, CompiledVersion1());

            NextFrame();
            first.Update(out var firstValue);
            second.Update(out var secondValue);

            Assert.Multiple(() =>
            {
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
                Assert.That(_createdInlays[0].ProduceCalls, Is.EqualTo(1));
                Assert.That(firstValue, Is.EqualTo(1));
                Assert.That(secondValue, Is.EqualTo(1));
            });
        }

        [Test]
        public void Update_SecondRegionUpdatesFirstInNextFrame_BothGetSameNewValue()
        {
            var nodeId = NewNodeId();
            var first = CreateRegion(nodeId, CompiledVersion1());
            var second = CreateRegion(nodeId, CompiledVersion1());
            NextFrame();
            first.Update(out _);

            NextFrame();
            second.Update(out var secondValue);
            first.Update(out var firstValue);

            Assert.Multiple(() =>
            {
                Assert.That(secondValue, Is.EqualTo(2));
                Assert.That(firstValue, Is.EqualTo(2));
            });
        }

        [Test]
        public void Update_RegionsWithDifferentNodeIds_UseSeparateInlays()
        {
            var first = CreateRegion(NewNodeId(), CompiledVersion1());
            var second = CreateRegion(NewNodeId(), CompiledVersion1());

            NextFrame();
            first.Update(out var firstValue);
            second.Update(out var secondValue);

            Assert.Multiple(() =>
            {
                Assert.That(_createdInlays, Has.Count.EqualTo(2));
                Assert.That(firstValue, Is.EqualTo(1));
                Assert.That(secondValue, Is.EqualTo(1));
            });
        }

        // Simulates a recompile / hot-swap: VL hands the existing region a factory pointing to a newly
        // compiled method. The state of the inner patch must survive.
        [Test]
        public void SetPatchInlayFactory_NewlyCompiledFactory_KeepsExistingInlayState()
        {
            var region = CreateRegion(NewNodeId(), CompiledVersion1());
            NextFrame();
            region.Update(out _);
            NextFrame();
            region.Update(out _);
            var recompiled = CompiledVersion2();
            Assume.That(recompiled.Method, Is.Not.EqualTo(CompiledVersion1().Method));

            AsRegion(region).SetPatchInlayFactory(recompiled);
            NextFrame();
            region.Update(out var value);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(3));
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
                Assert.That(_createdInlays[0].IsDisposed, Is.False);
            });
        }

        [Test]
        public void Update_RegionCreatedWithNewlyCompiledFactory_JoinsExistingInlay()
        {
            var nodeId = NewNodeId();
            var existing = CreateRegion(nodeId, CompiledVersion1());
            NextFrame();
            existing.Update(out _);

            var added = CreateRegion(nodeId, CompiledVersion2());
            NextFrame();
            added.Update(out var value);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(2));
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
            });
        }

        [Test]
        public void Dispose_NotLastRegionOfNode_KeepsInlayAlive()
        {
            var nodeId = NewNodeId();
            var first = CreateRegion(nodeId, CompiledVersion1());
            var second = CreateRegion(nodeId, CompiledVersion1());
            NextFrame();
            first.Update(out _);
            second.Update(out _);

            first.Dispose();
            NextFrame();
            second.Update(out var value);

            Assert.Multiple(() =>
            {
                Assert.That(_createdInlays[0].IsDisposed, Is.False);
                Assert.That(value, Is.EqualTo(2));
            });
        }

        [Test]
        public void Dispose_LastRegionOfNode_DisposesInlay()
        {
            var region = CreateRegion(NewNodeId(), CompiledVersion1());
            NextFrame();
            region.Update(out _);

            region.Dispose();

            Assert.That(_createdInlays[0].IsDisposed, Is.True);
        }

        // Documents what happens when VL replaces the region object itself (old one disposed before the
        // new one updates, e.g. when T changes): the shared state is released and starts over.
        [Test]
        public void Update_RegionRecreatedAfterLastOneDisposed_StartsWithNewInlay()
        {
            var nodeId = NewNodeId();
            var original = CreateRegion(nodeId, CompiledVersion1());
            NextFrame();
            original.Update(out _);
            NextFrame();
            original.Update(out _);

            original.Dispose();
            var recreated = CreateRegion(nodeId, CompiledVersion2());
            NextFrame();
            recreated.Update(out var value);

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(1));
                Assert.That(_createdInlays, Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void Update_WithoutPatchInlayFactory_ThrowsInvalidOperationException()
        {
            var region = new SingleInstancePerAppRegion<int>(NewNodeId(), AcquireFromPool);
            _regions.Add(region);

            Assert.Throws<InvalidOperationException>(() => region.Update(out _));
        }

        [Test]
        public void Update_AfterDispose_ThrowsObjectDisposedException()
        {
            var region = CreateRegion(NewNodeId(), CompiledVersion1());
            region.Dispose();

            Assert.Throws<ObjectDisposedException>(() => region.Update(out _));
        }

        [Test]
        public void AcknowledgeInput_LinkOnEveryUpdate_AddsWarningOnce()
        {
            var warnings = new FakeWarnings();
            var region = CreateRegion(NewNodeId(), CompiledVersion1(), warnings.Add);

            for (var i = 0; i < 3; i++)
            {
                NextFrame();
                AsRegion(region).AcknowledgeInput(Link, outerValue: 42);
                region.Update(out _);
            }

            Assert.Multiple(() =>
            {
                Assert.That(warnings.Added, Has.Count.EqualTo(1));
                Assert.That(warnings.ActiveCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void AcknowledgeInput_NoLinkInput_AddsNoWarning()
        {
            var warnings = new FakeWarnings();
            var region = CreateRegion(NewNodeId(), CompiledVersion1(), warnings.Add);

            NextFrame();
            AsRegion(region).AcknowledgeInput(Link with { IsLink = false }, outerValue: 42);
            region.Update(out _);

            Assert.That(warnings.Added, Is.Empty);
        }

        [Test]
        public void Update_NoLinkAcknowledgedSinceLastUpdate_RemovesWarning()
        {
            var warnings = new FakeWarnings();
            var region = CreateRegion(NewNodeId(), CompiledVersion1(), warnings.Add);
            NextFrame();
            AsRegion(region).AcknowledgeInput(Link, outerValue: 42);
            region.Update(out _);

            NextFrame();
            region.Update(out _);

            Assert.Multiple(() =>
            {
                Assert.That(warnings.Added, Has.Count.EqualTo(1));
                Assert.That(warnings.ActiveCount, Is.EqualTo(0));
            });
        }

        [Test]
        public void AcknowledgeInput_LinkReconnected_AddsWarningAgain()
        {
            var warnings = new FakeWarnings();
            var region = CreateRegion(NewNodeId(), CompiledVersion1(), warnings.Add);
            NextFrame();
            AsRegion(region).AcknowledgeInput(Link, outerValue: 42);
            region.Update(out _);
            NextFrame();
            region.Update(out _);

            NextFrame();
            AsRegion(region).AcknowledgeInput(Link, outerValue: 42);
            region.Update(out _);

            Assert.Multiple(() =>
            {
                Assert.That(warnings.Added, Has.Count.EqualTo(2));
                Assert.That(warnings.ActiveCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void Dispose_WithLinkWarning_RemovesWarning()
        {
            var warnings = new FakeWarnings();
            var region = CreateRegion(NewNodeId(), CompiledVersion1(), warnings.Add);
            NextFrame();
            AsRegion(region).AcknowledgeInput(Link, outerValue: 42);
            region.Update(out _);

            region.Dispose();

            Assert.That(warnings.ActiveCount, Is.EqualTo(0));
        }

        private static readonly InputDescription Link = new("link", typeof(int), typeof(int), IsLink: true);

        private SingleInstancePerAppRegion<int> CreateRegion(
            UniqueId nodeId,
            Func<ISingleInstanceInlay<int>> factory,
            Func<IDisposable>? addLinkWarning = null)
        {
            var region = new SingleInstancePerAppRegion<int>(nodeId, AcquireFromPool, addLinkWarning);
            AsRegion(region).SetPatchInlayFactory(factory);
            _regions.Add(region);
            return region;
        }

        // VL's pools (per app and system wide) require a running AppHost, so a ref-counted pool with the
        // same semantics is used: same key returns the same instance, last released handle disposes it.
        private IResourceHandle<SingleInstancePerAppRegion<int>.SharedInstance> AcquireFromPool(UniqueId nodeId)
            => _pool.Acquire(nodeId, () => new SingleInstancePerAppRegion<int>.SharedInstance(_clock));

        private static IRegion<ISingleInstanceInlay<int>> AsRegion(SingleInstancePerAppRegion<int> region) => region;

        private static UniqueId NewNodeId() => new(Guid.NewGuid().ToString(), "region", 0);

        private void NextFrame() => _clock.BeginFrame();

        // Two distinct lambdas stand in for two compilations of the inner patch (different methods).
        private Func<ISingleInstanceInlay<int>> CompiledVersion1() => () => Track(new CountingInlay());

        private Func<ISingleInstanceInlay<int>> CompiledVersion2() => () => Track(new CountingInlay());

        private CountingInlay Track(CountingInlay inlay)
        {
            _createdInlays.Add(inlay);
            return inlay;
        }

        private sealed class CountingInlay : ISingleInstanceInlay<int>, IDisposable
        {
            public int ProduceCalls { get; private set; }

            public bool IsDisposed { get; private set; }

            public int Produce() => ++ProduceCalls;

            public void Dispose() => IsDisposed = true;
        }

        private sealed class FakeWarnings
        {
            public List<Warning> Added { get; } = new();

            public int ActiveCount => Added.FindAll(w => !w.IsDisposed).Count;

            public IDisposable Add()
            {
                var warning = new Warning();
                Added.Add(warning);
                return warning;
            }

            public sealed class Warning : IDisposable
            {
                public bool IsDisposed { get; private set; }

                public void Dispose() => IsDisposed = true;
            }
        }

        private sealed class RefCountedPool<TResource>
            where TResource : IDisposable
        {
            private readonly Dictionary<UniqueId, (TResource Resource, int Count)> _entries = new();

            public IResourceHandle<TResource> Acquire(UniqueId key, Func<TResource> create)
            {
                var entry = _entries.TryGetValue(key, out var existing) ? existing : (create(), 0);
                _entries[key] = (entry.Item1, entry.Item2 + 1);
                return new Handle(this, key, entry.Item1);
            }

            private void Release(UniqueId key)
            {
                var (resource, count) = _entries[key];
                if (count > 1)
                {
                    _entries[key] = (resource, count - 1);
                    return;
                }

                _entries.Remove(key);
                resource.Dispose();
            }

            private sealed class Handle(RefCountedPool<TResource> pool, UniqueId key, TResource resource)
                : IResourceHandle<TResource>
            {
                private bool _disposed;

                public TResource Resource => resource;

                public void Dispose()
                {
                    if (_disposed)
                        return;

                    _disposed = true;
                    pool.Release(key);
                }
            }
        }
    }
}
