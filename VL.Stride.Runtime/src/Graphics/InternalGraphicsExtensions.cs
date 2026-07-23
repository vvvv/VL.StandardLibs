using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Stride.Core;
using System.Runtime.CompilerServices;
using Device = SharpDX.Direct3D11.Device;

namespace Stride.Graphics;

static class InternalGraphicsExtensions
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

    extension(GraphicsOutput @this)
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
    }
}