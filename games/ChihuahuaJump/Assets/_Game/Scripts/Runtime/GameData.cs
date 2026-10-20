using System;

namespace ChihuahuaJump
{
    /// <summary>Datos que se guardan entre partidas (chihuahuajump.json). Subir version si cambia el formato.</summary>
    [Serializable]
    public class GameData
    {
        public int version = 1;
        public int bestScore;
        public int cacaoTotal;
        public int runs;
    }
}
