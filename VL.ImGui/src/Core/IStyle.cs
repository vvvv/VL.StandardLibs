namespace VL.ImGui
{
    public interface IStyle
    {
        /// <summary>
        /// Sets the style on the curent ImGui context.
        /// </summary>
        /// <remarks>
        /// Avoid calling this method directly. Instead use <see cref="Context.ApplyStyle(IStyle?)"/> which enforces a safe usage pattern.
        /// </remarks>
        void Set(Context context);

        /// <summary>
        /// Resets the style on the current ImGui context.
        /// </summary>
        /// <remarks>
        /// Avoid calling this method directly. Instead use <see cref="Context.ApplyStyle(IStyle?)"/> which enforces a safe usage pattern.
        /// </remarks>
        void Reset(Context context);
    }

    // TODO: Could be kept internal (not needed) by exposing a ApplyStyle region
    public static class StyleExtensions
    {
        /// <summary>
        /// Applies the style on the current ImGui context. Intended to be called by a using statement.
        /// </summary>
        /// <example>
        /// <code>
        /// using (style.ApplyStyle()) { ... }
        /// </code>
        /// </example>
        /// <returns>A disposable which removes the style on dispose.</returns>
        public static Context.StyleFrame Apply(this IStyle? style)
        {
            var context = Context.Current;
            if (context != null)
                return context.ApplyStyle(style);
            return default;
        }
    }
}
