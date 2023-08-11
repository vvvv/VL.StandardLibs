using NUnit.Framework;
using Stride.Core.Mathematics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VL.Core;
using VL.Lib.Collections;
using VL.Core.CompilerServices;
using System.Reactive.Disposables;
using VL.AppServices.CompilerServices;
using VL.AppServices;
using VL.TestFramework;

namespace VL.Core.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        private TypeRegistryImpl typeRegistry;
        private TestAppHost appHost;
        private IVLFactory factory;
        private NodeContext context;

        [SetUp]
        public void Setup()
        {
            typeRegistry = new();
            appHost = new(typeRegistry);
            factory = appHost.Factory;
            context = NodeContext.Default;
            appHost.MakeCurrent().DisposeBy(appHost);
        }

        [TearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        class MyGenericSerializer<TFoo, TBar> : ISerializer<Tuple<TBar>>
        {
            public object Serialize(SerializationContext context, Tuple<TBar> value)
            {
                return context.Serialize("Item1", value.Item1);
            }

            public Tuple<TBar> Deserialize(SerializationContext context, object content, Type type)
            {
                return new Tuple<TBar>(context.Deserialize<TBar>(content, "Item1"));
            }
        }

        [Element(DocumentId = "X", PersistentId = "Y")]
        class SomeVLObject : PatchedObject
        {
            [CreateNew]
            public static SomeVLObject Create() => new SomeVLObject(default);

            public SomeVLObject __With__(int someValue)
            {
                if (someValue != SomeValue)
                    return new SomeVLObject(someValue);
                else
                    return this;
            }

            protected override IVLObject __With__(IReadOnlyDictionary<string, object> values)
            {
                return __With__(CompilationHelper.GetValueOrExisting(values, nameof(SomeValue), in SomeValue));
            }

            [Element]
            public int SomeValue;

            public SomeVLObject(int someValue)
            {
                SomeValue = someValue;
            }
        }

        [Test]
        public void GenericSerializerTest()
        {
            factory.RegisterSerializer<Tuple<object>, MyGenericSerializer<int, object>>();
            var t = new Tuple<float>(0.3f);
            var content = context.Serialize(t);
            var deserializedT = context.Deserialize<Tuple<float>>(content);
            Assert.AreEqual(t, deserializedT);
        }

        [Test]
        public void GenericObjectSerializerTest()
        {
            factory.RegisterSerializer<Tuple<object>, MyGenericSerializer<int, object>>();
            var t = new Tuple<object>(0.3f);
            var content = context.Serialize(t);
            var deserializedT = context.Deserialize<Tuple<object>>(content);
            Assert.AreEqual(t, deserializedT);
        }

        [Test]
        public void GenericObjectSerializerWithRegisteredTypesTest()
        {
            typeRegistry.RegisterType(typeof(float), "Foo");
            factory.RegisterSerializer<Tuple<object>, MyGenericSerializer<int, object>>();
            var t = new Tuple<object>(0.3f);
            var content = context.Serialize(t);
            Assert.IsTrue(content.ToString().Contains("Foo"));
            var deserializedT = context.Deserialize<Tuple<object>>(content);
            Assert.AreEqual(t, deserializedT);
        }

        [Test]
        public void SerializeSpreadOfSpreadTest()
        {
            VL.Lib.Mathematics.Serialization.RegisterSerializers(factory);
            var s1 = Spread.Create(Spread.Create(Spread.Create(Vector2.One)));
            var s2 = context.Deserialize<Spread<Spread<Spread<Vector2>>>>(context.Serialize(s1));
            Assert.AreEqual(s1[0][0][0], s2[0][0][0]);
        }

        [Test]
        public void SerializedRootElementHasNameOfTypeTest()
        {
            var list = new List<float>() { 1f, 2f };
            var element = context.Serialize(list);
            Assert.AreEqual("List", element.Name.LocalName);
        }

        [Test]
        public void DeserializeEmptyArray_Test()
        {
            var value = context.Deserialize<float[]>(new XElement("Array"));
            Assert.IsEmpty(value);
        }

        [Test]
        public void SerializeStackTest()
        {
            var stack1 = new Stack<float>(new[] { 1f, 2f });
            var stack2 = context.Deserialize<Stack<float>>(context.Serialize(stack1));
            Assert.AreEqual(stack1.Peek(), stack2.Peek());
        }

        [Test]
        public void SerializeImmutableStackTest()
        {
            var stack1 = ImmutableStack.CreateRange(new[] { 1f, 2f });
            var stack2 = context.Deserialize<ImmutableStack<float>>(context.Serialize(stack1));
            Assert.AreEqual(stack1.Peek(), stack2.Peek());
        }

        [Test]
        public void SerializeConcurrentStackTest()
        {
            var stack1 = new ConcurrentStack<float>(new[] { 1f, 2f });
            var stack2 = context.Deserialize<ConcurrentStack<float>>(context.Serialize(stack1));
            float f1, f2;
            stack1.TryPeek(out f1);
            stack2.TryPeek(out f2);
            Assert.AreEqual(f1, f2);
        }

        [Test]
        public void SerializeQueueTest()
        {
            var queue1 = new Queue<float>(new[] { 1f, 2f });
            var queue2 = context.Deserialize<Queue<float>>(context.Serialize(queue1));
            Assert.AreEqual(queue1.Peek(), queue2.Peek());
        }

        [Test]
        public void SerializeImmutableQueueTest()
        {
            var queue1 = ImmutableQueue.CreateRange(new[] { 1f, 2f });
            var queue2 = context.Deserialize<ImmutableQueue<float>>(context.Serialize(queue1));
            Assert.AreEqual(queue1.Peek(), queue2.Peek());
        }

        [Test]
        public void SerializeConcurrentQueueTest()
        {
            var queue1 = new ConcurrentQueue<float>(new[] { 1f, 2f });
            var queue2 = context.Deserialize<ConcurrentQueue<float>>(context.Serialize(queue1));
            float f1, f2;
            queue1.TryPeek(out f1);
            queue2.TryPeek(out f2);
            Assert.AreEqual(f1, f2);
        }

        [Test]
        public void SerializeWithoutDefaultValuesTest()
        {
            var value = new SomeVLObject(default(int));
            var content = context.Serialize(value, includeDefaults: false);
            Assert.IsFalse(content.ToString().Contains("SomeValue=\"0\""));
        }

        [Test]
        public void SerializeWithDefaultValuesTest()
        {
            var value = new SomeVLObject(default(int));
            var content = context.Serialize(value, includeDefaults: true);
            Assert.IsTrue(content.ToString().Contains("SomeValue=\"0\""));
        }

        [Test]
        public void SerializeDateTimeTest()
        {
            var value = DateTime.Now;
            var value2 = context.Deserialize<DateTime>(context.Serialize(value));
            Assert.AreEqual(value, value2);
        }

        [Test]
        public void SerializeDateTimeOffsetTest()
        {
            var value = DateTimeOffset.Now;
            var value2 = context.Deserialize<DateTimeOffset>(context.Serialize(value));
            Assert.AreEqual(value, value2);
        }

        [Test]
        public void SerializeGuidTest()
        {
            var value = Guid.NewGuid();
            var value2 = context.Deserialize<Guid>(context.Serialize(value));
            Assert.AreEqual(value, value2);
        }

        [Test]
        public void DeserializeV1Test()
        {
            // Say user patched a MyRecord [Foo] in the past and new backend emits it under the very mysterios name SomeVLObject
            // We should still be able to find it
            typeRegistry.RegisterType(typeof(SomeVLObject), "MyRecord", "Foo");

            var s = @"
<Tuple xmlns:r=""reflection"" r:Version=""1"">
  <Item1 r:Type=""System.Collections.Generic.List`1[[Foo.MyRecord, VL.DynamicAssembly_32, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]"">
    <Item SomeValue=""1"" />
  </Item1>
</Tuple>
";

            var c = XElement.Parse(s);
            var v = context.Deserialize<Tuple<object>>(c);
            Assert.AreEqual(1, ((List<SomeVLObject>)v.Item1)[0].SomeValue);
        }

        [Test]
        public void RuntimeTypeIsPreservedTest()
        {
            typeRegistry.RegisterType(typeof(SomeVLObject), "MyRecord", "Foo");

            var value = new SomeVLObject( default(int));
            var content = context.Serialize<object>(value, includeDefaults: true);
            var v = context.Deserialize<object>(content);
            Assert.IsTrue(v.GetType() == typeof(SomeVLObject));
        }
    }
}
