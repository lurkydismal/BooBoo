using System;
using System.Text;
using System.Numerics;
using BooBoo.Engine;
using BooBoo.Util;
using Newtonsoft.Json;
using Raylib_cs;

namespace BooBoo.GameState
{
    internal class CSelGameState : GameStateBase
    {
        static CselTable cselTable;
        UIDrawer drawer;
        public InputHandler p1Input, p2Input;
        public bool onePlayer = false;

        CselState cselState;
        int cselTimer;

        Vector2 p1Pos, p2Pos;
        int p1Timer, p2Timer;
        PlayerState p1State, p2State;
        int p1Color, p2Color;

        public override void Init()
        {
            gameState = this;
            drawer = new UIDrawer(FileHelper.LoadPrmAn("UI/Csel/CSelUI.prman"));
            foreach (string charId in SystemManager.GetAllIds())
                if (FileHelper.FileExists("UI/Csel/" + charId + ".prman"))
                    drawer.AddAdditionalPrmAn(FileHelper.LoadPrmAn("UI/Csel/" + charId + ".prman"));

            if (onePlayer)
                cselState = CselState.OnePlayerSelectP1;
            else
                cselState = CselState.TwoPlayerSelect;

            p1Pos = new Vector2(cselTable.P1Start[0], cselTable.P1Start[1]);
            p2Pos = new Vector2(cselTable.P2Start[0], cselTable.P2Start[1]);
            p1State = PlayerState.SelectingChar;
            p2State = PlayerState.SelectingChar;
        }

        public override void Update()
        {
            //Console.WriteLine(cselState);
            //Console.WriteLine(p1State);
            //Console.WriteLine(p2State);
            switch(cselState)
            {
                default:
                case CselState.StageSelectTimer:
                case CselState.DoneTimer:
                    cselTimer--;
                    if (cselTimer == 0)
                        cselState = (CselState)(int)cselState + 1;
                    break;
                case CselState.TwoPlayerSelect:
                    UpdatePlayer(p1Input, ref p1Pos, ref p1Timer, ref p1State, ref p1Color);
                    UpdatePlayer(p2Input, ref p2Pos, ref p2Timer, ref p2State, ref p2Color);
                    if (p1State == PlayerState.Done && p2State == PlayerState.Done)
                    {
                        cselTimer = 180;
                        cselState = CselState.DoneTimer;
                        //cselState = CselState.StageSelectTimer; we dont care about stage rn just go to battle
                    }
                    break;
                case CselState.StageSelect:
                    break;
                case CselState.Done:
                    End();
                    break;
            }
        }

        void UpdatePlayer(InputHandler input, ref Vector2 pos, ref int timer, ref PlayerState state, ref int color)
        {
            Vector2 move = input.GetDirectionVector();
            switch (state)
            {
                default:
                case PlayerState.SelectingCharTimer:
                case PlayerState.SelectingColorTimer:
                case PlayerState.DoneTimer:
                    timer--;
                    if (timer == 0)
                        state = (PlayerState)(int)state + 1;
                    break;
                case PlayerState.SelectingChar:
                    if (timer > 0)
                    {
                        timer--;
                        break;
                    }
                    if (Math.Abs(move.X) >= 0.5f)
                    {
                        int sign = Math.Sign(move.X);
                        pos.X += 1 * sign;
                        if (pos.X < 0)
                            pos.X = cselTable.CharTable[(int)pos.Y].Length - 1;
                        else if (pos.X >= cselTable.CharTable[(int)pos.Y].Length)
                            pos.X = 0;
                        timer = 15;
                    }
                    if(Math.Abs(move.Y) >= 0.5f)
                    {
                        int sign = Math.Sign(move.Y);
                        pos.Y += 1 * sign;
                        if (pos.Y < 0)
                            pos.Y = cselTable.CharTable.Length - 1;
                        else if (pos.Y >= cselTable.CharTable.Length)
                            pos.Y = 0;
                        timer = 15;
                    }

                    if (input.MenuSelectDown())
                    {
                        if (cselTable.CharTable[(int)pos.Y][(int)pos.X] == "Rng")
                        {
                            Retry:
                            pos.Y = Raylib.GetRandomValue(0, cselTable.CharTable.Length - 1);
                            pos.X = Raylib.GetRandomValue(0, cselTable.CharTable[(int)pos.Y].Length - 1);
                            if (cselTable.CharTable[(int)pos.Y][(int)pos.X] == "Rng")
                                goto Retry;
                        }
                        //state = PlayerState.SelectingColorTimer;
                        state = PlayerState.DoneTimer;
                        timer = 30;
                    }
                    break;
                case PlayerState.SelectingColor:
                    if(timer > 0)
                    {
                        timer--;
                        break;
                    }

                    int colorCount = ((CselTable.PositionThing)cselTable.GetPositionThingForChar(cselTable.CharTable[(int)pos.Y][(int)pos.X])).ColorCount;
                    int costumeCount = ((CselTable.PositionThing)cselTable.GetPositionThingForChar(cselTable.CharTable[(int)pos.Y][(int)pos.X])).CostumeCount;
                    
                    if(Math.Abs(move.X) >= 0.5f)
                    {
                        color += 1 * Math.Sign(move.X);
                        if (color < 0)
                            color = colorCount - 1;
                        else if (color >= colorCount)
                            color = 0;
                        timer = 15;
                    }

                    if(input.MenuSelectDown())
                    {
                        state = PlayerState.DoneTimer;
                        timer = 120;
                    }
                    else if(input.MenuBackDown())
                    {
                        state = PlayerState.SelectingCharTimer;
                        timer = 30;
                        color = 0;
                    }
                    break;
                case PlayerState.Done:
                    if(input.MenuBackDown())
                    {
                        state = PlayerState.SelectingCharTimer;
                        timer = 30;
                    }
                    break;
            }
        }

        public override void Draw()
        {
            Window.BeginDrawing();

            foreach (CselTable.PositionThing charPos in cselTable.Positioning)
            {
                drawer.DrawSingleFrame(new Vector2(charPos.X, charPos.Y), Vector2.One, "Icon_" + charPos.Char);
                drawer.DrawSingleFrame(new Vector2(charPos.X, charPos.Y), Vector2.One, "PortraitBorder");
            }
            drawer.DrawSingleFrame(new Vector2(cselTable.RngPos[0], cselTable.RngPos[1]), Vector2.One, "PortraitRng");

            if(p1Pos != p2Pos)
            {
                string p1Id = cselTable.CharTable[(int)p1Pos.Y][(int)p1Pos.X];
                string p2Id = cselTable.CharTable[(int)p2Pos.Y][(int)p2Pos.X];

                if (p1Id == "Rng")
                    drawer.DrawSingleFrame(new Vector2(cselTable.RngPos[0], cselTable.RngPos[1]), Vector2.One, "PortraitSelectP1");
                else
                {
                    CselTable.PositionThing charPos1 = (CselTable.PositionThing)cselTable.GetPositionThingForChar(p1Id);
                    drawer.DrawSingleFrame(new Vector2(charPos1.X, charPos1.Y), Vector2.One, "PortraitSelectP1");
                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "PortraitP1_" + charPos1.Char);
                }

                if (p2Id == "Rng")
                    drawer.DrawSingleFrame(new Vector2(cselTable.RngPos[0], cselTable.RngPos[1]), Vector2.One, "PortraitSelectP2");
                else
                {
                    CselTable.PositionThing charPos2 = (CselTable.PositionThing)cselTable.GetPositionThingForChar(p2Id);
                    drawer.DrawSingleFrame(new Vector2(charPos2.X, charPos2.Y), Vector2.One, "PortraitSelectP2");
                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "PortraitP2_" + charPos2.Char);
                }
                
                drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "NameCards");
                if(p1Id != "Rng")
                    drawer.DrawText(new Vector2(10.0f, 450.0f), 50, SystemManager.GetChar(p1Id).Name_Long, FontsManager.FontTypes.Metamorphous, Color.White);
                if(p2Id != "Rng")
                    drawer.DrawText(new Vector2(1000.0f, 450.0f), 50, SystemManager.GetChar(p2Id).Name_Long, FontsManager.FontTypes.Metamorphous, Color.White);
            }
            else
            {
                string id = cselTable.CharTable[(int)p1Pos.Y][(int)p1Pos.X];
                if (id == "Rng")
                {
                    drawer.DrawSingleFrame(new Vector2(cselTable.RngPos[0], cselTable.RngPos[1]), Vector2.One, "PortraitSelectP3");
                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "NameCards");
                }
                else
                {
                    CselTable.PositionThing charPos = (CselTable.PositionThing)cselTable.GetPositionThingForChar(id);
                    drawer.DrawSingleFrame(new Vector2(charPos.X, charPos.Y), Vector2.One, "PortraitSelectP3");
                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "PortraitP1_" + charPos.Char);
                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "PortraitP2_" + charPos.Char);

                    drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "NameCards");
                    drawer.DrawText(new Vector2(10.0f, 450.0f), 50, SystemManager.GetChar(charPos.Char).Name_Long, FontsManager.FontTypes.Metamorphous, Color.White);
                    drawer.DrawText(new Vector2(1000.0f, 450.0f), 50, SystemManager.GetChar(charPos.Char).Name_Long, FontsManager.FontTypes.Metamorphous, Color.White);

                }
            }

            Window.FinalizeDrawing();
        }

        public override void End()
        {
            Console.WriteLine("End");
            switchingGameState = true;
            drawer.Dispose();
            string[] charIds = new string[2] { cselTable.CharTable[(int)p1Pos.Y][(int)p1Pos.X], cselTable.CharTable[(int)p2Pos.Y][(int)p2Pos.X] };
            new BattleGameState { charIds = charIds, inputs = new InputHandler[] { p1Input, p2Input } }.Init();
        }

        public static void InitCselTable(CselTable table)
        {
            cselTable = table;
        }

        public struct CselTable
        {
            [JsonProperty("CharTable")]
            public string[][] CharTable;
            [JsonProperty("SongTable")]
            public string[] SongTable;
            [JsonProperty("StageTable")]
            public string[] StageTable;
            [JsonProperty("Positioning")]
            public PositionThing[] Positioning;
            [JsonProperty("RngPos")]
            public int[] RngPos;
            [JsonProperty("P1Start")]
            public int[] P1Start;
            [JsonProperty("P2Start")]
            public int[] P2Start;

            public PositionThing? GetPositionThingForChar(string id)
            {
                foreach(PositionThing pos in Positioning)
                    if(pos.Char == id)
                        return pos;
                return null;
            }

            public struct PositionThing //i couldnt think of a better name :skull:
            {
                [JsonProperty("Char")]
                public string Char;
                [JsonProperty("X")]
                public int X;
                [JsonProperty("Y")]
                public int Y;
                [JsonProperty("ColorCount")]
                public int ColorCount;
                [JsonProperty("CostumeCount")]
                public int CostumeCount;
            }
        }

        enum CselState
        {
            StartUp,
            TwoPlayerSelect,
            OnePlayerSelectP1,
            OnePlayerSelectP2,
            StageSelectTimer,
            StageSelect,
            DoneTimer,
            Done,
        }

        enum PlayerState
        {
            SelectingCharTimer,
            SelectingChar,
            SelectingColorTimer,
            SelectingColor,
            DoneTimer,
            Done,
        }
    }
}
