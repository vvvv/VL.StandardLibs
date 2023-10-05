#nullable enable

namespace VL.Video
{
    public enum ErrorState
    {
        /// <summary>No error.</summary>
        NoError = 0,
        /// <summary>The process of fetching the media resource was stopped at the user's request.</summary>
        Aborted = 1,
        /// <summary>A network error occurred while fetching the media resource.</summary>
        Network = 2,
        /// <summary>An error occurred while decoding the media resource.</summary>
        Decode = 3,
        /// <summary>The media resource is not supported.</summary>
        SrcNotSupported = 4,
        /// <summary>
        /// <para>An error occurred while encrypting the media resource. Supported in Windows 8.1 and later.</para>
        /// <para><see href="https://docs.microsoft.com/windows/win32/api/mfmediaengine/ne-mfmediaengine-mf_media_engine_err#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        Encrypted = 5
    }
}
