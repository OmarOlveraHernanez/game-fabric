namespace RescatePapi.Logic
{
    /// <summary>Curva de dificultad: la velocidad sube linealmente hasta un tope.</summary>
    public static class Difficulty
    {
        public static float SpeedAt(RunnerConfig c, float elapsedSeconds)
        {
            float s = c.BaseSpeed + c.Acceleration * elapsedSeconds;
            return s > c.MaxSpeed ? c.MaxSpeed : s;
        }

        /// <summary>0 = facil (velocidad base), 1 = dificil (velocidad maxima).</summary>
        public static float Level(RunnerConfig c, float speed) =>
            MathUtil.Clamp01((speed - c.BaseSpeed) / (c.MaxSpeed - c.BaseSpeed));
    }
}
