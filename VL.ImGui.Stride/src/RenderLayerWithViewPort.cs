﻿using Stride.Input;
using Stride.Rendering;
using Stride.Graphics;

namespace VL.ImGui
{
    internal sealed class RenderLayerWithViewPort : IWithViewport, IDisposable
    {
        private readonly MappedInputSource mappedInputSource;
        private IInputSource? inputSource;

        // Will set from RenderWidget
        public bool HasFocus { get; internal set; }
        public IGraphicsRendererBase? Layer { get; internal set; }
        public RenderView? RenderView { get; internal set; }
        public Viewport Viewport { get; set; } = new Viewport();

        // Will Set from ImGuiRenderer
        public IInputSource? ParentInputSource
        {
            get { return inputSource; }
            set
            {
                if (inputSource != value)
                {
                    if (value == null)
                    {
                        mappedInputSource.Disconnect(inputSource);
                        inputSource = null;
                    }
                    else
                    {
                        inputSource = value;
                        mappedInputSource.Connect(inputSource);
                    }
                }
            }
        }

        // Mapped Inputsource
        public IInputSource? MappedInputSource => HasFocus && mappedInputSource.Devices.Count > 0 ? mappedInputSource : null;
        
        public RenderLayerWithViewPort()
        {
            this.mappedInputSource = new MappedInputSource(this);
        }

        public void Dispose()
        {
            mappedInputSource.Disconnect(inputSource);
            mappedInputSource.Dispose();
        }
    }
}