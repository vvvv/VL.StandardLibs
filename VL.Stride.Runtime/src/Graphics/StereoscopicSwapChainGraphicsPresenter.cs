using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BackBufferResourceType = SharpDX.Direct3D11.Texture2D;
using CommandList = Stride.Graphics.CommandList;
using DataBox = Stride.Graphics.DataBox;
using Device = SharpDX.Direct3D11.Device;
using DXGI_Format = SharpDX.DXGI.Format;
using Feature = SharpDX.DXGI.Feature;
using Rational = Stride.Graphics.Rational;

namespace VL.Stride.Graphics;

/// <summary>
/// Graphics presenter for SwapChain with stereoscopic support.
/// </summary>
internal class StereoscopicSwapChainGraphicsPresenter : GraphicsPresenter
{
    private readonly Texture backBuffer;

    private readonly bool flipModelSupport;

    private readonly bool tearingSupport;

    private SwapChain swapChain;

    private int bufferCount;

    private bool useFlipModel;

    public StereoscopicSwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        : base(device, presentationParameters)
    {
        PresentInterval = presentationParameters.PresentationInterval;

        flipModelSupport = CheckFlipModelSupport(device);
        tearingSupport = CheckTearingSupport(device);

        // Initialize the swap chain
        swapChain = CreateSwapChain();

        backBuffer = device.CreateTexture().InitializeFromImpl(swapChain.GetBackBuffer<BackBufferResourceType>(0), Description.BackBufferFormat.IsSRgb());

        // Reload should get backbuffer from swapchain as well
        //backBufferTexture.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));

        static bool CheckFlipModelSupport(GraphicsDevice device)
        {
            try
            {
                // From https://github.com/walbourn/directx-vs-templates/blob/main/d3d11game_win32_dr/DeviceResources.cpp#L138
                using var dxgiDevice = device.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
                using var dxgiAdapter = dxgiDevice.Adapter;
                using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory4>();
                return dxgiFactory != null;
            }
            catch
            {
                // The requested interfaces need at least Windows 8
                return false;
            }
        }

        static unsafe bool CheckTearingSupport(GraphicsDevice device)
        {
            try
            {
                // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
                using var dxgiDevice = device.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
                using var dxgiAdapter = dxgiDevice.Adapter;
                using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory5>();
                if (dxgiFactory is null)
                    return false;

                int allowTearing = 0;
                dxgiFactory.CheckFeatureSupport(Feature.PresentAllowTearing, new IntPtr(&allowTearing), sizeof(int));
                return allowTearing != 0;
            }
            catch
            {
                // The requested interfaces need at least Windows 10
                return false;
            }
        }
    }

    public override Texture BackBuffer => backBuffer;

    public override object NativePresenter => swapChain;

    public override bool IsFullScreen
    {
        get
        {
#if STRIDE_PLATFORM_UWP
                return false;
#else
            return swapChain.IsFullScreen;
#endif
        }

        set
        {
#if !STRIDE_PLATFORM_UWP
            if (swapChain == null)
                return;

            var outputIndex = Description.PreferredFullScreenOutputIndex;

            // no outputs connected to the current graphics adapter
            var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length ? GraphicsDevice.Adapter.Outputs[outputIndex] : null;

            Output currentOutput = null;

            try
            {
                RawBool isCurrentlyFullscreen;
                swapChain.GetFullscreenState(out isCurrentlyFullscreen, out currentOutput);

                // check if the current fullscreen monitor is the same as new one
                // If not fullscreen, currentOutput will be null but output won't be, so don't compare them
                if (isCurrentlyFullscreen == value && (isCurrentlyFullscreen == false || (output != null && currentOutput != null && currentOutput.NativePointer == output.NativeOutput.NativePointer)))
                    return;
            }
            finally
            {
                currentOutput?.Dispose();
            }

            bool switchToFullScreen = value;
            // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
            var description = new ModeDescription(backBuffer.ViewWidth, backBuffer.ViewHeight, Description.RefreshRate.ToSharpDX(), (DXGI_Format)Description.BackBufferFormat);
            if (switchToFullScreen)
            {
                OnDestroyed();

                Description.IsFullScreen = true;

                OnRecreated();
            }
            else
            {
                Description.IsFullScreen = false;
                swapChain.IsFullScreen = false;

                // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
            }

            // If going to window mode: 
            if (!switchToFullScreen)
            {
                // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                description.RefreshRate = new SharpDX.DXGI.Rational(0, 0);
                swapChain.ResizeTarget(ref description);
            }
#endif
        }
    }

    public override void BeginDraw(CommandList commandList)
    {
    }

    public override void EndDraw(CommandList commandList, bool present)
    {
    }

    public override void Present()
    {
        try
        {
            var presentInterval = GraphicsDevice.Tags.Get(GraphicsPresenter.ForcedPresentInterval) ?? PresentInterval;

            // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
            // DXGI_PRESENT_ALLOW_TEARING can only be used with sync interval 0. It is recommended to always pass this
            // tearing flag when using sync interval 0 if CheckFeatureSupport reports that tearing is supported and the
            // app is in a windowed mode - including border-less fullscreen mode.
            var presentFlags = useFlipModel && tearingSupport && presentInterval == PresentInterval.Immediate && !Description.IsFullScreen
                ? PresentFlags.AllowTearing
                : PresentFlags.None;

            swapChain.Present((int)presentInterval, presentFlags);
        }
        catch (SharpDXException sharpDxException)
        {
            var deviceStatus = GraphicsDevice.GraphicsDeviceStatus;
            throw new GraphicsException($"Unexpected error on Present (device status: {deviceStatus})", sharpDxException, deviceStatus);
        }
    }

    protected override void OnNameChanged()
    {
        base.OnNameChanged();
        if (Name != null && GraphicsDevice != null && GraphicsDevice.IsDebugMode && swapChain != null)
        {
            swapChain.DebugName = Name;
        }
    }

    protected override void Destroy()
    {
        // Manually update back buffer texture
        backBuffer.OnDestroyed();
        backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

        swapChain.Dispose();
        swapChain = null;

        base.OnDestroyed();
    }

    public override void OnRecreated()
    {
        base.OnRecreated();

        // Recreate swap chain
        swapChain = CreateSwapChain();

        // Get newly created native texture
        var backBufferTexture = swapChain.GetBackBuffer<BackBufferResourceType>(0);

        // Put it in our back buffer texture
        // TODO: Update new size
        backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());
        backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
    }

    protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
    {
        // Manually update back buffer texture
        backBuffer.OnDestroyed();

        // Manually update all children textures
        var fastList = DestroyChildrenTextures(backBuffer);

        if (useFlipModel)
            format = ToSupportedFlipModelFormat(format); // See CreateSwapChainForDesktop

        // If format is same as before, using Unknown (None) will keep the current
        // We do that because on Win10/RT, actual format might be the non-srgb one and we don't want to switch to srgb one by mistake (or need #ifdef)
        // Eideren: the comment above isn't very clear, I think they mean that we don't want to swap to srgb because it'll crash with flip model
        //          I've added the flip model check above because the previous logic wasn't enough, see issue #1770
        //          Testing against swapChain format instead of the backbuffer as they may not match.
        if ((DXGI_Format)format == swapChain.Description.ModeDescription.Format)
            format = PixelFormat.None;

        swapChain.ResizeBuffers(bufferCount, width, height, (DXGI_Format)format, GetSwapChainFlags());

        // Get newly created native texture
        var backBufferTexture = swapChain.GetBackBuffer<BackBufferResourceType>(0);

        // Put it in our back buffer texture
        backBuffer.InitializeFromImpl(backBufferTexture, Description.BackBufferFormat.IsSRgb());

        foreach (var texture in fastList)
        {
            texture.InitializeFrom(backBuffer, texture.ViewDescription);
        }
    }

    protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
    {
        var newTextureDescription = DepthStencilBuffer.Description;
        newTextureDescription.Width = width;
        newTextureDescription.Height = height;

        // Manually update the texture
        DepthStencilBuffer.OnDestroyed();

        // Manually update all children textures
        var fastList = DestroyChildrenTextures(DepthStencilBuffer);

        // Put it in our back buffer texture
        DepthStencilBuffer.InitializeFrom(newTextureDescription);

        foreach (var texture in fastList)
        {
            texture.InitializeFrom(DepthStencilBuffer, texture.ViewDescription);
        }
    }

    /// <summary>
    /// Calls <see cref="Texture.OnDestroyed"/> for all children of the specified texture
    /// </summary>
    /// <param name="parentTexture">Specified parent texture</param>
    /// <returns>A list of the children textures which were destroyed</returns>
    private List<Texture> DestroyChildrenTextures(Texture parentTexture)
    {
        var fastList = new List<Texture>();
        var resources = GraphicsDevice.Resources;
        lock (resources)
        {
            foreach (var resource in resources)
            {
                var texture = resource as Texture;
                if (texture != null && texture.ParentTexture == parentTexture)
                {
                    texture.OnDestroyed();
                    fastList.Add(texture);
                }
            }
        }

        return fastList;
    }

    private SwapChain CreateSwapChain()
    {
        // Check for Window Handle parameter
        if (Description.DeviceWindowHandle == null)
        {
            throw new ArgumentException("DeviceWindowHandle cannot be null");
        }

        return CreateSwapChainForWindows();
    }

#if STRIDE_PLATFORM_UWP
        private SwapChain CreateSwapChainForUWP()
        {
            bufferCount = 2;
            var description = new SwapChainDescription1
            {
                // Automatic sizing
                Width = Description.BackBufferWidth,
                Height = Description.BackBufferHeight,
                Format = (DXGI_Format)Description.BackBufferFormat.ToNonSRgb(),
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription((int)Description.MultisampleCount, 0),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                // Use two buffers to enable flip effect.
                BufferCount = bufferCount,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
            };

            SwapChain swapChain = null;
            switch (Description.DeviceWindowHandle.Context)
            {
                case Games.AppContextType.UWPXaml:
                {
                    var nativePanel = ComObject.As<ISwapChainPanelNative>(Description.DeviceWindowHandle.NativeWindow);

                    // Creates the swap chain for XAML composition
                    swapChain = new SwapChain1(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeDevice(), ref description);

                    // Associate the SwapChainPanel with the swap chain
                    nativePanel.SwapChain = swapChain;

                    break;
                }

                case Games.AppContextType.UWPCoreWindow:
                {
                    using (var dxgiDevice = GraphicsDevice.NativeDevice().QueryInterface<SharpDX.DXGI.Device2>())
                    {
                        // Ensure that DXGI does not queue more than one frame at a time. This both reduces
                        // latency and ensures that the application will only render after each VSync, minimizing
                        // power consumption.
                        dxgiDevice.MaximumFrameLatency = 1;

                        // Next, get the parent factory from the DXGI Device.
                        using (var dxgiAdapter = dxgiDevice.Adapter)
                        using (var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory2>())
                            // Finally, create the swap chain.
                        using (var coreWindow = new SharpDX.ComObject(Description.DeviceWindowHandle.NativeWindow))
                        {
                            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory
                                , GraphicsDevice.NativeDevice(), coreWindow, ref description);
                        }
                    }

                    break;
                }
                default:
                    throw new NotSupportedException(string.Format("Window context [{0}] not supported while creating SwapChain", Description.DeviceWindowHandle.Context));
            }

            return swapChain;
        }
#endif
    /// <summary>
    /// Create the SwapChain on Windows.
    /// </summary>
    /// <returns></returns>
    private SwapChain CreateSwapChainForWindows()
    {
        var hwndPtr = Description.DeviceWindowHandle.Handle;
        if (hwndPtr != IntPtr.Zero)
        {
            return CreateSwapChainForDesktop(hwndPtr);
        }
        throw new InvalidOperationException($"The {nameof(WindowHandle)}.{nameof(WindowHandle.Handle)} must not be zero.");
    }

    private SwapChain CreateSwapChainForDesktop(IntPtr handle)
    {
        // https://devblogs.microsoft.com/directx/dxgi-flip-model/#what-do-i-have-to-do-to-use-flip-model
        useFlipModel = Description.MultisampleCount == MultisampleCount.None && flipModelSupport;

        var swapchainFormat = Description.BackBufferFormat;
        bufferCount = 1;

        if (useFlipModel)
        {
            swapchainFormat = ToSupportedFlipModelFormat(swapchainFormat);
            bufferCount = 2;
        }

        var description = new SwapChainDescription
        {
            ModeDescription = new ModeDescription(Description.BackBufferWidth, Description.BackBufferHeight, Description.RefreshRate.ToSharpDX(), (DXGI_Format)swapchainFormat),
            BufferCount = bufferCount, // TODO: Do we really need this to be configurable by the user?
            OutputHandle = handle,
            SampleDescription = new SampleDescription((int)Description.MultisampleCount, 0),
            SwapEffect = useFlipModel ? SwapEffect.FlipDiscard : SwapEffect.Discard,
            Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
            IsWindowed = true,
            Flags = GetSwapChainFlags(),
        };

        using var dxgiDevice = GraphicsDevice.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
        using var dxgiAdapter = dxgiDevice.Adapter;
        using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory>();

        var newSwapChain = new SwapChain(dxgiFactory, GraphicsDevice.NativeDevice, description);
        var swapChain3 = newSwapChain.QueryInterface<SwapChain3>();
        if (swapChain3 != null)
        {
            swapChain3.ColorSpace1 = (SharpDX.DXGI.ColorSpaceType)Description.OutputColorSpace;
            swapChain3.Dispose();
        }

        //prevent normal alt-tab
        dxgiFactory.MakeWindowAssociation(handle, WindowAssociationFlags.IgnoreAltEnter);

        if (Description.IsFullScreen)
        {
            // Before fullscreen switch
            newSwapChain.ResizeTarget(ref description.ModeDescription);

            // Switch to full screen
            newSwapChain.IsFullScreen = true;

            // This is really important to call ResizeBuffers AFTER switching to IsFullScreen 
            newSwapChain.ResizeBuffers(bufferCount, Description.BackBufferWidth, Description.BackBufferHeight, newFormat: default, description.Flags);
        }

        return newSwapChain;
    }

    private SwapChainFlags GetSwapChainFlags()
    {
        var flags = SwapChainFlags.None;
        if (Description.IsFullScreen)
            flags |= SwapChainFlags.AllowModeSwitch;

        // From https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays
        // It is recommended to always use the tearing flag when it is supported.
        if (useFlipModel && tearingSupport)
            flags |= SwapChainFlags.AllowTearing;

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
}

static class StrideInternalHelpers
{
    /// <summary>
    /// Converts to SharpDX representation.
    /// </summary>
    /// <returns>SharpDX.DXGI.Rational.</returns>
    internal static SharpDX.DXGI.Rational ToSharpDX(this Rational rational)
    {
        return new SharpDX.DXGI.Rational(rational.Numerator, rational.Denominator);
    }

    extension(GraphicsDevice @this)
    {
        internal Texture CreateTexture()
        {
            return CreateTexture(@this);

            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
            extern static Texture CreateTexture(GraphicsDevice i);
        }

        internal Device NativeDevice
        {
            get
            {
                return GetNativeDevice(@this);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_NativeDevice")]
                extern static Device GetNativeDevice(GraphicsDevice device);
            }
        }

        internal HashSet<GraphicsResourceBase> Resources
        {
            get
            {
                return GetResources(@this);
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "Resources")]
                extern static ref HashSet<GraphicsResourceBase> GetResources(GraphicsDevice device);
            }
        }
    }

    extension (GraphicsOutput @this)
    {
        internal Output NativeOutput
        {
            get
            {
                return GetNativeOutput(@this);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_NativeOutput")]
                extern static Output GetNativeOutput(GraphicsOutput output);
            }
        }
    }

    extension (GraphicsPresenter @this)
    {
        internal static PropertyKey<PresentInterval?> ForcedPresentInterval
        {
            get
            {
                return GetForcedPresentInterval(null);

                [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "ForcedPresentInterval")]
                extern static ref PropertyKey<PresentInterval?> GetForcedPresentInterval(GraphicsPresenter presenter);
            }
        }
    }

    extension (Texture @this)
    {
        internal Texture ParentTexture
        {
            get
            {
                return GetParentTexture(@this);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ParentTexture")]
                extern static Texture GetParentTexture(Texture texture);
            }
            set
            {
                SetParentTexture(@this, value);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ParentTexture")]
                extern static void SetParentTexture(Texture texture, Texture parentTexture);
            }
        }

        internal Texture InitializeFromImpl(Texture2D texture, bool isSrgb)
        {
            return InitializeFromImpl(@this, texture, isSrgb);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFromImpl))]
            extern static Texture InitializeFromImpl(Texture texture, Texture2D nativeTexture, bool isSrgb);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(@this, parentTexture, viewDescription, textureDatas);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFrom))]
            extern static Texture InitializeFrom(Texture texture, Texture parentTexture, TextureViewDescription viewDescription, DataBox[] textureDatas);
        }

        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            return InitializeFrom(@this, description, textureDatas);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFrom))]
            extern static Texture InitializeFrom(Texture texture, TextureDescription description, DataBox[] textureDatas);
        }

        internal void OnDestroyed()
        {
            OnDestroyed(@this);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(OnDestroyed))]
            extern static void OnDestroyed(Texture texture);
        }
    }

    extension (GraphicsResourceBase @this)
    {
        internal GraphicsResourceLifetimeState LifetimeState
        {
            get
            {
                return GetLifetimeState(@this);
            }
            set
            {
                GetLifetimeState(@this) = value;
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "LifetimeState")]
        extern static ref GraphicsResourceLifetimeState GetLifetimeState(GraphicsResourceBase resource);
    }
}