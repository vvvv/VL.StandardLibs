// Works only in Immediate mode

using ImGuiNET;
using VL.Core.Utils;
using VL.Lib.Collections;

namespace VL.ImGui.Widgets
{
    /// <summary>
    /// Get latest sort specs for the table.
    /// </summary>
    [GenerateNode(Category = "ImGui.Queries", IsStylable = false)]
    internal partial class TableGetSortSpecs : Query
    {
        public Spread<TableColumnSortSpecs> Value { get; private set; } = Spread<TableColumnSortSpecs>.Empty;

        internal override unsafe void UpdateCore(Context context)
        {
            var specs = ImGuiNET.ImGui.TableGetSortSpecs();

            // Null in case the table doesn't have the `Sortable` flag
            if (specs.NativePtr != null)
            {
                // The builder will take care of re-using the existing spread if the specs didn't change
                var builder = CollectionBuilders.GetBuilder(Value, specs.SpecsCount);
                var x = new ReadOnlySpan<ImGuiTableColumnSortSpecs>(specs.Specs, specs.SpecsCount);
                foreach (var s in x)
                    builder.Add(new TableColumnSortSpecs(s.ColumnUserID, s.ColumnIndex, s.SortOrder, s.SortDirection));
                Value = builder.Commit();
            }
            else
            {
                Value = Spread<TableColumnSortSpecs>.Empty;
            }
        }
    }

    public record struct TableColumnSortSpecs(uint ColumnUserID, short ColumnIndex, short SortOrder, ImGuiSortDirection SortDirection);
}
