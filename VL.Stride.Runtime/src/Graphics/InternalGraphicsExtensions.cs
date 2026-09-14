using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Core.UnsafeExtensions;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Graphics;

static class InternalGraphicsExtensions
{
    /// <summary>
    ///   Converts to a Silk.NET <see cref="Silk.NET.DXGI.Rational"/> representation.
    /// </summary>
    /// <returns>The converted <see cref="Silk.NET.DXGI.Rational"/>.</returns>
    internal static Silk.NET.DXGI.Rational ToSilk(this Rational rational)
    {
        return new Silk.NET.DXGI.Rational((uint)rational.Numerator, (uint)rational.Denominator);
    }

    internal static ComPtr<IDXGIFactory1> NativeDXGIFactory
    {
        get
        {
            var nativeFactoryProperty = typeof(GraphicsAdapterFactory).GetProperty("NativeFactory", BindingFlags.NonPublic | BindingFlags.Static);
            return (ComPtr<IDXGIFactory1>)nativeFactoryProperty.GetValue(null);
        }
    }

    internal static uint NativeDXGIFactoryVersion
    {
        get
        {
            var nativeFactoryVersionProperty = typeof(GraphicsAdapterFactory).GetProperty("NativeFactoryVersion", BindingFlags.NonPublic | BindingFlags.Static);
            return (uint)nativeFactoryVersionProperty.GetValue(null);
        }
    }

    /// <summary>
    ///   A flag that instructs the runtime to not use the <c>ALT+ENTER</c> key combination
    ///   to switch to full screen.
    /// </summary>
    internal const uint WindowAssociation_NoAltEnter = 2;

    extension(GraphicsDevice @this)
    {
        internal Texture CreateTexture()
        {
            return CreateTexture(@this);

            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
            extern static Texture CreateTexture(GraphicsDevice i);
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

    extension(GraphicsOutput @this)
    {
        internal ComPtr<IDXGIOutput> NativeOutput
        {
            get
            {
                return GetNativeOutput(@this);

                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_NativeOutput")]
                extern static ComPtr<IDXGIOutput> GetNativeOutput(GraphicsOutput output);
            }
        }
    }

    extension(GraphicsPresenter @this)
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

        internal Texture LeftEyeBuffer
        {
            get => GetLeftEyeBuffer(@this);
            set => SetLeftEyeBuffer(@this, value);
        }

        internal Texture RightEyeBuffer
        {
            get => GetRightEyeBuffer(@this);
            set => SetRightEyeBuffer(@this, value);
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_LeftEyeBuffer")]
        extern static Texture GetLeftEyeBuffer(GraphicsPresenter presenter);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_LeftEyeBuffer")]
        extern static void SetLeftEyeBuffer(GraphicsPresenter presenter, Texture value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_RightEyeBuffer")]
        extern static Texture GetRightEyeBuffer(GraphicsPresenter presenter);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_RightEyeBuffer")]
        extern static void SetRightEyeBuffer(GraphicsPresenter presenter, Texture value);
    }

    extension(Buffer @this)
    {
        internal ComPtr<ID3D11Buffer> NativeBuffer
        {
            get
            {
                return GetNativeBuffer(@this);
                [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_NativeBuffer")]
                extern static ComPtr<ID3D11Buffer> GetNativeBuffer(Buffer buffer);
            }
        }
    }

    extension(Texture @this)
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

        internal unsafe Texture InitializeFromImpl(ID3D11Texture2D* texture, bool treatAsSrgb)
        {
            return InitializeFromImpl(@this, texture, treatAsSrgb);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFromImpl))]
            extern static Texture InitializeFromImpl(Texture texture, ID3D11Texture2D* nativeTexture, bool treatAsSrgb);
        }

        internal Texture InitializeFrom(Texture parentTexture, ref readonly TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(@this, parentTexture, in viewDescription, textureDatas);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFrom))]
            extern static Texture InitializeFrom(Texture texture, Texture parentTexture, ref readonly TextureViewDescription viewDescription, DataBox[] textureDatas);
        }

        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            return InitializeFrom(@this, description, textureDatas);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(InitializeFrom))]
            extern static Texture InitializeFrom(Texture texture, TextureDescription description, DataBox[] textureDatas);
        }
    }

    extension(GraphicsResourceBase @this)
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

        internal void OnDestroyed(bool immediately = false)
        {
            OnDestroyed(@this, immediately);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(OnDestroyed))]
            extern static void OnDestroyed(GraphicsResourceBase resource, bool immediately);
        }
    }

    extension<T>(ComPtr<T> comPtr) where T : unmanaged, IComVtbl<T>
    {
        /// <summary>
        ///   Reinterprets a <see cref="ComPtr{T}"/> to an interface of type <typeparamref name="TFrom"/> as a
        ///   COM pointer to an interface of type <typeparamref name="TTo"/> which inherits from it
        ///   (i.e., from a more <strong>generic type</strong> to a more <strong>specific type</strong>).
        /// </summary>
        /// <returns>The COM pointer reinterpreted as a <see cref="ComPtr{T}"/> of type <typeparamref name="TTo"/>.</returns>
        /// <remarks>
        ///   ⚠️ Warning: This method is unsafe because it allows casting to a more specific interface type.
        ///   Only use this method if you are certain that the underlying COM object actually implements
        ///   the <typeparamref name="TTo"/> interface; otherwise, using the resulting COM pointer may lead to
        ///   undefined behavior, memory corruption, or application crashes.
        /// </remarks>
        public unsafe ComPtr<TTo> AsComPtrUnsafe<TTo>()
            where TTo : unmanaged, IComVtbl<TTo>, IComVtbl<T>
        {
            return new ComPtr<TTo> { Handle = (TTo*)comPtr.Handle };
        }

        /// <summary>
        ///   Associates a debug name to the private data of a DXGI or Direct3D object, useful to see a friendly name
        ///   in some graphics debuggers.
        /// </summary>
        /// <param name="name">The name to associate with the object.</param>
        /// <exception cref="NotSupportedException">Thrown when the specified COM pointer type is not supported.</exception>
        public unsafe void SetDebugName(string name)
        {
            var comPtrVtbl = *comPtr.Handle;

            switch (comPtrVtbl)
            {
                case IComVtbl<IDXGIObject>:
                    {
                        var nameSpan = name.GetAsciiSpan();
                        var nameSpanLength = (uint)nameSpan.Length;

                        var dxgiObject = CastComPtr<T, IDXGIObject>(comPtr);
                        dxgiObject.SetPrivateData(DebugObjectName, nameSpanLength, nameSpan);
                        break;
                    }
#if STRIDE_GRAPHICS_API_DIRECT3D11
            case IComVtbl<ID3D11DeviceChild>:
            {
                var nameSpan = name.GetAsciiSpan();
                var nameSpanLength = (uint) nameSpan.Length;

                var d3d11DeviceChild = CastComPtr<T, ID3D11DeviceChild>(comPtr);
                d3d11DeviceChild.SetPrivateData(DebugObjectName, nameSpanLength, nameSpan);
                break;
            }
#elif STRIDE_GRAPHICS_API_DIRECT3D12
            case IComVtbl<ID3D12DeviceChild>:
            {
                var d3d12DeviceChild = CastComPtr<T, ID3D12DeviceChild>(comPtr);
                d3d12DeviceChild.SetName(name);
                break;
            }
#endif
                default:
                    throw new NotSupportedException("The specified COM pointer type is not supported.");
            }

            if (LogDebugNames)
            {
                // Log the debug name for the object
                var typeName = typeof(T).Name;
                var ptr = (nint)comPtr.Handle;
                Debug.WriteLine($"Changed or set the debug name for {typeName} at 0x{ptr:X8} to '{name}'.");
            }
        }
    }

    /// <summary>
    ///   A flag indicating whether to log debug names for Direct3D objects whenever they are set.
    /// </summary>
    /// <remarks>Debugging will be extremely slow with Visual Studio.</remarks>
    public static bool LogDebugNames { get; set; } = false;


    // From d3dcommon.h in Windows SDK (WKPDID_D3DDebugObjectName)
    public unsafe static Guid* DebugObjectName
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ReadOnlySpan<byte> data = [
                0x22, 0x8C, 0x9B, 0x42,
                0x88, 0x91,
                0x0C, 0x4B,
                0x87,
                0x42,
                0xAC,
                0xB0,
                0xBF,
                0x85,
                0xC2,
                0x00
            ];

            Debug.Assert(data.Length == sizeof(Guid));

            return (Guid*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data));
        }
    }

    /// <summary>
    ///   Casts a COM pointer from one interface type to another.
    /// </summary>
    /// <typeparam name="TFrom">The source interface type.</typeparam>
    /// <typeparam name="TTo">The target interface type.</typeparam>
    /// <param name="comPtr">The COM pointer to be cast.</param>
    /// <returns>A new <see cref="ComPtr{TTo}"/> representing the casted COM pointer.</returns>
    /// <remarks>
    ///   This method performs a direct cast of the underlying pointer. It is the caller's responsibility
    ///   to ensure that the cast is valid.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ComPtr<TTo> CastComPtr<TFrom, TTo>(ComPtr<TFrom> comPtr)
        where TFrom : unmanaged, IComVtbl<TFrom>
        where TTo : unmanaged, IComVtbl<TTo>
    {
        return new ComPtr<TTo> { Handle = (TTo*)comPtr.Handle };
    }
}