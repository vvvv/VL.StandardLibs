#nullable enable
using System.Windows.Forms;
using VL.Core.Windowing;

namespace VL.Core.Windows;

/// <summary>
/// The Windows Forms side of a window of the window contract. Used by the node <c>Form (Window)</c> of VL.CoreLib.Windows.vl.
/// </summary>
public static class WindowsFormsWindow
{
    /// <summary>The window as a Windows Forms <see cref="Form"/> when it is one (the windows of <see cref="WinFormsAppWindowService"/> are), else null.</summary>
    public static Form? AsForm(IAppWindow? input) => input as Form;
}
