using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Buffer = Stride.Graphics.Buffer;

namespace VL.Stride.Shaders
{
    public static class NativeDeviceUtils
    {
        //command list
        static FieldInfo nativeDeviceContextFi;
        static FieldInfo unorderedAccessViewsFi;
        static FieldInfo nativeDeviceChildFi;
        static FieldInfo unorderedAccessViewFi;

        //graphics device
        static FieldInfo nativeDeviceFi;
        static MethodInfo registerBufferMemoryUsageMi;


        static FieldInfo geometryShaderFi;

        //stream out buffer
        static ConstructorInfo bufferCi;
        static FieldInfo bufferDescriptionFi;
        static FieldInfo nativeDescriptionFi;
        static FieldInfo elementCountFi;
        static PropertyInfo viewFlagsPi;
        static PropertyInfo viewFormatPi;
        static MethodInfo initializeViewsMi;

        //fast text renderer
        static FieldInfo simpleEffectFi;
        static PropertyInfo matrixTransformPi;

        static NativeDeviceUtils()
        {
            var comandListType = Type.GetType("Stride.Graphics.CommandList, Stride.Graphics");
            var comandListTypeFields = comandListType.GetRuntimeFields();
            nativeDeviceContextFi = comandListTypeFields.Where(fi => fi.Name == "nativeDeviceContext").First();
            unorderedAccessViewsFi = comandListTypeFields.Where(fi => fi.Name == "unorderedAccessViews").First();

            var graphicsResourceBaseType = Type.GetType("Stride.Graphics.GraphicsResourceBase, Stride.Graphics");
            nativeDeviceChildFi = graphicsResourceBaseType.GetFieldWithName("nativeDeviceChild");

            var graphicsResourceType = Type.GetType("Stride.Graphics.GraphicsResource, Stride.Graphics");
            unorderedAccessViewFi = graphicsResourceType.GetFieldWithName("unorderedAccessView");

            var pipelineStateType = Type.GetType("Stride.Graphics.PipelineState, Stride.Graphics");
            geometryShaderFi = pipelineStateType.GetFieldWithName("geometryShader");
 
            //graphics device native device
            var graphicsDeviceType = Type.GetType("Stride.Graphics.GraphicsDevice, Stride.Graphics");
            nativeDeviceFi = graphicsDeviceType.GetFieldWithName("nativeDevice");
            registerBufferMemoryUsageMi = graphicsDeviceType.GetmethodWithName("RegisterBufferMemoryUsage");

            //buffer
            var bufferType = Type.GetType("Stride.Graphics.Buffer, Stride.Graphics");
            var bufferTypeInfo = bufferType.GetTypeInfo();
            bufferCi = bufferTypeInfo.DeclaredConstructors.Where(ci => ci.GetParameters().Count() == 1).First();
            bufferDescriptionFi = bufferType.GetFieldWithName("bufferDescription");
            nativeDescriptionFi = bufferType.GetFieldWithName("nativeDescription");
            elementCountFi = bufferType.GetFieldWithName("elementCount");
            viewFlagsPi = bufferType.GetPropertyWithName("ViewFlags");
            viewFormatPi = bufferType.GetPropertyWithName("ViewFormat");
            initializeViewsMi = bufferType.GetmethodWithName("InitializeViews");

            var fastTextRendererType = Type.GetType("Stride.Graphics.FastTextRenderer, Stride.Graphics");
            simpleEffectFi = fastTextRendererType.GetFieldWithName("simpleEffect");

            if(fastTextRendererType.GetRuntimeProperties().Any(pi => pi.Name == "MatrixTransform"))
                matrixTransformPi = fastTextRendererType.GetPropertyWithName("MatrixTransform");

        }

        static FieldInfo GetFieldWithName(this Type t, string name)
        {
            return t.GetRuntimeFields().Where(i => i.Name == name).First();
        }

        static PropertyInfo GetPropertyWithName(this Type t, string name)
        {
            return t.GetRuntimeProperties().Where(i => i.Name == name).First();
        }

        static MethodInfo GetmethodWithName(this Type t, string name)
        {
            return t.GetRuntimeMethods().Where(i => i.Name == name).First();
        }

        static T InvokeMethod<T>(this MethodInfo mi, object instance, params object[] parameters)
        {
            return (T)mi.Invoke(instance, parameters);
        }

        static void InvokeMethod(this MethodInfo mi, object instance, params object[] parameters)
        {
            mi.Invoke(instance, parameters);
        }

        public static SharpDX.Direct3D11.DeviceContext GetNativeDeviceContext(this CommandList commandList)
        {
            return (SharpDX.Direct3D11.DeviceContext)nativeDeviceContextFi.GetValue(commandList);
        }

        public static SharpDX.Direct3D11.Device GetNativeDevice(this GraphicsDevice graphicsDevice)
        {
            return (SharpDX.Direct3D11.Device)nativeDeviceFi.GetValue(graphicsDevice);
        }

        public static void ComputeShaderReApplyUnorderedAccessView(this CommandList commandList, int slot, int counterValue)
        {
            var uavs = (SharpDX.Direct3D11.UnorderedAccessView[])unorderedAccessViewsFi.GetValue(commandList);
            commandList.GetNativeDeviceContext().ComputeShader.SetUnorderedAccessView(slot, uavs[slot], counterValue);
        }

        public static void DrawInstancedIndirect(this CommandList commandList, Buffer argsBuffer, int alignedByteOffsetForArgs)
        {
            var buffer = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(argsBuffer);
            commandList.GetNativeDeviceContext().DrawInstancedIndirect(buffer, alignedByteOffsetForArgs);
        }

        public static void DispatchIndirect(this CommandList commandList, Buffer argsBuffer, int alignedByteOffsetForArgs)
        {
            var buffer = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(argsBuffer);
            commandList.GetNativeDeviceContext().DispatchIndirect(buffer, alignedByteOffsetForArgs);
        }

        public static CommandList CopyStructureCount(this CommandList commandList, Buffer sourceBuffer, Buffer targetBuffer, int destinationAlignedByteOffset)
        {
            var buffer = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(targetBuffer);
            var uav = (SharpDX.Direct3D11.UnorderedAccessView)unorderedAccessViewFi.GetValue(sourceBuffer);
            commandList.GetNativeDeviceContext().CopyStructureCount(buffer, destinationAlignedByteOffset, uav);
            return commandList;
        }

        public static CommandList CopyResource(this CommandList commandList, Buffer sourceBuffer, Buffer targetBuffer)
        {
            var bufferS = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(sourceBuffer);
            var bufferT = (SharpDX.Direct3D11.Buffer)nativeDeviceChildFi.GetValue(targetBuffer);
            commandList.GetNativeDeviceContext().CopyResource(bufferS, bufferT);
            return commandList;
        }

        //public static 

        public static MutablePipelineState ReApplyGeometryStreamOutShader(this MutablePipelineState mutablePipelineState, GraphicsDevice graphicsDevice, EffectInstance geometryEffectInstance, string semanticName)
        {
            var bytecode = geometryEffectInstance.Effect.Bytecode;
            var reflection = bytecode.Reflection;

            var geometryBytecode = bytecode.Stages.First(s => s.Stage == ShaderStage.Geometry);

            // Calculate the strides
            var soStrides = new List<int>();
            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
            {
                for (int i = soStrides.Count; i < (streamOutputElement.Stream + 1); i++)
                {
                    soStrides.Add(0);
                }
                soStrides[streamOutputElement.Stream] += streamOutputElement.ComponentCount * sizeof(float);
            }


            SharpDX.Direct3D11.StreamOutputElement[] soElements;

            var soElems = new List<SharpDX.Direct3D11.StreamOutputElement>();
            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
            {
                var soElem = new SharpDX.Direct3D11.StreamOutputElement()
                {
                    Stream = streamOutputElement.Stream,
                    SemanticIndex = streamOutputElement.SemanticIndex,
                    SemanticName = streamOutputElement.SemanticName,
                    StartComponent = streamOutputElement.StartComponent,
                    ComponentCount = streamOutputElement.ComponentCount,
                    OutputSlot = streamOutputElement.OutputSlot
                };
                soElems.Add(soElem);
            }


            var nativeDevice = graphicsDevice.GetNativeDevice();
            //var oldGeomShader = (SharpDX.Direct3D11.GeometryShader)geometryShaderFi.GetValue(mutablePipelineState.CurrentState);
            //oldGeomShader.Dispose(); needed?
            var geometryShader = new SharpDX.Direct3D11.GeometryShader(nativeDevice, geometryBytecode, soElems.ToArray(), reflection.StreamOutputStrides, reflection.StreamOutputRasterizedStream);
            geometryShaderFi.SetValue(mutablePipelineState.CurrentState, geometryShader);
            return mutablePipelineState;
        }

        public static Buffer NewStreamOutBuffer(GraphicsDevice graphicsDevice, int sizeInBytes)
        {
            var b = (Buffer)bufferCi.Invoke(new[] { graphicsDevice });

            var bd = new BufferDescription(sizeInBytes, BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);

            var nbd = new SharpDX.Direct3D11.BufferDescription()
            {
                SizeInBytes = bd.SizeInBytes,
                StructureByteStride = 0,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                BindFlags = SharpDX.Direct3D11.BindFlags.VertexBuffer | SharpDX.Direct3D11.BindFlags.StreamOutput,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                Usage = SharpDX.Direct3D11.ResourceUsage.Default
            };

            //set fields
            bufferDescriptionFi.SetValue(b, bd);
            nativeDescriptionFi.SetValue(b, nbd);
            elementCountFi.SetValue(b, 1);

            //set properties
            viewFlagsPi.SetMethod.InvokeMethod(b, bd.BufferFlags);
            viewFormatPi.SetMethod.InvokeMethod(b, PixelFormat.None);

            //set native device child
            var nb = new SharpDX.Direct3D11.Buffer(graphicsDevice.GetNativeDevice(), nbd);
            nativeDeviceChildFi.SetValue(b, nb);

            initializeViewsMi.Invoke(b, new object[0]);

            registerBufferMemoryUsageMi.InvokeMethod(graphicsDevice, b.SizeInBytes);

            return b;
        }
    }
}
