using VL.ImGui.Widgets;


namespace VL.ImGui
{
    internal interface IContextWithSkia
    {
        public IntPtr AddLayer(SkiaWidget layer, System.Numerics.Vector2 pos, System.Numerics.Vector2 size);

        public void RemoveLayer(SkiaWidget layer);
    }

    internal sealed class SkiaContext : Context, IContextWithSkia
    {
        public readonly List<SkiaWidget> Layers = new List<SkiaWidget>();

        public IntPtr AddLayer(SkiaWidget layer, System.Numerics.Vector2 pos, System.Numerics.Vector2 size)
        {
            if (Layers.Contains(layer))
            {
                return Layers.IndexOf(layer) + 1;
            }
            else
            {
                Layers.Add(layer);
                return Layers.Count;
            }
        }

        public void RemoveLayer(SkiaWidget layer)
        {
            if (Layers.Contains(layer))
            {
                Layers.Remove(layer);
            }
        }

        public SkiaWidget? GetLayer(IntPtr index) 
        {
            if (Layers.Count >= index)
                return Layers.ElementAt((int)index - 1);
            else
                return null;
        }

        
    }
}
