namespace Tacos.Logic
{
    /// <summary>Tiempos y pagos de la cocina, iguales en todos los niveles. Ajustar aqui la "sensacion".</summary>
    public sealed class KitchenRules
    {
        public float Duration = 90f;          // segundos por turno
        public float TortillaHotTime = 2f;    // segundos en el comal para estar caliente
        public float TortillaBurnTime = 16f;  // a partir de aqui se quema
        public float CookTime = 4f;           // segundos de plancha para la carne
        public float MeatBurnTime = 14f;      // a partir de aqui se quema
        public int PlateCount = 2;
        public int GrillSlots = 2;
        public int MaxCustomerSlots = 3;
        public int BasePay = 10;
        public int PayPerExtra = 2;
        public int MaxTip = 5;                // propina maxima si se entrega con la paciencia llena
        public float FirstCustomerDelay = 0.8f;
    }

    /// <summary>
    /// Un turno (nivel). Cada turno agrega una sola mecanica nueva: es la regla que salio de probar la maqueta.
    /// </summary>
    public sealed class LevelConfig
    {
        public string Name;
        public string Description;
        public int MaxCustomers;      // clientes a la vez
        public float Patience;        // segundos que espera un cliente
        public int MaxExtras;         // extras maximos por pedido
        public bool NeedsTortilla;    // si hay que calentar tortillas en el comal
        public bool Hints;            // pistas paso a paso
        public int[] Stars;           // monedas para 1, 2 y 3 estrellas (calibradas con el bot de BalanceTests)
        public float GapMin, GapMax;  // segundos entre clientes nuevos
        public float LateAfter = 60f; // al final del turno los clientes tienen menos paciencia
        public float LatePatienceFactor = 0.85f;

        public static readonly LevelConfig[] Turnos =
        {
            new LevelConfig
            {
                Name = "Turno 1", Description = "Un cliente a la vez, pedidos cortos y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 1, NeedsTortilla = false, Hints = true,
                Stars = new[] { 60, 140, 200 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Name = "Turno 2", Description = "Hasta 2 clientes y pedidos con más extras.",
                MaxCustomers = 2, Patience = 45f, MaxExtras = 2, NeedsTortilla = false, Hints = false,
                Stars = new[] { 90, 150, 200 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Turno 3", Description = "Hasta 3 clientes y ahora calientas tortillas en el comal.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, Hints = false,
                Stars = new[] { 70, 140, 190 }, GapMin = 3f, GapMax = 5f
            }
        };
    }

    /// <summary>Un taquero elegible. Cada uno juega distinto con una ventaja simple.</summary>
    public sealed class TaqueroDef
    {
        public string Id, Name, Perk;
        public float CookMultiplier = 1f;      // < 1 = cocina mas rapido
        public float PatienceMultiplier = 1f;  // > 1 = clientes mas pacientes
        public int ComalSlots = 2;

        public static readonly TaqueroDef Guero = new TaqueroDef
            { Id = "guero", Name = "El Güero", Perk = "La carne se cocina 20 % más rápido.", CookMultiplier = 0.8f };
        public static readonly TaqueroDef Tio = new TaqueroDef
            { Id = "tio", Name = "El Tío", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f };
        public static readonly TaqueroDef Paisa = new TaqueroDef
            { Id = "paisa", Name = "El Paisa", Perk = "El comal calienta 4 tortillas a la vez.", ComalSlots = 4 };

        public static readonly TaqueroDef[] Starters = { Guero, Tio, Paisa };
    }
}
