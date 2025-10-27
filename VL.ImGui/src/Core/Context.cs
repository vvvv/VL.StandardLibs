﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
        public string? LabelWithoutHash = "Rumpelstilzchen";
        public string LabelForImGUI = "Rumpelstilzchen##666";

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
                ComputeLabelForImGui(label);
            }
        }

        public override string ToString() => $"Label: {Label}; LabelForImGui: {LabelForImGUI}";

        internal void ComputeLabelForImGui(string? label)
        {
            var i = label != null ? label.IndexOf("##", StringComparison.Ordinal) : -1;
            if (i >= 0)
            {
                LabelWithoutHash = label!.Substring(0, i);
                LabelForImGUI = label;
            }
            else
            {
                LabelWithoutHash = label;
                LabelForImGUI = $"{label}##__<{Id}>";
            }
        }

        public string Update(string? label)
        {
            Label = label;
            return LabelForImGUI;
        }

        public void DrawLabelInSameLine()
        {
            if (string.IsNullOrEmpty(LabelWithoutHash))
                return;

            ImGui.SameLine();
            ImGui.TextUnformatted(LabelWithoutHash);
        }
    }


    public class Context : IDisposable
    {
        private readonly IntPtr _context;
        private readonly List<Widget> _widgetsToReset = new List<Widget>();
        private bool _disposed = false;

        [ThreadStatic]
        internal static Context? Current = null;

        internal ImDrawListPtr DrawListPtr;
        internal DrawList DrawList;
        internal System.Numerics.Vector2 DrawListOffset;
        internal bool IsInBeginTables;
        internal ImFontPtr DefaultFont;

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

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ImGui.DestroyContext(_context);
            }

            _disposed = true;
        }

        internal readonly Dictionary<string, ImFontPtr> Fonts = new Dictionary<string, ImFontPtr>();

        internal ItemState? CapturedItemState { get; set; }

        /// <summary>
        /// Captures current item state (IsClicked, IsHovered, etc.) and sets it for subsequent queries after leaving the using block.
        /// The captured state will be unset by all widgets except query widgets (determined by <see cref="Widget.HasItemState"/>).
        /// </summary>
        internal ItemStateFrame CaptureItemState()
        {
            CapturedItemState = default;
            return new ItemStateFrame(this);
        }

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

        internal record struct ItemState(
            bool IsActivated,
            bool IsActive,
            bool IsLeftClicked,
            bool IsMiddleClicked,
            bool IsRightClicked,
            bool IsDeactived,
            bool IsDeactivedAfterEdit,
            bool IsEdited,
            bool IsFocused,
            bool IsHovered,
            bool IsToggledOpen,
            bool IsVisible);

        internal readonly struct ItemStateFrame : IDisposable
        {
            private readonly Context context;
            private readonly ItemState itemState;

            public ItemStateFrame(Context context)
            {
                this.context = context;
                this.itemState = new ItemState(
                    ImGui.IsItemActivated(),
                    ImGui.IsItemActive(),
                    ImGui.IsItemClicked(ImGuiMouseButton.Left),
                    ImGui.IsItemClicked(ImGuiMouseButton.Middle),
                    ImGui.IsItemClicked(ImGuiMouseButton.Right),
                    ImGui.IsItemDeactivated(),
                    ImGui.IsItemDeactivatedAfterEdit(),
                    ImGui.IsItemEdited(),
                    ImGui.IsItemFocused(),
                    ImGui.IsItemHovered(),
                    ImGui.IsItemToggledOpen(),
                    ImGui.IsItemVisible());
            }

            public void Dispose()
            {
                this.context.CapturedItemState = itemState;
            }
        }

        /// <summary>
        /// Applies the style on the ImGui context. Intended to be called by a using statement.
        /// </summary>
        /// <example>
        /// <code>
        /// using (context.ApplyStyle(style)) { ... }
        /// </code>
        /// </example>
        /// <returns>A disposable which removes the style on dispose.</returns>
        public StyleFrame ApplyStyle(IStyle? style, bool beforeNewFrame = false)
        {
            return new StyleFrame(this, style, beforeNewFrame);
        }

        public readonly struct StyleFrame : IDisposable
        {
            private readonly Context context;
            private readonly IStyle? style;

            public StyleFrame(Context context, IStyle? style, bool beforeNewFrame = false)
            {
                this.context = context;
                this.style = style;

                style?.Set(context);
            }

            public void Dispose()
            {
                style?.Reset(context);
            }
        }
    }
}
