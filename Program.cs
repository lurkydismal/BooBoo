using System;
using Raylib_cs;
using BooBoo.GameState;
using BooBoo.Engine;
using BooBoo.Util;

namespace BooBoo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            Window.InitWindow();
            Raylib.InitAudioDevice();
            SystemManager.InitSystem();

            //if (args.Length > 0 && args[0] == "-Battle")
            if(true)
            {
                string[] chars = new string[2];
                for (int i = 0; i < 2; i++)
                {
                    Console.WriteLine($"Player {i} id");
                    chars[i] = Console.ReadLine();
                    if (!SystemManager.CharExists(chars[i]))
                    {
                        Console.WriteLine("Char doesnt exist. Put valid char id");
                        i--;
                    }
                }

                new BattleGameState() { charIds = chars, inputs = new InputHandler[] { new InputHandler(true, -1), new InputHandler(false, 0) } }.Init();
            }
            else if(true)
                new CSelGameState { p1Input = new InputHandler(true, -1), p2Input = new InputHandler(false, 0) }.Init();

            while(!Raylib.WindowShouldClose())
            {
                GameStateBase.gameState.Update();

                //it wouldnt be ready to draw yet
                if (!GameStateBase.switchingGameState)
                    GameStateBase.gameState.Draw();
                else
                    GameStateBase.switchingGameState = false;
            }

            Raylib.CloseAudioDevice();
            Raylib.CloseWindow();
        }
    }
}
