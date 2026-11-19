namespace Tacos.Logic
{
    /// <summary>Tiempos y pagos de la cocina. Las mejoras compradas modifican una copia (ver CampaignProgress.BuildRules).</summary>
    public sealed class KitchenRules
    {
        public float TortillaHotTime = 2f;    // segundos en el comal para estar caliente
        public float TortillaBurnTime = 16f;  // a partir de aqui se quema
        public float CookTime = 4f;           // segundos para rellenar un cazo de carne
        public int PotPortions = 5;           // porciones de un cazo lleno
        public int PlateCount = 2;
        public int MaxCustomerSlots = 3;
        public int BasePay = 10;
        public int PayPerExtra = 2;
        public int MaxTip = 5;                // propina maxima si se entrega con la paciencia llena
        public float PatienceMultiplier = 1f; // mejora "Clientes pacientes"
        public float RushPatienceFactor = 0.6f; // cliente apurado: menos paciencia...
        public int RushTipMultiplier = 2;       // ...pero propina doble
        public float FirstCustomerDelay = 0.8f;

        // Racha y power-ups
        public int ServesPerPowerUp = 4;      // tacos bien entregados para cargar un power-up (con racha de 3+, rinde 1.5x)
        public int MaxPowerUpStock = 3;       // de cada uno
        public float NopalTime = 8f;          // segundos de paciencia congelada
        public float SalsaTime = 10f;         // segundos de propina doble
        public float PickupGapMin = 18f, PickupGapMax = 30f; // cada cuanto cae un power-up del cielo
        public float PickupLife = 10f;        // segundos que dura (unos 2 cayendo del cielo y el resto flotando)

        // Clientes especiales
        public int MariachiTacos = 3;
        public float MariachiPatience = 1.6f; // esperan mas porque piden mas
        public int MariachiBonus = 10;        // al recibir el ultimo taco
        public float BossPatience = 2.6f;     // el jefe final espera mas (pide 3 platos)
        public int BossBonus = 60;            // al quedar contento
        public float BossWrongPenalty = 0.2f; // si le das algo mal no se va, pero se desespera
        public int CriticoMultiplier = 3;
        public float BorrachitoChangeAt = 0.55f; // cambia el pedido al bajar de esta paciencia

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
        public Meat[] Meats = Zona1Meats;   // cazos en la choricera (el pastor sale del trompo)
        public float RushChance;      // probabilidad de cliente apurado
        public MissionRule Rule = MissionRule.None;
        public int[] Stars;           // monedas para 1, 2 y 3 estrellas (calibradas con el bot de BalanceTests)
        public float GapMin, GapMax;  // segundos entre clientes nuevos
        public float LateAfter = 60f; // al final del turno los clientes tienen menos paciencia
        public float LatePatienceFactor = 0.85f;
        public PowerUp[] PowerUps = NoPowerUps;      // power-ups que se pueden cargar o agarrar en esta mision
        public CustomerKind[] Specials = NoSpecials; // clientes especiales que pueden llegar
        public float SpecialChance;                  // probabilidad de que un cliente nuevo sea especial
        public string Intro;                         // lo nuevo de esta mision, lo explica el Tio al empezar
        public Stall Stall = Stall.Taqueria;         // de que puesto es la mision
        public float BossAt;                         // jefe final: segundo en que llega (0 = no hay)
        public int BossItems = 3;                    // platos que pide el jefe
        public string BossName;                      // como se presenta (lo dice la pantalla)
        public Base[] Bases;                         // bases que se pueden pedir (null = todas las del puesto)

        public StallDef StallDef => StallDef.Get(Stall);
        public Base[] OrderBases => Bases ?? StallDef.Bases;

        public static readonly PowerUp[] NoPowerUps = new PowerUp[0];
        public static readonly CustomerKind[] NoSpecials = new CustomerKind[0];
        static readonly PowerUp[] P1 = { PowerUp.Cebollitas };
        static readonly PowerUp[] P2 = { PowerUp.Cebollitas, PowerUp.Nopal };
        static readonly PowerUp[] P3 = { PowerUp.Cebollitas, PowerUp.Nopal, PowerUp.Salsa };
        static readonly PowerUp[] P4 = { PowerUp.Cebollitas, PowerUp.Nopal, PowerUp.Salsa, PowerUp.Trompo };
        static readonly CustomerKind[] S1 = { CustomerKind.Critico };
        static readonly CustomerKind[] S2 = { CustomerKind.Critico, CustomerKind.Borrachito };
        static readonly CustomerKind[] S3 = { CustomerKind.Critico, CustomerKind.Borrachito, CustomerKind.Mariachi };

        public static readonly Meat[] Zona1Meats = { Meat.Pastor, Meat.Asada };
        public static readonly Meat[] ConChorizo = { Meat.Pastor, Meat.Asada, Meat.Chorizo };
        public static readonly Meat[] CuatroCarnes = { Meat.Pastor, Meat.Asada, Meat.Chorizo, Meat.Suadero };

        /// <summary>Zona 1: el puesto callejero. 12 misiones.</summary>
        public static readonly LevelConfig[] Zona1 =
        {
            new LevelConfig
            {
                Name = "Primer turno", Description = "Un cliente a la vez, pedidos cortos y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 1, Hints = true,
                Intro = "¡Bienvenido a la taquería, mijo! Aquí todo se arrastra con el dedo: la carne al plato y el plato al cliente. Haz lo que te enseñe la manita.",
                Stars = new[] { 170, 355, 520 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Name = "Llegan más", Description = "Hasta 2 clientes y pedidos con más extras.",
                MaxCustomers = 2, Patience = 45f, MaxExtras = 2, PowerUps = P1,
                Intro = "Atiende clientes seguidos sin equivocarte y tu racha te gana unas Cebollitas asadas: yo atiendo al cliente más desesperado. ¡Y si cae un power-up del cielo, tócalo!",
                Stars = new[] { 155, 320, 380 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Al comal", Description = "Hasta 3 clientes y ahora calientas las tortillas en el domo de la choricera.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, PowerUps = P1, Hints = true,
                Intro = "Ahora calientas las tortillas: arrastra una de la pila al domo, espera a que brille y arrástrala al plato. Después va la carne.",
                Stars = new[] { 90, 185, 285 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Hora pico", Description = "Sin comal, pero los clientes llegan uno tras otro.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 2, PowerUps = P2,
                Intro = "¡Hora pico! Nuevo power-up: el Nopal congela la paciencia de todos unos segundos.",
                Stars = new[] { 145, 320, 535 }, GapMin = 1f, GapMax = 2f
            },
            new LevelConfig
            {
                Name = "Llega el chorizo", Description = "Nuevo cazo: chorizo. Ahora son 3 carnes.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 2, NeedsTortilla = true, Meats = ConChorizo,
                PowerUps = P2,
                Intro = "Llegó el chorizo: ahora hay 3 cazos en la choricera. Arrastra la carne cruda al cazo que se vacíe.",
                Stars = new[] { 85, 185, 290 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Name = "Cero errores", Description = "Con comal. Un solo taco equivocado o un cliente que se va y no hay estrellas.",
                MaxCustomers = 2, Patience = 45f, MaxExtras = 2, NeedsTortilla = true, Meats = ConChorizo, Rule = MissionRule.NoErrors,
                PowerUps = P2, Specials = S1, SpecialChance = 0.25f,
                Intro = "Cuidado con el Crítico: paga el triple si le das bien, pero si te equivocas se va.",
                Stars = new[] { 130, 305, 455 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Llega el suadero", Description = "Nuevo cazo: suadero. Ya tienes las 4 carnes.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroCarnes,
                PowerUps = P2, Specials = S1, SpecialChance = 0.15f,
                Intro = "Llegó el suadero: ya tienes las 4 carnes en la choricera. De aquí en adelante, todo se pone más duro.",
                Stars = new[] { 110, 260, 370 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Name = "Cliente apurado", Description = "Algunos clientes tienen prisa: esperan menos, pero dejan propina doble.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 2, NeedsTortilla = true, Meats = CuatroCarnes, RushChance = 0.5f,
                PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "Nuevo power-up: la Salsa de la abuela da propina doble un rato. ¡Úsala con los que traen prisa!",
                Stars = new[] { 120, 255, 415 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Cliente exigente", Description = "Pedidos largos: de 3 a 4 extras cada uno.",
                MaxCustomers = 2, Patience = 50f, MinExtras = 3, MaxExtras = 4, NeedsTortilla = true, Meats = CuatroCarnes,
                PowerUps = P3, Specials = S2, SpecialChance = 0.2f,
                Intro = "Ojo con el Borrachito: a media espera cambia su pedido. Fíjate bien en su globito.",
                Stars = new[] { 100, 215, 345 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Sin quemar nada", Description = "Si se te quema una tortilla en el domo, no hay estrellas.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroCarnes, Rule = MissionRule.NoBurn,
                PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Nuevo power-up: el Trompo en llamas llena los cazos y calienta las tortillas al instante.",
                Stars = new[] { 105, 220, 355 }, GapMin = 3f, GapMax = 5f
            },
            new LevelConfig
            {
                Name = "Solo pastor", Description = "Noche de pastor: todos quieren pastor y llegan con prisa.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroCarnes, OnlyMeat = Meat.Pastor, RushChance = 0.3f,
                PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Llegó el Mariachi! Pide 3 tacos iguales y no se va hasta tenerlos todos.",
                Stars = new[] { 90, 210, 330 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Name = "Jefe de zona", Description = "Turno de 2 minutos con todo: el reto completo del puesto callejero.",
                Duration = 120f, MaxCustomers = 3, Patience = 38f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true,
                RushChance = 0.3f, Meats = CuatroCarnes, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "Hoy viene Don Chema, el crítico de la tele: pide 3 platos, uno tras otro. Si se va enojado, no hay estrellas.",
                BossAt = 40f, BossName = "Don Chema, el crítico de la tele",
                Stars = new[] { 220, 455, 645 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        // ================= Mundo 2: Garnachas =================
        // Lo nuevo del mundo: VOLTEAR. Cada masa se voltea cuando un lado esta listo; si no, se quema.
        // Las misiones van metiendo masas y guisados; los power-ups y clientes especiales ya se conocen del mundo 1.
        public static readonly Meat[] DosGuisados = { Meat.Chicharron, Meat.Frijoles };
        public static readonly Meat[] TresGuisados = { Meat.Chicharron, Meat.Frijoles, Meat.Tinga };
        public static readonly Meat[] CuatroGuisados = { Meat.Chicharron, Meat.Frijoles, Meat.Tinga, Meat.PapaChorizo };
        static readonly Base[] SoloSope = { Base.Sope };
        static readonly Base[] SopeGordita = { Base.Sope, Base.Gordita };
        static readonly Base[] SoloQuesadilla = { Base.Quesadilla };

        public static readonly LevelConfig[] Garnachas =
        {
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Primer sope", Description = "Solo sopes, un cliente a la vez y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 1, NeedsTortilla = true, Hints = true,
                Meats = DosGuisados, Bases = SoloSope,
                Intro = "¡Bienvenido a las garnachas! El sope se cuece de los dos lados: cuando un lado esté dorado, tócalo para voltearlo. Si lo dejas, se quema.",
                Stars = new[] { 75, 140, 170 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Crema y queso", Description = "Hasta 2 clientes y pedidos con más cosas encima.",
                MaxCustomers = 2, Patience = 48f, MaxExtras = 2, NeedsTortilla = true, Meats = DosGuisados, Bases = SoloSope, PowerUps = P1,
                Intro = "Ahora piden crema, queso o lechuga encima. Arrastra cada cosa al plato después del guisado.",
                Stars = new[] { 65, 135, 180 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Llega la gordita", Description = "Nueva masa: gordita. Fíjate cuál pide cada cliente.",
                MaxCustomers = 2, Patience = 48f, MaxExtras = 2, NeedsTortilla = true, Meats = DosGuisados, Bases = SopeGordita, PowerUps = P1,
                Intro = "Llegó la gordita. Cada cliente pide sope o gordita: pon en el comal la masa que te pidan.",
                Stars = new[] { 65, 135, 175 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Tinga", Description = "Nuevo guisado: tinga de pollo. Hasta 3 clientes.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 2, NeedsTortilla = true, Meats = TresGuisados, Bases = SopeGordita, PowerUps = P2,
                Intro = "Nueva cazuela: tinga de pollo. Y ya pueden llegar 3 clientes a la vez.",
                Stars = new[] { 65, 130, 170 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Quesadillas", Description = "Nueva masa: quesadilla. Ya tienes las 3.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 2, NeedsTortilla = true, Meats = TresGuisados, PowerUps = P2,
                Intro = "Llegó la quesadilla: ya tienes sopes, gorditas y quesadillas. ¡Ojo con cuál pide cada quien!",
                Stars = new[] { 65, 130, 170 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Papa con chorizo", Description = "Nuevo guisado: papa con chorizo. Ya tienes los 4.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 2, NeedsTortilla = true, Meats = CuatroGuisados, PowerUps = P2,
                Specials = S1, SpecialChance = 0.15f,
                Intro = "Última cazuela: papa con chorizo. Ya tienes los 4 guisados, y puede llegar el Crítico.",
                Stars = new[] { 80, 160, 205 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Hora de la comida", Description = "Clientes con prisa: menos paciencia, propina doble.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroGuisados, RushChance = 0.3f,
                PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "¡Hora de la comida! Los que traen prisa esperan menos pero dejan doble propina.",
                Stars = new[] { 80, 160, 215 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Sin quemar nada", Description = "Regla: si se quema una masa, no hay estrellas.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroGuisados, Rule = MissionRule.NoBurn,
                PowerUps = P3, Specials = S2, SpecialChance = 0.15f,
                Intro = "Hoy no se quema nada: si una masa se te quema, la misión no da estrellas. Voltea a tiempo.",
                Stars = new[] { 65, 140, 200 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Bien cargadas", Description = "Pedidos largos: de 2 a 4 cosas encima.",
                MaxCustomers = 3, Patience = 44f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true, Meats = CuatroGuisados,
                PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Hoy las piden bien cargadas: crema, queso, lechuga y salsa. Revisa bien el globito.",
                Stars = new[] { 80, 160, 220 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Cero errores", Description = "Regla: un pedido equivocado y no hay estrellas.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroGuisados, Rule = MissionRule.NoErrors,
                PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "Cero errores: si entregas algo mal o se te va un cliente, la misión no da estrellas.",
                Stars = new[] { 65, 130, 205 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Noche de quesadillas", Description = "Todos quieren quesadilla: rápido y sin quemar.",
                MaxCustomers = 3, Patience = 40f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroGuisados, Bases = SoloQuesadilla,
                RushChance = 0.25f, PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "Noche de quesadillas: todos quieren quesadilla. Ten el comal siempre lleno.",
                Stars = new[] { 65, 140, 185 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Garnachas, Name = "Jefa de las garnachas", Description = "Turno de 2 minutos con todo: el reto completo del puesto.",
                Duration = 120f, MaxCustomers = 3, Patience = 40f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true,
                RushChance = 0.3f, Meats = CuatroGuisados, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "Hoy viene Doña Lupe, la de la fonda: pide 3 garnachas, una tras otra. Si se va enojada, no hay estrellas.",
                BossAt = 40f, BossName = "Doña Lupe, la de la fonda",
                Stars = new[] { 180, 365, 445 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        /// <summary>Todas las misiones de la campaña, mundo tras mundo (12 por mundo). El guardado usa este orden.</summary>
        public static readonly LevelConfig[][] Worlds = { Zona1, Garnachas };

        /// <summary>Los 3 turnos del prototipo T1 (las primeras 3 misiones).</summary>
        public static readonly LevelConfig[] Turnos = { Zona1[0], Zona1[1], Zona1[2] };
    }

    /// <summary>Un taquero elegible. Cada uno juega distinto con una ventaja simple.</summary>
    public sealed class TaqueroDef
    {
        public string Id, Name, Perk;
        public float CookMultiplier = 1f;      // < 1 = rellena los cazos mas rapido
        public float PatienceMultiplier = 1f;  // > 1 = clientes mas pacientes
        public int ComalSlots = 2;

        public static readonly TaqueroDef Guero = new TaqueroDef
            { Id = "guero", Name = "El Güero", Perk = "Rellena los cazos 20 % más rápido.", CookMultiplier = 0.8f };
        public static readonly TaqueroDef Tio = new TaqueroDef
            { Id = "tio", Name = "El Tío", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f };
        public static readonly TaqueroDef Paisa = new TaqueroDef
            { Id = "paisa", Name = "El Paisa", Perk = "Calienta 4 tortillas a la vez en el domo.", ComalSlots = 4 };

        public static readonly TaqueroDef[] Starters = { Guero, Tio, Paisa };
    }
}
