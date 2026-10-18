namespace __GAME__.Logic
{
    /// <summary>
    /// Ejemplo de logica en C# puro (sin Unity): se prueba en EditMode.
    /// Reemplazar por la simulacion del juego.
    /// </summary>
    public sealed class ScoreKeeper
    {
        public int Score { get; private set; }
        public int Best { get; private set; }

        public void Add(int points)
        {
            if (points <= 0) return;
            Score += points;
            if (Score > Best) Best = Score;
        }

        public void ResetRun() => Score = 0;
    }
}
