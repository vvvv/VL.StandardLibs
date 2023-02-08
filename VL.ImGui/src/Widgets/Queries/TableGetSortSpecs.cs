// Works only in Immediate mode, fails if the Table doesn't have `Sortable` flag.

using ImGuiNET;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get latest sort specs for the table.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetSortSpecs : Widget
    {
        public Spread<TableColumnSortSpecs> Value { get; private set; } = Spread<TableColumnSortSpecs>.Empty;

        internal override unsafe void UpdateCore(Context context)
        {
            var specs = ImGuiNET.ImGui.TableGetSortSpecs();

            var b = Spread.CreateBuilder<TableColumnSortSpecs>(specs.SpecsCount);
            var x = new ReadOnlySpan<ImGuiTableColumnSortSpecs>(specs.Specs, specs.SpecsCount);
            foreach (var s in x)
                b.Add(new TableColumnSortSpecs(s.ColumnUserID, s.ColumnIndex, s.SortOrder, s.SortDirection));
            Value = b.ToSpread();
        }
    }

    public record TableColumnSortSpecs(uint ColumnUserID, short ColumnIndex, short SortOrder, ImGuiSortDirection SortDirection);
}
