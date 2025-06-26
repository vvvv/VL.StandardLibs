using System.ComponentModel;
using NativeImGuiInputTextFlags = ImGuiNET.ImGuiInputTextFlags;

namespace VL.ImGui;

[Flags]
public enum ImGuiInputTextFlags : ulong
{
    None = NativeImGuiInputTextFlags.None,
    CharsDecimal = NativeImGuiInputTextFlags.CharsDecimal,
    CharsHexadecimal = NativeImGuiInputTextFlags.CharsHexadecimal,
    CharsUppercase = NativeImGuiInputTextFlags.CharsUppercase,
    CharsNoBlank = NativeImGuiInputTextFlags.CharsNoBlank,
    AutoSelectAll = NativeImGuiInputTextFlags.AutoSelectAll,
    EnterReturnsTrue = NativeImGuiInputTextFlags.EnterReturnsTrue,
    CallbackCompletion = NativeImGuiInputTextFlags.CallbackCompletion,
    CallbackHistory = NativeImGuiInputTextFlags.CallbackHistory,
    CallbackAlways = NativeImGuiInputTextFlags.CallbackAlways,
    CallbackCharFilter = NativeImGuiInputTextFlags.CallbackCharFilter,
    AllowTabInput = NativeImGuiInputTextFlags.AllowTabInput,
    CtrlEnterForNewLine = NativeImGuiInputTextFlags.CtrlEnterForNewLine,
    NoHorizontalScroll = NativeImGuiInputTextFlags.NoHorizontalScroll,
    AlwaysOverwrite = NativeImGuiInputTextFlags.AlwaysOverwrite,
    ReadOnly = NativeImGuiInputTextFlags.ReadOnly,
    Password = NativeImGuiInputTextFlags.Password,
    NoUndoRedo = NativeImGuiInputTextFlags.NoUndoRedo,
    CharsScientific = NativeImGuiInputTextFlags.CharsScientific,
    CallbackResize = NativeImGuiInputTextFlags.CallbackResize,
    CallbackEdit = NativeImGuiInputTextFlags.CallbackEdit,
    EscapeClearsAll = NativeImGuiInputTextFlags.EscapeClearsAll,

    // Custom flags we use in VL
    ItemDeactivationReturnsTrue = 1UL << 32,
    [Browsable(false)]
    CustomFlagsMask = 0xFFFFFFFF00000000
}

public static class ImGuiTextFlagsExtensions
{
    public static NativeImGuiInputTextFlags ToNative(this ImGuiInputTextFlags flags)
    {
        return (NativeImGuiInputTextFlags)(flags & ~ImGuiInputTextFlags.CustomFlagsMask);
    }
    public static ImGuiInputTextFlags ToVL(this NativeImGuiInputTextFlags flags)
    {
        return (ImGuiInputTextFlags)flags;
    }
}