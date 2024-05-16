using System;

namespace BooBoo.Engine
{
    internal static class Settings
    {
        public static int windowWidth = 1280;
        public static int windowHeight = 720;
        public static bool vsync = false;
        public static WindowMode windowMode = WindowMode.Window;

        public static bool drawFPS = true;

        public enum WindowMode
        {
            Window,
            Borderless,
            Fullscreen,
        }
    }
}
