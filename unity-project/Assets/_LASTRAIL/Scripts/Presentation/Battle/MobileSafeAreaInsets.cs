using System;

namespace LastRail.Presentation.Battle
{
    public sealed class MobileSafeAreaInsets
    {
        private MobileSafeAreaInsets(
            int viewportWidth,
            int viewportHeight,
            int top,
            int bottom,
            int left,
            int right)
        {
            ViewportWidth = viewportWidth;
            ViewportHeight = viewportHeight;
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public int ViewportWidth { get; }
        public int ViewportHeight { get; }
        public int Top { get; }
        public int Bottom { get; }
        public int Left { get; }
        public int Right { get; }
        public bool IsPortraitNineBySixteen => ViewportWidth * 16 == ViewportHeight * 9;

        public static MobileSafeAreaInsets Portrait(
            int viewportWidth,
            int viewportHeight,
            int top,
            int bottom,
            int left = 0,
            int right = 0)
        {
            if (viewportWidth <= 0)
            {
                throw new ArgumentException("Viewport width must be positive.", nameof(viewportWidth));
            }

            if (viewportHeight <= 0)
            {
                throw new ArgumentException("Viewport height must be positive.", nameof(viewportHeight));
            }

            if (viewportWidth * 16 != viewportHeight * 9)
            {
                throw new ArgumentException("Launch mobile combat HUD expects portrait 9:16 viewport.", nameof(viewportWidth));
            }

            if (top < 0 || bottom < 0 || left < 0 || right < 0)
            {
                throw new ArgumentException("Safe area insets cannot be negative.");
            }

            return new MobileSafeAreaInsets(viewportWidth, viewportHeight, top, bottom, left, right);
        }
    }
}
