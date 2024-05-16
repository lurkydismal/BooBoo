using System;
using System.Numerics;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Util
{
    internal static class UtilFunc
    {
        public static void LoadTexturesToGPU(this DPSpr spr)
        {
            foreach(DPSpr.Sprite sprite in spr.sprites.Values)
                unsafe
                {
                    uint id = Rlgl.LoadTexture(sprite.imageDataPtr, sprite.width, sprite.height,
                        sprite.indexed ? PixelFormat.UncompressedGrayscale : PixelFormat.UncompressedR8G8B8A8, 1);
                    //Console.WriteLine(id);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_MIN_FILTER, Rlgl.TEXTURE_FILTER_NEAREST);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_MAG_FILTER, Rlgl.TEXTURE_FILTER_NEAREST);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_WRAP_S, Rlgl.TEXTURE_WRAP_MIRROR_REPEAT);
                    Rlgl.TextureParameters(id, Rlgl.TEXTURE_WRAP_T, Rlgl.TEXTURE_WRAP_MIRROR_REPEAT);
                    spr.SpriteIsOnGPU(sprite.name, id);
                }
        }

        public static void DeleteTexturesFromGPU(this DPSpr spr)
        {
            foreach (DPSpr.Sprite sprite in spr.sprites.Values)
                Rlgl.UnloadTexture(sprite.glTexId);
        }

        public static void LoadTexturesToGPU(this PrmAn an)
        {
            foreach(PrmAn.Texture tex in an.textures.Values)
            {
                Image img = Raylib.LoadImageFromMemory(tex.name.Substring(tex.name.LastIndexOf(".")), tex.texDat);
                Texture2D texture = Raylib.LoadTextureFromImage(img);
                an.TexOnGPU(tex.id, texture.Id);
            }
        }

        public static void DeleteTexturesFromGPU(this PrmAn an)
        {
            foreach (PrmAn.Texture tex in an.textures.Values)
                Rlgl.UnloadTexture(tex.glTexId);
        }
    }
}
