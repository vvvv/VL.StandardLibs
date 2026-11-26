#nullable enable
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;
using VL.Core.PublicAPI;
using VL.Lib.Experimental;

namespace VL.Core.Tests
{
    [TestFixture]
    public class IfAssignedRegionTests
    {
        private static readonly InputDescription Link = new("link", typeof(int), typeof(int), IsLink: true);

        // VL sets AccumulatorId to the id of the input control point on both sides of the pair.
        private static readonly InputDescription AccumulatorInput = new("acc", typeof(float), typeof(float), AccumulatorId: "acc");
        private static readonly OutputDescription AccumulatorOutput = new("accOut", typeof(float), typeof(float), AccumulatorId: "acc");

        private readonly List<RecordingInlay<string>> _createdInlays = new();
        private IfAssignedRegion<string> _region = null!;

        [SetUp]
        public void SetUp()
        {
            _createdInlays.Clear();
            _region = new IfAssignedRegion<string>();
            AsRegion(_region).SetPatchInlayFactory(Compiled(inlay => { }));
        }

        [TearDown]
        public void TearDown() => _region.Dispose();

        [Test]
        public void Update_Null_DoesNotExecutePatch()
        {
            _region.Update(null, out var isAssigned);

            Assert.Multiple(() =>
            {
                Assert.That(isAssigned, Is.False);
                Assert.That(_createdInlays, Is.Empty);
            });
        }

        [Test]
        public void Update_Assigned_PassesInputIntoPatch()
        {
            _region.Update("a", out var isAssigned);
            _region.Update("b", out _);

            Assert.Multiple(() =>
            {
                Assert.That(isAssigned, Is.True);
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
                Assert.That(_createdInlays[0].Inputs, Is.EqualTo(new[] { "a", "b" }));
            });
        }

        [Test]
        public void Update_AssignedAfterNull_KeepsPatchState()
        {
            _region.Update("a", out _);
            _region.Update(null, out _);
            _region.Update("b", out _);

            Assert.Multiple(() =>
            {
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
                Assert.That(_createdInlays[0].Inputs, Is.EqualTo(new[] { "a", "b" }));
            });
        }

        [Test]
        public void Update_NullableValueTypeNull_DoesNotExecutePatch()
        {
            var inlays = new List<RecordingInlay<int?>>();
            using var region = CreateRegion(inlays);

            region.Update(null, out var isAssigned);

            Assert.Multiple(() =>
            {
                Assert.That(isAssigned, Is.False);
                Assert.That(inlays, Is.Empty);
            });
        }

        [Test]
        public void Update_NullableValueTypeAssigned_PassesInputIntoPatch()
        {
            var inlays = new List<RecordingInlay<int?>>();
            using var region = CreateRegion(inlays);

            region.Update(5, out var isAssigned);

            Assert.Multiple(() =>
            {
                Assert.That(isAssigned, Is.True);
                Assert.That(inlays[0].Inputs, Is.EqualTo(new int?[] { 5 }));
            });
        }

        [Test]
        public void Update_NonNullableValueType_AlwaysExecutesPatch()
        {
            var inlays = new List<RecordingInlay<int>>();
            using var region = CreateRegion(inlays);

            region.Update(0, out var isAssigned);

            Assert.Multiple(() =>
            {
                Assert.That(isAssigned, Is.True);
                Assert.That(inlays[0].Inputs, Is.EqualTo(new[] { 0 }));
            });
        }

        [Test]
        public void Constructor_NonNullableValueType_AddsWarning()
        {
            var warnings = new FakeWarnings();

            using var region = new IfAssignedRegion<int>(warnings.Add);

            Assert.That(warnings.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_ReferenceOrNullableValueType_AddsNoWarning()
        {
            var warnings = new FakeWarnings();

            using var referenceType = new IfAssignedRegion<string>(warnings.Add);
            using var nullableValueType = new IfAssignedRegion<int?>(warnings.Add);

            Assert.That(warnings.Added, Is.Empty);
        }

        [Test]
        public void Dispose_NonNullableValueType_RemovesWarning()
        {
            var warnings = new FakeWarnings();
            var region = new IfAssignedRegion<int>(warnings.Add);

            region.Dispose();

            Assert.That(warnings.ActiveCount, Is.EqualTo(0));
        }

        [Test]
        public void RegionAttribute_SupportsAccumulatorsOnly()
        {
            var attribute = typeof(IfAssignedRegion<>).GetCustomAttribute<RegionAttribute>();

            Assert.That(attribute?.SupportedBorderControlPoints, Is.EqualTo(ControlPointType.Accumulator));
        }

        [Test]
        public void RetrieveInput_Link_PassesOuterValueInside()
        {
            object? linkValue = null;
            AsRegion(_region).SetPatchInlayFactory(
                Compiled(inlay => AsRegion(_region).RetrieveInput(Link, inlay, out linkValue)));

            AsRegion(_region).AcknowledgeInput(Link, 7);
            _region.Update("a", out _);

            Assert.That(linkValue, Is.EqualTo(7));
        }

        [Test]
        public void RetrieveInput_Accumulator_PassesOuterValueInside()
        {
            object? innerValue = null;
            AsRegion(_region).SetPatchInlayFactory(
                Compiled(inlay => AsRegion(_region).RetrieveInput(AccumulatorInput, inlay, out innerValue)));

            AsRegion(_region).AcknowledgeInput(AccumulatorInput, 1f);
            _region.Update("a", out _);

            Assert.That(innerValue, Is.EqualTo(1f));
        }

        [Test]
        public void RetrieveOutput_AccumulatorPatchExecuted_ReturnsInnerValue()
        {
            AsRegion(_region).SetPatchInlayFactory(
                Compiled(inlay => AsRegion(_region).AcknowledgeOutput(AccumulatorOutput, inlay, 2f)));

            AsRegion(_region).AcknowledgeInput(AccumulatorInput, 1f);
            _region.Update("a", out _);
            AsRegion(_region).RetrieveOutput(AccumulatorOutput, out var outerValue);

            Assert.That(outerValue, Is.EqualTo(2f));
        }

        [Test]
        public void RetrieveOutput_AccumulatorPatchNotExecutedInThisFrame_ReturnsInputValue()
        {
            AsRegion(_region).SetPatchInlayFactory(
                Compiled(inlay => AsRegion(_region).AcknowledgeOutput(AccumulatorOutput, inlay, 2f)));
            AsRegion(_region).AcknowledgeInput(AccumulatorInput, 1f);
            _region.Update("a", out _);

            AsRegion(_region).AcknowledgeInput(AccumulatorInput, 3f);
            _region.Update(null, out _);
            AsRegion(_region).RetrieveOutput(AccumulatorOutput, out var outerValue);

            Assert.That(outerValue, Is.EqualTo(3f));
        }

        [Test]
        public void RetrieveOutput_AccumulatorWithoutAcknowledgedInput_ReturnsNull()
        {
            _region.Update(null, out _);
            AsRegion(_region).RetrieveOutput(AccumulatorOutput, out var outerValue);

            Assert.That(outerValue, Is.Null);
        }

        [Test]
        public void SetPatchInlayFactory_NewlyCompiledFactory_KeepsExistingInlay()
        {
            _region.Update("a", out _);

            AsRegion(_region).SetPatchInlayFactory(Compiled(inlay => { }));
            _region.Update("b", out _);

            Assert.Multiple(() =>
            {
                Assert.That(_createdInlays, Has.Count.EqualTo(1));
                Assert.That(_createdInlays[0].Inputs, Is.EqualTo(new[] { "a", "b" }));
            });
        }

        [Test]
        public void Dispose_AfterPatchExecuted_DisposesInlay()
        {
            _region.Update("a", out _);

            _region.Dispose();

            Assert.That(_createdInlays[0].IsDisposed, Is.True);
        }

        [Test]
        public void Update_AssignedWithoutPatchInlayFactory_ThrowsInvalidOperationException()
        {
            using var region = new IfAssignedRegion<string>();

            Assert.Throws<InvalidOperationException>(() => region.Update("a", out _));
        }

        [Test]
        public void Update_AfterDispose_ThrowsObjectDisposedException()
        {
            _region.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _region.Update("a", out _));
        }

        private static IRegion<IIfAssignedInlay<T>> AsRegion<T>(IfAssignedRegion<T> region) => region;

        private Func<IIfAssignedInlay<string>> Compiled(Action<RecordingInlay<string>> body) =>
            () =>
            {
                var inlay = new RecordingInlay<string>(body);
                _createdInlays.Add(inlay);
                return inlay;
            };

        private static IfAssignedRegion<T> CreateRegion<T>(List<RecordingInlay<T>> createdInlays)
        {
            var region = new IfAssignedRegion<T>();
            AsRegion(region).SetPatchInlayFactory(() =>
            {
                var inlay = new RecordingInlay<T>(_ => { });
                createdInlays.Add(inlay);
                return inlay;
            });
            return region;
        }

        private sealed class RecordingInlay<T> : IIfAssignedInlay<T>, IDisposable
        {
            private readonly Action<RecordingInlay<T>> _body;

            public RecordingInlay(Action<RecordingInlay<T>> body) => _body = body;

            public List<T> Inputs { get; } = new();

            public bool IsDisposed { get; private set; }

            public void Update(T input)
            {
                Inputs.Add(input);
                _body(this);
            }

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
    }
}
