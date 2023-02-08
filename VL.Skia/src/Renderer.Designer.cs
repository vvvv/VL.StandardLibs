using System;
using VL.Core;

namespace VL.Skia
{
    partial class SkiaRenderer
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            // In exported app pressing Alt+F4 causes the main form to close via a window close message which in turn calls dispose.
            // So we need to check for that as a double dispose causes a handle not created exception!
            if (IsDisposed)
                return;

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}