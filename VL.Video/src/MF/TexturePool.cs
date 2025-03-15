﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using VL.Lib.Basics.Imaging;
using VL.Lib.Basics.Video;

using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi.Common;

namespace VL.Video.MF
{
    /// <summary>
    /// Texture pooling mechanism used by the video player. Each texture gets wrapped in a video frame which allows to keep the texture alive through ref counting.
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    internal sealed unsafe class TexturePool : IDisposable
    {
        private readonly Stack<VideoTexture> pool = new();
        private readonly ID3D11Device* _device;
        private readonly D3D11_TEXTURE2D_DESC _description;

        private bool isDisposed;

        public TexturePool(ID3D11Device* device, D3D11_TEXTURE2D_DESC description)
        {
            _device = device;
            _description = description;
            _device->AddRef();
        }

        public VideoTexture Rent()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                    return pool.Pop();

                ID3D11Texture2D* texture2D;
                _device->CreateTexture2D(_description, null, &texture2D);
                return new VideoTexture(new IntPtr(texture2D), (int)_description.Width, (int)_description.Height, ToPixelFormat(_description.Format));
            }
        }

        public void Return(VideoTexture texture)
        {
            lock (pool)
            {
                if (isDisposed || pool.Count > 16)
                {
                    ((ID3D11Texture2D*)texture.NativePointer)->Release();
                }
                else
                {

                    pool.Push(texture);

                }
            }
        }

        private void Recycle()
        {
            lock (pool)
            {
                foreach (var t in pool)
                {
                    ((ID3D11Texture2D*)t.NativePointer)->Release();
                }
                pool.Clear();
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Recycle();
                _device->Release();
            }
        }

        static PixelFormat ToPixelFormat(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
                    return PixelFormat.B8G8R8A8;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
