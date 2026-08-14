#nullable enable
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.VirtualReality;
using VL.Core;
using VL.Stride.Rendering;

[assembly: ImportType(typeof(StereoscopicSettings), Category = "Stride.VirtualReality")]

namespace VL.Stride.Rendering;

/// <summary>
/// VR settings for stereoscopic rendering
/// </summary>
[ProcessNode]
public sealed class StereoscopicSettings
{
    private float eyeSeparation;
    private float viewerDistance;
    private CameraComponent? leftEye;
    private CameraComponent? rightEye;
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
    /// <param name="eyeSeparation">The distance between the eyes for stereoscopic rendering.</param>
    /// <param name="viewerDistance">The distance from the viewer to the screen for stereoscopic rendering.</param>
    /// <param name="leftEye">The camera component for the left eye.</param>
    /// <param name="rightEye">The camera component for the right eye.</param>
    /// <returns>The updated viewport settings.</returns>
    [return: Pin(Name = "Output")]
    public VRRendererSettings Update(float eyeSeparation = 0.065f, float viewerDistance = 1.0f, CameraComponent? leftEye = null, CameraComponent? rightEye = null)
    {
        this.eyeSeparation = eyeSeparation;
        this.viewerDistance = viewerDistance;
        this.leftEye = leftEye;
        this.rightEye = rightEye;
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
            var customCamera = (eye == Eyes.Left ? parent.leftEye : parent.rightEye);
            if (customCamera != null)
            {
                view = customCamera.ViewMatrix; 
                projection = customCamera.ProjectionMatrix;
                return;
            }
            

            var halfEyeSeparation = parent.eyeSeparation * 0.5f;
            var convergenceDistance = parent.viewerDistance > MathUtil.ZeroTolerance ? parent.viewerDistance : 1.0f;
            var verticalFieldOfViewRadians = MathUtil.DegreesToRadians(verticalFieldOfViewDegrees);
            var baseProjectionY = 1.0f / MathF.Tan(verticalFieldOfViewRadians * 0.5f);
            var baseProjectionX = baseProjectionY / aspectRatio;

            // Shift each eye's frustum in opposite directions so both eyes converge at viewerDistance.
            var horizontalOffset = (eye == Eyes.Left ? -1.0f : 1.0f) * halfEyeSeparation * baseProjectionX / convergenceDistance;
            var zScale = far / (near - far);
            var zOffset = near * far / (near - far);

            projection = new Matrix(baseProjectionX, 0, 0, 0,
                                    0, baseProjectionY, 0, 0,
                                    horizontalOffset, projectionYOffset, zScale, -1,
                                    0, 0, zOffset, 0);

            // Move the camera to the selected eye, then build the usual inverse camera transform.
            var eyeLocal = new Vector3((eye == Eyes.Left ? -halfEyeSeparation : halfEyeSeparation) * ViewScaling, 0.0f, 0.0f);
            Vector3 eyeWorld;
            Matrix fullRotation;
            var headRotationMatrix = ignoreHeadRotation ? Matrix.Identity : Matrix.RotationQuaternion(HeadRotation);
            Matrix.Multiply(ref headRotationMatrix, ref cameraRotation, out fullRotation);
            Vector3.TransformCoordinate(ref eyeLocal, ref fullRotation, out eyeWorld);
            var pos = cameraPosition + eyeWorld;

            Matrix.Transpose(ref fullRotation, out view);
            Vector3.TransformCoordinate(ref pos, ref view, out pos);
            view.TranslationVector = -pos;
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
