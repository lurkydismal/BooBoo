using System;
using System.Numerics;
using Raylib_cs;

namespace BooBoo.Engine
{
    internal static class Window
    {
        static RenderTexture2D window;

        public static void InitWindow()
        {
            Raylib.InitWindow(Settings.windowWidth, Settings.windowHeight, "BookieBop");
            Raylib.SetTargetFPS(60);
            window = Raylib.LoadRenderTexture(Settings.windowWidth, Settings.windowHeight);
        }

        public static void ResizeWindow()
        {
            Raylib.SetWindowSize(Settings.windowWidth, Settings.windowHeight);
            ConfigFlags flags = 0;
            flags |= Settings.windowMode == Settings.WindowMode.Fullscreen ? ConfigFlags.FullscreenMode : 0;
            flags |= Settings.windowMode == Settings.WindowMode.Borderless ? ConfigFlags.BorderlessWindowMode : 0;
            flags |= Settings.vsync ? ConfigFlags.VSyncHint : 0;
            Raylib.SetWindowState(flags);
        }

        /// <summary>
        /// Start drawing to main window
        /// </summary>
        public static void BeginDrawing()
        {
            Raylib.BeginTextureMode(window);
            Raylib.ClearBackground(Color.RayWhite);
        }

        /// <summary>
        /// Finalize the drawing to the main window. Will apply global ui and post process effects
        /// </summary>
        public static void FinalizeDrawing()
        {
            Raylib.EndTextureMode();
            Raylib.BeginDrawing();
            Raylib.DrawTextureRec(window.Texture, new Rectangle(0, 0, 1280, -720), Vector2.Zero, Color.White);
            if (Settings.drawFPS)
                Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        }
    }
}
