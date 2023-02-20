using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Stride.Graphics;

namespace VL.Stride.Spout
{
    public class SpoutSender : SpoutThing
    {
        public SpoutSender(string senderName, Texture srcTexture)
            :base()
        {
            SenderName = senderName;
            this.frame = srcTexture;
            textureDesc = new TextureDesc(Frame);
        }

        public void Initialize()
        {

            UpdateMaxSenders();

            long len = SenderNameLength * MaxSenders;

            SenderNamesMap = MemoryMappedFile.CreateOrOpen(SenderNamesMMF, len);

            if (AddNameToSendersList(senderName))
            {
                byte[] desc = textureDesc.ToByteArray();
                SenderDescriptionMap = MemoryMappedFile.CreateOrOpen(SenderName, desc.Length);
                using (var mmvs = SenderDescriptionMap.CreateViewStream())
                {
                    mmvs.Write(desc, 0, desc.Length);
                }

                //If we are the first/only sender, create a new ActiveSenderName map.
                //This is a separate shared memory containing just a sender name
                //that receivers can use to retrieve the current active Sender.
                ActiveSenderMap = MemoryMappedFile.CreateOrOpen(ActiveSenderMMF, SenderNameLength);
                using (var mmvs = ActiveSenderMap.CreateViewStream())
                {
                    var firstByte = mmvs.ReadByte();
                    if (firstByte == 0) //no active sender yet
                    {
                        mmvs.Position = 0;
                        mmvs.Write(GetNameBytes(SenderName), 0, SenderNameLength);
                    }
                }

            }
        }

        bool AddNameToSendersList(string name)
        {
            bool createdNew;
            Mutex mutex = new Mutex(true, SenderNamesMMF + "_mutex", out createdNew);
            if (mutex == null)
                return false;
            bool success = false;
            try
            {
                if (mutex.WaitOne(SpoutWaitTimeout))
                {
                    success = true;
                }
                else
                {
                    success = false;
                }
            }
            catch (AbandonedMutexException)
            {
                success = true;    
            }
            finally
            {
                if (success)
                {
                    List<string> senders = GetSenderNames();
                    if (senders.Contains(this.senderName))
                    {
                        success = false;
                    }
                    else
                    {
                        senders.Add(name);
                        WriteSenderNamesToMMF(senders);
                    }
                }
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
            return success;
        }

        void RemoveNameFromSendersList()
        {
            bool createdNew;
            Mutex mutex = new Mutex(true, SenderNamesMMF + "_mutex", out createdNew);
            if (mutex == null)
                return;
            try
            {
                mutex.WaitOne(SpoutWaitTimeout);
            }
            catch (AbandonedMutexException)
            {
                //Log.Add(e);     
            }
            finally
            {
                List<string> senders = GetSenderNames();
                if (senders.Contains(this.senderName))
                {
                    senders.Remove(senderName);
                    WriteSenderNamesToMMF(senders);
                }
                mutex.ReleaseMutex();
                mutex.Dispose();
            }
        }

        void WriteSenderNamesToMMF(List<string> senders)
        {
            using (var mmvs = SenderNamesMap.CreateViewStream())
            {
                for (int i = 0; i < MaxSenders; i++)
                {
                    byte[] bytes;
                    if (i < senders.Count)
                        bytes = GetNameBytes(senders[i]);
                    else //fill with 0s
                        bytes = new byte[SenderNameLength];

                    mmvs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public override void Dispose()
        {
            UpdateMaxSenders();
            RemoveNameFromSendersList();
            base.Dispose();
        }
    }
}
