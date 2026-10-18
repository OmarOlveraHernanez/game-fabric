namespace ChihuahuaJump.Logic
{
    /// <summary>Nivel de dificultad 0..1 segun el tiempo jugado.</summary>
    public static class Difficulty
    {
        public static float Level(GameConfig c, float elapsedSeconds) => MathUtil.Clamp01(elapsedSeconds / c.RampSeconds);
    }
}
