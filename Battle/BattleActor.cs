using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Linq;
using BlakieLibSharp;
using BooBoo.GameState;
using BooBoo.Util;
using Raylib_cs;
using NLua;

namespace BooBoo.Battle
{
    internal class BattleActor
    {
        public Vector3 position = Vector3.Zero;
        public Vector3 velocity = Vector3.Zero;
        public Vector3 velocityMod = Vector3.Zero;
        public Vector3 velocityMin = Vector3.Zero; //will stop applying mod on vel if it reaches this threshold
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;
        public int renderPriority = 0;
        public Direction dir = Direction.Left;
        public int posSign { get { return MathF.Sign(position.X); } }

        public int curHealth = 1;
        public int maxHealth = 1;

        InputHandler input;
        Lua luaCode;

        BattleGameState gameState = GameStateBase.gameState as BattleGameState;
        BattleStage stage = BattleStage.stage;

        public RenderMode renderMode;

        public int curPalNum = 0;
        public Texture2D[] palTextures { get; private set; }
        public Color multColor = Color.White;
        public PrmAn effects { get; private set; }
        public DPSpr sprites { get; private set; }
        public SprAn sprAn { get; protected set; }
        public StateList states { get; private set; }
        public string curState { get; private set; } = "CmnStand";
        public string curAnimName { get; private set; } = "CmnStand";
        public StatePosition curStatePos { get; private set; } = StatePosition.Standing;
        public bool animBlending = false;
        public SprAn.SprAnState curAnim {  get { return sprAn.GetState(curAnimName); } }
        public int curAnimFrame = 0;
        public SprAn.SprAnFrame curFrame { get { return sprAn.GetFrame(curAnimName, curAnimFrame); } }
        public AudioPlayer soundEffects { get; private set; }
        public UniqueAudioPlayer voiceLines { get; private set; }

        //Hurt values
        public int hitstopTime = 0;
        public int hitstunTime = 0; //if on ground this is time in hitstun state + state anim time, in air this is time before you can wake up.
        public int HKDTime = 0; //time youre stuck on the ground before you can wake up
        public int HKDBeginInvulTime = -1; //once this reaches zero you get set to invincible until you wake up
        public bool willCrumple = false; //if true it will go into crumple after stagger
        public HitstunStates hitstunState = HitstunStates.None;

        //Hit values
        public int damage = 0;
        public int hitstopOnHit = 0;
        public int hitstunOnHit = 0;
        public int HKDOnHit = 0;
        public int HKDInvulTimerOnHit = -1;
        public bool crumpleOnHit = false;
        public Vector2 dirOnHitGround = Vector2.Zero;
        public Vector2 dirOnHitAir = Vector2.Zero;
        public Vector2 dirModOnHitGround = Vector2.Zero;
        public Vector2 dirModOnHitAir = Vector2.Zero;
        public Vector2 dirMinOnHitGround = Vector2.Zero; 
        public Vector2 dirMinOnHitAir = Vector2.Zero; 
        public HitstunStates hitStateStanding = HitstunStates.CmnHurtStandWeak;
        public HitstunStates hitStateCrouching = HitstunStates.CmnHurtCrouchWeak;
        public HitstunStates hitStateAerial = HitstunStates.CmnHurtLaunch;
        public AttackHitAttribute attackAttribute = AttackHitAttribute.Body;

        public List<BattleActor> actorsWillHit = new List<BattleActor>(); //attacker will get a list of all actors it will hit
        public bool willBeHit = false; //if an actor is going to be hit this will be set to true
        //very next frame before any updates itll check hits, if any actor in actorswillbehit have will be hit set to true then hit will be
        //confirmed and will do hit code as needed

        public List<BattleActor> actorsHit = new List<BattleActor>(); //this list shows what actors youve hit. we have this so that if an attack only
        //hits once but has multiple frames with hitboxes it wont hit an actor multiple times.

        //the pushbox used for general collision with other players. will get the first box found as push, or return null if there are none
        public RectCollider? pushBox { 
            get
            {
                if (curFrame == null)
                    return null;
                foreach (RectCollider rect in curFrame.colliders)
                    if (rect.colliderType == RectCollider.ColliderType.Push)
                        return rect;
                return null;
            } 
        }
        public RectCollider[] colliders
        {
            get
            {
                if(curFrame == null)
                    return new RectCollider[0];
                return curFrame.colliders;
            }
        }
        public CollisionFlags collisionFlags = CollisionFlags.DefaultSettings;
        public float wallPushboxWidth = 0.5f; //This will be used to check distance from wall instead of pushbox

        public int frameActiveTime = -1;
        public int frameLength { get { return curFrame.frameLength; } }
        public bool ignoreFreeze = false;

        List<string> hitCancels = new List<string>();
        List<string> hitOrBlockCancles = new List<string>();
        List<StateList.State> cancableStates = new List<StateList.State>();
        List<BufferItem> inputBuffer = new List<BufferItem>() { new BufferItem() };


        public BattleActor opponent { get; private set; }
        List<BattleActor> children = new List<BattleActor>();
        public BattleActor parent { get; private set; }
        public BattleActor player { get; private set; }
        bool isPlayer = false;

        public Dictionary<string, EffectActor> effectsActive = new Dictionary<string, EffectActor>();
        List<string> effectsToDelete = new List<string>();

        public const float oneSixteth = 1.0f / 60.0f;
        public const float oneTwelveth = 1.0f / 12.0f;

        public BattleActor(SprAn sprAn, StateList states, InputHandler input, Lua luaCode,
            string startState, RenderMode renderMode, DPSpr sprites, Texture2D[] palTextures)
        {
            this.sprAn = sprAn;
            this.states = states;
            this.input = input;
            this.luaCode = luaCode;
            this.renderMode = renderMode;
            this.sprites = sprites;
            this.palTextures = palTextures;

            EnterState(startState);
        }

        ~BattleActor()
        {
            if(isPlayer)
            {
                sprites.DeleteTexturesFromGPU();
            }
        }

        public void MatchInit()
        {
            player = this;
            isPlayer = true;
            parent = null;
            CallLuaFunc("MatchInit", this);
        }

        public void Update()
        {
            if (!ignoreFreeze && gameState.superFreeze)
                goto BufferUpdate;

            //update effects
            foreach (EffectActor eff in effectsActive.Values)
                eff.Update();

            if (curFrame == null)
            {
                actorsWillHit.Clear();
                return;
            }

            //detect actors will hit first
            foreach(BattleActor actor in actorsWillHit)
                if(actor.willBeHit)
                {
                    //hurt actor code
                    actor.curHealth -= damage;
                    actor.hitstopTime = hitstopOnHit;
                    actor.hitstunTime = hitstunOnHit;
                    actor.HKDTime = HKDOnHit;
                    actor.HKDBeginInvulTime = HKDInvulTimerOnHit;
                    if (actor.curStatePos == StatePosition.Aerial)
                    {
                        actor.velocity = dirOnHitAir.ToVector3();
                        actor.velocityMod = dirModOnHitAir.ToVector3();
                        actor.velocityMin = dirMinOnHitAir.ToVector3();
                    }
                    else
                    {
                        actor.velocity = dirOnHitGround.ToVector3();
                        actor.velocityMod = dirModOnHitGround.ToVector3();
                        actor.velocityMin = dirMinOnHitGround.ToVector3();
                    }
                    actor.willCrumple = crumpleOnHit;
                    switch(actor.curStatePos)
                    {
                        case StatePosition.Standing:
                            actor.hitstunState = hitStateStanding;
                            actor.EnterState(Enum.GetName(typeof(HitstunStates), hitStateStanding));
                            break;
                        case StatePosition.Crouching:
                            actor.hitstunState = hitStateCrouching;
                            actor.EnterState(Enum.GetName(typeof(HitstunStates), hitStateCrouching));
                            break;
                        case StatePosition.Aerial:
                            actor.hitstunState = hitStateAerial;
                            actor.EnterState(Enum.GetName(typeof(HitstunStates), hitStateAerial));
                            break;
                    }
                    actor.FaceActor(this);

                    actor.willBeHit = false;

                    //hit code
                    hitstopTime = hitstopOnHit;
                    AddHitOrBlockCancels();
                    actorsHit.Add(actor);
                    CallLuaFunc(curState + "_Hit", this, actor);
                }
            actorsWillHit.Clear();

            hitstopTime = hitstopTime > 0 ? hitstopTime - 1 : 0;
            if (hitstopTime > 0)
                goto BufferUpdate;

            Vector3 opponentDist = GetDistanceFrom(opponent);
            switch (hitstunState) //this looks ugly but it works
            {
                default:
                case HitstunStates.None:
                    frameActiveTime++;
                    CallLuaFunc(curState + "_Tick", this);
                    if (states.HasState(curState) && states[curState].StateCanTurn)
                        if (MathF.Sign(opponentDist.X) == -(int)dir)
                        {
                            EnterState(states[curState].TurnState);
                            RemoveCancel(states[curState].NextState);
                        }

                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                        {
                            //Console.WriteLine("Looping");
                            if (states.HasState(curState))
                            {
                                StateList.State state = states.GetState(curState);
                                if (state.Looping)
                                {
                                    curAnimFrame = state.LoopPos;
                                    CallLuaFunc(curState + "_Loop", this);
                                }
                                else
                                    EnterState(state.NextState);
                            }
                            else
                                CallLuaFunc(curState + "_End", this);
                        }
                    }
                    break;
                case HitstunStates.CmnHurtStandWeak:
                case HitstunStates.CmnHurtStandMedium:
                case HitstunStates.CmnHurtStandHeavy:
                case HitstunStates.CmnHurtGutWeak:
                case HitstunStates.CmnHurtGutMedium:
                case HitstunStates.CmnHurtGutHeavy:
                case HitstunStates.CmnHurtCrouchWeak:
                case HitstunStates.CmnHurtCrouchMedium:
                case HitstunStates.CmnHurtCrouchHeavy:
                    if(hitstunTime >= 0)
                    {
                        hitstunTime--;
                        break;
                    }
                    frameActiveTime++;
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                        {
                            EnterState(hitstunState < HitstunStates.CmnHurtCrouchWeak ? "CmnStand" : "CmnCrouch");
                            hitstunState = HitstunStates.None;
                            velocity = Vector3.Zero;
                            velocityMod = Vector3.Zero;
                            velocityMin = Vector3.Zero;
                        }
                    }
                    break;
                case HitstunStates.CmnHurtStagger:
                    frameActiveTime++;
                    hitstunTime--;
                    if(hitstunTime <= 0)
                    {
                        EnterState("CmnHurtCrumple");
                        hitstunState = HitstunStates.CmnHurtCrumple;
                        break;
                    }
                    if(frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if(curAnimFrame >= curAnim.frameCount)
                            curAnimFrame = states.HitstunLoopData.StaggerLoopPos;
                    }
                    break;
                case HitstunStates.CmnHurtLaunch:
                case HitstunStates.CmnHurtLaunchToFall:
                case HitstunStates.CmnHurtFall:
                case HitstunStates.CmnHurtTrip:
                case HitstunStates.CmnHurtBlowback:
                case HitstunStates.CmnHurtDiagonalSpin:
                    frameActiveTime++;
                    hitstunTime--;
                    if (hitstunTime <= 0 && HasButtons(inputBuffer[0].button, ButtonType.A))
                    {
                        if (inputBuffer[0].IsLeft())
                            velocity.X = -2.0f * oneSixteth;
                        else if (inputBuffer[0].IsRight())
                            velocity.X = 2.0f * oneSixteth;
                        else
                            velocity.X = 0.0f;
                        if (inputBuffer[0].IsUp())
                            velocity.Y = 17.0f;
                        else if (inputBuffer[0].IsDown())
                            velocity.Y = 0.0f;
                        else
                            velocity.Y = 8.0f;
                        velocityMod = new Vector3(0.0f, -0.7f, 0.0f);
                        EnterState("CmnHurtWakeUpAir");
                        hitstunState = HitstunStates.CmnHurtWakeUpAir;
                        break;
                    }
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                            switch(hitstunState) //switch inside a switch this is so fucking ugly ewwwwww
                            {
                                default:
                                case HitstunStates.CmnHurtLaunch:
                                    curAnimFrame = states.HitstunLoopData.LaunchLoopPos; break;
                                case HitstunStates.CmnHurtLaunchToFall:
                                    EnterState("CmnHurtFall");
                                    hitstunState = HitstunStates.CmnHurtFall;
                                    break;
                                case HitstunStates.CmnHurtFall:
                                    curAnimFrame = states.HitstunLoopData.FallLoopPos; break;
                                case HitstunStates.CmnHurtTrip:
                                    curAnimFrame = states.HitstunLoopData.TripLoopPos; break;
                                case HitstunStates.CmnHurtBlowback:
                                    curAnimFrame = states.HitstunLoopData.BlowbackLoopPos; break;
                                case HitstunStates.CmnHurtDiagonalSpin:
                                    curAnimFrame = states.HitstunLoopData.DiagonalSpinLoopPos; break;
                            }
                    }
                    break;
                case HitstunStates.CmnHurtCrumple:
                case HitstunStates.CmnHurtLandFall:
                case HitstunStates.CmnHurtLandTrip:
                    frameActiveTime++;
                    HKDTime--;
                    if(HKDTime <= 0 && HasButtons(inputBuffer[0].button, ButtonType.A))
                    {
                        if (inputBuffer[0].IsLeft())
                            velocity.X = -2.0f * oneSixteth;
                        else if (inputBuffer[0].IsRight())
                            velocity.X = 2.0f * oneSixteth;
                        else
                            velocity.X = 0.0f;
                        velocity.Y = 14.0f;
                        velocityMod = new Vector3(0.0f, -0.7f, 0.0f);
                        EnterState("CmnHurtWakeUpGround");
                        hitstunState = HitstunStates.CmnHurtWakeUpAir;
                        break;
                    }
                    HKDBeginInvulTime--;
                    if (HKDBeginInvulTime == 0)
                        collisionFlags &= CollisionFlags.Invincible;
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if(curAnimFrame >= curAnim.frameCount)
                        {
                            if(HKDTime <= 0)
                            {
                                EnterState("CmnHurtWakeUpLazy");
                                hitstunState = HitstunStates.CmnHurtWakeUpLazy;
                            }
                            else
                            {
                                curAnimFrame--;
                                frameActiveTime = curFrame.frameLength - 1; //just wait until we can wake up
                            }
                        }
                    }
                    break;
                case HitstunStates.CmnHurtWakeUpAir:
                    frameActiveTime++;
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                            curAnimFrame = states.HitstunLoopData.WakeUpAirLoopPos;
                    }
                    break;
                case HitstunStates.CmnHurtWakeUpGround:
                    frameActiveTime++;
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                            curAnimFrame = states.HitstunLoopData.WakeUpGroundLoopPos;
                    }
                    break;
                case HitstunStates.CmnHurtWakeUpLand:
                case HitstunStates.CmnHurtWakeUpLazy:
                    frameActiveTime++;
                    if (frameActiveTime >= frameLength)
                    {
                        frameActiveTime = 0;
                        curAnimFrame++;
                        CallLuaFunc(curState + "_Update", this, curAnimFrame);
                        if (curAnimFrame >= curAnim.frameCount)
                        {
                            EnterState("CmnStand");
                            hitstunState = HitstunStates.None;
                        }
                    }
                    break;
            }

            #region physics update
            velocity += velocityMod;
            if ((MathF.Sign(velocityMin.X) == 1 && velocity.X <= velocityMin.X) ||
                (MathF.Sign(velocityMin.X) == -1 && velocity.X >= velocityMin.X))
            {
                velocity.X = velocityMin.X;
                velocityMod.X = 0.0f;
            }
            if ((MathF.Sign(velocityMin.Y) == 1 && velocity.Y <= velocityMin.Y) ||
                (MathF.Sign(velocityMin.Y) == -1 && velocity.Y >= velocityMin.Y))
            {
                velocity.Y = velocityMin.Y;
                velocityMod.Y = 0.0f;
            }
            position += new Vector3(velocity.X * (int)dir, velocity.Y, velocity.Z);
            if(velocity.Y <= 0.0f && (hitstunState == HitstunStates.CmnHurtLaunch || hitstunState == HitstunStates.CmnHurtDiagonalSpin))
            {
                EnterState("CmnHurtLaunchToFall");
                hitstunState = HitstunStates.CmnHurtLaunchToFall;
            }
            if(collisionFlags.HasFlag(CollisionFlags.Floor) && position.Y < 0.0f)
            {
                position.Y = 0.0f;
                velocity.Y = 0.0f;
                velocityMod.Y = 0.0f;
                if(hitstunState >= HitstunStates.CmnHurtLaunch && hitstunState <= HitstunStates.CmnHurtDiagonalSpin)
                {
                    if(hitstunState == HitstunStates.CmnHurtTrip)
                    {
                        EnterState("CmnHurtLandTrip");
                        hitstunState = HitstunStates.CmnHurtLandTrip;
                    }    
                    else
                    {
                        EnterState("CmnHurtLandFall");
                        hitstunState = HitstunStates.CmnHurtLandFall;
                    }
                }
                else if(states.HasState(curState))
                {
                    StateList.State state = states.GetState(curState);
                    if (state.LandToState)
                        EnterState(state.LandState);
                }
            }

            //pushbox collision
            foreach (BattleActor actor in gameState.actors)
            {
                if(actor == this) continue;
                if (actor.player == player)
                { if (!collisionFlags.HasFlag(CollisionFlags.ActorsOnSameTeam)) continue; }
                else if(!collisionFlags.HasFlag(CollisionFlags.ActorsOnDifferentTeams)) continue;

                RectCollider? ourPush = pushBox; RectCollider? theirPush = actor.pushBox;
                if (ourPush == null || theirPush == null)
                    continue;

                if (dir == Direction.Right)
                    ourPush = ourPush.Value.Flip();

                if(actor.dir == Direction.Right)
                    theirPush = theirPush.Value.Flip();

                if (!ourPush.Value.Overlaps(position.XY(), theirPush.Value, actor.position.XY()))
                    continue;

                //Console.WriteLine("Cols Overlap");

                Vector2 boxDist = ourPush.Value.GetDistanceFrom(position.XY(), theirPush.Value, actor.position.XY());

                if (boxDist.X < 0 && boxDist.X < theirPush.Value.width)
                {
                    if (!actor.TouchingWall() || !actor.TouchingDistanceWall(actor.GetDistanceFrom(player)))
                        actor.position.X = position.X - theirPush.Value.width;
                    else
                        position.X = actor.position.X + ourPush.Value.width;
                }
                else if (boxDist.X > 0 && boxDist.X > ourPush.Value.width)
                {
                    if(!actor.TouchingWall() || !actor.TouchingDistanceWall(actor.GetDistanceFrom(player)))
                        actor.position.X = position.X + ourPush.Value.width;
                    else
                        position.X = actor.position.X - theirPush.Value.width;
                }
            }

            opponentDist = GetDistanceFrom(opponent);
            if (TouchingDistanceWall(opponentDist))
                position.X = opponent.position.X + (stage.maxPlayerDistance * -MathF.Sign(opponentDist.X)) + wallPushboxWidth * MathF.Sign(position.X);

            if (TouchingWall())
                position.X = (stage.stageWidth - wallPushboxWidth) * MathF.Sign(position.X);
            //Console.WriteLine(position + "\t" + velocity + "\t" + opponentDist);

            //Hit Collision
            Vector2 ourPosXY = position.XY();
            foreach(BattleActor actor in gameState.actors)
            {
                if (actor == this) continue;
                if (actor.player == player && !collisionFlags.HasFlag(CollisionFlags.AttackMembersOfSameTeam)) continue;

                Vector2 theirPosXY = actor.position.XY();
                //the real meat of updating :skull:
                //holy fuck i need a way to optimize this
                for(int i = 0; i < colliders.Length; i++)
                {
                    RectCollider ourBox = colliders[i];
                    if (dir == Direction.Right)
                        ourBox = ourBox.Flip();
                    for(int j = 0; j < actor.colliders.Length; j++)
                    {
                        RectCollider theirBox = actor.colliders[j];
                        if (actor.dir == Direction.Right)
                            theirBox = theirBox.Flip();
                        if (ourBox.Overlaps(ourPosXY, theirBox, theirPosXY))
                        {
                            switch (ourBox.colliderType)
                            {
                                case RectCollider.ColliderType.Hurt:
                                    if(!InvulToAttrib(actor.attackAttribute) && !actor.actorsHit.Contains(this))
                                        willBeHit = true;
                                    break;
                                case RectCollider.ColliderType.Hit:
                                    if (!actor.InvulToAttrib(attackAttribute) && !actorsWillHit.Contains(actor))
                                        actorsWillHit.Add(actor);
                                    break;
                            }
                        }
                    }
                }
            }
            #endregion

            #region buffer update
            BufferUpdate:
            if (isPlayer)
            {
                int inpDir;
                ButtonType button;
                inpDir = input.GetInputDirection();
                button = input.GetBattleButton();

                BufferItem bufItem = new BufferItem();
                bufItem.direction = inpDir;
                bufItem.button = button;
                if (inputBuffer[0] != bufItem)
                {
                    inputBuffer.Insert(0, bufItem);
                    bufItem = inputBuffer[1];
                    bufItem.button |= ButtonType.Release;
                    inputBuffer[1] = bufItem;
                }
                else
                {
                    bufItem.chargeTime = inputBuffer[0].chargeTime + 1;
                    if (bufItem.chargeTime >= 5)
                        bufItem.button |= ButtonType.Charge;
                    inputBuffer[0] = bufItem;
                }

                while (inputBuffer.Count > 15)
                    inputBuffer.RemoveAt(inputBuffer.Count - 1);

                for (int i = inputBuffer.Count - 1; i > 0; i--)
                {
                    bufItem = inputBuffer[i];
                    bufItem.timeInBuffer++;
                    inputBuffer[i] = bufItem;
                    if (bufItem.timeInBuffer >= 60)
                        inputBuffer.RemoveAt(i);
                }
            }
            #endregion

            #region input check
            if(isPlayer && hitstopTime <= 0)
            {
                List<StateList.State> cancels = new List<StateList.State>();
                foreach (StateList.State state in cancableStates)
                {
                    if (!HasButtons(inputBuffer[0].button, state.Button))
                        continue;

                    if (CheckBuffer(state.Input))
                        cancels.Add(state);
                }

                if (cancels.Count > 0)
                {
                    StateList.State cancelInto = cancels[0];
                    foreach (StateList.State state in cancels)
                        cancelInto = cancelInto.Priority > state.Priority ? cancelInto : state;
                    EnterState(cancelInto.Name);
                    return;
                }
            }
            #endregion

            //delete effects
            foreach (string eff in effectsToDelete)
                if (effectsActive.ContainsKey(eff))
                    effectsActive.Remove(eff);
            effectsToDelete.Clear();
        }

        public void Draw()
        {
            if (curFrame == null)
                return;

            if(renderMode == RenderMode.Sprite)
            {
                //Console.WriteLine("Drawing sprite");
                Shader spriteShader = gameState.spriteShader;
                Raylib.BeginShaderMode(spriteShader);
                Raylib.SetShaderValueTexture(spriteShader, gameState.sprShaderPalLoc, palTextures[curPalNum]);
                SprAn.SprAnFrame frame;
                if (animBlending && curAnimFrame + 1 < curAnim.frameCount)
                    frame = curFrame.BlendFrames(curAnim.frames[curAnimFrame + 1], (float)frameActiveTime / (float)frameLength);
                else
                    frame = curFrame;

                foreach(SprAn.FrameUv uv in frame.uvs)
                {
                    DPSpr.Sprite sprite;
                    if (sprites.HasSprite(uv.textureName))
                    {
                        //Console.WriteLine(sprites.GetSpriteGLId(uv.textureName));
                        sprite = sprites.GetSprite(uv.textureName);
                        Rlgl.SetTexture(sprite.glTexId);
                    }
                    else
                        sprite = new DPSpr.Sprite();

                    Rlgl.DisableBackfaceCulling();
                    Rlgl.PushMatrix();
                    {
                        Rlgl.Translatef(position.X + uv.position.X * (int)dir, position.Y + uv.position.Y, position.Z);
                        Rlgl.Rotatef(rotation.Y + uv.rotation.Y, 0.0f, 1.0f, 0.0f);
                        Rlgl.Rotatef(rotation.X + uv.rotation.X, 1.0f, 0.0f, 0.0f);
                        Rlgl.Rotatef(rotation.Z + uv.rotation.Z, 0.0f, 0.0f, 1.0f);
                        Rlgl.Scalef(scale.X * uv.scale.X * (int)dir, scale.Y * uv.scale.Y, scale.Z);

                        Rlgl.Begin(DrawMode.Quads);

                        if(uv.uv.Z == 0.0f || uv.uv.W == 0.0f) //draw full texture if uv width or height is 0
                        {
                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(0.0f, 0.0f);
                            Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(0.0f, 1.0f);
                            Rlgl.Vertex3f(0.0f, -sprite.height  * 0.01f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(1.0f, 1.0f);
                            Rlgl.Vertex3f(sprite.width * 0.01f, -sprite.height * 0.01f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(1.0f, 0.0f);
                            Rlgl.Vertex3f(sprite.width * 0.01f, 0.0f, 0.0f);
                        }
                        else
                        {
                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(uv.uv.X / sprite.width, uv.uv.Y / sprite.height);
                            Rlgl.Vertex3f(0.0f, 0.0f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(uv.uv.X / sprite.width, (uv.uv.Y + uv.uv.W) / sprite.height);
                            Rlgl.Vertex3f(0.0f, -uv.uv.W * 0.01f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f((uv.uv.X + uv.uv.Z) / sprite.width, (uv.uv.Y + uv.uv.W) / sprite.height);
                            Rlgl.Vertex3f(uv.uv.Z * 0.01f, -uv.uv.W * 0.01f, 0.0f);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f((uv.uv.X + uv.uv.Z) / sprite.width, uv.uv.Y / sprite.height);
                            Rlgl.Vertex3f(uv.uv.Z * 0.01f, 0.0f, 0.0f);
                        }

                        Rlgl.End();
                        Rlgl.PopMatrix();
                        Rlgl.SetTexture(0);
                    }
                }

                Raylib.EndShaderMode();
            }

            EffectActor[] drawEffects = effectsActive.Values.OrderBy(eff => eff.renderPriority).ToArray();
            foreach (EffectActor eff in drawEffects)
                eff.Draw();
        }

        public void Draw2DEffects()
        {
            foreach (EffectActor actor in effectsActive.Values)
                actor.Draw2D();
        }

        public void EnterState(string state, bool callEnd = true)
        {
            //Remove invincibility
            collisionFlags &= ~CollisionFlags.Invincible;
            collisionFlags &= ~CollisionFlags.InvulLow;
            collisionFlags &= ~CollisionFlags.InvulHigh;
            collisionFlags &= ~CollisionFlags.InvulProjectile;
            if (callEnd)
                CallLuaFunc(curState + "_End", this);
            curState = state;
            curAnimName = state;
            curAnimFrame = 0;
            frameActiveTime = -1;

            cancableStates.Clear();
            hitCancels.Clear();
            hitOrBlockCancles.Clear();
            if(states.HasState(state))
            {
                StateList.State cancel = states.GetState(state);
                curStatePos = cancel.Position; //this is such a shoehorn 💀
                StateList.State current = states.GetState(curState);
                List<StateList.State> allStates;
                if(current.AutoAddCancels)
                    switch(cancel.stateType)
                    {
                        default:
                        case StateType.Neutral:
                            allStates = states.AllStates;
                            foreach (StateList.State sta in allStates)
                            {
                                if (sta.Name == curState || sta.Position != current.Position)
                                    continue;
                                if (sta.CancelType == CancelType.OnlyHitOrBlock || sta.CancelType == CancelType.OnlyWhenSpecified)
                                    continue;
                                cancableStates.Add(sta);
                            }
                            break;
                        case StateType.Normal:
                            allStates = new List<StateList.State>();
                            allStates.AddRange(states.SpecialStates);
                            allStates.AddRange(states.SuperStates);
                            foreach(StateList.State sta in allStates)
                            {
                                if (current.Position == StatePosition.Aerial && sta.Position != StatePosition.Aerial)
                                    continue;
                                if (sta.CancelType != CancelType.Whenever)
                                    continue;
                                hitOrBlockCancles.Add(sta.Name);
                            }
                            break;
                        case StateType.Super:
                            break;
                    }
                foreach (string sta in cancel.WhiffCancels)
                    if (states.HasState(sta))
                        cancableStates.Add(states.GetState(sta));
                hitCancels.AddRange(cancel.HitCancels);
                hitOrBlockCancles.AddRange(cancel.HitOrBlockCancels);
            }

            CallLuaFunc(state + "_Init", this);
        }

        public void SetAnimation(string anim)
        {
            curAnimName = anim;
            curAnimFrame = 0;
        }

        public BattleActor CreateChild(string startState, float offsetX, float offsetY)
        {
            BattleActor actor = new BattleActor(sprAn, states, input, luaCode, startState, RenderMode.Sprite, sprites, palTextures);
            actor.position.X = position.X + offsetX;
            actor.position.Y = position.Y + offsetY;
            actor.position.Z = position.Z;
            actor.dir = dir;
            actor.parent = this;
            actor.player = player;
            children.Add(actor);
            gameState.actors.Add(actor);
            return actor;
        }

        public void SetVelocity(float x, float y)
        {
            velocity.X = x * oneSixteth;
            velocity.Y = y * oneSixteth;
        }

        public void SetVelocityMod(float x, float y)
        {
            velocityMod.X = x * oneSixteth;
            velocityMod.Y = y * oneSixteth;
        }

        public void AddPosition(float x, float y)
        {
            position.X += x * (int)dir;
            position.Y += y;
        }

        public bool TouchingWall()
        {
            return collisionFlags.HasFlag(CollisionFlags.StageWall) && MathF.Abs(position.X + wallPushboxWidth * MathF.Sign(position.X)) > stage.stageWidth;
        }

        public bool TouchingDistanceWall(Vector3 opponentDist)
        {
            return collisionFlags.HasFlag(CollisionFlags.DistanceWall) && 
                MathF.Abs(opponentDist.X + wallPushboxWidth * MathF.Sign(position.X)) > stage.maxPlayerDistance;
        }

        public void SetInvincibility(bool b)
        {
            collisionFlags |= b ? CollisionFlags.Invincible : ~CollisionFlags.Invincible;
        }

        public bool InvulToAttrib(AttackHitAttribute attribute)
        {
            if (collisionFlags.HasFlag(CollisionFlags.Invincible))
                return false;
            switch(attackAttribute)
            {
                default:
                case AttackHitAttribute.Body:
                    return false;
                case AttackHitAttribute.Low:
                    return collisionFlags.HasFlag(CollisionFlags.InvulLow);
                case AttackHitAttribute.High:
                    return collisionFlags.HasFlag(CollisionFlags.InvulHigh);
                case AttackHitAttribute.Projectile:
                    return collisionFlags.HasFlag(CollisionFlags.InvulProjectile);
            }
        }

        public void AttackMacroWeak()
        {
            RefreshHit();
            damage = 20;
            hitstopOnHit = 7;
            hitstunOnHit = 8;
            HKDOnHit = 0;
            HKDInvulTimerOnHit = -1;
            crumpleOnHit = false;
            dirOnHitGround = new Vector2(-4.6f * oneSixteth, 0.0f);
            dirModOnHitGround = new Vector2(0.5f * oneSixteth, 0.0f);
            dirMinOnHitGround = new Vector2(-0.01f * oneSixteth, 0.0f);
            dirOnHitAir = new Vector2(-4.6f * oneSixteth, 12.0f * oneSixteth);
            dirModOnHitAir = new Vector2(0.0f, -0.7f * oneSixteth);
            dirMinOnHitAir = Vector2.Zero;
            hitStateStanding = HitstunStates.CmnHurtStandWeak;
            hitStateCrouching = HitstunStates.CmnHurtCrouchWeak;
            hitStateAerial = HitstunStates.CmnHurtLaunch;
            attackAttribute = AttackHitAttribute.Body;
        }

        public void AttackMacroMedium()
        {
            RefreshHit();
            damage = 30;
            hitstopOnHit = 9;
            hitstunOnHit = 10;
            HKDOnHit = 0;
            HKDInvulTimerOnHit = -1;
            crumpleOnHit = false;
            dirOnHitGround = new Vector2(-7.6f * oneSixteth, 0.0f);
            dirModOnHitGround = new Vector2(0.55f * oneSixteth, 0.0f);
            dirMinOnHitGround = new Vector2(-0.01f * oneSixteth, 0.0f);
            dirOnHitAir = new Vector2(-7.6f * oneSixteth, 17.0f * oneSixteth);
            dirModOnHitAir = new Vector2(0.0f, -0.7f * oneSixteth);
            dirMinOnHitAir = Vector2.Zero;
            hitStateStanding = HitstunStates.CmnHurtStandMedium;
            hitStateCrouching = HitstunStates.CmnHurtCrouchMedium;
            hitStateAerial = HitstunStates.CmnHurtLaunch;
            attackAttribute = AttackHitAttribute.Body;
        }

        public void AttackMacroHeavy()
        {
            RefreshHit();
            damage = 40;
            hitstopOnHit = 12;
            hitstunOnHit = 14;
            HKDOnHit = 0;
            HKDInvulTimerOnHit = -1;
            crumpleOnHit = false;
            dirOnHitGround = new Vector2(-9.8f * oneSixteth, 0.0f);
            dirModOnHitGround = new Vector2(0.68f * oneSixteth, 0.0f);
            dirMinOnHitGround = new Vector2(-0.01f * oneSixteth, 0.0f);
            dirOnHitAir = new Vector2(-9.8f * oneSixteth, 22.0f * oneSixteth);
            dirModOnHitAir = new Vector2(0.0f, -0.7f * oneSixteth);
            dirMinOnHitAir = Vector2.Zero;
            hitStateStanding = HitstunStates.CmnHurtStandHeavy;
            hitStateCrouching = HitstunStates.CmnHurtCrouchHeavy;
            hitStateAerial = HitstunStates.CmnHurtLaunch;
            attackAttribute = AttackHitAttribute.Body;
        }

        public void Delete()
        { 
            if(isPlayer)
            {
                Console.WriteLine("Trying to delete player, unable to do that, only delete children of player");
                return;
            }

            parent.children.Remove(this);
            for (int i = children.Count - 1; i >= 0; i--)
                children[i].Delete();
        }

        public EffectActor SpawnEffect(string animName, float offsetX, float offsetY, 
            bool loop = false, EffectActor.EffectFlags flags = EffectActor.EffectFlags.Default)
        {
            EffectActor actor = EffectActor.BeginAnim(this, effects, false, animName, loop);
            actor.dir = dir;
            actor.SetFlags(flags);
            if (flags.HasFlag(EffectActor.EffectFlags.FollowActorPos))
                actor.position = new Vector3(offsetX * (int)dir, offsetY, 0.0f);
            else
                actor.position = position + new Vector3(offsetX, offsetY, 0.0f);
            effectsActive.Add(animName, actor);
            return actor;
        }

        public void SetEffectRenderPriority(string eff, int priority)
        {
            if (effectsActive.ContainsKey(eff))
                effectsActive[eff].renderPriority = priority;
        }

        public void QueueDeleteEffect(string eff)
        {
            effectsToDelete.Add(eff);
        }

        public void PlaySound(string sound)
        {
            soundEffects.Play(sound);
        }

        public void CallLuaFunc(string function, params object[] args)
        {
            LuaFunction func = luaCode[function] as LuaFunction;
            if (func != null)
                func.Call(args);
        }

        public int GetInputDir()
        {
            int rtrn = inputBuffer[0].direction;
            if (dir == Direction.Right)
                if (rtrn == 1 || rtrn == 4 || rtrn == 7)
                    rtrn += 2;
                else if (rtrn == 3 || rtrn == 6 || rtrn == 9)
                    rtrn -= 2;
            return rtrn;
        }

        public void AddHitOrBlockCancels()
        {
            foreach (string state in hitOrBlockCancles)
                if (states.HasState(state))
                    cancableStates.Add(states.GetState(state));
        }

        public void RemoveCancel(string state)
        {
            foreach(StateList.State sta in cancableStates)
                if(sta.Name == state)
                {
                    cancableStates.Remove(sta);
                    break;
                }
        }

        public void Flip()
        {
            dir = (Direction)((int)dir * -1);
        }

        public void FaceActor(BattleActor actor)
        {
            dir = (Direction)MathF.Sign(GetDistanceFrom(actor).X);
        }

        public void RefreshHit()
        {
            actorsHit.Clear();
        }

        public void SetOpponent(BattleActor opponent)
        {
            this.opponent = opponent;
        }

        public void SetEffects(PrmAn effects)
        {
            if (this.effects == null)
                this.effects = effects;
        }

        public void SetSounds(AudioPlayer sounds)
        {
            if(soundEffects == null)
                soundEffects = sounds;
        }

        public void SetVoices(UniqueAudioPlayer voices)
        {
            if (voiceLines == null)
                voiceLines = voices;
        }

        public Vector3 GetDistanceFrom(BattleActor other)
        {
            if (other == null)
                return Vector3.Zero;
            return -(position - other.position);
        }

        public static bool HasButtons(ButtonType check, ButtonType checkFor)
        {
            return (check & checkFor) == checkFor;
        }

        public bool CheckBuffer(InputType input)
        {
            if ((int)input <= 9)
            {
                if (input == InputType.None)
                    return true;
                int inputDirection = inputBuffer[0].direction;
                if(dir == Direction.Right)
                    if (inputDirection == 1 || inputDirection == 4 || inputDirection == 7)
                        inputDirection += 2;
                    else if (inputDirection == 3 || inputDirection == 6 || inputDirection == 9)
                        inputDirection -= 2;
                return inputDirection == (int)input;
            }
            else if ((int)input >= 10 && (int)input <= 12)
                switch (input)
                {
                    case InputType.Crouching_Any:
                        return inputBuffer[0].direction >= 1 && inputBuffer[0].direction <= 3;
                    case InputType.Standing_Any:
                        return inputBuffer[0].direction >= 4 && inputBuffer[0].direction <= 6;
                    case InputType.Aerial_Any:
                        return inputBuffer[0].direction >= 7 && inputBuffer[0].direction <= 9;
                }

            string inputAsStr = Enum.GetName(typeof(InputType), input);
            inputAsStr = inputAsStr.Substring(1);
            int[] dirs = new int[inputAsStr.Length];
            for (int i = 0; i < dirs.Length; i++)
                dirs[i] = inputAsStr[i] - '0';

            if(dir == Direction.Right)
                for(int i = 0; i < dirs.Length; i++)
                    if (dirs[i] == 1 || dirs[i] == 4 || dirs[i] == 7)
                        dirs[i] += 2;
                    else if (dirs[i] == 3 || dirs[i] == 6 || dirs[i] == 9)
                        dirs[i] -= 2;

            if (inputBuffer.Count < 3)
                return false;

            int startPos = -1;
            for (int i = 0; i < 3; i++)
                if (inputBuffer[i].direction == dirs[dirs.Length - 1])
                {
                    startPos = i;
                    break;
                }

            if (dirs.Length == 2)
            {
                if (startPos < 0 || inputBuffer.Count < startPos + 2)
                    return false;

                if (inputBuffer[startPos + 1].direction == 5 && inputBuffer[startPos + 2].direction == dirs[1])
                    return true;
                return false;
            }
            //more than 2

            if (startPos < 0 || inputBuffer.Count < startPos + dirs.Length + 2)
                return false;

            int dirCheckingFor = dirs.Length - 2;
            for (int i = startPos; i < startPos + dirs.Length + 2; i++)
            {
                if (inputBuffer[i].direction == dirs[dirCheckingFor])
                    dirCheckingFor--;

                if (dirCheckingFor < 0)
                    break;
            }

            return dirCheckingFor < 0;
        }


        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum RenderMode
        {
            Sprite,
            Mesh,
        }

        public enum Direction
        {
            Left = 1,
            Right = -1,
        }

        public struct BufferItem
        {
            public int direction { get; set; }
            public ButtonType button { get; set; }
            public int timeInBuffer { get; set; }
            public int chargeTime { get; set; }

            public static bool operator ==(BufferItem a, BufferItem b)
            {
                return a.direction == b.direction && a.button == b.button;
            }

            public static bool operator !=(BufferItem a, BufferItem b)
            {
                return !(a == b);
            }

            public override bool Equals([NotNullWhen(true)] object obj)
            {
                return this == (BufferItem)obj;
            }

            public override int GetHashCode()
            {
                return direction ^ (int)button ^ timeInBuffer ^ chargeTime;
            }

            public bool IsRight()
            {
                return direction == 9 || direction == 6 || direction == 3;
            }

            public bool IsLeft()
            {
                return direction == 7 || direction == 4 || direction == 1;
            }

            public bool IsUp()
            {
                return direction == 7 || direction == 8 || direction == 9;
            }

            public bool IsDown()
            {
                return direction == 1 || direction == 2 || direction == 3;
            }
        }
    }
}
