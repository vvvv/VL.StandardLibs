#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.VirtualReality;
using VL.Core;
using VL.Stride.Rendering;

[assembly: ImportType(typeof(StereoscopicSettings), Category = "Stride.VirtualReality.Internal")]

namespace VL.Stride.Rendering;

/// <summary>
/// Settings for stereoscopic rendering
/// </summary>
[ProcessNode]
public sealed class StereoscopicSettings
{
    private CameraComponent? leftEye;
    private CameraComponent? rightEye;
    private bool swapEyes;
    private readonly VRRendererSettings vrSettings;
    private readonly StereoscopicVRDevice stereoscopicVRDevice;
    private readonly bool stereoAvailable;

    public StereoscopicSettings(NodeContext nodeContext)
    {
        vrSettings = new VRRendererSettings()
        {
            Enabled = true,
            IgnoreCameraRotation = false,
            VRDevice = stereoscopicVRDevice = new StereoscopicVRDevice(this)
        };
        var graphicsDevice = nodeContext.AppHost.Services.GetRequiredService<Game>().GraphicsDevice;
        using var dxgiDevice = graphicsDevice.NativeDevice.QueryInterface<SharpDX.DXGI.Device>();
        using var dxgiAdapter = dxgiDevice.Adapter;
        using var dxgiFactory = dxgiAdapter.GetParent<SharpDX.DXGI.Factory4>();
        stereoAvailable = dxgiFactory.IsWindowedStereoEnabled;
    }

    /// <summary>
    /// Updates the stereoscopic settings with the given eye separation and viewer distance.
    /// </summary>
    /// <param name="leftEye">The camera component for the left eye.</param>
    /// <param name="rightEye">The camera component for the right eye.</param>
    /// <param name="swapEyes">Whether to swap the left and right eye cameras.</param>
    /// <returns>The updated viewport settings.</returns>
    [return: Pin(Name = "Output")]
    public VRRendererSettings Update(CameraComponent? leftEye = null, CameraComponent? rightEye = null, bool swapEyes = false)
    {
        this.leftEye = leftEye;
        this.rightEye = rightEye;
        this.swapEyes = swapEyes;
        this.vrSettings.VRDevice = stereoscopicVRDevice;
        return vrSettings;
    }

    /// <summary>
    /// Indicates whether stereoscopic rendering is available on the current system.
    /// </summary>
    [Fragment(IsDefault = true)]
    public bool StereoAvailable => stereoAvailable;

    internal sealed class StereoscopicVRDevice : VRDevice
    {
        private readonly StereoscopicSettings parent;

        private float verticalFieldOfViewDegrees = CameraComponent.DefaultVerticalFieldOfView;
        private float aspectRatio = CameraComponent.DefaultAspectRatio;
        private float projectionYOffset;

        public override Size2 OptimalRenderFrameSize => Presenter != null ? new Size2(Presenter.BackBuffer.Width, Presenter.BackBuffer.Height) : Size2.Zero;

        public override Size2 ActualRenderFrameSize { get => OptimalRenderFrameSize; protected set => throw new NotImplementedException(); }
        public override Texture? MirrorTexture { get; protected set; }
        public override float RenderFrameScaling { get; set; }

        public override DeviceState State => DeviceState.Valid;

        public override Vector3 HeadPosition => default;

        public override Quaternion HeadRotation => Quaternion.Identity;

        public override Vector3 HeadLinearVelocity => default;

        public override Vector3 HeadAngularVelocity => default;

        public override TouchController? LeftHand => null;

        public override TouchController? RightHand => null;

        public override TrackedItem[] TrackedItems => Array.Empty<TrackedItem>();

        public override bool CanInitialize => StereoAvailable;

        public GraphicsPresenter? Presenter { get; internal set; }

        public bool StereoAvailable => parent.StereoAvailable;

        public StereoscopicVRDevice(StereoscopicSettings parent)
        {
            this.parent = parent;
        }

        public void SetCameraProjectionParameters(float verticalFieldOfViewDegrees, float aspectRatio, float projectionYOffset)
        {
            if (verticalFieldOfViewDegrees <= 0.0f || verticalFieldOfViewDegrees >= 179.0f)
                return;

            if (aspectRatio <= MathUtil.ZeroTolerance)
                return;

            this.verticalFieldOfViewDegrees = verticalFieldOfViewDegrees;
            this.aspectRatio = aspectRatio;
            this.projectionYOffset = projectionYOffset;
        }

        public override void ReadEyeParameters(Eyes eye, float near, float far, ref Vector3 cameraPosition, ref Matrix cameraRotation, bool ignoreHeadRotation, bool ignoreHeadPosition, out Matrix view, out Matrix projection)
        {
            if (parent.swapEyes)
            {
                eye = (eye == Eyes.Left ? Eyes.Right : Eyes.Left);
            }

            var customCamera = (eye == Eyes.Left ? parent.leftEye : parent.rightEye);
            if (customCamera != null)
            {
                view = customCamera.ViewMatrix; 
                projection = customCamera.ProjectionMatrix;
                return;
            }

            throw new NotSupportedException("Left and right eye cameras must be passed from outside.");
        }

        public override void Enable(GraphicsDevice device, GraphicsDeviceManager graphicsDeviceManager, bool requireMirror, int mirrorWidth, int mirrorHeight)
        {
        }

        public override void Commit(CommandList commandList, Texture renderFrame)
        {
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void Draw(GameTime gameTime)
        {
        }
    }
}
