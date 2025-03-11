#nullable enable

using System;
using System.Windows.Forms;
using SkiaSharp;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dxgi.Common;
using Windows.Win32.Graphics.Dxgi;
using Windows.Win32.Graphics.Direct3D12;
using static Windows.Win32.PInvoke;
using static Windows.Win32.Graphics.Dxgi.Common.DXGI_FORMAT;
using static Windows.Win32.Graphics.Dxgi.DXGI_USAGE;
using static Windows.Win32.Graphics.Dxgi.DXGI_SWAP_EFFECT;
using static Windows.Win32.Graphics.Direct3D12.D3D12_COMMAND_LIST_TYPE;
using static Windows.Win32.Graphics.Direct3D12.D3D12_RESOURCE_BARRIER_TYPE;
using static Windows.Win32.Graphics.Direct3D12.D3D12_RESOURCE_STATES;
using Windows.Win32.System.Com;
using VL.Core;

namespace VL.Skia;

// Good resources
// https://github.com/google/skia/blob/9c42f62925a75e2fd920bb725da6f19c3cd2f621/tools/window/win/D3D12WindowContext_win.cpp#L168
// https://github.com/microsoft/DirectX-Graphics-Samples/blob/master/Samples/Desktop/D3D12HelloWorld/src/HelloWindow/D3D12HelloWindow.cpp

public unsafe class SkiaD3D12Control : SkiaControlBase
{
    const uint FrameCount = 2;
    private uint m_frameIndex;
    private ID3D12Fence* m_fence;
    private ulong m_fenceValue;
    private SafeFileHandle m_fenceEvent;
    private ID3D12CommandQueue* commandQueue;
    private IDXGISwapChain3* swapChain3;
    private GRD3DBackendContext d3dContext;
    private GRContext grContext;
    private IDXGIFactory4* factory;
    private IDXGIAdapter1* adapter;
    private ID3D12Device2* device;
    private ID3D12GraphicsCommandList* commandList;
    private ID3D12CommandAllocator* allocator;
    private ID3D12Debug* debug;

    public SkiaD3D12Control()
    {
        SetStyle(ControlStyles.Opaque, true);
        SetStyle(ControlStyles.UserPaint, true);
        SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        DoubleBuffered = false;
        ResizeRedraw = true;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        LoadPipeline();

        base.OnHandleCreated(e);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        DestroyPipeline();

        base.OnHandleDestroyed(e);
    }

    unsafe void LoadPipeline()
    {
        var dxgiFactoryFlags = default(DXGI_CREATE_FACTORY_FLAGS);

        ID3D12Debug* debug;
        if (D3D12GetDebugInterface(typeof(ID3D12Debug).GUID, (void**)&debug).Succeeded)
        {
            debug->EnableDebugLayer();
            dxgiFactoryFlags |= DXGI_CREATE_FACTORY_FLAGS.DXGI_CREATE_FACTORY_DEBUG;
            this.debug = debug;
        }

        CreateDXGIFactory2(dxgiFactoryFlags, typeof(IDXGIFactory4).GUID, out var p);
        factory = (IDXGIFactory4*)p;

        adapter = GetAdapter(factory);

        ID3D12Device2* device;
        D3D12CreateDevice((IUnknown*)adapter, Windows.Win32.Graphics.Direct3D.D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0, typeof(ID3D12Device).GUID, (void**)&device);
        this.device = device;

        var queueDesc = new D3D12_COMMAND_QUEUE_DESC()
        {
            Flags = D3D12_COMMAND_QUEUE_FLAGS.D3D12_COMMAND_QUEUE_FLAG_NONE,
            Type = D3D12_COMMAND_LIST_TYPE.D3D12_COMMAND_LIST_TYPE_DIRECT
        };
        device->CreateCommandQueue(in queueDesc, typeof(ID3D12CommandQueue).GUID, out p);
        commandQueue = (ID3D12CommandQueue*)p;

        // Describe and create the swap chain.
        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1()
        {
            BufferCount = FrameCount,
            Width = (uint)Width,
            Height = (uint)Height,
            Format = DXGI_FORMAT_R8G8B8A8_UNORM,
            BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
            SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
            SampleDesc = new DXGI_SAMPLE_DESC() { Count = 1, Quality = 0 }
        };
        IDXGISwapChain1* swapChain1;
        factory->CreateSwapChainForHwnd((IUnknown*)commandQueue, (HWND)Handle, in swapChainDesc, default, default, &swapChain1);
        swapChain1->QueryInterface(out this.swapChain3);
        swapChain1->Release();

        device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, typeof(ID3D12CommandAllocator).GUID, out p);
        allocator = (ID3D12CommandAllocator*)p;
        device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, (ID3D12CommandAllocator*)p, default, in ID3D12GraphicsCommandList.IID_Guid, out p);
        commandList = (ID3D12GraphicsCommandList*)p;
        commandList->Close();

        // Create synchronization objects.
        {
            device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, typeof(ID3D12Fence).GUID, out p);
            m_fence = (ID3D12Fence*)p;

            // Create an event handle to use for frame synchronization.
            m_fenceEvent = CreateEvent(default, false, false, default(string));
        }

        // SKIA
        d3dContext = new GRD3DBackendContext()
        {
            Adapter = (nint)adapter,
            Device = (nint)device,
            Queue = (nint)commandQueue,
        };
        grContext = GRContext.CreateDirect3D(d3dContext);
    }

    void DestroyPipeline()
    {
        grContext.Flush();
        grContext.Submit(synchronous: true);

        if (m_fence->GetCompletedValue() < m_fenceValue)
        {
            m_fence->SetEventOnCompletion(m_fenceValue, m_fenceEvent);
            WaitForSingleObject(m_fenceEvent, INFINITE);
        }

        grContext.Dispose();
        d3dContext.Dispose();

        m_fenceEvent.Dispose();
        m_fence->Release();
        commandList->Release();
        allocator->Release();
        swapChain3->Release();
        commandQueue->Release();
        device->Release();
        adapter->Release();
        factory->Release();
        if (debug != null)
            debug->Release();
    }

    protected override void OnResize(EventArgs e)
    {
        if (grContext is null)
            return;

        grContext.Flush();
        grContext.Submit(synchronous: true);

        if (m_fence->GetCompletedValue() < m_fenceValue)
        {
            m_fence->SetEventOnCompletion(m_fenceValue, m_fenceEvent);
            WaitForSingleObject(m_fenceEvent, INFINITE);
        }

        swapChain3->ResizeBuffers(0, (uint)Width, (uint)Height, DXGI_FORMAT_R8G8B8A8_UNORM, default);

        base.OnResize(e);
    }

    static IDXGIAdapter1* GetAdapter(IDXGIFactory4* factory4)
    {
        factory4->QueryInterface<IDXGIFactory6>(out var factory6);
        try
        {
            //if (factory4 is IDXGIFactory6 factory6)
            {
                for (uint adapterIndex = 0; true; adapterIndex++)
                {
                    factory6->EnumAdapterByGpuPreference(adapterIndex, DXGI_GPU_PREFERENCE.DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, typeof(IDXGIAdapter1).GUID, out var p);
                    var adapter1 = (IDXGIAdapter1*)p;
                    //if (p is IDXGIAdapter1 adapter1)
                    {
                        if (adapter1->GetDesc1().Flags.HasFlag(DXGI_ADAPTER_FLAG.DXGI_ADAPTER_FLAG_SOFTWARE))
                            continue;

                        return adapter1;
                    }
                }
            }
            throw new NotImplementedException();
        }
        finally
        {
            factory6->Release();
        }
    }

    static void Transititon(ID3D12GraphicsCommandList* commandList, ID3D12Resource* resource, D3D12_RESOURCE_STATES from, D3D12_RESOURCE_STATES to)
    {
        var barrier = new D3D12_RESOURCE_BARRIER()
        {
            Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
            Anonymous = new()
            {
                Transition = new D3D12_RESOURCE_TRANSITION_BARRIER()
                {
                    pResource = resource,
                    Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
                    StateBefore = from,
                    StateAfter = to
                }
            }
        };
        commandList->ResourceBarrier(1, &barrier);
    }

    protected override sealed void OnPaintCore(PaintEventArgs e)
    {
        // Wait until the previous frame is finished.
        if (m_fence->GetCompletedValue() < m_fenceValue)
        {
            m_fence->SetEventOnCompletion(m_fenceValue, m_fenceEvent);
            WaitForSingleObject(m_fenceEvent, INFINITE);
        }

        allocator->Reset();
        commandList->Reset(allocator);

        m_frameIndex = swapChain3->GetCurrentBackBufferIndex();
        swapChain3->GetBuffer(m_frameIndex, typeof(ID3D12Resource).GUID, out var p);
        var renderTarget = (ID3D12Resource*)p;

        RenderSkia(renderTarget);

        // To avoid warnings in the debug output, we need to transition the render target to the correct state.
        Transititon(commandList, renderTarget, D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT);

        renderTarget->Release();
        commandList->Close();
        var commandLists = commandList;
        commandQueue->ExecuteCommandLists(1, (ID3D12CommandList**)&commandLists);

        // Present the frame.
        swapChain3->Present(1, 0);

        commandQueue->Signal(m_fence, ++m_fenceValue);
    }

    void RenderSkia(ID3D12Resource* nativeRenderTarget)
    {
        using var info = new GRD3DTextureResourceInfo()
        {
            Resource = (nint)nativeRenderTarget,
            ResourceState = (uint)D3D12_RESOURCE_STATE_PRESENT,
            Format = (uint)DXGI_FORMAT_R8G8B8A8_UNORM,
            LevelCount = 1,
            SampleCount = 1,
            SampleQualityPattern = 0,
        };
        using var renderTarget = new GRBackendRenderTarget(Width, Height, info);
        using var surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;

        CallerInfo = CallerInfo.InRenderer(renderTarget.Width, renderTarget.Height, canvas, null);

        try
        {
            using (new SKAutoCanvasRestore(canvas, true))
            {
                OnPaint(CallerInfo);
            }
            surface.Flush(submit: true);
        }
        catch (Exception exception)
        {
            RuntimeGraph.ReportException(exception);
        }

        //canvas.Clear(SKColors.PowderBlue);
        //using var paint = new SKPaint();
        //using var font = new SKFont(SKTypeface.Default, size: 24);
        //canvas.DrawText("Hello from Direct3D12", 50, 50, font, paint);
        //surface.Flush(submit: true);
    }
}