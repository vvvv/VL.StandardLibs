using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VL.Core.CompilerServices;
using Stride.Core.Mathematics;
using VL.AppServices.CompilerServices;
using VL.AppServices;
using VL.AppServices.Serialization;
using VL.TestFramework;

namespace VL.Core.Tests
{
    [TestFixture]
    public class TypeRegistrationTests
    {
        TestAppHost appHost;
        TypeRegistryImpl registry;
        AdaptiveImplementationProvider adaptiveProvider;

        [SetUp]
        public void Setup()
        {
            registry = new TypeRegistryImpl(new VLTypeInfoFactory(scanAssemblies: false));
            appHost = new(registry);
            appHost.Services.RegisterService(adaptiveProvider = new AdaptiveImplementationProvider(ImmutableArray<Type>.Empty));
        }

        [TearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        static class DictionaryOperations
        {
            [CreateDefault]
            public static ImmutableDictionary<TKey, TValue> CreateDefault<TKey, TValue>() => ImmutableDictionary<TKey, TValue>.Empty;
        }

        static class ListOperations
        {
            [CreateNew]
            public static List<T> CreateNew<T>(NodeContext c) => new List<T>();
        }

        static class VectorOperations
        {
            [CreateDefault]
            public static Vector4 CreateDefault() => Vector4.UnitW;

            [CreateNew]
            public static Vector4 CreateNew(NodeContext c) => Vector4.Zero;
        }

        static class ArrayOperations
        {
            [CreateDefault]
            public static T[] CreateDefault<T>() => Array.Empty<T>();
        }

        static class ArrayBuilderOperations
        {
            [CreateNew]
            public static ImmutableArray<T>.Builder CreateNew<T>(NodeContext c) => ImmutableArray.CreateBuilder<T>();
        }

        static class Tuple2Operations
        {
            [CreateDefault]
            public static (T1, T2) CreateDefault<T1, T2, AdM>() where AdM : IAdaptiveCreateDefault<T1>, IAdaptiveCreateDefault<T2>
            {
                var a = default(AdM);
                a.CreateDefault(out T1 v1);
                a.CreateDefault(out T2 v2);
                return (v1, v2);
            }
        }

        interface IAdaptiveCreateDefault<T>
        {
            void CreateDefault(out T value);
        }

        struct AdaptiveImplementations : IAdaptiveCreateDefault<string>, IAdaptiveCreateDefault<float>
        {
            public void CreateDefault(out float value)
            {
                value = 1f;
            }

            public void CreateDefault(out string value)
            {
                value = string.Empty;
            }
        }

        class EmptyLookupTable
        {

        }

        [Element]
        class MyPatch<T>
        {
            [CreateNew]
            public static MyPatch<T> CreateNew(NodeContext c, [SerializedDefaultValue("1", false)] int x) => new MyPatch<T>(x);

            [CreateDefault]
            public static MyPatch<T> CreateDefault() => new MyPatch<T>(0);

            public MyPatch(int x)
            {
                X = x;
            }

            public int X { get; }
        }

        [Element]
        class MyPatch2<T1, T2>
        {
            [CreateDefault]
            public static MyPatch2<T1, T2> CreateDefault<AdM>() where AdM : IAdaptiveCreateDefault<T1>, IAdaptiveCreateDefault<T2>
            {
                return new MyPatch2<T1, T2>() { SomeTuple = Tuple2Operations.CreateDefault<T1, T2, AdM>() };
            }

            public (T1, T2) SomeTuple { get; set; }
        }
        
        [Test]
        public void CreateDefault_ImmutableDictionary_Test()
        {
            registry.RegisterType(typeof(ImmutableDictionary<,>), supportType: typeof(DictionaryOperations));
            var value = appHost.GetDefaultValue(typeof(ImmutableDictionary<string, object>));
            Assert.AreEqual(value, DictionaryOperations.CreateDefault<string, object>());
        }

        [Test]
        public void CreateDefault_Array_Test()
        {
            registry.RegisterType(typeof(object[]), supportType: typeof(ArrayOperations));
            var value = appHost.GetDefaultValue(typeof(string[]));
            Assert.AreEqual(value, ArrayOperations.CreateDefault<string>());
        }

        [Test]
        public void CreateDefault_ValueType_Test()
        {
            registry.RegisterType(typeof(Vector4), supportType: typeof(VectorOperations));
            var value = appHost.GetDefaultValue(typeof(Vector4));
            Assert.AreEqual(value, VectorOperations.CreateDefault());
        }

        [Test]
        public void CreateNew_ValueType_Test()
        {
            registry.RegisterType(typeof(Vector4), supportType: typeof(VectorOperations));
            var value = appHost.CreateInstance(typeof(Vector4));
            Assert.AreEqual(value, VectorOperations.CreateNew(null));
        }

        [Test]
        public void CreateNew_ImmutableDictionary_Test()
        {
            registry.RegisterType(typeof(List<>), supportType: typeof(ListOperations));
            var value = appHost.CreateInstance(typeof(List<string>));
            Assert.IsNotNull(value);
        }

        [Test]
        public void CreateDefault_MyPatch_Test()
        {
            registry.RegisterType(typeof(MyPatch<>));
            var value = appHost.GetDefaultValue(typeof(MyPatch<string>)) as MyPatch<string>;
            Assert.IsNotNull(value);
        }

        [Test]
        public void CreateNew_MyPatch_Test()
        {
            registry.RegisterType(typeof(MyPatch<>));
            var value = appHost.CreateInstance(typeof(MyPatch<string>)) as MyPatch<string>;
            Assert.IsNotNull(value);
            Assert.AreEqual(1, value.X);
        }

        [Test]
        public void CreateNew_NestedType_Test()
        {
            registry.RegisterType(typeof(ImmutableArray<>.Builder), supportType: typeof(ArrayBuilderOperations));
            var value = appHost.CreateInstance(typeof(ImmutableArray<float>.Builder));
            Assert.IsNotNull(value);
            Assert.IsInstanceOf<ImmutableArray<float>.Builder>(value);
        }

        [Test]
        public void Rendering_NestedType_Test()
        {
            registry.RegisterType(typeof(ImmutableArray<>.Builder), name: "ArrayBuilder", supportType: typeof(ArrayBuilderOperations));
            registry.RegisterType(typeof(float), name: "Float32");
            var typeInfo = registry.GetTypeInfo(typeof(ImmutableArray<float>.Builder));
            Assert.AreEqual("ArrayBuilder<Float32>", typeInfo.ToString());
        }

        [Test]
        public void AdaptiveCreateDefault_ImportedType_Test()
        {
            registry.RegisterType(typeof(ValueTuple<,>), name: "Tuple", supportType: typeof(Tuple2Operations));
            adaptiveProvider.RegisterAdaptiveImplementation(typeof(AdaptiveImplementations));
            var value = (ValueTuple<string, float>)(appHost.GetDefaultValue(typeof(ValueTuple<string, float>)));
            Assert.AreEqual(string.Empty, value.Item1);
            Assert.AreEqual(1f, value.Item2);
        }

        [Test]
        public void AdaptiveCreateDefault_PatchedType_Test()
        {
            registry.RegisterType(typeof(MyPatch2<,>));
            registry.RegisterType(typeof(ValueTuple<,>), name: "Tuple", supportType: typeof(Tuple2Operations));
            adaptiveProvider.RegisterAdaptiveImplementation(typeof(AdaptiveImplementations));
            var myPatch = (MyPatch2<string, float>)(appHost.GetDefaultValue(typeof(MyPatch2<string, float>)));
            var value = myPatch.SomeTuple;
            Assert.AreEqual(string.Empty, value.Item1);
            Assert.AreEqual(1f, value.Item2);
        }
    }
}
