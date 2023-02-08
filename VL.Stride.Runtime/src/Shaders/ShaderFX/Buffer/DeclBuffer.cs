using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;


namespace VL.Stride.Shaders.ShaderFX
{
    public class DeclBuffer : DeclResource<Buffer>
    {
        public DeclBuffer(string resourceGroupName = null) : base(resourceGroupName)
        {
        }
    }
}
