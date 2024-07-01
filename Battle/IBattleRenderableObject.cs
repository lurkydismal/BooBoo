namespace BooBoo.Battle
{
    internal interface IBattleRenderableObject
    {
        public int renderPriority { get; set; }

        public abstract void Draw();
    }
}
