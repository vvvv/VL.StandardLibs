// DirectShow definitions pulled from the headers - see https://medium.com/home-wireless/how-to-control-a-webcam-using-c-and-mediafoundation-9fcb1a6f0eb8

using System;
using System.Runtime.InteropServices;

namespace VL.Video.MediaFoundation
{
    // See CppSDK\SDK\include\shared\ksmedia.h
    //    typedef enum {
    //        KSPROPERTY_CAMERACONTROL_PAN,                       // RW O
    //        KSPROPERTY_CAMERACONTROL_TILT,                      // RW O
    //        KSPROPERTY_CAMERACONTROL_ROLL,                      // RW O
    //        KSPROPERTY_CAMERACONTROL_ZOOM,                      // RW O
    //        KSPROPERTY_CAMERACONTROL_EXPOSURE,                  // RW O
    //        KSPROPERTY_CAMERACONTROL_IRIS,                      // RW O
    //        KSPROPERTY_CAMERACONTROL_FOCUS                      // RW O
    //    KSPROPERTY_VIDCAP_CAMERACONTROL;

    /// <summary>
    /// The list of camera property settings
    /// </summary>
    internal enum CameraControlPropertyName
    {
        Pan = 0,
        Tilt,
        Roll,
        Zoom,
        Exposure,
        Iris,
        Focus
    }

    /// <summary>
    /// Is the setting automatic?
    /// </summary>
    [Flags]
    enum CameraControlFlags
    {
        None = 0x0,
        Auto = 0x0001,
        Manual = 0x0002
    }

    /// <summary>
    /// The IAMCameraControl interface controls web camera settings such as zoom, pan, aperture adjustment,
    /// or shutter speed. To obtain this interface, cast a MediaSource.
    /// </summary>
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("C6E13370-30AC-11d0-A18C-00A0C9118956"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAMCameraControl
    {
        /// <summary>
        /// Get the range and default value of a specified camera property.
        /// </summary>
        /// <param name="Property">The property to query.</param>
        /// <param name="pMin">The minimum value of the property.</param>
        /// <param name="pMax">The maximum value of the property.</param>
        /// <param name="pSteppingDelta">The step size for the property.</param>
        /// <param name="pDefault">The default value of the property. </param>
        /// <param name="pCapsFlags">Can it be controlled automatically or manually?</param>
        [PreserveSig]
        uint GetRange(
            [In] CameraControlPropertyName Property,
            [Out] out int pMin,
            [Out] out int pMax,
            [Out] out int pSteppingDelta,
            [Out] out int pDefault,
            [Out] out CameraControlFlags pCapsFlags
            );

        /// <summary>
        /// Set a specified property on the camera.
        /// </summary>
        /// <param name="Property">The property to set.</param>
        /// <param name="lValue">The new value of the property.</param>
        /// <param name="Flags">Control it manually or automatically.</param>
        [PreserveSig]
        uint Set(
            [In] CameraControlPropertyName Property,
            [In] int lValue,
            [In] CameraControlFlags Flags
            );

        /// <summary>
        /// Get the current setting of a camera property.
        /// </summary>
        /// <param name="Property">The property to retrieve.</param>
        /// <param name="lValue">The current value of the property.</param>
        /// <param name="Flags">Is it currently manual or automatic?.</param>
        [PreserveSig]
        uint Get(
            [In] CameraControlPropertyName Property,
            [Out] out int lValue,
            [Out] out CameraControlFlags Flags
            );
    }

    /// <summary>
    /// The list of video camera settings
    /// </summary>
    enum VideoProcAmpProperty
    {
        Brightness = 0,
        Contrast,
        Hue,
        Saturation,
        Sharpness,
        Gamma,
        ColorEnable,
        WhiteBalance,
        BacklightCompensation,
        Gain
    }

    /// <summary>
    /// The auto and manual flag
    /// </summary>
    [Flags]
    enum VideoProcAmpFlags
    {
        None = 0x0,
        Auto = 0x0001,
        Manual = 0x0002
    }

    /// <summary>
    /// The IAMVideoProcAmp interface controls video camera settings such as brightness, contrast, hue,
    /// or saturation. To obtain this interface, cast the MediaSource.
    /// </summary>
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
    Guid("C6E13360-30AC-11D0-A18C-00A0C9118956"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAMVideoProcAmp
    {
        /// <summary>
        /// Get the range and default value of a camera property.
        /// </summary>
        /// <param name="Property">The property.</param>
        /// <param name="pMin">The min value.</param>
        /// <param name="pMax">The max value.</param>
        /// <param name="pSteppingDelta">The step size.</param>
        /// <param name="pDefault">The default value.</param>
        /// <param name="pCapsFlags">Shows if it can be controlled automatically and/or manually.</param>
        [PreserveSig]
        uint GetRange(
            [In] VideoProcAmpProperty Property,
            [Out] out int pMin,
            [Out] out int pMax,
            [Out] out int pSteppingDelta,
            [Out] out int pDefault,
            [Out] out VideoProcAmpFlags pCapsFlags
            );

        /// <summary>
        /// Set a specified property on the camera.
        /// </summary>
        /// <param name="Property">The property to set.</param>
        /// <param name="lValue">The new value of the property.</param>
        /// <param name="Flags">The auto or manual setting.</param>
        [PreserveSig]
        uint Set(
            [In] VideoProcAmpProperty Property,
            [In] int lValue,
            [In] VideoProcAmpFlags Flags
            );

        /// <summary>
        /// Get the current setting of a camera property.
        /// </summary>
        /// <param name="Property">The property to retrieve.</param>
        /// <param name="lValue">The current value of the property.</param>
        /// <param name="Flags">Is it manual or automatic?</param>
        [PreserveSig]
        uint Get(
            [In] VideoProcAmpProperty Property,
            [Out] out int lValue,
            [Out] out VideoProcAmpFlags Flags
            );
    }
}