using System;
using System.Collections.Generic;
using System.Text;
using BooBoo.Util;
using Raylib_cs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BooBoo.Engine
{
    internal static class FontsManager
    {
        public static Font[] Fonts;

        public static void LoadFonts(FontPath[] FontPaths)
        {
            Fonts = new Font[FontPaths.Length];
            foreach (FontPath font in FontPaths)
                Fonts[(int)font.Font] = (Font)FileHelper.LoadFont(font.Path);
        }

        public static Font GetFont(FontTypes font)
        {
            return Fonts[(int)font];
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FontTypes
        { 
            Metamorphous,
        }

        public struct FontPath
        {
            [JsonProperty("Font")]
            public FontTypes Font;
            [JsonProperty("Path")]
            public string Path;
        }
    }
}
