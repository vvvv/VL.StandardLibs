using System.Runtime.InteropServices;

namespace VL.Stride.Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DispatchArgs
    {
        public DispatchArgs(int x = 1, int y = 1, int z = 1)
        {
            this.TGX = x;
            this.TGY = y;
            this.TGZ = z;
        }

        public int TGX;
        public int TGY;
        public int TGZ;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DrawInstancedArgs
    {
        public DrawInstancedArgs(int vertexCount = 1, int instanceCount = 1, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            this.VertexCount = vertexCount;
            this.InstanceCount = instanceCount;
            this.StartInstanceLocation = startInstanceLocation;
            this.StartVertexLocation = startVertexLocation;
        }

        public int VertexCount;
        public int InstanceCount;
        public int StartVertexLocation;
        public int StartInstanceLocation;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DrawIndexedInstancedArgs
    {
        public DrawIndexedInstancedArgs(int indicesCount = 1, int instanceCount = 1, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            this.IndicesCount = indicesCount;
            this.InstanceCount = instanceCount;
            this.StartIndexLocation = startIndexLocation;
            this.BaseVertexLocation = baseVertexLocation;
            this.StartInstanceLocation = startInstanceLocation;
        }

        public int IndicesCount;
        public int InstanceCount;
        public int StartIndexLocation;
        public int BaseVertexLocation;
        public int StartInstanceLocation;
    }
}
