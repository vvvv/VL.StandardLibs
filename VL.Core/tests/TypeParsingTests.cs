using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using VL.AppServices;
using VL.Lib.Collections;

namespace VL.Core.Tests
{
    [TestFixture]
    public class TypeParsingTests
    {
        static TypeRegistry SetupRegistry()
        {
            var registry = new TypeRegistryImpl();
            registry.RegisterType(typeof(object), "Object", "Primitive");
            registry.RegisterType(typeof(int), "Integer32", "Primitive");
            registry.RegisterType(typeof(List<>), "MutableList", "Collections");
            registry.RegisterType(typeof(ImmutableArray<>.Builder), "ArrayBuilder", "Collections");
            registry.RegisterType(typeof(IEnumerable<>), "Sequence", "Collections");
            registry.RegisterType(typeof(IEnumerable), "Sequence", "Collections.NonGeneric");
            registry.RegisterType(typeof(Spread<>), "Spread");
            return registry;
        }

        [Test]
        public static void ParseInteger_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(int), registry.GetTypeByName("Integer32").ClrType);
        }

        [Test]
        public static void ParseListOfInteger_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(List<int>), registry.GetTypeByName("MutableList<Integer32>").ClrType);
        }

        [Test]
        public static void ParseNestedArrayBuilderOfInteger_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(ImmutableArray<int>.Builder), registry.GetTypeByName("ArrayBuilder<Integer32>").ClrType);
        }

        [Test]
        public static void ParseNonRegisteredObservableOfInteger_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(IObservable<int>), registry.GetTypeByName("IObservable`1 [System] <Integer32>").ClrType);
        }

        [Test]
        public static void ParseGenericSequence_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(IEnumerable<>), registry.GetTypeByName("Sequence [Collections]").ClrType);
        }

        [Test]
        public static void ParseNonGenericSequence_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(IEnumerable), registry.GetTypeByName("Sequence [Collections.NonGeneric]").ClrType);
        }

        [Test]
        public static void ParseSpreads_Test()
        {
            var registry = SetupRegistry();
            Assert.AreEqual(typeof(Spread<>), registry.GetTypeByName("Spread").ClrType);
            Assert.AreEqual(typeof(Spread<object>), registry.GetTypeByName("Spread<Object>").ClrType);
            Assert.AreEqual(typeof(Spread<int>), registry.GetTypeByName("Spread<Integer32>").ClrType);
            Assert.AreEqual(typeof(Spread<Spread<ImmutableArray<int>.Builder>>), registry.GetTypeByName("Spread<Spread<ArrayBuilder<Integer32>>>").ClrType);
        }
    }
}
