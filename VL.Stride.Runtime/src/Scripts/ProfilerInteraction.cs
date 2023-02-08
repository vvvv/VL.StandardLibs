
using System;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Core.Diagnostics;
using Stride.Input;
using Stride.Engine;
using Stride.Profiling;
using Stride.Core;
using VL.Core;

namespace VL.Stride
{
    public class ProfilerInteraction 
    {
        public bool Enabled;

        /// <summary>
        /// The time between two refreshes of the profiling information in milliseconds.
        /// </summary>
        [Display(2, "Refresh interval (ms)")]
        public float RefreshTime { get; set; } = 500;

        /// <summary>
        /// Gets or set the sorting mode of the profiling entries
        /// </summary>
        [Display(1, "Sort by")]
        public GameProfilingSorting SortingMode { get; set; } = GameProfilingSorting.ByTime;

        /// <summary>
        /// Gets or sets the type of the profiling to display: CPU or GPU
        /// </summary>
        [Display(0, "Filter")]
        public GameProfilingResults FilteringMode { get; set; } = GameProfilingResults.Fps;

        /// <summary>
        /// Gets or sets the current profiling result page to display.
        /// </summary>
        [Display(3, "Display page")]
        public int ResultPage { get; set; } = 1;

        public IKeyboardDevice Keyboard { get; set; }

        public void Update()
        {
            if (Keyboard != null)
            {
                if (Keyboard.IsKeyDown(Keys.LeftShift) && Keyboard.IsKeyDown(Keys.LeftCtrl) && Keyboard.IsKeyReleased(Keys.P))
                {
                    Enabled = !Enabled;
                }

                if (Enabled)
                {
                    // toggle the filtering mode
                    if (Keyboard.IsKeyPressed(Keys.F5))
                    {
                        FilteringMode = (GameProfilingResults)(((int)FilteringMode + 1) % Enum.GetValues(typeof(GameProfilingResults)).Length);
                    }
                    // toggle the sorting mode
                    if (Keyboard.IsKeyPressed(Keys.F6))
                    {
                        SortingMode = (GameProfilingSorting)(((int)SortingMode + 1) % Enum.GetValues(typeof(GameProfilingSorting)).Length);
                    }

                    // update the result page
                    if (Keyboard.IsKeyPressed(Keys.F7))
                    {
                        ResultPage = Math.Max(1, --ResultPage);
                    }
                    else if (Keyboard.IsKeyPressed(Keys.F8))
                    {
                        ++ResultPage;
                    }
                    if (Keyboard.IsKeyPressed(Keys.D1))
                    {
                        ResultPage = 1;
                    }
                    else if (Keyboard.IsKeyPressed(Keys.D2))
                    {
                        ResultPage = 2;
                    }
                    else if (Keyboard.IsKeyPressed(Keys.D3))
                    {
                        ResultPage = 3;
                    }
                    else if (Keyboard.IsKeyPressed(Keys.D4))
                    {
                        ResultPage = 4;
                    }
                    else if (Keyboard.IsKeyPressed(Keys.D5))
                    {
                        ResultPage = 5;
                    }

                    // update the refreshing speed
                    if (Keyboard.IsKeyPressed(Keys.Subtract) || Keyboard.IsKeyPressed(Keys.OemMinus))
                    {
                        RefreshTime = Math.Min(RefreshTime * 2, 10000);
                    }
                    else if (Keyboard.IsKeyPressed(Keys.Add) || Keyboard.IsKeyPressed(Keys.OemPlus))
                    {
                        RefreshTime = Math.Max(RefreshTime / 2, 100);
                    }
                } 
            }
        }
    }
}