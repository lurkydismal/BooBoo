using System;
using System.Numerics;
using BooBoo.Engine;
using Raylib_cs;
using BlakieLibSharp;

namespace BooBoo.Util
{
    internal class UIDrawer : IDisposable
    {
        public PrmAn prmAn { get; private set; }

        public bool inAnimation { get; private set; } = false;
        public string animation { get; private set; } = "";
        public int animationFrame { get; private set; } = 0;
        public int animationTimer { get; private set; } = 0;
        public Vector2 animationPos = Vector2.Zero;
        public Vector2 animationScale = Vector2.One;
        public event Action AnimationEnded;

        public UIDrawer(PrmAn prmAn)
        {
            this.prmAn = prmAn;
        }

        public void AddAdditionalPrmAn(PrmAn newPrman)
        {
            prmAn.Combine(newPrman);
        }

        public void DrawSingleFrame(Vector2 pos, Vector2 scale, string frameName)
        {
            if (!prmAn.frames.ContainsKey(frameName))
                return;
            PrmAn.Frame frame = prmAn.frames[frameName];
            foreach(PrmAn.Layer layer in frame.layers)
            {
                if(!layer.drawIn2d)
                    continue;

                DrawLayer(layer, pos, scale);
            }
        }

        public void DrawBlendedFrame(Vector2 pos, Vector2 scale, string mainFrame, string blendedFrame, float blendAmount)
        {
            PrmAn.Frame frame = prmAn.BlendFrames(mainFrame, blendedFrame, blendAmount);
            foreach(PrmAn.Layer layer in frame.layers)
            {
                if (!layer.drawIn2d)
                    continue;
                DrawLayer(layer, pos, scale);
            }
        }

        public void DrawText(Vector2 pos, int size, string text, FontsManager.FontTypes font, Color color)
        {
            Vector2 screenScale = new Vector2(Raylib.GetScreenWidth() / 1280.0f, Raylib.GetScreenHeight() / 720.0f);
            pos *= screenScale;
            Raylib.DrawTextEx(FontsManager.GetFont(font), text, pos, size, 5.0f * screenScale.X, color);
        }

        public void BeginAnimation(string animation, Vector2 pos, Vector2 scale)
        {
            if (!prmAn.animations.ContainsKey(animation))
                return;
            this.animation = animation;
            inAnimation = true;
            animationPos = pos;
            animationScale = scale;
            animationFrame = 0;
            animationTimer = 0;
        }

        public void UpdateAndDrawAnimation()
        {
            if (!inAnimation)
                return;
            animationTimer++;
            PrmAn.Animation anim = prmAn.animations[animation];
            if (animationTimer > anim.frameTimes[animationFrame])
            {
                animationTimer = 0;
                animationFrame++;
                if(animationFrame >= anim.frameCount)
                {
                    inAnimation = false;
                    AnimationEnded();
                    return;
                }
            }
            PrmAn.Frame frame = prmAn.BlendFrames(anim.frames[animationFrame], 
                animationFrame + 1 < anim.frameCount ? anim.frames[animationFrame] : "", animationTimer / anim.frameTimes[animationFrame]);

            foreach(PrmAn.Layer layer in frame.layers)
            {
                if (!layer.drawIn2d)
                    continue;

                DrawLayer(layer, animationPos, animationScale);
            }
        }

        void DrawLayer(PrmAn.Layer layer, Vector2 pos, Vector2 scale)
        {
            Vector2 screenScale = new Vector2(Raylib.GetScreenWidth() / 1280.0f, Raylib.GetScreenHeight() / 720.0f);

            Rlgl.DisableBackfaceCulling();
            Rlgl.PushMatrix();
            Rlgl.Translatef((layer.position.X + pos.X) * screenScale.X, (layer.position.Y + pos.Y) * screenScale.Y, layer.position.Z);
            Rlgl.Rotatef(layer.rotation.Y, 0.0f, 0.1f, 0.0f);
            Rlgl.Rotatef(layer.rotation.X, 0.1f, 0.0f, 0.0f);
            Rlgl.Rotatef(layer.rotation.Z, 0.0f, 0.0f, 0.1f);
            Rlgl.Scalef(layer.scale.X * scale.X, layer.scale.Y * scale.Y, layer.scale.Z);

            Rlgl.Color4f((layer.colMult[0] + layer.colAdd[0]) / 255.0f, (layer.colMult[1] + layer.colAdd[1]) / 255.0f,
                (layer.colMult[2] + layer.colAdd[2]) / 255.0f, (layer.colMult[3] + layer.colAdd[3]) / 255.0f);

            if (layer.additive)
                Raylib.BeginBlendMode(BlendMode.Additive);

            Vector2 texSize;
            if (prmAn.textures.ContainsKey(layer.texId))
            {
                Rlgl.SetTexture(prmAn.textures[layer.texId].glTexId);
                texSize = new Vector2(prmAn.textures[layer.texId].width, prmAn.textures[layer.texId].height);
            }
            else
                texSize = Vector2.Zero;

            switch (layer.primitiveType)
            {
                default:
                case PrmAn.PrimitiveType.Plane:
                    Rlgl.Begin(DrawMode.Quads);

                    Rlgl.TexCoord2f(layer.uv.X / texSize.X, layer.uv.Y / texSize.Y);
                    Rlgl.Vertex2f(0.0f, 0.0f);

                    Rlgl.TexCoord2f(layer.uv.X / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                    Rlgl.Vertex2f(0.0f, layer.uv.W * screenScale.Y);

                    Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                    Rlgl.Vertex2f(layer.uv.Z * screenScale.X, layer.uv.W * screenScale.Y);

                    Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, layer.uv.Y / texSize.Y);
                    Rlgl.Vertex2f(layer.uv.Z * screenScale.X, 0.0f);

                    Rlgl.End();
                    break;
            }

            Rlgl.PopMatrix();
            Raylib.EndBlendMode();
            Rlgl.SetTexture(0);
        }

        public void Dispose()
        {
            foreach (PrmAn.Texture tex in prmAn.textures.Values)
                Rlgl.UnloadTexture(tex.glTexId);
        }
    }
}
