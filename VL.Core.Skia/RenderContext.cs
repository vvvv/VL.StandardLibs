using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using VL.Core;
using VL.Lib.Basics.Video;
using VL.Skia.Egl;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;
using static Windows.Win32.Graphics.Direct3D11.D3D11_1_CREATE_DEVICE_CONTEXT_STATE_FLAG;
using static VL.Core.Skia.D3D11Utils;
using Windows.Win32.Graphics.Direct3D;
using System.Drawing;
using VL.Core.Skia;
using System.Reactive.Disposables;
using VL.Lib.Collections;

namespace VL.Skia
{
    public unsafe sealed class RenderContext
    {
        public const int ResourceCacheLimit = 512 * 1024 * 1024;

        /// <summary>
        /// Returns the render context for the current thread.
        /// </summary>
        /// <returns>The render context for the current thread.</returns>
        public static unsafe RenderContext ForCurrentThread()
        {
            var appHost = AppHost.CurrentOrGlobal;
            return appHost.Services.GetOrAddService(s =>
            {

                if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                {
                    // TODO: Hmm, because of extensions assemblies being part of service scope we always create a Stride game this way - ideas?
                    if (s.GetService<IGraphicsDeviceProvider>() is IGraphicsDeviceProvider graphicsDeviceProvider &&
                        graphicsDeviceProvider.Type == GraphicsDeviceType.Direct3D11)
                    {
                        var device = EglDevice.FromD3D11(graphicsDeviceProvider.NativePointer);
                        return New(device, 0, graphicsDeviceProvider.UsesLinearColorspace).DisposeBy(appHost);
                    }
                    else
                    {
                        // We need a device for each thread
                        // EglDisplay will take ownership via refcounting. It's therefor correct to release it here.
                        using var device = EglDevice.NewD3D11();
                        return New(device, 0, useLinearColorspace: false).DisposeBy(appHost);
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }, allowToAskParent: false /* Please don't */);
        }

        public static RenderContext New(EglDevice device, int msaaSamples, bool useLinearColorspace)
        {
            var display = EglDisplay.FromDevice(device);
            return New(display, msaaSamples, useLinearColorspace, needsContextSwitch: !device.IsOwner);
        }

        public static RenderContext New(EglDisplay display, int msaaSamples, bool useLinearColorspace, bool needsContextSwitch)
        {
            var context = EglContext.New(display, msaaSamples);
            context.MakeCurrent(default);
            var backendContext = GRGlInterface.CreateAngle();
            if (backendContext is null)
                throw new Exception("Failed to create ANGLE backend context");
            var skiaContext = GRContext.CreateGl(backendContext);
            if (skiaContext is null)
                throw new Exception("Failed to create Skia backend graphics context");

            // 512MB instead of the default 96MB
            skiaContext.SetResourceCacheLimit(ResourceCacheLimit);
            return new RenderContext(context, backendContext, skiaContext, useLinearColorspace, needsContextSwitch);
        }

        public readonly EglContext EglContext;
        public readonly GRContext SkiaContext;

        private readonly GRGlInterface BackendContext;
        private readonly Thread thread;
        private ID3DDeviceContextState* deviceContextState;
        private bool hasRtv;

        RenderContext(EglContext eglContext, GRGlInterface backendContext, GRContext skiaContext, bool useLinearColorspace, bool needsContextSwitch)
        {
            EglContext = eglContext ?? throw new ArgumentNullException(nameof(eglContext));
            BackendContext = backendContext ?? throw new ArgumentNullException(nameof(backendContext));
            SkiaContext = skiaContext ?? throw new ArgumentNullException(nameof(skiaContext));
            UseLinearColorspace = useLinearColorspace;
            NeedsContextSwitch = needsContextSwitch;
            thread = Thread.CurrentThread;

            if (needsContextSwitch && OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                //deviceContextState = (ID3DDeviceContextState*)eglContext.Dislpay.Device?.ContextState;
                //using var ctx = GetD3D11DeviceContext1();
                //ID3D11RenderTargetView** rtvs = stackalloc ID3D11RenderTargetView*[(int)D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT];
                //ID3D11DepthStencilView* dstv;
                //ID3D11UnorderedAccessView** uoav = stackalloc ID3D11UnorderedAccessView*[(int)D3D11_PS_CS_UAV_REGISTER_COUNT];
                //ctx.Ptr->OMGetRenderTargets(D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, rtvs, &dstv);
                //ctx.Ptr->OMGetRenderTargetsAndUnorderedAccessViews(D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, rtvs, &dstv, 0, D3D11_PS_CS_UAV_REGISTER_COUNT, uoav);

                try
                {
                    //ctx.Ptr->OMSetRenderTargets(0);

                    //using var device = GetD3D11Device1();
                    //D3D_FEATURE_LEVEL chosenFeatureLevel;
                    //ID3DDeviceContextState* deviceContextState;
                    //device.Ptr->CreateDeviceContextState(
                    //    0,
                    //    [D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0],
                    //    D3D11_SDK_VERSION,
                    //    in ID3D11Device1.IID_Guid,
                    //    &chosenFeatureLevel,
                    //    &deviceContextState);



                    //this.deviceContextState = deviceContextState;

                }
                finally
                {
                    //ctx.Ptr->OMSetRenderTargets(D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT, rtvs, dstv);
                    //for (int i = 0; i < D3D11_SIMULTANEOUS_RENDER_TARGET_COUNT; i++)
                    //{
                    //    if (rtvs[i] != null)
                    //        rtvs[i]->Release();
                    //}
                    //if (dstv != null)
                    //    dstv->Release();
                }
            }
        }

        public bool UseLinearColorspace { get; }

        public bool NeedsContextSwitch { get; }

        public int MsaaSamples => EglContext.MsaaSamples;

        private RenderContext DisposeBy(AppHost appHost)
        {
            appHost.TakeOwnership(Disposable.Create(this, renderContext => renderContext.Dispose()));
            return this;
        }

        private void Dispose()
        {
            if (deviceContextState != null && OperatingSystem.IsWindowsVersionAtLeast(8))
                deviceContextState->Release();

            SkiaContext.Dispose();
            BackendContext.Dispose();
            EglContext.Dispose();
        }

        public Scope MakeCurrent(EglSurface surface = null)
        {
#if DEBUG
            CheckThreadAccess();
#endif

            var deviceContext = default(ComPtr<ID3D11DeviceContext1>);
            if (NeedsContextSwitch && OperatingSystem.IsWindowsVersionAtLeast(8))
            {
                deviceContext = GetD3D11DeviceContext1();
                if (deviceContextState is null)
                {
                    using var device = GetD3D11Device1();
                    D3D_FEATURE_LEVEL chosenFeatureLevel;
                    ID3DDeviceContextState* deviceContextState;
                    device.Ptr->CreateDeviceContextState(
                        0,
                        [device.Ptr->GetFeatureLevel()],
                        D3D11_SDK_VERSION,
                        in ID3D11Device1.IID_Guid,
                        &chosenFeatureLevel,
                        &deviceContextState);
                    this.deviceContextState = deviceContextState;
                }
            }

            //EglContext.MakeCurrent(surface);

            return new Scope(this, deviceContext, deviceContextState, EglContext, surface);
        }

        private void CheckThreadAccess()
        {
            if (Thread.CurrentThread != thread)
                throw new InvalidOperationException("MakeCurrent called on the wrong thrad");
        }


        private ID3D11Device* GetD3D11Device()
        {
            if (EglContext.Dislpay.TryGetD3D11Device(out var d3dDevice))
                return (ID3D11Device*)d3dDevice;
            return null;
        }

        [SupportedOSPlatform("windows8.0")]
        private ComPtr<ID3D11Device1> GetD3D11Device1() => D3D11Utils.GetD3D11Device1(GetD3D11Device());

        [SupportedOSPlatform("windows8.0")]
        private ComPtr<ID3D11DeviceContext1> GetD3D11DeviceContext1() => D3D11Utils.GetD3D11DeviceContext1(GetD3D11Device());

        public unsafe ref struct Scope : IDisposable
        {
            readonly RenderContext renderContext;
            readonly ID3D11DeviceContext1* deviceContext;

            private readonly EglContext eglContext;
            private readonly nint read;
            private readonly nint draw;

            readonly ID3DDeviceContextState* previousState;

            //private readonly uint n;
            //private readonly uint p;

            internal Scope(RenderContext renderContext, ID3D11DeviceContext1* deviceContext, ID3DDeviceContextState* newState, EglContext eglContext, EglSurface surface)
            {
                this.renderContext = renderContext;
                this.deviceContext = deviceContext;
                //this.newState = newState;

                this.eglContext = eglContext;
                read = NativeEgl.eglGetCurrentSurface(NativeEgl.EGL_READ);
                draw = NativeEgl.eglGetCurrentSurface(NativeEgl.EGL_DRAW);

                if (OperatingSystem.IsWindowsVersionAtLeast(8) && newState != null)
                {
                    ID3DDeviceContextState* previousState;
                    deviceContext->SwapDeviceContextState(newState, &previousState);
                    this.previousState = previousState;
                    //newState->Release();
                }

                NativeEgl.eglMakeCurrent(eglContext.Dislpay, surface, surface, eglContext);
            }

            public void Dispose()
            {
                NativeEgl.eglMakeCurrent(eglContext.Dislpay, draw, read, eglContext);

                if (OperatingSystem.IsWindowsVersionAtLeast(8) && deviceContext != null)
                {
                    //ID3DDeviceContextState* newState;
                    deviceContext->OMSetRenderTargets(0); // Needed for window Resize - really don't understand why after the Swap the render targets would still be bound
                    deviceContext->SwapDeviceContextState(previousState/*, &newState*/);
                    previousState->Release();
                    //renderContext.deviceContextState = newState;
                    deviceContext->Release();
                }
            }

            //static uint GetRefCount(ID3DDeviceContextState* x)
            //{
            //    x->AddRef();
            //    return x->Release();
            //}
        }
    }
}
