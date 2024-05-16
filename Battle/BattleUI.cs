using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using BooBoo.Util;
using BlakieLibSharp;
using Raylib_cs;

namespace BooBoo.Battle
{
    internal class BattleUI
    {
        public BattleActor player1 { get; private set; }
        public BattleActor player2 { get; private set; }
        UIDrawer drawer;

        public BattleUI(BattleActor player1, BattleActor player2, PrmAn ui)
        {
            this.player1 = player1;
            this.player2 = player2;
            drawer = new UIDrawer(ui);
        }

        public void DrawLayer1()
        {
            //what the fuck
            drawer.DrawBlendedFrame(Vector2.Zero, Vector2.One, "HealthBarEmptyP1", "HealthBarFullP1", (float)((float)player1.curHealth / (float)player1.maxHealth));
            drawer.DrawBlendedFrame(Vector2.Zero, Vector2.One, "HealthBarEmptyP2", "HealthBarFullP2", (float)((float)player2.curHealth / (float)player2.maxHealth));
            drawer.DrawSingleFrame(Vector2.Zero, Vector2.One, "Top");
        }
    }
}
