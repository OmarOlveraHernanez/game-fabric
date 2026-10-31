namespace RescatePapi.Logic
{
    /// <summary>Curva de dificultad: velocidad (lineal hasta un tope; hoy fija) y nivel por tiempo.</summary>
    public static class Difficulty
    {
        public static float SpeedAt(RunnerConfig c, float elapsedSeconds)
        {
            float s = c.BaseSpeed + c.Acceleration * elapsedSeconds;
            return s > c.MaxSpeed ? c.MaxSpeed : s;
        }

        /// <summary>0 = inicio, 1 = dificultad maxima (a los DifficultyRampTime segundos).</summary>
        public static float LevelAt(RunnerConfig c, float elapsedSeconds) =>
            c.DifficultyRampTime <= 0f ? 1f : MathUtil.Clamp01(elapsedSeconds / c.DifficultyRampTime);
    }
}
