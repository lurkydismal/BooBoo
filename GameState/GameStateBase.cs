using System;

namespace BooBoo.GameState
{
    internal abstract class GameStateBase
    {
        public static GameStateBase gameState { get; protected set; }
        public static bool switchingGameState { get; set; } //not protected because after we just dont draw the first frame simple.
        //what does that mean idk its 4 am im just writing words ill prolly change it later

        public abstract void Init();
        public abstract void Update();
        public abstract void Draw();
        public abstract void End();
    }
}
