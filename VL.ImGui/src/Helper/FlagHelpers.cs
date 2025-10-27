using ImGuiNET;

namespace VL.ImGui;

internal static class FlagHelpers
{
    public const ImGuiHoveredFlags DelayMask = ImGuiHoveredFlags.DelayNone | ImGuiHoveredFlags.DelayShort | ImGuiHoveredFlags.DelayNormal | ImGuiHoveredFlags.NoSharedDelay;
    public const ImGuiHoveredFlags AllowedMaskForIsWindowHovered = ImGuiHoveredFlags.ChildWindows | ImGuiHoveredFlags.RootWindow | ImGuiHoveredFlags.AnyWindow | ImGuiHoveredFlags.NoPopupHierarchy | ImGuiHoveredFlags.DockHierarchy | ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.ForTooltip | ImGuiHoveredFlags.Stationary;
    public const ImGuiHoveredFlags AllowedMaskForIsItemHovered = ImGuiHoveredFlags.AllowWhenBlockedByPopup | ImGuiHoveredFlags.AllowWhenBlockedByActiveItem | ImGuiHoveredFlags.AllowWhenOverlapped | ImGuiHoveredFlags.AllowWhenDisabled | ImGuiHoveredFlags.NoNavOverride | ImGuiHoveredFlags.ForTooltip | ImGuiHoveredFlags.Stationary | DelayMask;

    public static ImGuiHoveredFlags ForWindowHovered(this ImGuiHoveredFlags flags) => flags & AllowedMaskForIsWindowHovered;
    public static ImGuiHoveredFlags ForItemHovered(this ImGuiHoveredFlags flags) => flags & AllowedMaskForIsItemHovered;
}
