using VL.Lib.Basics.Resources;

namespace VL.Video.MediaFoundation
{
    sealed class VideoFrameRefCounter : IRefCounter<VideoFrame>
    {
        public static readonly VideoFrameRefCounter Default = new VideoFrameRefCounter();

        public void Init(VideoFrame resource)
        {
        }

        public void AddRef(VideoFrame resource)
        {
            resource?.AddRef();
        }

        public void Release(VideoFrame resource)
        {
            resource?.Release();
        }
    }
}
