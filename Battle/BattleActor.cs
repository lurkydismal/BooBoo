using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
        public Vector3 rotation = Vector3.Zero;
        public Vector3 scale = Vector3.One;
        public float renderOffset = 0.001f;
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
        public DPSpr sprites { get; private set; }
        public SprAn sprAn { get; protected set; }
        public StateList states { get; private set; }
        public string curState { get; private set; } = "CmnStand";
        public string curAnimName { get; private set; } = "CmnStand";
        public bool animBlending = false;
        public SprAn.SprAnState curAnim {  get { return sprAn.GetState(curAnimName); } }
        public int curAnimFrame = 0;
        public SprAn.SprAnFrame curFrame { get { return sprAn.GetFrame(curAnimName, curAnimFrame); } }

        //Hurt values
        public int hitstopTime = 0;
        public int hitstunTime = 0; //if on ground this is time in hitstun state + state anim time, in air this is time before you can wake up.
        public int HKDTime = 0; //time youre stuck on the ground before you can wake up
        public int HKDBeginInvulTime = -1; //once this reaches zero you get set to invincible until you wake up
        public HitstunStates hitstunState = HitstunStates.None;
        public Vector2 hitDirection = Vector2.Zero;
        public Vector2 hitDirMod = Vector2.Zero;
        public Vector2 hitDirMin = Vector2.Zero; //once direction reaches this itll stop applying mod on whatever axis

        //Hit values
        public int damage = 0;
        public int hitstopOnHit = 0;
        public int hitstunOnHit = 0;
        public int HKDOnHit = 0;
        public int HKDInvulTimerOnHit = -1;
        public Vector2 dirOnHit = Vector2.Zero;
        public Vector2 dirModOnHit = Vector2.Zero;
        public Vector2 dirMinOnHit = Vector2.Zero; //once direction reaches this itll stop applying mod on whatever axis
        public HitstunStates hitStateStanding = HitstunStates.CmnHurtStandWeak;
        public HitstunStates hitStateCrouching = HitstunStates.CmnHurtCrouchWeak;
        public HitstunStates hitStateAerial = HitstunStates.CmnHurtLaunch;


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
        public CollisionFlags collisionFlags = CollisionFlags.DefaultSettings;
        public float wallPushboxWidth = 0.5f; //This will be used to check distance from wall instead of pushbox

        public int frameActiveTime = -1;
        public int frameLength { get { return curFrame.frameLength; } }

        List<StateList.State> cancableStates = new List<StateList.State>();
        List<BufferItem> inputBuffer = new List<BufferItem>() { new BufferItem() };


        public BattleActor opponent { get; private set; }
        List<BattleActor> children = new List<BattleActor>();
        public BattleActor parent { get; private set; }
        public BattleActor player { get; private set; }
        bool isPlayer = false;

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
            if (curFrame == null)
                return;

            hitstopTime = hitstopTime > 0 ? hitstopTime-- : 0;
            if (hitstopTime > 0)
                goto BufferUpdate;

            frameActiveTime++;
            CallLuaFunc(curState + "_Tick", this);
            Vector3 opponentDist = GetDistanceFrom(opponent);
            if (states.HasState(curState) && states[curState].StateCanTurn)
                if (MathF.Sign(opponentDist.X) == -(int)dir)
                    EnterState(states[curState].TurnState);

            if(frameActiveTime >= frameLength)
            {
                frameActiveTime = -1;
                curAnimFrame++;
                CallLuaFunc(curState + "_Update", this, curAnimFrame);
                if(curAnimFrame >= curAnim.frameCount)
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

            #region physics update
            velocity += velocityMod;
            position += new Vector3(velocity.X * (int)dir, velocity.Y, velocity.Z);
            if(collisionFlags.HasFlag(CollisionFlags.Floor) && position.Y < 0.0f)
            {
                position.Y = 0.0f;
                velocity.Y = 0.0f;
                velocityMod.Y = 0.0f;
                if(states.HasState(curState))
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

                if (!ourPush.Value.Overlaps(position.XY(), theirPush.Value, actor.position.XY()))
                    continue;

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
                    frame = curFrame.BlendFrames(curAnim.frames[curAnimFrame + 1], frameActiveTime / frameLength);
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
                            Rlgl.Vertex3f(0.0f, 0.0f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(0.0f, 1.0f);
                            Rlgl.Vertex3f(0.0f, -sprite.height  * 0.01f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(1.0f, 1.0f);
                            Rlgl.Vertex3f(sprite.width * 0.01f, -sprite.height * 0.01f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(1.0f, 0.0f);
                            Rlgl.Vertex3f(sprite.width * 0.01f, 0.0f, renderOffset);
                        }
                        else
                        {
                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(uv.uv.X / sprite.width, uv.uv.Y / sprite.height);
                            Rlgl.Vertex3f(0.0f, 0.0f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f(uv.uv.X / sprite.width, (uv.uv.Y + uv.uv.W) / sprite.height);
                            Rlgl.Vertex3f(0.0f, -uv.uv.W * 0.01f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f((uv.uv.X + uv.uv.Z) / sprite.width, (uv.uv.Y + uv.uv.W) / sprite.height);
                            Rlgl.Vertex3f(uv.uv.Z * 0.01f, -uv.uv.W * 0.01f, renderOffset);

                            Rlgl.Color4ub(multColor.R, multColor.G, multColor.B, multColor.A);
                            Rlgl.TexCoord2f((uv.uv.X + uv.uv.Z) / sprite.width, uv.uv.Y / sprite.height);
                            Rlgl.Vertex3f(uv.uv.Z * 0.01f, 0.0f, renderOffset);
                        }

                        Rlgl.End();
                        Rlgl.PopMatrix();
                        Rlgl.SetTexture(0);
                    }
                }

                Raylib.EndShaderMode();
            }
        }

        public void EnterState(string state, bool callEnd = true)
        {
            if(callEnd)
                CallLuaFunc(curState + "_End", this);
            curState = state;
            curAnimName = state;
            curAnimFrame = 0;
            frameActiveTime = -1;

            cancableStates.Clear();
            if(states.HasState(state))
            {
                StateList.State cancel = states.GetState(state);
                StateList.State current = states.GetState(curState);
                List<StateList.State> allStates;
                switch(cancel.stateType)
                {
                    default:
                    case StateType.Neutral:
                        allStates = states.AllStates;
                        foreach (StateList.State sta in allStates)
                        {
                            if (sta.Name == curState || sta.Name == current.NextState ||
                                (sta.Position != current.Position && (sta.stateType != StateType.Neutral || sta.stateType != StateType.Normal)))
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
                            cancableStates.Add(sta);
                        }
                        break;
                }
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
            children.Add(actor);
            actor.parent = this;
            actor.player = player;
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

        public bool TouchingWall()
        {
            return collisionFlags.HasFlag(CollisionFlags.StageWall) && MathF.Abs(position.X + wallPushboxWidth * MathF.Sign(position.X)) > stage.stageWidth;
        }

        public bool TouchingDistanceWall(Vector3 opponentDist)
        {
            return collisionFlags.HasFlag(CollisionFlags.DistanceWall) && MathF.Abs(opponentDist.X + wallPushboxWidth * MathF.Sign(position.X)) > stage.maxPlayerDistance;
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

        public void SetOpponent(BattleActor opponent)
        {
            this.opponent = opponent;
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
        }
    }
}
