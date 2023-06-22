namespace VL.ImGui
{
    public interface IStyle
    {
        /// <summary>
        /// Sets the style on the curent ImGui context.
        /// </summary>
        /// <remarks>
        /// Avoid calling this method directly. Instead use <see cref="StyleExtensions.Apply(IStyle?)"/> which enforces a safe usage pattern.
        /// </remarks>
        void Set(Context context);

        /// <summary>
        /// Resets the style on the current ImGui context.
        /// </summary>
        /// <remarks>
        /// Avoid calling this method directly. Instead use <see cref="StyleExtensions.Apply(IStyle?)"/> which enforces a safe usage pattern.
        /// </remarks>
        void Reset(Context context);
    }

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
        public static StyleFrame Apply(this IStyle? style)
        {
            var context = Context.Current;
            if (context != null && style != null)
                return new StyleFrame(context, style);
            return default;
        }

        public readonly struct StyleFrame : IDisposable
        {
            private readonly Context context;
            private readonly IStyle style;

            public StyleFrame(Context context, IStyle style)
            {
                this.context = context;
                this.style = style;

                style.Set(context);
            }

            public void Dispose()
            {
                style?.Reset(context);
            }
        }
    }
}
