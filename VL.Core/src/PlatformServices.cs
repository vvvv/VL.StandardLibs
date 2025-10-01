namespace VL.Core
{
    public static class PlatformServices
    {
        public static IPlatformServices Default => AppHost.CurrentOrGlobal.Services.GetService<IPlatformServices>();
    }

    public interface IPlatformServices
    {
        IClipboard Clipboard { get; }
        Optional<string> ShowFileDialog(string initialDirectory, string filter = "All files (*.*)|*.*");
        Optional<string> ShowDirectoryDialog(string initialDirectory, string selectedPath);
    }

    public interface IClipboard
    {
        public string GetText();
        public void SetText(string text);
    }

    public static class Clipboard
    {
        public static string GetText() => PlatformServices.Default.Clipboard?.GetText();

        public static void SetText(string text) => PlatformServices.Default.Clipboard?.SetText(text);
    }
}
