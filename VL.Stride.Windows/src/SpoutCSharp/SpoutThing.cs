using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using Stride.Graphics;
using Microsoft.Win32;  // for accessing the registry 

namespace VL.Stride.Spout
{
    public abstract class SpoutThing : IDisposable
    {
        protected const string SenderNamesMMF = "SpoutSenderNames";
        protected const string ActiveSenderMMF = "ActiveSenderName";
        protected const int SpoutWaitTimeout = 100;
        protected const int MaxSendersDefault = 64;
        public const int SenderNameLength = 256;

        protected MemoryMappedFile SenderDescriptionMap;
        protected MemoryMappedFile SenderNamesMap;
        protected MemoryMappedFile ActiveSenderMap;
        protected Texture frame;
        protected TextureDesc textureDesc;
        protected string senderName;

        public int MaxSenders { get; private set; } = MaxSendersDefault;

        public Texture Frame
        {
            get { return frame; }
        }
        
        public virtual string SenderName
        {
            get { return senderName; }
            set { senderName = value; }
        }

        public virtual void Dispose()
        {
            if (SenderDescriptionMap != null)
                SenderDescriptionMap.Dispose();

            if (SenderNamesMap != null)
                SenderNamesMap.Dispose();

            if (ActiveSenderMap != null)
                ActiveSenderMap.Dispose();
        }

        public List<string> GetSenderNames()
        {
            int len = MaxSenders * SenderNameLength;

            List<string> namesList = new List<string>();
            StringBuilder name = new StringBuilder();

            //Read the memory mapped file in to a byte array

            using (var mmvs = SenderNamesMap.CreateViewStream())
            {
                var b = new byte[len];
                mmvs.Read(b, 0, len);

                //split into strings searching for the nulls 
                for (int i = 0; i < len; i++)
                {
                    if (b[i] == 0)
                    {
                        if (name.Length == 0)
                        {
                            i += SenderNameLength - (i % SenderNameLength) - 1;
                            continue;
                        }
                        namesList.Add(name.ToString());
                        name.Clear();
                    }
                    else
                        name.Append((char)b[i]);
                }
            }

            return namesList;
        }

        protected byte[] GetNameBytes(string name)
        {
            byte[] b = new byte[SenderNameLength];
            byte[] nameBytes = Encoding.ASCII.GetBytes(name);
            Array.Copy(nameBytes, b, nameBytes.Length);
            return b;
        }

        // see https://github.com/vvvv/vvvv-sdk/blob/develop/vvvv45/src/nodes/plugins/System/SpoutSender.cs#L37
        protected void UpdateMaxSenders() 
        {
            MaxSenders = MaxSendersDefault; 

            RegistryKey subkey = Registry.CurrentUser.OpenSubKey("Software\\Leading Edge\\Spout");
            if (subkey != null)
            {
                int m = (int)subkey.GetValue("MaxSenders"); // Get the value
                if (m > 0)
                {
                    MaxSenders = m; // Set the global max senders value
                }
            }
        }


    }
}
