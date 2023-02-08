namespace VL.ImGui.Editors
{
    public interface IObjectEditor
    {
        bool NeedsMoreThanOneLine => false;

        void Draw(Context? context);
    }
}
