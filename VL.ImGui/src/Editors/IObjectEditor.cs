namespace VL.ImGui.Editors
{
    public interface IObjectEditor
    {
        bool NeedsMoreThanOneLine => false;
        bool HasContentToDraw => true;

        void Draw(Context? context);
    }
}
