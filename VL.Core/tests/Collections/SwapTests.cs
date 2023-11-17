#pragma warning disable CS0649
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using VL.AppServices;
using VL.AppServices.CompilerServices;
using VL.AppServices.Hotswap;
using VL.Core.CompilerServices;
using VL.Lib.Collections;
using VL.Lib.Primitive;
using VL.Lib.Reactive;
using VL.TestFramework;
using CacheMananger2_ = VL.AppServices.CompilerServices.Intrinsics.CacheManager<(object, object), (int, int, int, int, int, int, int, int, object, int)>;

namespace VL.Core.Tests
{
    [TestFixture]
    public class SwapTests
    {
        private TestAppHost appHost;

        [SetUp]
        public void Setup()
        {
            appHost = new(new TypeRegistryImpl(new VLTypeInfoFactory(scanAssemblies: false)));
            appHost.MakeCurrent().DisposeBy(appHost);
            Foo.__IsOutdated = false;
            Foo2.__IsOutdated = false;
        }

        [TearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void ImmutableDictionary_()
        {
            var b = ImmutableDictionary.CreateBuilder<string, float>();
            b.Add("a", -1);
            b.Add("b", +2);
            var d = b.ToImmutable();

            var newType = typeof(ImmutableDictionary<object, float>);
            var d2_ = d.RestoreObject(newType);
            var d2 = d2_ as ImmutableDictionary<object, float>;

            Assert.That(d2, Is.Not.Null);
            
            Assert.AreNotEqual(d.GetType(), d2.GetType());

            Assert.AreEqual(d2["a"], -1);
            Assert.AreEqual(d2["b"], +2);
        }


        [Test]
        public void FromMutableToImmutableDictionary()
        {
            var b = new Dictionary<string, float>
            {
                { "a", -1 },
                { "b", +2 }
            };

            var newType = typeof(ImmutableDictionary<object, float>);
            var d2_ = b.RestoreObject(newType);
            var d2 = d2_ as ImmutableDictionary<object, float>;

            Assert.That(d2, Is.Not.Null);

            Assert.AreNotEqual(b.GetType(), d2.GetType());

            Assert.AreEqual(d2["a"], -1);
            Assert.AreEqual(d2["b"], +2);
        }

        [Test]
        public void FromMutableToConcurrentDictionary()
        {
            var b = new Dictionary<string, float>
            {
                { "a", -1 },
                { "b", +2 }
            };

            var newType = typeof(ConcurrentDictionary<object, float>);
            var d2_ = b.RestoreObject(newType);
            var d2 = d2_ as ConcurrentDictionary<object, float>;

            Assert.That(d2, Is.Not.Null);

            Assert.AreNotEqual(b.GetType(), d2.GetType());

            Assert.AreEqual(d2["a"], -1);
            Assert.AreEqual(d2["b"], +2);
        }

        [Test]
        public void FromImmutableToMutableDictionary()
        {
            var b = ImmutableDictionary.CreateBuilder<string, float>();
            b.Add("a", -1);
            b.Add("b", +2);
            var d = b.ToImmutable();

            var newType = typeof(Dictionary<object, float>);
            var d2_ = b.RestoreObject(newType);
            var d2 = d2_ as Dictionary<object, float>;

            Assert.That(d2, Is.Not.Null);

            Assert.AreNotEqual(b.GetType(), d2.GetType());

            Assert.AreEqual(d2["a"], -1);
            Assert.AreEqual(d2["b"], +2);
        }

        [Test]
        public void FromImmutableToDictionaryBuilder()
        {
            var b = ImmutableDictionary.CreateBuilder<string, float>();
            b.Add("a", -1);
            b.Add("b", +2);
            var d = b.ToImmutable();

            var newType = typeof(ImmutableDictionary<object, float>.Builder);
            var d2_ = b.RestoreObject(newType);
            var d2 = d2_ as ImmutableDictionary<object, float>.Builder;

            Assert.That(d2, Is.Not.Null);

            Assert.AreNotEqual(b.GetType(), d2.GetType());

            Assert.AreEqual(d2["a"], -1);
            Assert.AreEqual(d2["b"], +2);
        }

        [Test]
        public void Channel_()
        {
            var c = ChannelHelpers.CreateChannelOfType(typeof(string)) as Channel<string>;
            c.Value = "a";

            var newType = typeof(Channel<object>);
            var c2_ = c.RestoreObject(newType);
            var c2 = c2_ as Channel<object>;

            Assert.That(c2, Is.Not.Null);

            Assert.AreNotEqual(c.GetType(), c2.GetType());

            Assert.AreEqual(c2.Value, "a");
        }

        [Test]
        public void Channel_To_NonGeneric()
        {
            var c = ChannelHelpers.CreateChannelOfType(typeof(string)) as Channel<string>;
            c.Value = "a";

            var newType = typeof(IChannel);
            var c2_ = c.RestoreObject(newType);
            var c2 = c2_ as IChannel;

            Assert.That(c2, Is.Not.Null);

            Assert.AreEqual(c.GetType(), c2.GetType());

            Assert.AreEqual(c2.Object, "a");
        }

        [Test]
        public void Optional_HasValue()
        {
            var c = new Optional<string>("a");
            var newType = typeof(Optional<object>);
            var c2 = (Optional<object>)c.RestoreObject(newType);
            Assert.That(c2, Is.Not.Null);
            Assert.AreNotEqual(c.GetType(), c2.GetType());
            Assert.AreEqual(c2.Value, "a");
        }

        [Test]
        public void Optional_HasNoValue()
        {
            var c = new Optional<string>();
            var newType = typeof(Optional<object>);
            var c2 = (Optional<object>)c.RestoreObject(newType);
            Assert.That(c2, Is.Not.Null);
            Assert.AreNotEqual(c.GetType(), c2.GetType());
            Assert.That(c2.HasNoValue);
        }

        [Test]
        public void Optional_FromValue()
        {
            var c = "a";
            var newType = typeof(Optional<object>);
            var c2 = (Optional<object>)c.RestoreObject(newType);
            Assert.That(c2, Is.Not.Null);
            Assert.AreNotEqual(c.GetType(), c2.GetType());
            Assert.AreEqual(c2.Value, "a");
        }

        [Test]
        public void CacheManager_()
        {
            var c = new VL.AppServices.CompilerServices.Intrinsics.CacheManager<(string, string), (int, int, int, int, int, int, int, int, int, int)>
                ((10, 9, 8, 7, 6, 5, 4, 3, 2, 1));
            c.InputsChanged(("good", "boy"));

            var newType = typeof(CacheMananger2_);
            var c2_ = c.RestoreObject(newType);
            var c2 = c2_ as CacheMananger2_;

            Assert.That(c2, Is.Not.Null);

            Assert.AreNotEqual(c.GetType(), c2.GetType());

            //Assert.AreEqual(c2.Input.Item2, "boy"); 
            Assert.AreEqual(c2.Outputs.Item9, 2);

            // there is bit missing here
            // very interesting would be the state of the cache.
            // or does it never need to (no signature change possible - anonymous types don't have signature)
        }

        [Test]
        public void Spread_()
        {
            var spread = Spread<string>.Empty.Add("a").Add("b");

            var newType = typeof(Spread<object>);
            var spread2 = spread.RestoreObject(newType) as Spread<object>;

            Assert.That(spread2, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), spread2.GetType());

            Assert.AreEqual(spread2[0], "a");
            Assert.AreEqual(spread2[1], "b");

            { 
                var monster = Spread<IEnumerable<Spread<object>>>.Empty
                    .Add(Spread<Spread<object>>.Empty
                        .Add(Spread<object>.Empty
                            .Add(1)
                            .Add("a")
                        )
                        .Add(Spread<object>.Empty
                            .Add(2)
                            .Add("b")
                        ))
                    .Add(Spread<Spread<object>>.Empty
                        .Add(Spread<object>.Empty
                            .Add(3)
                            .Add("c")
                        )
                        .Add(Spread<object>.Empty
                            .Add(4)
                            .Add("d".YieldReturn())
                        ));

                Assert.AreEqual(monster, monster.RestoreObject(typeof(object)));
                Assert.AreEqual(monster, monster.RestoreObject(typeof(IEnumerable)));
                Assert.AreEqual(monster, monster.RestoreObject(typeof(IEnumerable<IEnumerable<object>>)));
                Assert.IsTrue(monster.SequenceEqual<IEnumerable<Spread<object>>>(
                    (IEnumerable<IEnumerable<Spread<object>>>)monster.RestoreObject(typeof(IEnumerable<IEnumerable<Spread<object>>>))));
                Assert.IsTrue(monster.SequenceEqual<IEnumerable<Spread<object>>>(
                    (Spread<IEnumerable<Spread<object>>>)monster.RestoreObject(typeof(Spread<IEnumerable<Spread<object>>>))));
                Assert.IsTrue(monster.SequenceEqual<IEnumerable<Spread<object>>>(
                    (IEnumerable<Spread<Spread<object>>>)monster.RestoreObject(typeof(IEnumerable<Spread<Spread<object>>>))));
                Assert.IsTrue(monster.SequenceEqual<IEnumerable<Spread<object>>>(
                    (Spread<Spread<Spread<object>>>)monster.RestoreObject(typeof(Spread<Spread<Spread<object>>>))));
            }

            {
                var monster = new List<Spread<IEnumerable<object>>>();
                monster
                    .Add(Spread<IEnumerable<object>>.Empty
                        .Add(Spread<object>.Empty
                            .Add(1)
                            .Add("a")
                        )
                        .Add(Spread<object>.Empty
                            .Add(2)
                            .Add("b")
                        ));
                monster
                    .Add(Spread<IEnumerable<object>>.Empty
                        .Add(Spread<object>.Empty
                            .Add(3)
                            .Add("c")
                        )
                        .Add(Spread<object>.Empty
                            .Add(4)
                            .Add("d".YieldReturn())
                        ));

                Assert.IsTrue(monster.SequenceEqual(
                    (Spread<Spread<IEnumerable<object>>>)monster.RestoreObject(typeof(Spread<Spread<IEnumerable<object>>>))));
                Assert.IsTrue(monster.SequenceEqual(
                    (Spread<IEnumerable<IEnumerable<object>>>)monster.RestoreObject(typeof(Spread<IEnumerable<IEnumerable<object>>>))));
                Assert.IsTrue(monster.SequenceEqual(
                    (IEnumerable<Spread<IEnumerable<object>>>)monster.RestoreObject(typeof(IEnumerable<Spread<IEnumerable<object>>>))));
            }
        }

        [Test]
        public void ArrayToSpread_()
        {
            var array = new string[]{ "a", "b"};

            var newType = typeof(Spread<object>);
            var spread = array.RestoreObject(newType) as Spread<object>;

            Assert.That(spread, Is.Not.Null);

            Assert.AreNotEqual(array.GetType(), spread.GetType());

            Assert.AreEqual(spread[0], "a");
            Assert.AreEqual(spread[1], "b");
        }

        [Test]
        public void SpreadToArray_()
        {
            var spread = Spread.Create("a", "b");

            var newType = typeof(object[]);
            var array = spread.RestoreObject(newType) as object[];

            Assert.That(array, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), array.GetType());

            Assert.AreEqual(array[0], "a");
            Assert.AreEqual(array[1], "b");
        }

        [Test]
        public void SpreadToBuilder()
        {
            var spread = Spread.Create("a", "b");

            var newType = typeof(SpreadBuilder<object>);
            var spreadbuilder = spread.RestoreObject(newType) as ISpreadBuilder;

            Assert.That(spreadbuilder, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), spreadbuilder.GetType());

            Assert.AreEqual(spreadbuilder[0], "a");
            Assert.AreEqual(spreadbuilder[1], "b");
        }


        [Test]
        public void ToMutableList()
        {
            var spread = Spread.Create<object>("a", "b");

            var newType = typeof(List<string>);
            var list = spread.RestoreObject(newType) as List<string>;

            Assert.That(list, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), list.GetType());

            Assert.AreEqual(list[0], "a");
            Assert.AreEqual(list[1], "b");
        }

        [Test]
        public void ToImmutableList()
        {
            var spread = Spread.Create<object>("a", "b");

            var newType = typeof(ImmutableList<string>);
            var list = spread.RestoreObject(newType) as ImmutableList<string>;

            Assert.That(list, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), list.GetType());

            Assert.AreEqual(list[0], "a");
            Assert.AreEqual(list[1], "b");
        }

        [Test]
        public void ToImmutableHashSet()
        {
            var spread = Spread.Create<object>("a", "b");

            var newType = typeof(ImmutableHashSet<string>);
            var set = spread.RestoreObject(newType) as ImmutableHashSet<string>;

            Assert.That(set, Is.Not.Null);

            Assert.AreNotEqual(spread.GetType(), set.GetType());
            Assert.IsTrue(set.Count == 2);
            set = set.Remove("a").Remove("b");
            Assert.IsTrue(set.Count == 0);
        }

        [Test]
        public void NewManagedFieldGetsInitialized()
        {
            var p = new State1();
            var p2 = CompilationHelper.Restore<State2>(p, appHost.RootContext);

            Assert.IsNotNull(p2);
            Assert.IsNotNull(p2.managedField);
        }

        [Test]
        public void SignatureChangeGetsSwapped()
        {
            var p = new State2() { managedField = new Foo(1f) };

            Foo.__IsOutdated = true;
            var p2 = CompilationHelper.Restore<State3>(p, appHost.RootContext);

            Assert.IsNotNull(p2);
            Assert.IsNotNull(p2.managedField);
            Assert.AreEqual(p.managedField.X, p2.managedField.X);
        }

        void TestDataLoss(Action action, bool dataLossExpected)
        {
            var hadDataLoss = false;
            var msg = "";
            var hotswaptrouble = HotswapTrouble._;
            CompilationHelper.OnReport += CompilationHelper_OnDataLoss;
            try
            {
                action();
            }
            finally
            {
                CompilationHelper.OnReport -= CompilationHelper_OnDataLoss;
            }

            if (dataLossExpected)
                Assert.IsTrue(hadDataLoss, $"Data loss was expected, but didn't trigger: {msg}. Reason: {hotswaptrouble}");
            else
                Assert.IsFalse(hadDataLoss, $"Unexpected data loss: {msg}. Reason: {hotswaptrouble}");

            void CompilationHelper_OnDataLoss(NodeContext arg1, string arg2, HotswapTrouble hotswapTrouble)
            {
                hadDataLoss = hotswapTrouble.HasFlag(HotswapTrouble.DataLoss);
                msg = arg2;
                hotswaptrouble = hotswapTrouble;
            }
        }

        [Test]
        public void NoDataLossExpected_TheFieldWasChangedByTheUser()
        {
            // This case simulates the user replacing a node of type Foo with SomeProcess
            // We expect Foo to get disposed of and a new SomeProcess created
            var p = new State3() { managedField = new Foo2(1f, 1f) };

            var p2 = CompilationHelper.Restore<State5>(p, appHost.RootContext);
            Assert.IsNotNull(p2);
            Assert.IsNotNull(p2.managedField);
        }

        [Test]
        public void RestoreOfSpreadWithAbstractElementTypeShouldntThrow()
        {
            // This case simulates the user replacing a node of type Foo with SomeProcess
            // We expect Foo to get disposed of and a new SomeProcess created
            var p = new State6() { widgets = Spread.Create<object>(new SomeUnfriendlyWidget() ) };

            State7 p2 = default;
            TestDataLoss(() => p2 = CompilationHelper.Restore<State7>(p, appHost.RootContext), dataLossExpected: false);
            Assert.IsNotNull(p2);
            Assert.AreEqual(p.widgets, p2.widgets);
        }

        //[Test]
        //public static void SynchronizerVLObjectInput_()
        //{
        //    var c = new SynchronizerVLObjectInput<object, Foo, int>();  //<TState, TInput, TOutput>

        //    c.InputsChanged(("good", "boy"));

        //    var newType = typeof(CacheMananger2_);
        //    var c2_ = CompilationHelper.RestoreObject(newType, NodeContext.Default, c);
        //    var c2 = c2_ as CacheMananger2_;

        //    Assert.That(c2, Is.Not.Null);

        //    Assert.AreNotEqual(c.GetType(), c2.GetType());

        //    //Assert.AreEqual(c2.Input.Item2, "boy"); 
        //    Assert.AreEqual(c2.Outputs.Item9, 2);

        //    // there is bit missing here
        //    // very interesting would be the state of the cache.
        //    // or does it never need to (no signature change possible - anonymous types don't have signature)
        //}






        // we could consider using the Hoswap Particle pseudocode for deeper tests

        [Element(DocumentId = "FooDoc", PersistentId = "Foo")]
        class Foo : SwappableVLObject<Foo>
        {
            [CreateNew]
            public static Foo CreateNew(NodeContext nodeContext, float x) => new Foo(x);

            public Foo(float x)
            {
                __State = new _State() { x = x };
            }

            public float X
            {
                get
                {
                    var s = CompilationHelper.Restore<_State>(ref __State, Context);
                    return s.x;
                }
            }

            public override VLObjectProgram __Program__ => new _Program();

            class _State
            {
                public float x;
            }

            class _Program : VLObjectProgram<Foo> { }
        }

        [Element(DocumentId = "FooDoc", PersistentId = "Foo")]
        class Foo2 : SwappableVLObject<Foo2>
        {
            [CreateNew]
            public static Foo2 CreateNew(NodeContext nodeContext, float x, float y) => new Foo2(x, y);

            public Foo2(float x, float y)
            {
                __State = new _State() { x = x, y = y };
            }

            public float X
            {
                get
                {
                    var s = CompilationHelper.Restore<_State>(ref __State, Context);
                    return s.x;
                }
            }

            public override VLObjectProgram __Program__ => new _Program();

            class _State
            {
                public float x;
                public float y;
            }

            class _Program : VLObjectProgram<Foo> { }
        }
        
        class SomeUnfriendlyWidget : PatchedObject
        {

        }

        class State1
        {
        }

        class State2
        {
            [Element(IsManaged = true)]
            public Foo managedField;
        }

        class State3
        {
            [Element(IsManaged = true)]
            public Foo2 managedField;
        }

        class State4
        {
            [Element(IsManaged = true)]
            public ImportedProcess<Foo> managedField;
        }

        class State5
        {
            [Element(IsManaged = true)]
            public ImportedProcess<Foo2> managedField;
        }

        class State6
        {
            public Spread<object> widgets;
        }

        class State7
        {
            public Spread<object> widgets;
        }

        class ImportedProcess<T>
        {
            [CreateNew]
            public static ImportedProcess<T> CreateNew(NodeContext nodeContext) => new ImportedProcess<T>();
        }
    }
}
