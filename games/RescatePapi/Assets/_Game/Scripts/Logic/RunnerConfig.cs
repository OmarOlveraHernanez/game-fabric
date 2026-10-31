namespace RescatePapi.Logic
{
    /// <summary>
    /// Parametros de juego. Unidades: 1 unidad ~ altura del jugador.
    /// Ajustar aqui la "sensacion"; las pruebas de diseno verifican que siga siendo justo.
    /// </summary>
    public sealed class RunnerConfig
    {
        // Jugador (x fija; y = 0 es el suelo)
        public float PlayerX = -6f;
        public float PlayerWidth = 0.9f;
        public float PlayerHeight = 0.9f;
        public float HitboxInset = 0.12f;          // tolerancia en colisiones con enemigos

        // Salto
        public float Gravity = 60f;                // magnitud (u/s^2) al subir
        public float FallGravityMultiplier = 1.4f; // cae mas rapido de lo que sube
        public float JumpVelocity = 18f;
        public float JumpCutMultiplier = 0.45f;    // al soltar antes de tiempo -> salto corto
        public float MinJumpTime = 0.08f;          // el corte no aplica antes de esto
        public float JumpBufferTime = 0.12f;       // tap un poco antes de aterrizar cuenta

        // Velocidad: CONSTANTE (subirla marea y no deja ver el arte). La dificultad sube con el
        // tiempo por los enemigos: mas seguidos y con comportamientos nuevos (ver Serpiente/Aguila).
        public float BaseSpeed = 10.5f;              // con 7 el tap corto ya no libra la serpiente (ver GameDesignTests)
        public float MaxSpeed = 10.5f;
        public float Acceleration = 0f;            // 0 = velocidad fija
        public float DifficultyRampTime = 120f;    // segundos hasta la dificultad maxima (nivel 1)

        // Generacion
        public float SpawnX = 14f;
        public float DespawnX = -14f;
        public float FirstHazardDelay = 1.5f;      // segundos hasta el primer enemigo
        public float GapTimeMinEasy = 1.1f;        // segundos entre enemigos
        public float GapTimeMaxEasy = 1.8f;
        public float GapTimeMinHard = 0.8f;
        public float GapTimeMaxHard = 1.15f;

        // Serpiente: baja y larga, en el suelo. Tamano fijo (el sprite no se deforma).
        public float SerpentWeight = 1f;
        public float SerpentMinWidth = 1.2f, SerpentMaxWidth = 1.2f;
        public float SerpentMinHeight = 0.55f, SerpentMaxHeight = 0.55f;
        // Serpiente saltarina: brinca justo al llegar al jugador y pasa POR ENCIMA (hay que NO saltar)
        public float SerpentHopFromLevel = 0.3f;   // ~36 s
        public float SerpentHopChance = 0.45f;
        public float SerpentHopHeight = 1.5f;      // altura maxima de su base
        public float SerpentHopDuration = 0.7f;    // segundos en el aire; el pico coincide con el jugador

        // Jaguar: alto; puede embestir (acelera al acercarse)
        public float JaguarWeight = 1f;
        public float JaguarAspect = 0.80f;         // ancho = alto * aspecto; 0.80 = silueta del sprite IA (jaguar de pie)
        public float JaguarMinHeight = 1.1f;
        public float JaguarMaxHeightEasy = 1.35f, JaguarMaxHeightHard = 1.65f;
        public float JaguarChargeFromLevel = 0.3f;
        public float JaguarChargeChance = 0.5f;
        public float JaguarChargeDistance = 9f;    // distancia al jugador en la que embiste
        public float JaguarChargeSpeed = 3f;       // velocidad extra al embestir
        public float JaguarMaxWidth => JaguarMaxHeightHard * JaguarAspect;

        // Aguila: vuela a media altura; de pie pasa por encima, cualquier salto choca
        public float EagleWeight = 0.8f;
        public float EagleFromLevel = 0.15f;
        public float EagleWidth = 1.4f;
        public float EagleThickness = 0.6f;
        public float EagleMinBottom = 1.05f, EagleMaxBottom = 1.3f; // tamano fijo; solo varia la altura de vuelo
        // Aguila en picada: baja a ras de suelo antes de llegar (hay que SALTARLA)
        public float EagleDiveFromLevel = 0.45f;   // ~54 s
        public float EagleDiveChance = 0.45f;
        public float EagleDiveDistance = 8f;       // borde izquierdo del aguila -> jugador al lanzarse (~0.9 s antes)
        public float EagleDiveBottom = 0.15f;      // vuelo rasante
        public float EagleDiveSpeed = 4f;          // u/s de bajada (y de subida al pasar)

        // Premios
        public float CacaoChancePerGap = 0.65f;
        public int CacaoMinCount = 3, CacaoMaxCount = 5;
        public float CacaoSpacing = 0.9f;
        public float CacaoSize = 0.45f;
        public float CacaoGroundY = 0.45f;         // centro de la fila a ras de suelo
        public float CacaoArcOverJaguarChance = 0.5f;
        public float PollitoChancePerGap = 0.07f;
        public float PollitoSize = 0.6f;
        public float PollitoY = 1.3f;              // pide un saltito
        public float ShieldInvulnerableTime = 1f;

        // Puntuacion y flujo
        public float ScorePerUnit = 1f;            // 1 punto por unidad recorrida
        public float RestartDelay = 0.5f;          // evita reiniciar por accidente
    }
}
