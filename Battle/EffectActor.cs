using System;
using System.Collections.Generic;
using System.Numerics;
using BooBoo.GameState;
using BlakieLibSharp;
using Raylib_cs;
using static BlakieLibSharp.PrmAn;

namespace BooBoo.Battle
{
    //this class is basically just ui drawer on drugs
    internal class EffectActor
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;
        public BattleActor.Direction dir = BattleActor.Direction.Left;

        BattleActor parent;
        PrmAn prmAn;
        EffectType effectType;
        EffectFlags effectFlags;
        string anim = "";
        int animTimer = 0;
        int animFrame = 0;
        bool loopAnim = false;
        string barBlendFrame = "";
        int barStartVal = 0;
        int barEndVal = 1;
        int barCurVal = 0;

        public static EffectActor BeginAnim(BattleActor parent, PrmAn prmAn, bool UI, string anim, bool loop)
        {
            EffectActor actor = new EffectActor(parent, prmAn, UI ? EffectType.UIAnim : EffectType.WorldSpaceAnim);
            actor.anim = anim;
            actor.loopAnim = loop;
            return actor;
        }

        public static EffectActor BeginSingleFrame(BattleActor parent, PrmAn prmAn, bool UI, string frame)
        {
            EffectActor actor = new EffectActor(parent, prmAn, UI ? EffectType.UISingleFrame : EffectType.WorldSpaceSingleFrame);
            actor.anim = frame;
            return actor;
        }

        public static EffectActor BeginBar(BattleActor parent, PrmAn prmAn, bool UI, string mainFrame, string blendFrame, int startVal, int endVal, int val)
        {
            EffectActor actor = new EffectActor(parent, prmAn, UI ? EffectType.UIBar : EffectType.WorldSpaceBar);
            actor.anim = mainFrame;
            actor.barBlendFrame = blendFrame;
            actor.barStartVal = startVal;
            actor.barEndVal = endVal;
            actor.barCurVal = val;
            return actor;
        }

        private EffectActor(BattleActor parent, PrmAn prmAn, EffectType effectType)
        {
            this.parent = parent;
            this.prmAn = prmAn;
            this.effectType = effectType;
            effectFlags = EffectFlags.HaveActorHitstop | EffectFlags.DeleteWhenActorGetsHit;
        }

        public void Update(int barVal = 0) //only need to set to barCurVal if this is a bar
        {
            if (effectFlags.HasFlag(EffectFlags.HaveActorHitstop) && parent.hitstopTime > 0)
                return;
            if(effectType == EffectType.UIBar ||  effectType == EffectType.WorldSpaceBar)
                barCurVal = barVal;
            else if(effectType == EffectType.WorldSpaceAnim || effectType == EffectType.UIAnim)
            {
                Animation animation = prmAn.GetAnim(anim);
                animTimer++;
                if(animTimer >= animation.frameTimes[animFrame])
                {
                    animTimer = 0;
                    animFrame++;
                    if(animFrame >= animation.frameCount)
                    {
                        animFrame = 0; //set it to 0 so that if we accidently draw then it wont die
                        if(!loopAnim)
                            parent.QueueDeleteEffect(anim);
                    }    
                }
            }
        }

        public void Draw()
        {
            if ((int)effectType % 2 == 1)
                return;
            
            Frame frame;

            switch(effectType)
            {
                default:
                case EffectType.WorldSpaceAnim:
                    Animation animation = prmAn.GetAnim(anim);
                    if (animFrame + 1 >= animation.frameCount)
                        frame = prmAn.GetFrame(animation.frames[animFrame]);
                    else
                        frame = prmAn.BlendFrames(animation.frames[animFrame], animation.frames[animFrame + 1], 
                            (float)((float)animTimer / (float)animation.frameTimes[animFrame]));
                    break;
                case EffectType.WorldSpaceSingleFrame:
                    frame = prmAn.GetFrame(anim);
                    break;
                case EffectType.WorldSpaceBar:
                    frame = prmAn.BlendFrames(anim, barBlendFrame, Math.Clamp((float)barCurVal / (float)barEndVal, barStartVal, barEndVal));
                    break;
            }

            Vector3 pos = position;
            if (effectFlags.HasFlag(EffectFlags.FollowActorPos))
                pos += parent.position;
            foreach (Layer layer in frame.layers)
                DrawLayer(layer, pos, scale);
        }

        public void Draw2D()
        {
            if ((int)effectType % 2 == 0)
                return;

            Frame frame;

            switch (effectType)
            {
                default:
                case EffectType.UIAnim:
                    Animation animation = prmAn.GetAnim(anim);
                    if (animFrame + 1 >= animation.frameCount)
                        frame = prmAn.GetFrame(animation.frames[animFrame]);
                    else
                        frame = prmAn.BlendFrames(animation.frames[animFrame], animation.frames[animFrame + 1],
                            (float)animTimer / (float)animation.frameTimes[animFrame]);
                    break;
                case EffectType.UISingleFrame:
                    frame = prmAn.GetFrame(anim);
                    break;
                case EffectType.UIBar:
                    frame = prmAn.BlendFrames(anim, barBlendFrame, Math.Clamp((float)barCurVal / (float)barEndVal, barStartVal, barEndVal));
                    break;
            }

            Vector3 pos = position;
            if (effectFlags.HasFlag(EffectFlags.FollowActorPos))
                pos += parent.position;
            foreach (Layer layer in frame.layers)
                DrawLayer(layer, pos, scale, true);
        }

        void DrawLayer(Layer layer, Vector3 pos, Vector3 scale, bool twoD = false)
        {
            Vector3 screenScale = twoD ? new Vector3(Raylib.GetScreenWidth() / 1280.0f, Raylib.GetScreenHeight() / 720.0f, 0.0f) : Vector3.One;

            if(layer.palNum >= 0)
            {
                Shader shader = (GameStateBase.gameState as BattleGameState).spriteShader;
                int shaderLoc = (GameStateBase.gameState as BattleGameState).sprShaderPalLoc;
                Raylib.SetShaderValueTexture(shader, shaderLoc, parent.palTextures[layer.palNum]);
                Raylib.BeginShaderMode(shader);
            }

            Rlgl.DisableBackfaceCulling();
            Rlgl.PushMatrix();
            Rlgl.Translatef((layer.position.X + pos.X) * screenScale.X, (layer.position.Y + pos.Y) * screenScale.Y, layer.position.Z);
            Rlgl.Rotatef(layer.rotation.Y, 0.0f, 0.1f, 0.0f);
            Rlgl.Rotatef(layer.rotation.X, 0.1f, 0.0f, 0.0f);
            Rlgl.Rotatef(layer.rotation.Z, 0.0f, 0.0f, 0.1f);
            Rlgl.Scalef(layer.scale.X * scale.X, layer.scale.Y * scale.Y, layer.scale.Z);


            if (layer.additive)
                Raylib.BeginBlendMode(BlendMode.Additive);

            Rlgl.Color4f((layer.colMult[0] + layer.colAdd[0]) / 255.0f, (layer.colMult[1] + layer.colAdd[1]) / 255.0f,
                (layer.colMult[2] + layer.colAdd[2]) / 255.0f, (layer.colMult[3] + layer.colAdd[3]) / 255.0f);

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
                case PrimitiveType.Plane:
                    Rlgl.Begin(DrawMode.Quads);

                    if (twoD)
                    {
                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex2f(0.0f, 0.0f);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex2f(0.0f, layer.uv.W * screenScale.Y);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex2f(layer.uv.Z * screenScale.X, layer.uv.W * screenScale.Y);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex2f(layer.uv.Z * screenScale.X, 0.0f);
                    }
                    else
                    {
                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                        Rlgl.TexCoord2f(layer.uv.X / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex3f(0.0f, -layer.uv.W * 0.01f, 0.0f);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, (layer.uv.Y + layer.uv.W) / texSize.Y);
                        Rlgl.Vertex3f(layer.uv.Z * 0.01f * (int)dir, -layer.uv.W * 0.01f, 0.0f);

                        Rlgl.TexCoord2f((layer.uv.X + layer.uv.Z) / texSize.X, layer.uv.Y / texSize.Y);
                        Rlgl.Vertex3f(layer.uv.Z * 0.01f * (int)dir, 0.0f, 0.0f);
                    }

                    Rlgl.End();
                    break;
            }

            Rlgl.PopMatrix();
            Raylib.EndBlendMode();
            Rlgl.SetTexture(0);
            Raylib.EndShaderMode();
        }

        public void SetFlags(EffectFlags flags)
        {
            effectFlags = flags;
        }

        public enum EffectType
        {
            WorldSpaceAnim,
            UIAnim,
            WorldSpaceSingleFrame,
            UISingleFrame,
            WorldSpaceBar,
            UIBar,
        }

        [Flags]
        public enum EffectFlags
        {
            FollowActorPos = 0b_0001,
            HaveActorHitstop = 0b_0010,
            DeleteWhenActorGetsHit = 0b_0100,
            DeleteWhenActorSwitchesState = 0b_1000,

            Default = FollowActorPos | HaveActorHitstop | DeleteWhenActorGetsHit,
        }
    }
}
