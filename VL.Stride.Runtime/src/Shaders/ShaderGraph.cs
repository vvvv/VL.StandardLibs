using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VL.Stride.Effects.ComputeFX;
using VL.Stride.Effects.ShaderFX;
using VL.Stride.Effects.TextureFX;
using VL.Stride.Rendering;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Images;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;
using static VL.Stride.Shaders.ShaderFX.ShaderFXUtils;
using System.Reactive.Disposables;
using VL.Stride.Rendering.ComputeEffect;
using VL.Stride.Effects;

namespace VL.Stride.Shaders.ShaderFX
{
    public static class ShaderGraph
    {
        /// <summary>
        /// Gives access to the <see cref="CompositeDisposable"/> whose lifetime is tied to the graph being built.
        /// See https://github.com/vvvv/VL.Stride/pull/451 for details.
        /// </summary>
        internal static readonly PropertyKey<CompositeDisposable> GraphSubscriptions = new PropertyKey<CompositeDisposable>(nameof(GraphSubscriptions), typeof(ShaderGraph));

        public static IComputeVoid SomeComputeFXGraph(Buffer buffer)
        {
            var bufferDecl = new DeclBuffer();
            bufferDecl.Resource = buffer;
            var getItem = new GetItemBuffer<float>(bufferDecl, new ComputeValue<uint>());

            var value1 = new ComputeValue<float>();
            var value2 = new ComputeValue<float>();
            var value3 = new ComputeValue<float>();

            var var1 = DeclAndSetVar(getItem);

            var var2 = DeclAndSetVar(value2);

            var var3 = DeclAndSetVar(value3);

            var first = BuildPlus(var1, var2);
            var second = BuildPlus(first, var1);
            var third = BuildPlus(second, var3);

            var finalOrder = BuildFinalShaderGraph(third);

            return finalOrder;
        }

        public static IComputeVoid BuildFinalShaderGraph(IComputeNode root, IEnumerable<IComputeNode> excludes = null)
        {
            if (root is null)
                return new ComputeOrder();

            var tree = root is IComputeVoid ? new[] { root } : root.GetChildren();

            var visited = excludes != null ? new HashSet<IComputeNode>(excludes) : new HashSet<IComputeNode>();
            var flat = tree.TraversePostOrder(n => n.GetChildren(), visited).ToList();

            var statements = flat.OfType<IComputeVoid>();

            var finalOrder = new ComputeOrder(statements);
            return finalOrder;
        }

        public static SetVar<float> BuildPlus(SetVar<float> var1, SetVar<float> var2)
        {
            var getter1 = ShaderFXUtils.GetVarValue(var1);
            var getter2 = ShaderFXUtils.GetVarValue(var2);

            var plus = new BinaryOperation<float>("Plus", getter1, getter2);

            return DeclAndSetVar("PlusResult", plus);
        }

        public static IEnumerable<T> TraversePostOrder<T>(this IEnumerable<T> e, Func<T, IEnumerable<T>> f, HashSet<T> visited) where T : IComputeNode
        {
            foreach (var item in e)
            {
                if (!visited.Contains(item))
                {
                    visited.Add(item);

                    var children = f(item).TraversePostOrder(f, visited);

                    foreach (var child in children)
                        yield return child;

                    yield return item;
                }
            }
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> e, Func<T, IEnumerable<T>> f)
        {
            return e.SelectMany(c => f(c).Flatten(f)).Concat(e);
        }

        public static IEnumerable<T> ExpandPreOrder<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> elementSelector)
        {
            var stack = new Stack<IEnumerator<T>>();
            var e = source.GetEnumerator();
            try
            {
                while (true)
                {
                    while (e.MoveNext())
                    {
                        var item = e.Current;
                        yield return item;
                        var elements = elementSelector(item);
                        if (elements == null) continue;
                        stack.Push(e);
                        e = elements.GetEnumerator();
                    }
                    if (stack.Count == 0) break;
                    e.Dispose();
                    e = stack.Pop();
                }
            }
            finally
            {
                e.Dispose();
                while (stack.Count != 0) stack.Pop().Dispose();
            }
        }

        public static VLComputeEffectShader ComposeComputeShader(GraphicsDevice graphicsDevice, IServiceRegistry services, IComputeVoid root)
        {
            var computeEffect = new VLComputeEffectShader(RenderContext.GetShared(services), "ComputeFXGraph");

            if (root != null)
            {
                var context = NewShaderGeneratorContext(graphicsDevice, computeEffect.Parameters, computeEffect.Subscriptions);

                var key = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
                var shaderSource = root.GenerateShaderSource(context, key);

                var mixin = new ShaderClassSource("ComputeFXGraph").CreateMixin();
                mixin.AddComposition("Root", shaderSource);

                computeEffect.Parameters.Set(EffectNodeBaseKeys.EffectNodeBaseShader, mixin);
            }

            return computeEffect;
        }

        public static TextureFXEffect ComposeShader(GraphicsDevice graphicsDevice, IComputeValue<Vector4> root)
        {
            var effectImageShader = new TextureFXEffect("TextureFXGraphEffect");

            if (root != null)
            {
                var context = NewShaderGeneratorContext(graphicsDevice, effectImageShader.Parameters, effectImageShader.Subscriptions);

                var key = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
                var shaderSource = root.GenerateShaderSource(context, key);

                var mixin = new ShaderMixinSource();
                mixin.AddComposition("Root", shaderSource);

                effectImageShader.Parameters.Set(TextureFXGraphKeys.TextureFXRoot, mixin);
            }
            return effectImageShader;
        }

        public static DynamicEffectInstance ComposeDrawShader(GraphicsDevice graphicsDevice, IComputeValue<Vector4> vertexRoot, IComputeValue<Vector4> pixelRoot)
        {
            var effectImageShader = new DynamicDrawEffectInstance("ShaderFXGraphEffect");

            if (vertexRoot != null && pixelRoot != null)
            {
                var context = NewShaderGeneratorContext(graphicsDevice, effectImageShader.Parameters, effectImageShader.Subscriptions);

                var key = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
                var vertexShaderSource = vertexRoot.GenerateShaderSource(context, key);
                var pixelShaderSource = pixelRoot.GenerateShaderSource(context, key);

                var mixin = new ShaderMixinSource();
                mixin.AddComposition("VertexRoot", vertexShaderSource);
                mixin.AddComposition("PixelRoot", pixelShaderSource);

                effectImageShader.Parameters.Set(ShaderFXGraphKeys.ShaderFXRoot, mixin);
            }
            return effectImageShader;
        }

        class DynamicDrawEffectInstance : DynamicEffectInstance
        {
            internal readonly CompositeDisposable Subscriptions = new CompositeDisposable();

            public DynamicDrawEffectInstance(string effectName, ParameterCollection parameters = null) : base(effectName, parameters)
            {
            }

            protected override void Destroy()
            {
                Subscriptions.Dispose();
                base.Destroy();
            }
        }

        public static ShaderSource ComposeShaderSource(GraphicsDevice graphicsDevice, IComputeNode root, CompositeDisposable subscriptions)
        {
            if (root != null)
            {
                try
                {
                    var context = NewShaderGeneratorContext(graphicsDevice, new ParameterCollection(), subscriptions);

                    var key = new MaterialComputeColorKeys(MaterialKeys.DiffuseMap, MaterialKeys.DiffuseValue, Color.White);
                    return root.GenerateShaderSource(context, key);
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        internal static ShaderGeneratorContext NewShaderGeneratorContext(GraphicsDevice graphicsDevice, ParameterCollection parameters, CompositeDisposable subscriptions)
        {
            var context = new ShaderGeneratorContext(graphicsDevice)
            {
                Parameters = parameters
            };
            context.Tags.Set(GraphSubscriptions, subscriptions);
            return context;
        }

        public static bool TryGetSubscriptions(this ShaderGeneratorContext context, out CompositeDisposable subscriptions)
        {
            return context.Tags.TryGetValue(GraphSubscriptions, out subscriptions);
        }
    }
}
