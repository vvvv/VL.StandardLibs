using ImGuiNET;
using System;
using System.Collections.Generic;
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


    public class Context : IDisposable
    {
        [ThreadStatic]
        public static int WidgetCreationCounter;

        [ThreadStatic]
        static Dictionary<object, string> Labels = new Dictionary<object, string>();

        private readonly IntPtr _context;
        private readonly List<Widget> _widgetsToReset = new List<Widget>();

        [ThreadStatic]
        internal static Context? Current = null;

        internal ImDrawListPtr DrawListPtr;
        internal DrawList DrawList;
        internal System.Numerics.Vector2 DrawListOffset;

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

        internal static string GetLabel(object widget, string? label)
        {
            if (!string.IsNullOrWhiteSpace(label))
                return label;

            if (Labels is null)
                Labels = new Dictionary<object, string>();

            if (Labels.TryGetValue(widget, out label))
                return label;

            label = $"##__<{++WidgetCreationCounter}>";
            Labels.Add(widget, label);
            return label;
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
