using Stride.Core.Diagnostics;

namespace VL.Stride.Rendering
{
    public static class VLProfilerKeys
    {
        public static ProfilingKey BeforeSceneRenderProfilingKey = new ProfilingKey("VL Before Scene Renderer");
        public static ProfilingKey InSceneRenderProfilingKey = new ProfilingKey("VL In Scene Renderer");
        public static ProfilingKey AfterSceneRenderProfilingKey = new ProfilingKey("VL After Scene Renderer");
    }
}
