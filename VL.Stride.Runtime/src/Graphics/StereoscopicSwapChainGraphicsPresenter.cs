using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Stride.Graphics;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;

using Feature = Silk.NET.DXGI.Feature;
using BackBufferResourceType = Silk.NET.Direct3D11.ID3D11Texture2D;

using static System.Runtime.CompilerServices.Unsafe;

namespace VL.Stride.Graphics;

/// <summary>
/// Graphics presenter for SwapChain with stereoscopic support.
/// </summary>
internal unsafe class StereoscopicSwapChainGraphicsPresenter : GraphicsPresenter
{
    private readonly Texture backBuffer;

    /// <inheritdoc/>
    public override Texture BackBuffer => backBuffer;

    private readonly bool flipModelSupport;

    private readonly bool tearingSupport;

    private bool useFlipModel;

    // We assume a minimum of IDXGISwapChain1 support (DXGI 1.2, Windows 7+ / UWP)
    private IDXGISwapChain1* swapChain;
    private uint swapChainVersion;

    /// <summary>
    ///   Gets the internal DXGI Swap-Chain.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <c>AddRef()</c> on the COM pointer to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    internal ComPtr<IDXGISwapChain1> NativeSwapChain => new ComPtr<IDXGISwapChain1>(swapChain);

    /// <summary>
    ///   Gets the version number of the native DXGI Swap-Chain supported.
    /// </summary>
    /// <value>
    ///   This indicates the latest DXGI Swap-Chain interface version supported by this Swap-Chain.
    ///   For example, if the value is 4, then this Swap-Chain supports up to <see cref="IDXGISwapChain4"/>.
    /// </value>
    internal uint NativeSwapChainVersion => swapChainVersion;

    private int bufferCount;
    private uint bufferSwapIndex;

    // TODO: This boxes the ComPtr, which is not ideal
    /// <inheritdoc/>
    public override object NativePresenter => NativeSwapChain;

    /// <inheritdoc/>
    public override bool IsFullScreen
    {
        get => GetFullScreenState();
        set => SetFullscreenState(value);
    }


    public StereoscopicSwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        : base(device, presentationParameters)
    {
        PresentInterval = presentationParameters.PresentationInterval;

        CheckDeviceFeatures(out flipModelSupport, out tearingSupport);

        // Initialize the swap chain
        CreateSwapChain();

        // Gets the native Back-Buffer from the Swap-Chain.
        //   This increments the reference count of the COM object,
        //   so we need to Release() it when discarding or swapping it.
        var nativeBackBuffer = GetBackBuffer<BackBufferResourceType>();

        // Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
        // compensate with Release() to return the reference count to its previous value
        backBuffer = device.CreateTexture().InitializeFromImpl(nativeBackBuffer, Description.BackBufferFormat.IsSRgb);
        nativeBackBuffer.Release();

        UpdateStereoEyeBuffers();

        // Reload should get backbuffer from swapchain as well
        //backBufferTexture.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));

        //
        // Determines if the Graphics Device supports the flip model and tearing.
        //
        static void CheckDeviceFeatures(out bool supportsFlipModel, out bool supportsTearing)
        {
            // TODO: Should we move this to GraphicsAdapterFactory? It's system-wide after all, not adapter-specific

            var dxgiFactory = InternalGraphicsExtensions.NativeDXGIFactory;
            var dxgiFactoryVersion = InternalGraphicsExtensions.NativeDXGIFactoryVersion;

            supportsFlipModel = CheckFlipModelSupport(dxgiFactoryVersion);
            supportsTearing = CheckTearingSupport(dxgiFactoryVersion, dxgiFactory);
        }

        //
        // Determines if the DXGI adapter and the system supports the flip model.
        // From https://github.com/walbourn/directx-vs-templates/blob/main/d3d11game_win32_dr/DeviceResources.cpp#L138
        //
        static bool CheckFlipModelSupport(uint dxgiFactoryVersion)
        {
            // The requested interfaces need at least Windows 8 and IDXGIFactory4
            return dxgiFactoryVersion >= 4;
        }

        //
        // Determines if the DXGI adapter and the system supports tearing, also known as "vsync-off".
        // This flag is particularly useful for variable refresh rate displays.
        // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
        //
        static unsafe bool CheckTearingSupport(uint dxgiFactoryVersion, ComPtr<IDXGIFactory1> dxgiFactory1)
        {
            // The requested interfaces need at least Windows 10 and IDXGIFactory5
            if (dxgiFactoryVersion < 5)
                return false;

            var dxgiFactory5 = dxgiFactory1.AsComPtrUnsafe<IDXGIFactory1, IDXGIFactory5>();

            int allowTearing = 0;
            HResult result = dxgiFactory5.CheckFeatureSupport(Feature.PresentAllowTearing, ref allowTearing, sizeof(int));

            return result.IsSuccess && allowTearing != 0;
        }
    }

    /// <summary>
    ///   Gets one of the Swap-Chain Back-Buffers.
    /// </summary>
    /// <typeparam name="TD3DResource">The interface of the surface to resolve from the Back-Buffer.</typeparam>
    /// <param name="index">
    ///   A zero-based buffer index.
    ///   If the swap effect is not <see cref="SwapEffect.Sequential"/>, this method only has
    ///   access to the first Buffer; for this case (which is the default), set the index to zero.
    /// </param>
    /// <returns>Returns a reference to a Back-Buffer Texture.</returns>
    private ComPtr<TD3DResource> GetBackBuffer<TD3DResource>(uint index = 0) where TD3DResource : unmanaged, IComVtbl<TD3DResource>
    {
        // NOTE: The Swap-Chain Back-Buffer is a COM object, so this AddRef()s.
        //       It must be released when swapping or discarding the reference.

        swapChain->GetBuffer(index, out ComPtr<TD3DResource> resource);
        return resource;
    }

    /// <summary>
    ///   Determines if the Swap-Chain is presenting in fullscreen mode, and to which output.
    /// </summary>
    /// <param name="fullScreenOutput">
    ///   When this method returns,
    ///   <list type="bullet">
    ///     <item>If the Swap-Chain is presenting in fullscreen mode, contains the output (screen) to which it is presenting.</item>
    ///     <item>If the Swap-Chain is presenting to a window, contains a <see langword="null"/> pointer.</item>
    ///   </list>
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if the Swap-Chain is in fullscreen mode; <see langword="false"/> otherwise.
    /// </returns>
    private bool GetFullScreenState(out ComPtr<IDXGIOutput> fullScreenOutput)
    {
        int isFullScreen = default;
        fullScreenOutput = default;
        swapChain->GetFullscreenState(ref isFullScreen, ref fullScreenOutput);

        return isFullScreen != 0;
    }

    /// <summary>
    ///   Determines if the Swap-Chain is presenting in fullscreen mode.
    /// </summary>
    /// <returns>
    ///   <see langword="true"/> if the Swap-Chain is in fullscreen mode; <see langword="false"/> otherwise.
    /// </returns>
    private bool GetFullScreenState()
    {
        SkipInit(out int isFullScreen);
        swapChain->GetFullscreenState(ref isFullScreen, ppTarget: null);

        return isFullScreen != 0;
    }

    /// <summary>
    ///   Sets the presentation mode of the Graphics Presenter.
    /// </summary>
    /// <param name="isFullScreen">
    ///   A value indicating whether the presentation will be in full screen.
    ///   <list type="bullet">
    ///     <item><see langword="true"/> if the presentation will be in full screen.</item>
    ///     <item><see langword="false"/> if the presentation will be in a window.</item>
    ///   </list>
    /// </param>
    private void SetFullscreenState(bool isFullScreen)
    {
        if (swapChain is null)
            return;

        var outputIndex = Description.PreferredFullScreenOutputIndex;

        var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length
                ? GraphicsDevice.Adapter.Outputs[outputIndex]
                // There are no outputs connected to the current Graphics Adapter
                : null;

        bool isCurrentlyFullscreen = GetFullScreenState(out var currentOutput);

        currentOutput.Release();

        // Check if the current fullscreen monitor is the same as the new one.
        // If not fullscreen, currentOutput will be null but output won't be, so don't compare them
        if (isCurrentlyFullscreen == isFullScreen &&
            (isCurrentlyFullscreen is false || (output is not null && currentOutput.Handle != null && currentOutput.Handle == output.NativeOutput.Handle)))
            return;

        bool switchToFullScreen = isFullScreen;

        // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
        var description = new ModeDesc
        {
            Width = (uint)backBuffer.ViewWidth,
            Height = (uint)backBuffer.ViewHeight,
            RefreshRate = Description.RefreshRate.ToSilk(),
            Format = (Format)Description.BackBufferFormat
        };
        if (switchToFullScreen)
        {
            OnDestroyed();

            Description.IsFullScreen = true;

            OnRecreated();
        }
        else
        {
            Description.IsFullScreen = false;
            HResult result = swapChain->SetFullscreenState(Fullscreen: 0, pTarget: null);

            if (result.IsFailure)
                result.Throw();

            // Call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
            Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
        }

        // If going to window mode:
        if (!switchToFullScreen)
        {
            // Call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
            description.RefreshRate = default;
            HResult result = swapChain->ResizeTarget(in description);

            if (result.IsFailure)
                result.Throw();
        }
    }

    public override void EndDraw(CommandList commandList, bool present)
    {
        // Transition the back-buffer to Present so the upcoming IDXGISwapChain::Present sees
        // it in the required layout. Skipped when the caller won't Present (no-draw frames,
        // headless tests) — the back buffer stays in its current layout for next frame.
        if (present)
            commandList.ResourceBarrierTransition(BackBuffer, BarrierLayout.Present);
    }

    /// <inheritdoc/>
    /// <exception cref="GraphicsDeviceException">
    ///   An unexpected error occurred while presenting the Swap-Chain. Check the status of the Graphics Device
    ///   for more information (<see cref="GraphicsDeviceException.Status"/>).
    /// </exception>
    public override void Present()
    {
        var presentInterval = GraphicsDevice.Tags.Get(GraphicsPresenter.ForcedPresentInterval) ?? PresentInterval;

        // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
        //   DXGI_PRESENT_ALLOW_TEARING can only be used with sync interval 0. It is recommended to always pass this
        //   tearing flag when using sync interval 0 if CheckFeatureSupport reports that tearing is supported and the
        //   app is in a windowed mode - including border-less fullscreen mode.

        var presentFlags = useFlipModel && tearingSupport && presentInterval == PresentInterval.Immediate && !Description.IsFullScreen
            ? DXGI.PresentAllowTearing
            : 0;

        HResult result = swapChain->Present((uint)presentInterval, presentFlags);

        if (result.IsFailure)
        {
            var deviceStatus = GraphicsDevice.GraphicsDeviceStatus;

            var exception = Marshal.GetExceptionForHR(result);
            throw new GraphicsDeviceException($"Unexpected error on Present (device status: {deviceStatus})", exception, deviceStatus);
        }
    }

    /// <inheritdoc/>
    protected override void OnNameChanged()
    {
        base.OnNameChanged();

        if (GraphicsDevice.IsDebugMode is true && Name is not null && swapChain is not null)
        {
            new ComPtr<IDXGISwapChain1>(swapChain).SetDebugName(Name);
        }
    }

    protected override void Destroy()
    {
        // Drain the GPU before releasing the swap-chain and its buffers: the last Present may
        // still be in flight, and DXGI only tears the swap-chain down once it completes.
        GraphicsDevice.WaitForGpuIdle();

        // Manually update back buffer texture
        backBuffer.OnDestroyed();
        backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

        this.LeftEyeBuffer?.Dispose();
        this.LeftEyeBuffer = null;
        this.RightEyeBuffer?.Dispose();
        this.RightEyeBuffer = null;

        if (swapChain is not null)
        {
            // Release the swap-chain and its buffers
            swapChain->Release();
            swapChain = null;
        }

        base.OnDestroyed();
    }

    public override void OnRecreated()
    {
        base.OnRecreated();

        // Recreate swap chain
        CreateSwapChain();

        // Get the newly created native Texture
        //   This increments the reference count of the COM object,
        //   so we need to Release() it when discarding or swapping it.
        var backBufferTexture = GetBackBuffer<BackBufferResourceType>(0);
        bufferSwapIndex = 0;

        // Put it in our Back-Buffer Texture
        //   Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
        //   compensate with Release() to return the reference count to its previous value
        backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb);
        backBufferTexture.Release();

        UpdateStereoEyeBuffers();

        backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
    }

    protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
    {
        HResult result;

        // Manually update the Back-Buffer Texture
        backBuffer.OnDestroyed(immediately: true);

        // Manually update all children Textures (Views)
        var childrenTextures = DestroyChildrenTextures(backBuffer);

        if (useFlipModel)
            format = ToSupportedFlipModelFormat(format); // See CreateSwapChainForDesktop

        // If format is same as before, using Unknown (None) will keep the current
        // We do that because on Win10/RT, actual format might be the non-sRGB one and we don't want to switch to sRGB one by mistake (or need #ifdef)
        // Eideren: the comment above isn't very clear, I think they mean that we don't want to swap to sRGB because it'll crash with flip model
        //          I've added the flip model check above because the previous logic wasn't enough, see issue #1770
        //          Testing against swapChain format instead of the backbuffer as they may not match.

        SkipInit(out SwapChainDesc swapChainDesc);
        result = swapChain->GetDesc(ref swapChainDesc);

        if (result.IsFailure)
            result.Throw();

        if ((Format)format == swapChainDesc.BufferDesc.Format)
            format = PixelFormat.None;

        result = swapChain->ResizeBuffers((uint)bufferCount, (uint)width, (uint)height, (Format)format, (uint)GetSwapChainFlags());

        if (result.IsFailure)
            result.Throw();

        // Get the newly created native Texture
        //   This increments the reference count of the COM object,
        //   so we need to Release() it when discarding or swapping it.
        var backBufferTexture = GetBackBuffer<BackBufferResourceType>();
        bufferSwapIndex = 0;

        // Put it in our back buffer texture
        //   Texture.InitializeFromImpl also increments the reference count when storing the COM pointer;
        //   compensate with Release() to return the reference count to its previous value
        backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb);
        backBufferTexture.Release();

        foreach (var childTexture in childrenTextures)
        {
            childTexture.InitializeFrom(parentTexture: backBuffer, in childTexture.ViewDescription);
        }

        UpdateStereoEyeBuffers();
    }

    protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
    {
        var newTextureDescription = DepthStencilBuffer.Description with
        {
            Width = width,
            Height = height
        };

        // Manually update the Depth-Stencil Buffer
        DepthStencilBuffer.OnDestroyed(immediately: true);

        // Manually update all children Textures (Views)
        var childrenTextures = DestroyChildrenTextures(DepthStencilBuffer);

        // Put it in our Depth-Stencil Buffer
        DepthStencilBuffer.InitializeFrom(newTextureDescription);

        foreach (var childTexture in childrenTextures)
        {
            childTexture.InitializeFrom(parentTexture: DepthStencilBuffer, in childTexture.ViewDescription);
        }
    }

    private void CreateSwapChain()
    {
        if (Description.DeviceWindowHandle is null)
            throw new InvalidOperationException("DeviceWindowHandle cannot be null");

        CreateSwapChainForWindows();
    }

    /// <summary>
    /// Create the SwapChain on Windows.
    /// </summary>
    /// <returns></returns>
    private void CreateSwapChainForWindows()
    {
        var hwndPtr = Description.DeviceWindowHandle.Handle;
        if (hwndPtr == 0)
            throw new InvalidOperationException($"The {nameof(WindowHandle)}.{nameof(WindowHandle.Handle)} must not be zero.");

        CreateSwapChainForDesktop(hwndPtr);
    }

    private void CreateSwapChainForDesktop(IntPtr handle)
    {
        if (!flipModelSupport)
            throw new GraphicsException("Stereoscopic swap chain requires DXGI flip model support.");

        if (Description.MultisampleCount != MultisampleCount.None)
            throw new GraphicsException("Stereoscopic swap chain does not support multisampling.");

        useFlipModel = true;

        var swapchainFormat = ToSupportedFlipModelFormat(Description.BackBufferFormat);
        bufferCount = 2;

        var modeDescription = new ModeDesc
        {
            Width = (uint)Description.BackBufferWidth,
            Height = (uint)Description.BackBufferHeight,
            RefreshRate = Description.RefreshRate.ToSilk(),
            Format = (Format)swapchainFormat,

            ScanlineOrdering = ModeScanlineOrder.Unspecified,  // TODO: Make this configurable?
            Scaling = ModeScaling.Unspecified  // TODO: Make this configurable?
        };

        var description = new SwapChainDesc1
        {
            BufferCount = (uint)bufferCount,
            BufferUsage = DXGI.UsageBackBuffer | DXGI.UsageRenderTargetOutput,
            SwapEffect = SwapEffect.FlipSequential,

            Width = modeDescription.Width,
            Height = modeDescription.Height,
            Format = modeDescription.Format,

            SampleDesc = new SampleDesc(count: (uint)Description.MultisampleCount, quality: 0),
            Scaling = Scaling.None,
            Stereo = true,
            AlphaMode = AlphaMode.Unspecified,  // TODO: Make this configurable
            Flags = (uint)GetSwapChainFlags()
        };
        var fullscreenDescription = new SwapChainFullscreenDesc
        {
            Windowed = !Description.IsFullScreen,
            RefreshRate = modeDescription.RefreshRate,
            Scaling = modeDescription.Scaling,
            ScanlineOrdering = modeDescription.ScanlineOrdering
        };


        using var dxgiDevice = GraphicsDevice.NativeDevice.QueryInterface<IDXGIDevice2>();
        ComPtr<IDXGIAdapter> dxgiAdapter = default;
        dxgiDevice.GetAdapter(ref dxgiAdapter);
        using var nativeFactory = dxgiAdapter.GetParent<IDXGIFactory2>();
        dxgiAdapter.Dispose();

        dxgiDevice.SetMaximumFrameLatency(1);

        ComPtr<IDXGISwapChain1> newSwapChain = default;

        ComPtr<IDXGIOutput> doNotRestrictOutput = default;
        HResult result = nativeFactory.CreateSwapChainForHwnd(GraphicsDevice.NativeDevice, handle, in description, in fullscreenDescription, doNotRestrictOutput, ref newSwapChain);

        if (result.IsFailure)
            result.Throw();

        swapChain = newSwapChain;
        swapChainVersion = GetLatestDxgiSwapChainVersion(newSwapChain);

        // We need a IDXGISwapChain3 to enable output color space setting to support HDR outputs
        if (swapChainVersion >= 3)
        {
            var swapChain3 = newSwapChain.AsComPtrUnsafe<IDXGISwapChain1, IDXGISwapChain3>();
            swapChain3.SetColorSpace1((Silk.NET.DXGI.ColorSpaceType)Description.OutputColorSpace);
        }

        // Prevent switching between windowed and fullscreen modes by pressing Alt+ENTER
        nativeFactory.MakeWindowAssociation(handle, InternalGraphicsExtensions.WindowAssociation_NoAltEnter);

        if (Description.IsFullScreen)
        {
            // Before fullscreen switch
            newSwapChain.ResizeTarget(in modeDescription);

            // Switch to fullscreen
            newSwapChain.SetFullscreenState(Fullscreen: 1, pTarget: ref NullRef<IDXGIOutput>());

            // It's really important to call ResizeBuffers AFTER switching to IsFullScreen
            newSwapChain.ResizeBuffers((uint)bufferCount,
                                       (uint)Description.BackBufferWidth,
                                       (uint)Description.BackBufferHeight,
                                       NewFormat: default,
                                       description.Flags);
        }
    }

    private void UpdateStereoEyeBuffers()
    {
        this.LeftEyeBuffer?.Dispose();
        this.RightEyeBuffer?.Dispose();

        this.LeftEyeBuffer = backBuffer.ToTextureView(new TextureViewDescription { ArraySlice = 0, Type = ViewType.Single });
        this.RightEyeBuffer = backBuffer.ToTextureView(new TextureViewDescription { ArraySlice = 1, Type = ViewType.Single });
    }

    /// <summary>
    ///   Returns the appropriate flags for the Swap-Chain given the configuration and system capabilities.
    /// </summary>
    /// <returns>The most appropriate <see cref="SwapChainFlag"/>s.</returns>
    private SwapChainFlag GetSwapChainFlags()
    {
        SwapChainFlag flags = 0;

        if (Description.IsFullScreen)
            flags |= SwapChainFlag.AllowModeSwitch;

        // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
        // It is recommended to always use the tearing flag when it is supported.
        if (useFlipModel && tearingSupport)
            flags |= SwapChainFlag.AllowTearing;

        return flags;
    }

    /// <summary>
    /// Flip model does not support certain format, this method ensures it is in a supported format.
    /// https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-flip-model
    /// For HDR see: https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Will throw if the given format does not have a direct analog supported by the flip model
    /// </exception>
    static PixelFormat ToSupportedFlipModelFormat(PixelFormat pixelFormat)
    {
        var nonSRgb = pixelFormat.ToNonSRgb();
        switch (nonSRgb)
        {
            case PixelFormat.R16G16B16A16_Float: // scRGB HDR, should use PresenterColorSpace.RgbFullG10NoneP709, gets converted by windows to display color space
            case PixelFormat.R10G10B10A2_UNorm: // HDR10/BT.2100 HDR, should use PresenterColorSpace.RgbFullG2084NoneP2020, directly sent to display
            case PixelFormat.B8G8R8A8_UNorm:
            case PixelFormat.R8G8B8A8_UNorm:
                return nonSRgb;
            default: throw new ArgumentException($"Format '{pixelFormat}' is not supported when using flip swap", nameof(pixelFormat));
        }
    }

    /// <summary>
    ///   Queries the latest DXGI Swap-Chain version supported.
    /// </summary>
    private static uint GetLatestDxgiSwapChainVersion(IDXGISwapChain1* dxgiSwapChain)
    {
        HResult result;
        uint dxgiSwapChainVersion;

        if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain4>(out _)).IsSuccess)
        {
            dxgiSwapChainVersion = 4;
            dxgiSwapChain->Release();
        }
        else if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain3>(out _)).IsSuccess)
        {
            dxgiSwapChainVersion = 3;
            dxgiSwapChain->Release();
        }
        else if ((result = dxgiSwapChain->QueryInterface<IDXGISwapChain2>(out _)).IsSuccess)
        {
            dxgiSwapChainVersion = 2;
            dxgiSwapChain->Release();
        }
        else
        {
            dxgiSwapChainVersion = 1;
        }

        return dxgiSwapChainVersion;
    }
}