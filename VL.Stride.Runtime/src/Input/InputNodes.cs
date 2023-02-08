using Stride.Input;

namespace VL.Stride.Input
{
    /// <summary>
    /// A few static methods with null checks for easy usability.
    /// </summary>
    public static class InputNodes
    {
        /// <summary>
        /// Determines whether the specified key is pressed since the previous update.
        /// </summary>
        /// <param name="input">The keyboard</param>
        /// <param name="key">The key</param>
        /// <returns><c>true</c> if the specified key is pressed; otherwise, <c>false</c>.</returns>
        public static bool IsKeyPressed(IKeyboardDevice input, Keys key)
        {
            return input?.IsKeyPressed(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified key is released since the previous update.
        /// </summary>
        /// <param name="input">The keyboard</param>
        /// <param name="key">The key</param>
        /// <returns><c>true</c> if the specified key is released; otherwise, <c>false</c>.</returns>
        public static bool IsKeyReleased(IKeyboardDevice input, Keys key)
        {
            return input?.IsKeyReleased(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified key is being pressed down
        /// </summary>
        /// <param name="input">The keyboard</param>
        /// <param name="key">The key</param>
        /// <returns><c>true</c> if the specified key is being pressed down; otherwise, <c>false</c>.</returns>
        public static bool IsKeyDown(IKeyboardDevice input, Keys key)
        {
            return input?.IsKeyDown(key) ?? false;
        }

        /// <summary>
        /// Determines whether the specified mouse button is pressed since the previous update.
        /// </summary>
        /// <param name="input">The mouse</param>
        /// <param name="mouseButton">The mouse button</param>
        /// <returns><c>true</c> if the specified mouse button is pressed since the previous update; otherwise, <c>false</c>.</returns>
        public static bool IsButtonPressed(IMouseDevice input, MouseButton mouseButton)
        {
            return input?.IsButtonPressed(mouseButton) ?? false;
        }

        /// <summary>
        /// Determines whether the specified mouse button is released.
        /// </summary>
        /// <param name="input">The mouse</param>
        /// <param name="mouseButton">The mouse button</param>
        /// <returns><c>true</c> if the specified mouse button is released; otherwise, <c>false</c>.</returns>
        public static bool IsButtonReleased(IMouseDevice input, MouseButton mouseButton)
        {
            return input?.IsButtonReleased(mouseButton) ?? false;
        }

        /// <summary>
        /// Determines whether the specified button is being pressed down
        /// </summary>
        /// <param name="input">The mouse</param>
        /// <param name="mouseButton">The mouse button</param>
        /// <returns><c>true</c> if the specified button is being pressed down; otherwise, <c>false</c>.</returns>
        public static bool IsButtonDown(IMouseDevice input, MouseButton mouseButton)
        {
            return input?.IsButtonDown(mouseButton) ?? false;
        }
    }
}
