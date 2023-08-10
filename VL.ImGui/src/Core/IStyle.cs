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
}
