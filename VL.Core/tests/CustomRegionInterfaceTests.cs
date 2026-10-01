#nullable enable

using System;
using NUnit.Framework;
using VL.Core.PublicAPI;

namespace VL.Core.Tests
{
    [TestFixture]
    public class CustomRegionInterfaceTests
    {
        [Test]
        public void FactoryWithInputsDoesNotRequireInlayCreateOperation()
        {
            var region = new FactoryRegion();
            region.SetPatchInlayFactory(value => new InlayWithoutCreate(value));

            Assert.AreEqual(42, region.Factory(42).Key);
        }

        [Test]
        public void SingleTypeParameterInterfaceUsesParameterlessFactoryContract()
        {
            Assert.That(typeof(IRegion<string>).GetInterfaces(), Does.Contain(typeof(IRegion<Func<string>, string>)));
        }

        [Test]
        public void LegacyExplicitImplementationWorksThroughGenericBase()
        {
            IRegion<Func<string>, string> region = new LegacyRegion();
            region.SetPatchInlayFactory(() => "inlay");

            Assert.AreEqual("inlay", ((LegacyRegion)region).Factory());
        }

        private interface IInlayWithoutCreate
        {
            int Key { get; }
        }

        private sealed record InlayWithoutCreate(int Key) : IInlayWithoutCreate;

        private sealed class FactoryRegion : IRegion<Func<int, IInlayWithoutCreate>, IInlayWithoutCreate>
        {
            public Func<int, IInlayWithoutCreate> Factory { get; private set; } = null!;

            public void SetPatchInlayFactory(Func<int, IInlayWithoutCreate> patchInlayFactory) => Factory = patchInlayFactory;

            public void AcknowledgeInput(in InputDescription description, object? outerValue) { }

            public void RetrieveOutput(in OutputDescription description, out object? outerValue) => outerValue = null;

            public void RetrieveInput(in InputDescription description, IInlayWithoutCreate patchInlay, out object? innerValue) => innerValue = null;

            public void AcknowledgeOutput(in OutputDescription description, IInlayWithoutCreate patchInlay, object? innerValue) { }
        }

        private sealed class LegacyRegion : IRegion<string>
        {
            public Func<string> Factory { get; private set; } = null!;

            void IRegion<string>.SetPatchInlayFactory(Func<string> patchInlayFactory) => Factory = patchInlayFactory;

            void IRegion<string>.AcknowledgeInput(in InputDescription description, object? outerValue) { }

            void IRegion<string>.RetrieveOutput(in OutputDescription description, out object? outerValue) => outerValue = null;

            void IRegion<string>.RetrieveInput(in InputDescription description, string patchInlay, out object? innerValue) => innerValue = null;

            void IRegion<string>.AcknowledgeOutput(in OutputDescription description, string patchInlay, object? innerValue) { }
        }
    }
}
