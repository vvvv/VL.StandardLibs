using System;
using VL.Lib.Basics.Resources;

namespace VL.Video.MediaFoundation
{
    public abstract class Converter<TOut> : IDisposable
        where TOut: class
    {
        private readonly Producing<TOut> output = new Producing<TOut>();
        private VideoFrame current;

        public virtual void Dispose()
        {
            output.Dispose();
        }

        public TOut Update(VideoFrame frame)
        {
            if (frame != current)
            {
                this.current = frame;
                output.Resource = frame != null ? Convert(frame) : null;
            }
            return output.Resource;
        }

        protected abstract TOut Convert(VideoFrame frame);
    }
}
