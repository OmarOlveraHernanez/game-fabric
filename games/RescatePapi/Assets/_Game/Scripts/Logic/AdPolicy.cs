namespace RescatePapi.Logic
{
    /// <summary>
    /// Cuando mostrar el anuncio entre partidas (intersticial), con moderacion:
    /// nunca en las primeras partidas de la sesion, cada N partidas y con un tiempo minimo entre anuncios.
    /// Un video con premio que el jugador eligio ver cuenta como anuncio (reinicia la cuenta).
    /// </summary>
    public sealed class AdPolicy
    {
        public int FirstAfterRuns = 2;           // la 1a vez, despues de 2 partidas de la sesion
        public int EveryRuns = 2;                // luego cada 2 partidas
        public float MinSecondsBetween = 60f;    // y nunca mas seguido que esto (antes 3 / 3 / 90 s)

        int _runsSession, _runsSinceAd;
        float _lastAdTime = float.NegativeInfinity;

        /// <summary>Llamar al terminar cada partida.</summary>
        public void RunFinished() { _runsSession++; _runsSinceAd++; }

        public bool ShouldShowInterstitial(float now)
        {
            if (_runsSession < FirstAfterRuns) return false;
            if (_runsSinceAd < EveryRuns) return false;
            return now - _lastAdTime >= MinSecondsBetween;
        }

        /// <summary>Llamar cuando se mostro cualquier anuncio (intersticial o video con premio).</summary>
        public void AdShown(float now) { _runsSinceAd = 0; _lastAdTime = now; }

        /// <summary>Texto para diagnostico: cuanto falta para el siguiente intersticial.</summary>
        public string Describe(float now)
        {
            int runs = System.Math.Max(System.Math.Max(0, FirstAfterRuns - _runsSession), EveryRuns - _runsSinceAd);
            float secs = System.Math.Max(0f, MinSecondsBetween - (now - _lastAdTime));
            return runs <= 0 && secs <= 0f ? "toca al salir" : $"faltan {System.Math.Max(0, runs)} partidas y {secs:0} s";
        }
    }
}
