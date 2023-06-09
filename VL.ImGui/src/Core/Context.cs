using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VL.ImGui.Widgets;
using VL.ImGui.Widgets.Primitives;
using VL.Model;

namespace VL.ImGui
{
    using ImGui = ImGuiNET.ImGui;

    public static class ContextHelpers
    {        
        public static Context? Validate(this Context? c) => c ?? Context.Current;
    }

    public struct WidgetLabel
    {
        static int widgetCreationCounter;
        int Id;
        string? label = "Rumpelstilzchen";
        public string? LabelForImGUI = "Rumpelstilzchen##666";

        public WidgetLabel()
        {
            Id = widgetCreationCounter++;
        }

        public string? Label
        {
            get => label;
            set
            {
                if (label == value) return;
                label = value;
                LabelForImGUI = ComputeLabelForImGui(label);
            }
        }

        public override string ToString() => $"Label: {Label}; LabelForImGui: {LabelForImGUI}";

        internal string ComputeLabelForImGui(string? label)
        {
            var autoGenerate = string.IsNullOrWhiteSpace(label) || !label.Contains("##");
            if (!autoGenerate)
                return label!;

            label = label == null ? string.Empty : label;
            label = $"{label}##__<{Id}>";
            return label;
        }

        public string? Update(string? label)
        {
            Label = label;
            return LabelForImGUI;
        }
    }


    public class Context : IDisposable
    {
        private readonly IntPtr _context;
        private readonly List<Widget> _widgetsToReset = new List<Widget>();

        [ThreadStatic]
        internal static Context? Current = null;

        internal ImDrawListPtr DrawListPtr;
        internal DrawList DrawList;
        internal System.Numerics.Vector2 DrawListOffset;
        internal bool IsInBeginTables;

        public Context()
        {
            _context = ImGui.CreateContext();
        }

        public virtual void NewFrame()
        {
            try
            {
                foreach (var widget in _widgetsToReset)
                    widget.Reset();
            }
            finally
            {
                _widgetsToReset.Clear();
                ImGui.NewFrame();
            }
        }

        public Frame MakeCurrent()
        {
            return new Frame(_context, this);
        }

        public void Update(Widget? widget)
        {
            if (widget is null)
                return;

            widget.Update(this);
            _widgetsToReset.Add(widget);
        }

        internal void AddToResetQueue(Widget widget)
        {
            _widgetsToReset.Add(widget);
        }

        internal void SetDrawList(DrawList drawList)
        {
            DrawList = drawList;

            DrawListPtr = drawList switch
            {
                DrawList.AtCursor => ImGui.GetWindowDrawList(),
                DrawList.Window => ImGui.GetWindowDrawList(),
                DrawList.Foreground => ImGui.GetForegroundDrawList(),
                DrawList.Background => ImGui.GetBackgroundDrawList(),
                _ => throw new NotImplementedException()
            };

            DrawListOffset = drawList switch
            {
                DrawList.AtCursor => ImGui.GetWindowPos() + ImGui.GetCursorPos() - new System.Numerics.Vector2(ImGui.GetScrollX(), ImGui.GetScrollY()),
                DrawList.Window => ImGui.GetWindowPos(),
                DrawList.Foreground => default,
                DrawList.Background => default,
                _ => throw new NotImplementedException()
            };             

            // TODO: All points are drawn in the main viewport. In order to have them drawn inside the window without having to transform them manually
            // we should look into the drawList.AddCallback(..., ...) method. It should allow us to modify the transformation matrix and clipping rects.
        }

        public void Dispose()
        {
            ImGui.DestroyContext(_context);
        }

        internal readonly Dictionary<string, ImFontPtr> Fonts = new Dictionary<string, ImFontPtr>();

        public readonly struct Frame : IDisposable
        {
            readonly IntPtr previous;
            readonly Context? previous2;

            public Frame(IntPtr context, Context c)
            {
                previous = ImGui.GetCurrentContext();
                ImGui.SetCurrentContext(context);
                previous2 = Current;
                Current = c;
            }

            public void Dispose()
            {
                Current = previous2;
                ImGui.SetCurrentContext(previous);
            }
        }
    }
}
