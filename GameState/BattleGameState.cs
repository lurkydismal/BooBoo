using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;
using BooBoo.Battle;
using BooBoo.Util;
using BooBoo.Engine;
using Newtonsoft.Json;
using BlakieLibSharp;
using Raylib_cs;
using NLua;
using System.Linq;

namespace BooBoo.GameState
{
    internal class BattleGameState : GameStateBase
    {
        public List<BattleActor> actors { get; private set; } = new List<BattleActor>();
        List<BattleActor> actorsToDraw = new List<BattleActor>();
        public BattleActor player1, player2;
        public string[] charIds;
        public InputHandler[] inputs;
        public BattleUI ui { get; private set; }
        public BattleStage stage { get; private set; }

        public Shader spriteShader { get; private set; }
        public int sprShaderPalLoc { get; private set; }

        RenderTexture2D bgTex, uiTex1, charTex, charBoxTex, uiTex2, superBg;
        Rectangle renderTexRect;

        public bool drawBoxes = true;
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
            charTex = Raylib.LoadRenderTexture(1280, 720);
            charBoxTex = Raylib.LoadRenderTexture(1280, 720);
            uiTex1 = Raylib.LoadRenderTexture(1280, 720);
            renderTexRect = new Rectangle(0, 0, 1280, -720);
            stage = new BattleStage("Debug");

            for (int i = 0; i < charIds.Length; i++)
            {
                Console.WriteLine("Loading char " + i);
                DPArc charArc = FileHelper.LoadArchive($"Char/{charIds[i]}.dparc");
                CharLoadDat charLoad = JsonConvert.DeserializeObject<CharLoadDat>(charArc.GetFile("Char.init").DataAsString());
                DPSpr charSprite = new DPSpr(charArc.GetFile(charLoad.Sprites[0]).data);
                charSprite.LoadTexturesToGPU();
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
                luaCode.DoString(charArc.GetFile(charLoad.Scripts[0]).DataAsString());
                actors.Add(new BattleActor(yuSprAn, charList, inputs[i], luaCode, "CmnStand", charLoad.RenderMode, charSprite, palTextures));
                charArc.Dispose();
                Console.WriteLine("Finished Loading char " + i);
                if(i == 0 && charIds[1] == charIds[0])
                {
                    i++;
                    actors.Add(new BattleActor(yuSprAn, charList, inputs[i], luaCode, "CmnStand", charLoad.RenderMode, charSprite, palTextures));
                }
            }
            if (actors.Count == 2)
            {
                actors[0].SetOpponent(actors[1]);
                actors[0].position.X = -3.0f;
                actors[0].collisionFlags = CollisionFlags.DefaultPlayerSettings;
                actors[1].SetOpponent(actors[0]);
                actors[1].position.X = 3.0f;
                actors[1].renderOffset = -0.001f;
                actors[0].collisionFlags = CollisionFlags.DefaultPlayerSettings;
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
            {
                actor.Update();
                actorsToDraw.Add(actor);
            }

            stage.Update();
            BattleCamera.activeCamera.Update();
        }

        public override void Draw()
        {
            actorsToDraw = actorsToDraw.OrderBy(actor => actor.renderOffset).ToList();

            //Draw characters first for shadows and stuff later
            Raylib.BeginTextureMode(charTex);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Raylib.BeginMode3D(BattleCamera.activeCamera);
            foreach (BattleActor actor in actorsToDraw)
                actor.Draw();
            Raylib.EndMode3D();
            Raylib.EndTextureMode();

            //Draw Boxes
            if (drawBoxes)
            {
                Raylib.BeginTextureMode(charBoxTex);
                Raylib.ClearBackground(new Color(0, 0, 0, 0));
                Raylib.BeginMode3D(BattleCamera.activeCamera);
                foreach (BattleActor actor in actorsToDraw)
                {
                    if (actor.curFrame == null)
                        continue;
                    foreach (RectCollider rect in actor.curFrame.colliders)
                        Raylib.DrawCube(actor.position + rect.GetCenter().ToVector3(actor.renderOffset + 0.01f), rect.width,
                            rect.height, 0.0f, colliderTypeColors[(int)rect.colliderType]);
                }
                Raylib.EndMode3D();
                Raylib.EndTextureMode();
            }
            actorsToDraw.Clear();
             
            //Draw bg 1
            Raylib.BeginTextureMode(bgTex);
            stage.Draw();
            Raylib.EndTextureMode();

            //Draw UI 1
            Raylib.BeginTextureMode(uiTex1);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            ui.DrawLayer1();
            Raylib.EndTextureMode();

            //Draw them to screen
            Window.BeginDrawing();
            Raylib.DrawTextureRec(bgTex.Texture, renderTexRect, Vector2.Zero, Color.White);
            Raylib.DrawTextureRec(uiTex1.Texture, renderTexRect, Vector2.Zero, Color.White);
            Raylib.DrawTextureRec(charTex.Texture, renderTexRect, Vector2.Zero, Color.White);
            if (drawBoxes)
                Raylib.DrawTextureRec(charBoxTex.Texture, renderTexRect, Vector2.Zero, Color.White);
            Window.FinalizeDrawing();
        }

        public override void End()
        {
            Raylib.UnloadShader(spriteShader);

            Raylib.UnloadRenderTexture(bgTex);
            Raylib.UnloadRenderTexture(charTex);
            Raylib.UnloadRenderTexture(charBoxTex);
            Raylib.UnloadRenderTexture(uiTex1);
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
    }
}
