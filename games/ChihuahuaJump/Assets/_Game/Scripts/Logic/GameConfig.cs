namespace ChihuahuaJump.Logic
{
    /// <summary>
    /// Parametros del juego (pantalla vertical fija). x en [-HalfWidth, HalfWidth]; y = 0 es el fondo.
    /// Ajustar aqui la "sensacion"; las pruebas de diseno verifican que siga siendo justo.
    /// </summary>
    public sealed class GameConfig
    {
        // Campo
        public float HalfWidth = 3.5f;             // 7 unidades de ancho (cabe en pantallas 9:20)
        public float TopY = 15f;                   // donde aparecen los enemigos que caen

        // Trampolin (lo carga el dueno)
        public float TrampolineY = 1.2f;           // superficie del trampolin
        public float TrampolineWidth = 1.8f;
        public float TrampolineSpeed = 10f;        // velocidad maxima (u/s) al seguir el dedo o con teclado
        public float TrampolineOverhang = 1.2f;      // cuanto puede salir el dueno por la orilla (para desviar al perro hacia adentro)

        // Chihuahua
        public float DogSize = 0.6f;
        public float Gravity = 26f;
        public float BounceVelocity = 22.5f;       // ~9.7 u sobre el trampolin (llega a ~3/4 de pantalla), vuelo ~1.7 s
        public float MaxSideSpeed = 6.5f;          // velocidad lateral si pega en la orilla
        public float CenterDeadZone = 0.3f;        // fraccion central del trampolin que rebota recto (perdona imprecision)
        public float StartDogY = 3f;

        // Dificultad: nivel 0 -> 1 en RampSeconds
        public float RampSeconds = 90f;

        // Enemigos
        public float SpawnIntervalEasy = 2.8f, SpawnIntervalHard = 1.2f;
        public int MaxHazardsEasy = 2, MaxHazardsHard = 3;   // tope de enemigos simultaneos
        public float FirstSpawnDelay = 4f;
        public float SerpentWeight = 1f;
        public float JaguarWeight = 0.8f, JaguarFromLevel = 0.1f;
        public float EagleWeight = 0.8f, EagleFromLevel = 0.2f;
        public float SerpentWidth = 0.45f, SerpentHeight = 1.1f;
        public float SerpentFallEasy = 3f, SerpentFallHard = 5.5f;
        public float JaguarWidth = 1.1f, JaguarHeight = 0.8f;
        public float JaguarFallMultiplier = 1.4f;
        public float EagleWidth = 1.1f, EagleHeight = 0.5f;
        public float EagleFallMultiplier = 1.15f;  // cae en picada en diagonal
        public float EagleSideSpeed = 2.2f;        // velocidad lateral; rebota en las paredes
        public float HitboxInset = 0.08f;
        // Aviso anticipado: cada enemigo tarda al menos esto (s) en llegar a la zona del perro desde que aparece.
        // Debe ser mayor que un vuelo completo para que siempre haya un rebote para reaccionar.
        public float HazardLeadTime = 2.3f;
        public float VisibleTopY = 16f;            // borde superior visible (arriba de esto se muestra un aviso)

        /// <summary>Altura maxima que alcanza la parte alta del perro.</summary>
        public float DogPeakY => TrampolineY + BounceVelocity * BounceVelocity / (2f * Gravity) + DogSize;

        // Premios: flotan quietos a alturas alcanzables durante un rato; los atrapa el chihuahua
        public float PickupInterval = 2f;
        public float PickupLifetime = 6f;
        public int CacaoGroupMax = 3;              // filas de 1 a 3 granos
        public float CacaoSpacing = 0.75f;
        public float CacaoSize = 0.45f;
        public float CollectMargin = 0.25f;        // area extra generosa para atrapar premios
        public float PollitoChance = 0.08f;
        public float PollitoSize = 0.6f;
        public float ShieldInvulnerableTime = 1f;

        // Flujo
        public float RestartDelay = 0.5f;
    }
}
