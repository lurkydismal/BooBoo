using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using BooBoo.Battle;
using BooBoo.Util;
using BooBoo.Engine;
using Newtonsoft.Json;
using BlakieLibSharp;
using Raylib_cs;
using NLua;

namespace BooBoo.GameState
{
    internal class BattleGameState : GameStateBase
    {
        public List<BattleActor> actors { get; private set; } = new List<BattleActor>();
        public BattleActor player1, player2;
        public string[] charIds;
        public InputHandler[] inputs;
        public BattleUI ui { get; private set; }
        public BattleStage stage { get; private set; }

        public Shader spriteShader { get; private set; }
        public int sprShaderPalLoc { get; private set; }

        RenderTexture2D bgTex, charBoxTex, superBg;

        public bool superFreeze { get; private set; } = false;
        public int superFreezeTime { get; private set; } = 0;
        BattleActor freezeStarter = null;

        public bool drawBoxes = false;
        static readonly Color[] colliderTypeColors = {
            new Color(255, 255, 60, 180),
            new Color(90, 255, 60, 180),
            new Color(255, 60, 60, 180),
            new Color(60, 75, 255, 180),
            new Color(175, 60, 255, 180),
            new Color(175, 100, 255, 180),
            new Color(175, 140, 255, 180),
            new Color(145, 20, 20, 180),
            new Color(79, 255, 226, 180)
        };

        public override void Init()
        {
            gameState = this;
            spriteShader = (Shader)FileHelper.LoadShader("Shader/SpriteChar.vert", "Shader/SpriteChar.frag");
            sprShaderPalLoc = Raylib.GetShaderLocation(spriteShader, "palette");

            bgTex = Raylib.LoadRenderTexture(1280, 720);
            charBoxTex = Raylib.LoadRenderTexture(1280, 720);
            stage = new BattleStage("Debug");

            DPArc commonArchive = FileHelper.LoadArchive("Char/Cmn.dparc");
            PrmAn commonEffects = new PrmAn(commonArchive.GetFile("CmnEffect.prman").data);
            commonEffects.LoadTexturesToGPU();
            AudioPlayer commonSounds = AudioPlayer.LoadSoundsInArchive(commonArchive, "Audio");

            for (int i = 0; i < charIds.Length; i++)
            {
                Console.WriteLine("Loading char " + i);
                DPArc charArc = FileHelper.LoadArchive($"Char/{charIds[i]}.dparc");
                CharLoadDat charLoad = JsonConvert.DeserializeObject<CharLoadDat>(charArc.GetFile("Char.init").DataAsString());
                DPSpr charSprite = new DPSpr(charArc.GetFile(charLoad.Sprites[0]).data);
                charSprite.LoadTexturesToGPU();
                PrmAn effects = charArc.FileExists(charLoad.Effects) ? new PrmAn(charArc.GetFile(charLoad.Effects).data) : null;
                if (effects != null)
                {
                    effects.LoadTexturesToGPU();
                    effects.Combine(commonEffects);
                }
                else
                    effects = commonEffects;
                AudioPlayer sounds = commonSounds;
                StateList charList = JsonConvert.DeserializeObject<StateList>(charArc.GetFile(charLoad.StateLists[0]).DataAsString());
                charList.UpdateStateMap();
                SprAn yuSprAn = new SprAn(charArc.GetFile(charLoad.SprAn).data);
                CharLoadDat.PaletteLoadStruct pal = charLoad.Palettes[0];
                Texture2D[] palTextures = new Texture2D[pal.ColorsInPal];
                for (int i2 = 0; i2 < pal.ColorsInPal; i2++)
                {
                    Image img = Raylib.LoadImageFromMemory(".png", charArc.GetFile(pal.Pals[i2]).data);
                    palTextures[i2] = Raylib.LoadTextureFromImage(img);
                    Raylib.UnloadImage(img);
                    Raylib.SetTextureFilter(palTextures[i2], TextureFilter.Point);
                }
                Lua luaCode = new Lua();
                luaCode.LoadCLRPackage();
                luaCode.DoString(commonArchive.GetFile("CmnScript.lua").DataAsString());
                luaCode.DoString(charArc.GetFile(charLoad.Scripts[0]).DataAsString());
                BattleActor actor = new BattleActor(yuSprAn, charList, inputs[i], luaCode, "CmnStand", charLoad.RenderMode, charSprite, palTextures);
                actor.SetEffects(effects);
                actor.SetSounds(sounds);
                actors.Add(actor);
                Console.WriteLine("Finished Loading char " + i);
                if(i == 0 && charIds[0] == charIds[1])
                {
                    i++;
                    luaCode = new Lua();
                    luaCode.LoadCLRPackage();
                    luaCode.DoString(commonArchive.GetFile("CmnScript.lua").DataAsString());
                    luaCode.DoString(charArc.GetFile(charLoad.Scripts[0]).DataAsString());
                    actor = new BattleActor(yuSprAn, charList, inputs[i], luaCode, "CmnStand", charLoad.RenderMode, charSprite, palTextures);
                    actor.SetEffects(effects);
                    actor.SetSounds(sounds);
                    actors.Add(actor);
                }
                charArc.Dispose();
            }
            if (actors.Count == 2)
            {
                actors[0].SetOpponent(actors[1]);
                actors[0].position.X = -3.0f;
                actors[0].collisionFlags = CollisionFlags.DefaultPlayerSettings;
                actors[0].renderPriority = 1;
                actors[1].SetOpponent(actors[0]);
                actors[1].position.X = 3.0f;
                actors[1].collisionFlags = CollisionFlags.DefaultPlayerSettings;
                actors[1].dir = BattleActor.Direction.Right;
                ui = new BattleUI(actors[0], actors[1], FileHelper.LoadPrmAn("UI/Battle/BattleUI.prman"));
                new BattleCamera() { player1 = actors[0], player2 = actors[1] };
            }
            else
                new BattleCamera() { player1 = actors[0], player2 = actors[0] };
            int actorCount = actors.Count;
            for (int i = 0; i < actorCount; i++)
                actors[i].MatchInit();
        }

        public override void Update()
        {
            foreach(BattleActor actor in actors)
                actor.Update();

            if (superFreeze)
            {
                superFreezeTime--;
                if (superFreezeTime <= 0)
                {
                    superFreeze = false;
                    freezeStarter.ignoreFreeze = false;
                    freezeStarter = null;
                }
            }

            stage.Update();
            BattleCamera.activeCamera.Update();
        }

        public override void Draw()
        {
            List<IBattleRenderableObject> actorsToDraw = new List<IBattleRenderableObject>();
            foreach(BattleActor actor in actors)
            {
                actorsToDraw.Add(actor);
                foreach (EffectActor eff in actor.effectsActive.Values)
                    actorsToDraw.Add(eff);
            }
            actorsToDraw = actorsToDraw.OrderBy(obj => obj.renderPriority).ToList();

            Raylib.BeginTextureMode(bgTex);
            Raylib.ClearBackground(Color.Blank);
            stage.Draw();
            Raylib.EndMode3D();
            Raylib.EndTextureMode();

            //Draw Boxes
            if (drawBoxes)
            {
                Raylib.BeginTextureMode(charBoxTex);
                Raylib.ClearBackground(Color.Blank);
                Raylib.BeginMode3D(BattleCamera.activeCamera);
                foreach (IBattleRenderableObject obj in actorsToDraw)
                {
                    BattleActor actor = obj as BattleActor;
                    if (actor == null)
                        continue;
                    if (actor.curFrame == null)
                        continue;
                    foreach (RectCollider rect in actor.curFrame.colliders)
                        Raylib.DrawCube(actor.position + new Vector3(rect.x * (int)actor.dir + rect.width / 2.0f * (int)actor.dir, rect.y + rect.height / 2.0f, 0.0f), 
                            rect.width, rect.height, 0.0f, colliderTypeColors[(int)rect.colliderType]);
                }
                Raylib.EndMode3D();
                Raylib.EndTextureMode();
            }

            //Draw them to screen
            Window.BeginDrawing();
            Raylib.DrawTextureRec(bgTex.Texture, new Rectangle(0, 0, bgTex.Texture.Width, -bgTex.Texture.Height), Vector2.Zero, Color.White);
            ui.DrawLayer1();
            Raylib.BeginMode3D(BattleCamera.activeCamera);
            foreach (IBattleRenderableObject actor in actorsToDraw)
                actor.Draw();
            Raylib.EndMode3D();
            if (drawBoxes)
                Raylib.DrawTextureRec(charBoxTex.Texture, new Rectangle(0, 0, charBoxTex.Texture.Width, -charBoxTex.Texture.Height), Vector2.Zero, Color.White);
            Window.FinalizeDrawing();
        }

        public override void End()
        {
            Raylib.UnloadShader(spriteShader);

            Raylib.UnloadRenderTexture(bgTex);
            Raylib.UnloadRenderTexture(charBoxTex);
        }

        public static void BeginSuperFreeze(int time, BattleActor starter)
        {
            BattleGameState state = gameState as BattleGameState;
            state.superFreeze = true;
            state.superFreezeTime = time;
            state.freezeStarter = starter;
            starter.ignoreFreeze = true;
        }

        struct CharLoadDat
        {
            [JsonProperty("Char")]
            public string Char;
            [JsonProperty("SprAn")]
            public string SprAn;
            [JsonProperty("RenderMode")]
            public BattleActor.RenderMode RenderMode;
            [JsonProperty("CostumeCount")]
            public int CostumeCount;
            [JsonProperty("Sprites")]
            public string[] Sprites;
            [JsonProperty("Effects")]
            public string Effects;
            [JsonProperty("PalCount")]
            public int PalCount;
            [JsonProperty("Palettes")]
            public PaletteLoadStruct[] Palettes;
            [JsonProperty("StateLists")]
            public string[] StateLists;
            [JsonProperty("Scripts")]
            public string[] Scripts;

            public struct PaletteLoadStruct
            {
                [JsonProperty("ColorsInPal")]
                public int ColorsInPal;
                [JsonProperty("Pals")]
                public string[] Pals;
            }
        }

        struct SoundLoadDat
        {
            [JsonProperty("Name")]
            public string Name;
            [JsonProperty("File")]
            public string File;
        }
    }
}
