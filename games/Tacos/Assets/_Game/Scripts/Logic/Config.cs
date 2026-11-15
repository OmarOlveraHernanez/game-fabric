namespace Tacos.Logic
{
    /// <summary>Tiempos y pagos de la cocina. Las mejoras compradas modifican una copia (ver CampaignProgress.BuildRules).</summary>
    public sealed class KitchenRules
    {
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
        public float PatienceMultiplier = 1f; // mejora "Clientes pacientes"
        public float RushPatienceFactor = 0.6f; // cliente apurado: menos paciencia...
        public int RushTipMultiplier = 2;       // ...pero propina doble
        public float FirstCustomerDelay = 0.8f;

        public KitchenRules Clone() => (KitchenRules)MemberwiseClone();
    }

    /// <summary>Regla extra de una mision: si se rompe, la mision no da estrellas.</summary>
    public enum MissionRule { None, NoErrors, NoBurn }

    /// <summary>
    /// Una mision (nivel). Cada mision agrega UNA cosa nueva: es la regla que salio de probar la maqueta.
    /// </summary>
    public sealed class LevelConfig
    {
        public string Name;
        public string Description;
        public float Duration = 90f;  // segundos del turno
        public int MaxCustomers;      // clientes a la vez
        public float Patience;        // segundos que espera un cliente
        public int MinExtras = 1;     // extras minimos por pedido
        public int MaxExtras;         // extras maximos por pedido
        public bool NeedsTortilla;    // si hay que calentar tortillas en el comal
        public bool Hints;            // pistas paso a paso
        public Meat OnlyMeat = Meat.None; // pedidos de una sola carne
        public float RushChance;      // probabilidad de cliente apurado
        public MissionRule Rule = MissionRule.None;
        public int[] Stars;           // monedas para 1, 2 y 3 estrellas (calibradas con el bot de BalanceTests)
        public float GapMin, GapMax;  // segundos entre clientes nuevos
        public float LateAfter = 60f; // al final del turno los clientes tienen menos paciencia
        public float LatePatienceFactor = 0.85f;

        /// <summary>Zona 1: el puesto callejero. 12 misiones.</summary>
        public static readonly LevelConfig[] Zona1 =
        {
            new LevelConfig
            {
                Name = "Primer turno", Description = "Un cliente a la vez, pedidos cortos y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 1, Hints = true,
                Stars = new[] { 60, 140, 200 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Name = "Llegan más", Description = "Hasta 2 clientes y pedidos con más extras.",
                MaxCustomers = 2, Patience = 45f, MaxExtras = 2,
                Stars = new[] { 90, 150, 200 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Al comal", Description = "Hasta 3 clientes y ahora calientas tortillas en el comal.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true,
                Stars = new[] { 70, 140, 190 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Hora pico", Description = "Sin comal, pero los clientes llegan uno tras otro.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 2,
                Stars = new[] { 120, 180, 220 }, GapMin = 1f, GapMax = 2f
            },
            new LevelConfig
            {
                Name = "Cero errores", Description = "Con comal. Un solo taco equivocado o un cliente que se va y no hay estrellas.",
                MaxCustomers = 2, Patience = 45f, MaxExtras = 2, NeedsTortilla = true, Rule = MissionRule.NoErrors,
                Stars = new[] { 110, 180, 225 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Cliente apurado", Description = "Algunos clientes tienen prisa: esperan menos, pero dejan propina doble.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 2, NeedsTortilla = true, RushChance = 0.5f,
                Stars = new[] { 110, 190, 245 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Cliente exigente", Description = "Pedidos largos: de 3 a 4 extras cada uno.",
                MaxCustomers = 2, Patience = 50f, MinExtras = 3, MaxExtras = 4, NeedsTortilla = true,
                Stars = new[] { 100, 180, 240 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Sin quemar nada", Description = "Si se te quema una tortilla o una carne, no hay estrellas.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, Rule = MissionRule.NoBurn,
                Stars = new[] { 100, 170, 225 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Meta alta", Description = "El mismo puesto, pero el dueño quiere más monedas.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true,
                Stars = new[] { 120, 180, 230 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Name = "Solo pastor", Description = "Noche de pastor: todos quieren pastor y llegan con prisa.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, OnlyMeat = Meat.Pastor, RushChance = 0.3f,
                Stars = new[] { 110, 180, 235 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Name = "Todo junto", Description = "Comal, 3 clientes, pedidos largos y algunos con prisa.",
                MaxCustomers = 3, Patience = 40f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true, RushChance = 0.3f,
                Stars = new[] { 100, 170, 240 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Jefe de zona", Description = "Turno de 2 minutos con todo: el reto completo del puesto callejero.",
                Duration = 120f, MaxCustomers = 3, Patience = 38f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true,
                RushChance = 0.3f, Stars = new[] { 130, 230, 310 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        /// <summary>Los 3 turnos del prototipo T1 (las primeras 3 misiones).</summary>
        public static readonly LevelConfig[] Turnos = { Zona1[0], Zona1[1], Zona1[2] };
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
