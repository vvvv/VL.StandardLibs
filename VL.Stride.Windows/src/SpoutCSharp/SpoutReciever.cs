//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.IO.MemoryMappedFiles;
//using System.IO;

//namespace VL.Stride.Spout
//{
//    public class SpoutReciever: SpoutThing 
//    {
//        public SpoutReciever(Game game, GraphicsDevice gfxDev, string senderName)
//            :base(game)
//        {
//            this.senderName = senderName;
//            gfxDevice = gfxDev;
//        }

//        public override void Update(GameTime gameTime)
//        {
//            base.Update(gameTime);
//            bool success;
//            textureDesc = TryGetTextureDesc(out success);
//            if (success)
//            {
//                if (frame == null)
//                    frame = new RenderTarget2D(gfxDevice, (IntPtr)textureDesc.SharedHandle);
//                else
//                {
//                    if (frame.IsDisposed)
//                        return;
//                    if ((uint)frame.GetSharedHandle().ToInt64() != textureDesc.SharedHandle ||  frame.Width != textureDesc.Width || frame.Height != textureDesc.Height)
//                    {
//                        frame.Dispose();
//                        frame = new RenderTarget2D(gfxDevice, (IntPtr)textureDesc.SharedHandle);
//                    }
//                }
//            }
//            else
//            {
//                //frame.Dispose();
//            }
//        }

//        TextureDesc TryGetTextureDesc(out bool success)
//        {
//            TextureDesc desc = new TextureDesc();
//            try
//            {
//                sharedMemory = MemoryMappedFile.OpenExisting(senderName);
//                sharedMemoryStream = sharedMemory.CreateViewStream();
//                desc = new TextureDesc(sharedMemoryStream);
//                success = true;
//            }
//            catch (Exception e)
//            {
//                success = false;
//                Log.Add(e);
//            }
//            return desc;
//        }

//        public override void Initialize()
//        {
//            base.Initialize();
//            bool success;
//            this.textureDesc = TryGetTextureDesc(out success);
//            if (success)
//            {
//                frame = new RenderTarget2D(gfxDevice, (IntPtr)textureDesc.SharedHandle);
//            }
//        }

//    }
//}
