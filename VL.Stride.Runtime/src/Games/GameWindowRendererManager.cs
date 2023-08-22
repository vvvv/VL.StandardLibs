// Similiar code as in GraphicsDeviceManager
// This class should be kept internal
// There's a PR trying to solve the code duplication https://github.com/stride3d/stride/pull/923

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;

namespace VL.Stride.Games
{
    /// <summary>
    /// Manages the <see cref="GraphicsDevice"/> lifecycle.
    /// </summary>
    public class GameWindowRendererManager : ComponentBase
    {
        #region Fields

        /// <summary>
        /// Default width for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 1280;

        /// <summary>
        /// Default height for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferHeight = 720;

        private GameWindowRenderer windowRenderer;
        private GameWindow window;
        private readonly object lockDeviceCreation;

        private bool deviceSettingsChanged;

        private bool isFullScreen;

        private MultisampleCount preferredMultisampleCount;

        private PixelFormat preferredBackBufferFormat;

        private int preferredBackBufferHeight;

        private int preferredBackBufferWidth;

        private Rational preferredRefreshRate;

        private PixelFormat preferredDepthStencilFormat;

        private DisplayOrientation supportedOrientations;

        private bool synchronizeWithVerticalRetrace;

        private int preferredFullScreenOutputIndex;

        private bool isChangingDevice;

        private int resizedBackBufferWidth;

        private int resizedBackBufferHeight;

        private bool isBackBufferToResize = false;

        private DisplayOrientation currentWindowOrientation;

        private IGraphicsDeviceFactory graphicsDeviceFactory;

        private bool isReallyFullScreen;

        private ColorSpace preferredColorSpace;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceManager" /> class.
        /// </summary>
        internal GameWindowRendererManager()
        {
            lockDeviceCreation = new object();

            // Set defaults
            PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
            PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            PreferredBackBufferWidth = 1280;
            PreferredBackBufferHeight = 720;
            PreferredRefreshRate = new Rational(60, 1);
            PreferredMultisampleCount = MultisampleCount.None;
            PreferredGraphicsProfile = new[]
                {
                    GraphicsProfile.Level_11_1,
                    GraphicsProfile.Level_11_0,
                    GraphicsProfile.Level_10_1,
                    GraphicsProfile.Level_10_0,
                    GraphicsProfile.Level_9_3,
                    GraphicsProfile.Level_9_2,
                    GraphicsProfile.Level_9_1,
                };

        }

        internal void Initialize(GameWindowRenderer windowRenderer, GraphicsDevice graphicsDevice, IGraphicsDeviceFactory graphicsDeviceFactory)
        {
            GraphicsDevice = graphicsDevice;
            this.graphicsDeviceFactory = graphicsDeviceFactory;
            if (graphicsDeviceFactory == null)
            {
                throw new InvalidOperationException("IGraphicsDeviceFactory is not registered as a service");
            }
            this.windowRenderer = windowRenderer;
            this.window = windowRenderer.Window;

            window.ClientSizeChanged += Window_ClientSizeChanged;
            window.OrientationChanged += Window_OrientationChanged;
            window.FullscreenChanged += Window_FullscreenChanged;
        }

        #endregion

        #region Public Events

        public event EventHandler<EventArgs> DeviceCreated;

        public event EventHandler<EventArgs> DeviceDisposing;

        public event EventHandler<EventArgs> DeviceReset;

        public event EventHandler<EventArgs> DeviceResetting;

        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        #endregion

        #region Public Properties

        public GraphicsDevice GraphicsDevice { get; internal set; }

        /// <summary>
        /// Gets or sets the list of graphics profile to select from the best feature to the lower feature. See remarks.
        /// </summary>
        /// <value>The graphics profile.</value>
        /// <remarks>
        /// By default, the PreferredGraphicsProfile is set to { <see cref="GraphicsProfile.Level_11_1"/>, 
        /// <see cref="GraphicsProfile.Level_11_0"/>,
        /// <see cref="GraphicsProfile.Level_10_1"/>,
        /// <see cref="GraphicsProfile.Level_10_0"/>,
        /// <see cref="GraphicsProfile.Level_9_3"/>,
        /// <see cref="GraphicsProfile.Level_9_2"/>,
        /// <see cref="GraphicsProfile.Level_9_1"/>}
        /// </remarks>
        public GraphicsProfile[] PreferredGraphicsProfile { get; set; }

        /// <summary>
        /// Gets or sets the shader graphics profile that will be used to compile shaders. See remarks.
        /// </summary>
        /// <value>The shader graphics profile.</value>
        /// <remarks>If this property is not set, the profile used to compile the shader will be taken from the <see cref="GraphicsDevice"/> 
        /// based on the list provided by <see cref="PreferredGraphicsProfile"/></remarks>
        public GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        /// Gets or sets the device creation flags that will be used to create the <see cref="GraphicsDevice"/>
        /// </summary>
        /// <value>The device creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags { get; set; }

        /// <summary>
        /// If populated the engine will try to initialize the device with the same unique id
        /// </summary>
        public string RequiredAdapterUid { get; set; }

        /// <summary>
        /// Gets or sets the default color space.
        /// </summary>
        /// <value>The default color space.</value>
        public ColorSpace PreferredColorSpace
        {
            get
            {
                return preferredColorSpace;
            }
            set
            {
                if (preferredColorSpace != value)
                {
                    preferredColorSpace = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Sets the preferred graphics profile.
        /// </summary>
        /// <param name="levels">The levels.</param>
        /// <seealso cref="PreferredGraphicsProfile"/>
        public void SetPreferredGraphicsProfile(params GraphicsProfile[] levels)
        {
            PreferredGraphicsProfile = levels;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is full screen.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        public bool IsFullScreen
        {
            get
            {
                return isFullScreen;
            }

            set
            {
                if (isFullScreen == value) return;

                isFullScreen = value;
                deviceSettingsChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prefer multi sampling].
        /// </summary>
        /// <value><c>true</c> if [prefer multi sampling]; otherwise, <c>false</c>.</value>
        public MultisampleCount PreferredMultisampleCount
        {
            get
            {
                return preferredMultisampleCount;
            }

            set
            {
                if (preferredMultisampleCount != value)
                {
                    preferredMultisampleCount = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get
            {
                return preferredBackBufferFormat;
            }

            set
            {
                if (preferredBackBufferFormat != value)
                {
                    preferredBackBufferFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get
            {
                return preferredBackBufferHeight;
            }

            set
            {
                if (preferredBackBufferHeight != value)
                {
                    preferredBackBufferHeight = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get
            {
                return preferredBackBufferWidth;
            }

            set
            {
                if (preferredBackBufferWidth != value)
                {
                    preferredBackBufferWidth = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get
            {
                return preferredDepthStencilFormat;
            }

            set
            {
                if (preferredDepthStencilFormat != value)
                {
                    preferredDepthStencilFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred refresh rate.
        /// </summary>
        /// <value>The preferred refresh rate.</value>
        public Rational PreferredRefreshRate
        {
            get
            {
                return preferredRefreshRate;
            }

            set
            {
                if (preferredRefreshRate != value)
                {
                    preferredRefreshRate = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex
        {
            get
            {
                return preferredFullScreenOutputIndex;
            }

            set
            {
                if (preferredFullScreenOutputIndex != value)
                {
                    preferredFullScreenOutputIndex = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the supported orientations.
        /// </summary>
        /// <value>The supported orientations.</value>
        public DisplayOrientation SupportedOrientations
        {
            get
            {
                return supportedOrientations;
            }

            set
            {
                if (supportedOrientations != value)
                {
                    supportedOrientations = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [synchronize with vertical retrace].
        /// </summary>
        /// <value><c>true</c> if [synchronize with vertical retrace]; otherwise, <c>false</c>.</value>
        public bool SynchronizeWithVerticalRetrace
        {
            get
            {
                return synchronizeWithVerticalRetrace;
            }
            set
            {
                if (synchronizeWithVerticalRetrace != value)
                {
                    synchronizeWithVerticalRetrace = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Applies the changes from this instance and change or create the <see cref="GraphicsDevice"/> according to the new values.
        /// </summary>
        public void ApplyRequestedChanges()
        {
            if (GraphicsDevice != null && deviceSettingsChanged)
            {
                ChangeOrCreateDevice(false);
            }
        }

        #endregion

        protected static DisplayOrientation SelectOrientation(DisplayOrientation orientation, int width, int height, bool allowLandscapeLeftAndRight)
        {
            if (orientation != DisplayOrientation.Default)
            {
                return orientation;
            }

            if (width <= height)
            {
                return DisplayOrientation.Portrait;
            }

            if (allowLandscapeLeftAndRight)
            {
                return DisplayOrientation.LandscapeRight | DisplayOrientation.LandscapeLeft;
            }

            return DisplayOrientation.LandscapeRight;
        }

        protected override void Destroy()
        {
            if (window != null)
            {
                window.ClientSizeChanged -= Window_ClientSizeChanged;
                window.OrientationChanged -= Window_OrientationChanged;
            }

            base.Destroy();
        }

        /// <summary>
        /// Determines whether this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>.
        /// </summary>
        /// <param name="newDeviceInfo">The new device info.</param>
        /// <returns><c>true</c> if this instance this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
        {
            // By default, a reset is compatible when we stay under the same graphics profile.
            return GraphicsDevice.Features.RequestedProfile == newDeviceInfo.GraphicsProfile;
        }

        /// <summary>
        /// Finds the best device that is compatible with the preferences defined in this instance.
        /// </summary>
        /// <param name="anySuitableDevice">if set to <c>true</c> a device can be selected from any existing adapters, otherwise, it will select only from default adapter.</param>
        /// <returns>The graphics device information.</returns>
        public virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
        {
            var preferredParameters = SetupPreferredParameters();

            var devices = graphicsDeviceFactory.FindBestDevices(preferredParameters);
            if (devices.Count == 0)
            {
                // Nothing was found; first, let's check if graphics profile was actually supported
                // Note: we don't do this preemptively because in some cases it seems to take lot of time (happened on a test machine, several seconds freeze on ID3D11Device.Release())
                GraphicsProfile availableGraphicsProfile;
                if (!IsPreferredProfileAvailable(preferredParameters.PreferredGraphicsProfile, out availableGraphicsProfile))
                {
                    throw new InvalidOperationException($"Graphics profiles [{string.Join(", ", preferredParameters.PreferredGraphicsProfile)}] are not supported by the device. The highest available profile is [{availableGraphicsProfile}].");
                }

                // Otherwise, there was just no screen mode
                throw new InvalidOperationException("No screen modes found");
            }

            RankDevices(devices);

            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found after ranking");
            }
            return devices[0];
        }

        private GameGraphicsParameters SetupPreferredParameters()
        {          
            // Setup preferred parameters before passing them to the factory
            var preferredParameters = new GameGraphicsParameters
            {
                PreferredBackBufferWidth = PreferredBackBufferWidth,
                PreferredBackBufferHeight = PreferredBackBufferHeight,
                PreferredBackBufferFormat = PreferredBackBufferFormat,
                PreferredDepthStencilFormat = PreferredDepthStencilFormat,
                PreferredRefreshRate = PreferredRefreshRate,
                PreferredFullScreenOutputIndex = PreferredFullScreenOutputIndex,
                IsFullScreen = IsFullScreen,
                PreferredMultisampleCount = PreferredMultisampleCount,
                SynchronizeWithVerticalRetrace = SynchronizeWithVerticalRetrace,
                PreferredGraphicsProfile = (GraphicsProfile[])PreferredGraphicsProfile.Clone(),
                ColorSpace = PreferredColorSpace,
                RequiredAdapterUid = RequiredAdapterUid,
            };

            // Remap to Srgb backbuffer if necessary
            if (PreferredColorSpace == ColorSpace.Linear)
            {
                // If the device support SRgb and ColorSpace is linear, we use automatically a SRgb backbuffer
                if (preferredParameters.PreferredBackBufferFormat == PixelFormat.R8G8B8A8_UNorm)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                }
                else if (preferredParameters.PreferredBackBufferFormat == PixelFormat.B8G8R8A8_UNorm)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                }
            }
            else
            {
                // If we are looking for gamma and the backbuffer format is SRgb, switch back to non srgb
                if (preferredParameters.PreferredBackBufferFormat == PixelFormat.R8G8B8A8_UNorm_SRgb)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
                }
                else if (preferredParameters.PreferredBackBufferFormat == PixelFormat.B8G8R8A8_UNorm_SRgb)
                {
                    preferredParameters.PreferredBackBufferFormat = PixelFormat.B8G8R8A8_UNorm;
                }
            }

            // Setup resized value if there is a resize pending
            if (isBackBufferToResize)
            {
                preferredParameters.PreferredBackBufferWidth = resizedBackBufferWidth;
                preferredParameters.PreferredBackBufferHeight = resizedBackBufferHeight;
            }

            return preferredParameters;
        }

        private static float GetIntersectionSize(GraphicsOutput output, Rectangle windowBounds)
        {
            var desktopBounds = ApplyDesktopBoundsFix(output.DesktopBounds);
            var intersection = Rectangle.Intersect(windowBounds, desktopBounds);
            return intersection.Width * intersection.Height;
        }

        private static Rectangle ApplyDesktopBoundsFix(Rectangle bounds)
        {
            //fix bounds, bug in SharpDX?
            return new Rectangle(bounds.X, bounds.Y, bounds.Width - bounds.X, bounds.Height - bounds.Y);
        }

        /// <summary>
        /// Ranks a list of <see cref="GraphicsDeviceInformation"/> before creating a new device.
        /// </summary>
        /// <param name="foundDevices">The list of devices that can be reorder.</param>
        protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            // Don't sort if there is a single device (mostly for XAML/WP8)
            if (foundDevices.Count == 1)
            {
                return;
            }

            foundDevices.Sort(
                (left, right) =>
                {
                    var leftParams = left.PresentationParameters;
                    var rightParams = right.PresentationParameters;

                    var leftAdapter = left.Adapter;
                    var rightAdapter = right.Adapter;

                    // Sort by GraphicsProfile
                    if (left.GraphicsProfile != right.GraphicsProfile)
                    {
                        return left.GraphicsProfile <= right.GraphicsProfile ? 1 : -1;
                    }

                    // Sort by FullScreen mode
                    if (leftParams.IsFullScreen != rightParams.IsFullScreen)
                    {
                        return IsFullScreen != leftParams.IsFullScreen ? 1 : -1;
                    }

                    // Sort by BackBufferFormat
                    int leftFormat = CalculateRankForFormat(leftParams.BackBufferFormat);
                    int rightFormat = CalculateRankForFormat(rightParams.BackBufferFormat);
                    if (leftFormat != rightFormat)
                    {
                        return leftFormat >= rightFormat ? 1 : -1;
                    }

                    // Sort by MultisampleCount
                    if (leftParams.MultisampleCount != rightParams.MultisampleCount)
                    {
                        return leftParams.MultisampleCount <= rightParams.MultisampleCount ? 1 : -1;
                    }

                    // Sort by AspectRatio
                    var targetAspectRatio = (PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0) ? (float)DefaultBackBufferWidth / DefaultBackBufferHeight : (float)PreferredBackBufferWidth / PreferredBackBufferHeight;
                    var leftDiffRatio = Math.Abs(((float)leftParams.BackBufferWidth / leftParams.BackBufferHeight) - targetAspectRatio);
                    var rightDiffRatio = Math.Abs(((float)rightParams.BackBufferWidth / rightParams.BackBufferHeight) - targetAspectRatio);
                    if (Math.Abs(leftDiffRatio - rightDiffRatio) > 0.2f)
                    {
                        return leftDiffRatio >= rightDiffRatio ? 1 : -1;
                    }

                    // Sort by PixelCount
                    int leftPixelCount;
                    int rightPixelCount;
                    if (IsFullScreen)
                    {
                        if (((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)) &&
                            PreferredFullScreenOutputIndex < leftAdapter.Outputs.Length &&
                            PreferredFullScreenOutputIndex < rightAdapter.Outputs.Length)
                        {
                            // assume we got here only adapters that have the needed number of outputs:
                            var leftOutput = leftAdapter.Outputs[PreferredFullScreenOutputIndex];
                            var rightOutput = rightAdapter.Outputs[PreferredFullScreenOutputIndex];

                            leftPixelCount = leftOutput.CurrentDisplayMode.Width * leftOutput.CurrentDisplayMode.Height;
                            rightPixelCount = rightOutput.CurrentDisplayMode.Width * rightOutput.CurrentDisplayMode.Height;
                        }
                        else
                        {
                            leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                        }
                    }
                    else if ((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0))
                    {
                        leftPixelCount = rightPixelCount = DefaultBackBufferWidth * DefaultBackBufferHeight;
                    }
                    else
                    {
                        leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                    }

                    int leftDeltaPixelCount = Math.Abs((leftParams.BackBufferWidth * leftParams.BackBufferHeight) - leftPixelCount);
                    int rightDeltaPixelCount = Math.Abs((rightParams.BackBufferWidth * rightParams.BackBufferHeight) - rightPixelCount);
                    if (leftDeltaPixelCount != rightDeltaPixelCount)
                    {
                        return leftDeltaPixelCount >= rightDeltaPixelCount ? 1 : -1;
                    }

                    // Sort by default Adapter, default adapter first
                    if (left.Adapter != right.Adapter)
                    {
                        if (left.Adapter.IsDefaultAdapter)
                        {
                            return -1;
                        }

                        if (right.Adapter.IsDefaultAdapter)
                        {
                            return 1;
                        }
                    }

                    return 0;
                });
        }

        protected virtual bool IsPreferredProfileAvailable(GraphicsProfile[] preferredProfiles, out GraphicsProfile availableProfile)
        {
            availableProfile = GraphicsProfile.Level_9_1;

            var graphicsProfiles = Enum.GetValues(typeof(GraphicsProfile));

            foreach (var graphicsAdapter in GraphicsAdapterFactory.Adapters)
            {
                foreach (var graphicsProfileValue in graphicsProfiles)
                {
                    var graphicsProfile = (GraphicsProfile)graphicsProfileValue;
                    if (graphicsProfile > availableProfile && graphicsAdapter.IsProfileSupported(graphicsProfile))
                        availableProfile = graphicsProfile;
                }
            }

            foreach (var preferredProfile in preferredProfiles)
            {
                if (availableProfile >= preferredProfile)
                    return true;
            }

            return false;
        }

        private int CalculateRankForFormat(PixelFormat format)
        {
            if (format == PreferredBackBufferFormat)
            {
                return 0;
            }

            if (CalculateFormatSize(format) == CalculateFormatSize(PreferredBackBufferFormat))
            {
                return 1;
            }

            return int.MaxValue;
        }

        private int CalculateFormatSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.R10G10B10A2_UNorm:
                    return 32;

                case PixelFormat.B5G6R5_UNorm:
                case PixelFormat.B5G5R5A1_UNorm:
                    return 16;
            }

            return 0;
        }

        protected virtual void OnDeviceCreated(object sender, EventArgs args)
        {
            DeviceCreated?.Invoke(sender, args);
        }

        protected virtual void OnDeviceDisposing(object sender, EventArgs args)
        {
            DeviceDisposing?.Invoke(sender, args);
        }
        
        protected virtual void OnDeviceReset(object sender, EventArgs args)
        {
            DeviceReset?.Invoke(sender, args);
        }
        
        protected virtual void OnDeviceResetting(object sender, EventArgs args)
        {
            DeviceResetting?.Invoke(sender, args);
        }
        
        protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            PreparingDeviceSettings?.Invoke(sender, args);
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            var clientSize = window.ClientBounds.Size;
            if (!isChangingDevice && !window.IsFullscreen && ((clientSize.Height != 0) || (clientSize.Width != 0)))
            {
                resizedBackBufferWidth = clientSize.Width;
                resizedBackBufferHeight = clientSize.Height;

                deviceSettingsChanged = true;
                isBackBufferToResize = true;

                ApplyRequestedChanges();

                Debug.WriteLine("Manager Window Resize To: " + window.ClientBounds);
            }
        }

        private void Window_OrientationChanged(object sender, EventArgs e)
        {
            if ((!isChangingDevice && ((window.ClientBounds.Height != 0) || (window.ClientBounds.Width != 0))) && (window.CurrentOrientation != currentWindowOrientation))
            {
                if ((window.ClientBounds.Height > window.ClientBounds.Width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                    (window.ClientBounds.Width > window.ClientBounds.Height && preferredBackBufferHeight > preferredBackBufferWidth))
                {
                    //Client size and Back Buffer size are different things
                    //in this case all we care is if orientation changed, if so we swap width and height
                    var w = PreferredBackBufferWidth;
                    PreferredBackBufferWidth = PreferredBackBufferHeight;
                    PreferredBackBufferHeight = w;

                    ApplyRequestedChanges();
                }
            }
        }

        private void Window_FullscreenChanged(object sender, EventArgs eventArgs)
        {
            if (sender is GameWindow window)
            {
                IsFullScreen = window.IsFullscreen;
                if (IsFullScreen)
                {
                    var windowBounds = window.ClientBounds;
                    windowBounds.X = window.Position.X;
                    windowBounds.Y = window.Position.Y;

                    var outputs = GraphicsDevice.Adapter.Outputs.ToList();

                    var output = outputs.OrderByDescending(o => GetIntersectionSize(o, windowBounds)).First();

                    PreferredFullScreenOutputIndex = outputs.IndexOf(output);

                    if (window.PreferredFullscreenSize.X > 0)
                        resizedBackBufferWidth = window.PreferredFullscreenSize.X;
                    else if (output.CurrentDisplayMode != null) // Can be null in EyeFinity span setups
                        resizedBackBufferWidth = output.CurrentDisplayMode.Width;
                    else
                        resizedBackBufferWidth = ApplyDesktopBoundsFix(output.DesktopBounds).Width;

                    if (window.PreferredFullscreenSize.Y > 0)
                        resizedBackBufferHeight = window.PreferredFullscreenSize.Y;
                    else if (output.CurrentDisplayMode != null) // Can be null in EyeFinity span setups
                        resizedBackBufferHeight = output.CurrentDisplayMode.Height;
                    else
                        resizedBackBufferHeight = ApplyDesktopBoundsFix(output.DesktopBounds).Height;
                }
                else
                {
                    resizedBackBufferWidth = window.PreferredWindowedSize.X;
                    resizedBackBufferHeight = window.PreferredWindowedSize.Y;
                }

                deviceSettingsChanged = true;
                isBackBufferToResize = true;

                ApplyRequestedChanges();
            }
        }

        private void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        private void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            OnDeviceDisposing(sender, e);
        }

        private void ChangeOrCreateDevice(bool forceCreate)
        {
            // We make sure that we won't be call by an asynchronous event (windows resized)
            lock (lockDeviceCreation)
            {
                using (Profiler.Begin(GraphicsDeviceManagerProfilingKeys.CreateDevice))
                {
                    //game.ConfirmRenderingSettings(GraphicsDevice == null); //if Device is null we assume we are still at game creation phase

                    isChangingDevice = true;
                    var width = window.ClientBounds.Width;
                    var height = window.ClientBounds.Height;

                    //If the orientation is free to be changed from portrait to landscape we actually need this check now, 
                    //it is mostly useful only at initialization actually tho because Window_OrientationChanged does the same logic on runtime change
                    if (window.CurrentOrientation != currentWindowOrientation)
                    {
                        if ((height > width && preferredBackBufferWidth > preferredBackBufferHeight) ||
                            (width > height && preferredBackBufferHeight > preferredBackBufferWidth))
                        {
                            //Client size and Back Buffer size are different things
                            //in this case all we care is if orientation changed, if so we swap width and height
                            (preferredBackBufferWidth, preferredBackBufferHeight) = (preferredBackBufferHeight, preferredBackBufferWidth);
                        }
                    }

                    var isBeginScreenDeviceChange = false;
                    try
                    {
                        // Notifies the game window for the new orientation
                        var orientation = SelectOrientation(supportedOrientations, preferredBackBufferWidth, preferredBackBufferHeight, true);

                        // Desktop doesn't have orientation (unless on Windows 8?)
                        //window.SetSupportedOrientations(orientation);

                        var graphicsDeviceInformation = FindBestDevice(forceCreate);

                        OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(graphicsDeviceInformation));

                        var presentationParameters = graphicsDeviceInformation.PresentationParameters;
                        var presenter = windowRenderer.Presenter;

                        isFullScreen = presentationParameters.IsFullScreen;

                        // In high-DPI cases changing the window size for non-borderless windows doesn't work properly in SDL.
                        // We basically end up in a feedback where the width and height of the window shrink.
                        // In other words calling SDL_SetWindowSize(window, SDL_GetWindowSize(window)) moves and shrinks the window.
                        // Let's therefor break the feedback by resizing the window only when necessary.
                        if (width != presentationParameters.BackBufferWidth || height != presentationParameters.BackBufferHeight || window.IsFullscreen != presentationParameters.IsFullScreen)
                        {
                            window.BeginScreenDeviceChange(presentationParameters.IsFullScreen);
                            isBeginScreenDeviceChange = true;
                        }

                        bool needToCreateNewDevice = true;

                        // If we are not forced to create a new device and this is already an existing GraphicsDevice
                        // try to reset and resize it.
                        if (!forceCreate && GraphicsDevice != null)
                        {
                            if (CanResetDevice(graphicsDeviceInformation))
                            {
                                try
                                {
                                    var newWidth = presentationParameters.BackBufferWidth;
                                    var newHeight = presentationParameters.BackBufferHeight;
                                    var newFormat = presentationParameters.BackBufferFormat;
                                    var newOutputIndex = presentationParameters.PreferredFullScreenOutputIndex;

                                    presenter.Description.PreferredFullScreenOutputIndex = newOutputIndex;
                                    presenter.Description.RefreshRate = presentationParameters.RefreshRate;
                                    presenter.Resize(newWidth, newHeight, newFormat);

                                    // Change full screen if needed
                                    presenter.IsFullScreen = presentationParameters.IsFullScreen && !window.FullscreenIsBorderlessWindow;

                                    needToCreateNewDevice = false;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }

                        // If we still need to create a device, then we need to create it
                        if (needToCreateNewDevice)
                        {
                            //CreateDevice(graphicsDeviceInformation);
                            throw new NotImplementedException("Device creation not yet supported for GameWindowRenderer");
                        }

                        if (GraphicsDevice == null)
                        {
                            throw new InvalidOperationException("Unexpected null GraphicsDevice");
                        }

                        // Make sure to copy back color space to GraphicsDevice
                        GraphicsDevice.ColorSpace = presentationParameters.ColorSpace;

                        presentationParameters = presenter.Description;
                        isReallyFullScreen = presentationParameters.IsFullScreen;
                        if (presentationParameters.BackBufferWidth != 0)
                        {
                            width = presentationParameters.BackBufferWidth;
                        }

                        if (presentationParameters.BackBufferHeight != 0)
                        {
                            height = presentationParameters.BackBufferHeight;
                        }
                        deviceSettingsChanged = false;
                    }
                    finally
                    {
                        if (isBeginScreenDeviceChange)
                        {
                            window.EndScreenDeviceChange(width, height);

                            // Can we live without it?
                            //if (!window.FullscreenIsBorderlessWindow)
                            //    window.SetIsReallyFullscreen(isReallyFullScreen);
                        }

                        currentWindowOrientation = window.CurrentOrientation;
                        isChangingDevice = false;
                    }
                }
            }
        }
    }
}
