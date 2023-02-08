using System;
using System.Timers;

namespace VL.Sync
{
    public class SyncClient
    {
        object FLock = new object();
        double FReceivedStreamTime;
        double FReceivedTimeStamp;
        int FFrameCounter;
        IIRFilter FStreamDiffFilter;

        public SyncClient()
        {
            FStreamDiffFilter.Thresh = 1;
            FStreamDiffFilter.Alpha = 0.95;
        }

        public void ReceiveServerAnswer(byte[] data, double timeStamp)
        {
            lock (FLock)
            {
                FReceivedStreamTime = BitConverter.ToDouble(data, 0);
                FReceivedTimeStamp = BitConverter.ToDouble(data, 8);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="streamDuration">Duration of the stream being synced</param>
        /// <param name="streamPosition">Current position of the stream being synced</param>
        /// <param name="constantOffset">A deliberate constant offset added to the clients stream time</param>
        /// <param name="seekThreshold">If exceeded, the sync will jump</param>
        /// <param name="seekTime">The time to jump to</param>
        /// <param name="doSeek">It true, the stream should jump to the Seek Time</param>
        /// <param name="adjustTime">The time to adjust the stream in milliseconds</param>
        public void Update(double timeStamp, double streamDuration, double streamPosition, double constantOffset, double seekThreshold, out double seekTime, out bool doSeek, out double adjustTime)
        {
            var fCount = 5;
            lock (FLock)
            {
                var offset = timeStamp - FReceivedTimeStamp;
                var streamDiff = FReceivedStreamTime - streamPosition + offset + constantOffset;
                
                if (Math.Abs(streamDiff) > streamDuration * 0.5) //assuming server or client has looped
                    streamDiff = streamDiff - streamDuration * Math.Sign(streamDiff); //substract or add one duration

                doSeek = Math.Abs(streamDiff) > seekThreshold;

                FStreamDiffFilter.Update(streamDiff);

                seekTime = FReceivedStreamTime + offset + 0.05;

                var doAdjust = Math.Abs(FStreamDiffFilter.Value) > 0.001;

                if (!doSeek && FFrameCounter == 0 && doAdjust)
                {
                    adjustTime = FStreamDiffFilter.Value;
                }
                else
                {
                    adjustTime = 0;
                }

                FFrameCounter = (++FFrameCounter) % fCount;
            }
        }
    }

    public struct IIRFilter
    {
        public double Value;
        public double Alpha;
        public double Thresh;

        /// <summary>
        /// Update filter state. If the difference between 
        /// the current filter value and the new value is greater than 
        /// the threshold then filter value is set to new value.
        /// </summary>
        /// <param name="newValue">Target value</param>
        /// <returns>Filtered target value</returns>
        public double Update(double newValue)
        {
            var diff = Math.Abs(newValue - Value);
            Value = diff > Thresh ? newValue : newValue * (1 - Alpha) + Value * Alpha;
            return Value;
        }
    }
}
