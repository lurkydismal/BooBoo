using System;
using System.Collections.Generic;
using System.Numerics;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Battle
{
    //this class is basically just ui drawer on drugs
    internal class EffectActor
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;

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
                PrmAn.Animation animation = prmAn.GetAnim(anim);
                animTimer++;
                if(animTimer > animation.frameTimes[animFrame])
                {
                    animTimer = 0;
                    animFrame++;
                    if(animFrame >= animation.frameCount)
                    {
                        animFrame = 0; //set it to 0 so that if we accidently draw then it wont die
                        if(!loopAnim)
                            parent.QueueDeleteEffect(this);
                    }    
                }
            }
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
        }
    }
}
