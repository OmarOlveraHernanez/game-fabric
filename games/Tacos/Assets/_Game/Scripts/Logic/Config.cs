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

        // ================= Mundo 3: Elotes =================
        // Lo nuevo del mundo: EL ORDEN. La mayonesa va antes que el queso y el chile en polvo.
        // No hay comal: la base sale sola (elote y asado en palito, esquite en vaso).
        public static readonly Meat[] SoloElote = { Meat.Elote };
        public static readonly Meat[] EloteEsquite = { Meat.Elote, Meat.Esquite };
        public static readonly Meat[] TodoElotes = { Meat.Elote, Meat.Esquite, Meat.EloteAsado };
        public static readonly Meat[] SoloEsquites = { Meat.Esquite, Meat.EloteAsado };

        public static readonly LevelConfig[] Elotes =
        {
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Primer elote", Description = "Solo elotes, un cliente a la vez y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 2, Hints = true, Meats = SoloElote,
                Intro = "¡Bienvenido a los elotes! Arrastra un elote de la olla a la charolita. Ojo: PRIMERO la mayonesa, si no, el queso y el chile no se pegan.",
                Stars = new[] { 145, 305, 460 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Mayonesa primero", Description = "Hasta 2 clientes. Recuerda: mayonesa, luego lo demás.",
                MaxCustomers = 2, Patience = 48f, MinExtras = 2, MaxExtras = 3, Meats = SoloElote, PowerUps = P1,
                Intro = "Ya piden con todo: mayonesa, queso y chile. Siempre la mayonesa primero.",
                Stars = new[] { 115, 260, 475 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Llega el esquite", Description = "Nuevo: esquite en vaso. Fíjate qué pide cada quien.",
                MaxCustomers = 2, Patience = 48f, MaxExtras = 3, Meats = EloteEsquite, PowerUps = P1,
                Intro = "Llegó el esquite: sale del cazo y va en vasito. Unos piden elote y otros esquite.",
                Stars = new[] { 130, 290, 500 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Limón y del que pica", Description = "Hasta 3 clientes; ahora también limón y salsa picante.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, Meats = EloteEsquite, PowerUps = P2,
                Intro = "Ya pueden llegar 3 clientes. El limón y la salsa picante van cuando quieras; la mayonesa sigue yendo antes del queso.",
                Stars = new[] { 130, 285, 470 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Elote asado", Description = "Nuevo: elote asado en el anafre. Ya tienes las 3 estaciones.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, Meats = TodoElotes, PowerUps = P2,
                Intro = "Llegó el elote asado: sale del anafre bien doradito y va en palito. Ya tienes las 3 estaciones del carrito.",
                Stars = new[] { 130, 295, 475 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Domingo en la plaza", Description = "Llegan los clientes especiales.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, Meats = TodoElotes, PowerUps = P2, Specials = S1, SpecialChance = 0.15f,
                Intro = "Domingo en la plaza: hay mucha gente y puede llegar el Crítico.",
                Stars = new[] { 170, 360, 610 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Salida de la escuela", Description = "Clientes con prisa: menos paciencia, propina doble.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 3, Meats = TodoElotes, RushChance = 0.3f, PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "¡Salida de la escuela! Los que traen prisa esperan menos pero dejan doble propina.",
                Stars = new[] { 180, 410, 660 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Bien preparados", Description = "Pedidos largos: de 3 a 5 cosas encima.",
                MaxCustomers = 3, Patience = 46f, MinExtras = 3, MaxExtras = 5, Meats = TodoElotes, PowerUps = P3, Specials = S2, SpecialChance = 0.15f,
                Intro = "Hoy los quieren bien preparados: mayonesa, queso, chile, limón y del que pica. Revisa bien el globito.",
                Stars = new[] { 115, 275, 505 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Cero errores", Description = "Regla: un pedido equivocado y no hay estrellas.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 4, Meats = TodoElotes, Rule = MissionRule.NoErrors, PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Cero errores: si entregas algo mal o se te va un cliente, la misión no da estrellas.",
                Stars = new[] { 140, 345, 545 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Noche de antojo", Description = "Esquites y elotes asados: cuida que no se acaben.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 4, Meats = SoloEsquites, RushChance = 0.2f, PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "Noche de antojo: todos quieren esquite o elote asado. Cuida que no se te vacíen.",
                Stars = new[] { 135, 310, 535 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "Feria del pueblo", Description = "Mucha gente con prisa y clientes especiales.",
                MaxCustomers = 3, Patience = 40f, MinExtras = 2, MaxExtras = 4, Meats = TodoElotes, RushChance = 0.3f, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Feria del pueblo! Llega gente de todos lados. Que no se te olvide la mayonesa.",
                Stars = new[] { 145, 325, 560 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Elotes, Name = "La maestra Chela", Description = "Turno de 2 minutos con todo y la jefa final.",
                Duration = 120f, MaxCustomers = 3, Patience = 40f, MinExtras = 2, MaxExtras = 4, RushChance = 0.3f, Meats = TodoElotes, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                BossAt = 40f, BossName = "la maestra Chela, la más exigente de la escuela",
                Intro = "Hoy viene la maestra Chela: pide 3 elotes, uno tras otro, y revisa que la mayonesa vaya primero. Si se va enojada, no hay estrellas.",
                Stars = new[] { 300, 605, 915 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            },
        };

        // ================= Mundo 4: Pozoleria =================
        // Lo nuevo del mundo: EL TAMAÑO. El plato se toma de su pila y va directo a la tabla (no hay comal);
        // el plato grande lleva 2 cucharones. Seis cositas para encima: lechuga, rabano, cebolla, oregano, piquin y limon.
        static readonly Meat[] SoloRojo = { Meat.PozoleRojo };
        static readonly Meat[] RojoBlanco = { Meat.PozoleRojo, Meat.PozoleBlanco };
        public static readonly Meat[] TresPozoles = { Meat.PozoleRojo, Meat.PozoleBlanco, Meat.PozoleVerde };
        static readonly Base[] SoloChico = { Base.PlatoChico };
        static readonly Base[] SoloGrande = { Base.PlatoGrande };

        public static readonly LevelConfig[] Pozole =
        {
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Primer pozole", Description = "Plato chico, un cliente a la vez y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 2, NeedsTortilla = true, Hints = true, Meats = SoloRojo, Bases = SoloChico,
                Intro = "¡Bienvenido a la pozolería! Arrastra un plato de la pila a la tabla, luego un cucharón de pozole y al final lo que lleva encima.",
                Stars = new[] { 125, 265, 400 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Bien servido", Description = "Hasta 2 clientes y más cositas encima.",
                MaxCustomers = 2, Patience = 48f, MinExtras = 2, MaxExtras = 3, NeedsTortilla = true, Meats = SoloRojo, Bases = SoloChico, PowerUps = P1,
                Intro = "Ahora lo piden con lechuga, rábano, orégano y su limón. Fíjate bien en el globito.",
                Stars = new[] { 110, 245, 420 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Plato grande", Description = "Nuevo: plato grande, ¡lleva 2 cucharones!",
                MaxCustomers = 2, Patience = 50f, MaxExtras = 2, NeedsTortilla = true, Hints = true, Meats = SoloRojo, PowerUps = P1,
                Intro = "Llegó el plato grande: lleva DOS cucharones de pozole. Si le pones uno solo, el cliente no lo acepta.",
                Stars = new[] { 100, 220, 370 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Pozole blanco", Description = "Nueva olla: pozole blanco. Hasta 3 clientes.",
                MaxCustomers = 3, Patience = 48f, MaxExtras = 3, NeedsTortilla = true, Meats = RojoBlanco, PowerUps = P2,
                Intro = "Nueva olla: pozole blanco. Y ya pueden llegar 3 clientes a la vez.",
                Stars = new[] { 95, 215, 370 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Pozole verde", Description = "Nueva olla: pozole verde. Ya tienes los 3.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, NeedsTortilla = true, Meats = TresPozoles, PowerUps = P2,
                Intro = "Llegó el pozole verde: ya tienes rojo, blanco y verde. ¡Ojo con cuál pide cada quien!",
                Stars = new[] { 100, 220, 370 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Domingo familiar", Description = "Llegan los clientes especiales.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, NeedsTortilla = true, Meats = TresPozoles, PowerUps = P2, Specials = S1, SpecialChance = 0.15f,
                Intro = "Domingo familiar: hay mucha gente y puede llegar el Crítico.",
                Stars = new[] { 130, 290, 480 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Salida de misa", Description = "Clientes con prisa: menos paciencia, propina doble.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, NeedsTortilla = true, Meats = TresPozoles, RushChance = 0.3f, PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "¡Salida de misa! Los que traen prisa esperan menos pero dejan doble propina.",
                Stars = new[] { 130, 285, 500 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Con todo", Description = "Pedidos largos: de 3 a 5 cosas encima.",
                MaxCustomers = 3, Patience = 48f, MinExtras = 3, MaxExtras = 5, NeedsTortilla = true, Meats = TresPozoles, PowerUps = P3, Specials = S2, SpecialChance = 0.15f,
                Intro = "Hoy lo quieren con todo: lechuga, rábano, cebolla, orégano, piquín y limón. Revisa bien el globito.",
                Stars = new[] { 105, 235, 410 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Cero errores", Description = "Regla: un pedido equivocado y no hay estrellas.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 4, NeedsTortilla = true, Meats = TresPozoles, Rule = MissionRule.NoErrors, PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Cero errores: si entregas algo mal o se te va un cliente, la misión no da estrellas.",
                Stars = new[] { 115, 255, 450 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Noche del Grito", Description = "15 de septiembre: puros platos grandes.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, NeedsTortilla = true, Meats = TresPozoles, Bases = SoloGrande, RushChance = 0.2f, PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "¡Noche del Grito! Todos quieren plato grande: dos cucharones cada uno. Cuida que no se vacíen las ollas.",
                Stars = new[] { 100, 220, 380 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Fiesta del barrio", Description = "Mucha gente con prisa y clientes especiales.",
                MaxCustomers = 3, Patience = 42f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true, Meats = TresPozoles, RushChance = 0.3f, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Fiesta del barrio! Llega gente de todos lados. Fíjate en el tamaño de cada plato.",
                Stars = new[] { 100, 230, 415 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Pozole, Name = "Don Ramiro", Description = "Turno de 2 minutos con todo y el jefe final.",
                Duration = 120f, MaxCustomers = 3, Patience = 42f, MinExtras = 2, MaxExtras = 4, NeedsTortilla = true, RushChance = 0.3f, Meats = TresPozoles,
                PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                BossAt = 40f, BossName = "Don Ramiro, el del mercado",
                Intro = "Hoy viene Don Ramiro, el del mercado: pide 3 pozoles, uno tras otro, y no perdona un plato grande con un solo cucharón. Si se va enojado, no hay estrellas.",
                Stars = new[] { 240, 500, 725 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        // ================= Mundo 5: Tamales =================
        // Lo nuevo del mundo: EL COMBO (tamal con su atole) y la vaporera, que rinde 8 pero tarda en cocer.
        // El tamal va en su hoja o en guajolota (bolillo), directo de la pila al plato. El dulce no lleva crema ni queso.
        static readonly Meat[] SoloVerde = { Meat.TamalVerde };
        static readonly Meat[] VerdeRojo = { Meat.TamalVerde, Meat.TamalRojo };
        static readonly Meat[] TresTamales = { Meat.TamalVerde, Meat.TamalRojo, Meat.TamalRajas };
        public static readonly Meat[] CuatroTamales = { Meat.TamalVerde, Meat.TamalRojo, Meat.TamalRajas, Meat.TamalDulce };
        static readonly Base[] SoloHoja = { Base.Hoja };
        static readonly Base[] SoloGuajolota = { Base.Guajolota };

        public static readonly LevelConfig[] Tamales =
        {
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Primer tamal", Description = "Tamal en su hoja, un cliente a la vez y pistas paso a paso.",
                MaxCustomers = 1, Patience = 60f, MaxExtras = 1, NeedsTortilla = true, Hints = true, Meats = SoloVerde, Bases = SoloHoja,
                Intro = "¡Bienvenido a los tamales! Arrastra una hoja al plato, luego un tamal de la vaporera y, si lo pide, su atole.",
                Stars = new[] { 130, 280, 405 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Verde y rojo", Description = "Hasta 2 clientes: tamal verde y tamal rojo.",
                MaxCustomers = 2, Patience = 48f, MaxExtras = 2, NeedsTortilla = true, Meats = VerdeRojo, Bases = SoloHoja, PowerUps = P1,
                Intro = "Ahora hay verde y rojo. La vaporera rinde mucho, pero tarda en cocer: rellénala ANTES de que se acabe.",
                Stars = new[] { 125, 270, 460 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "La guajolota", Description = "Nuevo: guajolota, el tamal en bolillo.",
                MaxCustomers = 2, Patience = 50f, MaxExtras = 2, NeedsTortilla = true, Hints = true, Meats = VerdeRojo, PowerUps = P1,
                Intro = "Llegó la guajolota: el tamal va dentro de un bolillo. Cada cliente pide hoja o guajolota.",
                Stars = new[] { 125, 270, 470 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Rajas con queso", Description = "Nuevo tamal: rajas con queso. Hasta 3 clientes.",
                MaxCustomers = 3, Patience = 48f, MaxExtras = 2, NeedsTortilla = true, Meats = TresTamales, PowerUps = P2,
                Intro = "Nueva olla de la vaporera: rajas con queso. Y ya pueden llegar 3 clientes.",
                Stars = new[] { 125, 280, 430 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Tamal dulce", Description = "Nuevo: tamal dulce. ¡No lleva crema ni queso!",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 2, NeedsTortilla = true, Meats = CuatroTamales, PowerUps = P2,
                Intro = "Llegó el tamal dulce, el rosita: va solo o con su atole. NO le pongas crema ni queso.",
                Stars = new[] { 125, 285, 440 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Desayuno en la oficina", Description = "Llegan los clientes especiales.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 2, NeedsTortilla = true, Meats = CuatroTamales, PowerUps = P2, Specials = S1, SpecialChance = 0.15f,
                Intro = "Desayuno en la oficina: hay mucha gente y puede llegar el Crítico.",
                Stars = new[] { 165, 365, 550 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Antes del trabajo", Description = "Clientes con prisa: menos paciencia, propina doble.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 2, NeedsTortilla = true, Meats = CuatroTamales, RushChance = 0.3f, PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "¡Todos van tarde al trabajo! Los que traen prisa esperan menos pero dejan doble propina.",
                Stars = new[] { 160, 375, 595 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Combo completo", Description = "Todos piden su combo: tamal, cositas y atole.",
                MaxCustomers = 3, Patience = 48f, MinExtras = 2, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroTamales, PowerUps = P3, Specials = S2, SpecialChance = 0.15f,
                Intro = "Hoy todos quieren el combo completo: tamal con crema, queso y su atole.",
                Stars = new[] { 125, 305, 535 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Cero errores", Description = "Regla: un pedido equivocado y no hay estrellas.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroTamales, Rule = MissionRule.NoErrors, PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Cero errores: si entregas algo mal o se te va un cliente, la misión no da estrellas.",
                Stars = new[] { 140, 325, 540 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Puras guajolotas", Description = "Todos quieren guajolota y llegan con prisa.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroTamales, Bases = SoloGuajolota, RushChance = 0.25f, PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "Mañana de guajolotas: todos la quieren en bolillo. Ten la vaporera siempre llena.",
                Stars = new[] { 140, 320, 560 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Día de la Candelaria", Description = "Mucha gente con prisa y clientes especiales.",
                MaxCustomers = 3, Patience = 42f, MinExtras = 2, MaxExtras = 3, NeedsTortilla = true, Meats = CuatroTamales, RushChance = 0.3f, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Día de la Candelaria! A todos les salió el muñequito en la rosca y vienen por sus tamales.",
                Stars = new[] { 135, 305, 540 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Tamales, Name = "Doña Cuca", Description = "Turno de 2 minutos con todo y la jefa final.",
                Duration = 120f, MaxCustomers = 3, Patience = 42f, MinExtras = 2, MaxExtras = 3, NeedsTortilla = true, RushChance = 0.3f, Meats = CuatroTamales,
                PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                BossAt = 40f, BossName = "Doña Cuca, la abuelita tamalera",
                Intro = "Hoy viene Doña Cuca, la abuelita tamalera: pide 3 combos, uno tras otro, y se sabe todos los trucos. Si se va enojada, no hay estrellas.",
                Stars = new[] { 275, 585, 865 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        // ================= Mundo 6: La Feria =================
        // TODO JUNTO: un antojo de cada puesto en el mismo changarro, cada uno con sus reglas
        // (pina solo al pastor, la mayonesa primero en el elote, el atole con el tamal). La base sale sola.
        static readonly Meat[] TacoElote = { Meat.Pastor, Meat.Elote };
        static readonly Meat[] TacoElotePozole = { Meat.Pastor, Meat.Elote, Meat.PozoleBlanco };
        public static readonly Meat[] TodaLaFeria = { Meat.Pastor, Meat.Elote, Meat.PozoleBlanco, Meat.TamalVerde };
        static readonly Meat[] FeriaDulce = { Meat.Elote, Meat.TamalVerde };

        public static readonly LevelConfig[] Feria =
        {
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Llegó la feria", Description = "Tacos y elotes en el mismo puesto, con pistas.",
                MaxCustomers = 2, Patience = 56f, MaxExtras = 2, Hints = true, Meats = TacoElote,
                Intro = "¡Bienvenido a la feria! Aquí se vende de todo y cada antojo lleva lo suyo: la salsa solo al taco y la mayonesa primero en el elote.",
                Stars = new[] { 145, 315, 530 }, GapMin = 1.5f, GapMax = 2.5f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Llega el pozole", Description = "Nueva olla: pozole rojo. Hasta 3 clientes.",
                MaxCustomers = 3, Patience = 50f, MaxExtras = 2, Meats = TacoElotePozole, PowerUps = P2,
                Intro = "Llegó el pozole blanco: con cebolla, chile y su salsita. Ya pueden llegar 3 clientes.",
                Stars = new[] { 145, 320, 465 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Llegan los tamales", Description = "Nueva olla: tamales. Ya tienes los 4 antojos.",
                MaxCustomers = 3, Patience = 48f, MaxExtras = 2, Meats = TodaLaFeria, PowerUps = P2,
                Intro = "Llegaron los tamales: con queso y su champurrado. Ya tienes los 4 antojos de la feria.",
                Stars = new[] { 155, 335, 450 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Juegos mecánicos", Description = "Llegan los clientes especiales.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, Meats = TodaLaFeria, PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "Ya abrieron los juegos mecánicos: hay mucha gente y puede llegar el Crítico.",
                Stars = new[] { 180, 405, 605 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "La rueda de la fortuna", Description = "Clientes con prisa: menos paciencia, propina doble.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, Meats = TodaLaFeria, RushChance = 0.3f, PowerUps = P3, Specials = S1, SpecialChance = 0.15f,
                Intro = "¡Se acaba la vuelta de la rueda! Los que traen prisa esperan menos pero dejan doble propina.",
                Stars = new[] { 180, 400, 675 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Bien cargados", Description = "Pedidos largos: de 2 a 4 cosas encima.",
                MaxCustomers = 3, Patience = 46f, MinExtras = 2, MaxExtras = 4, Meats = TodaLaFeria, PowerUps = P3, Specials = S2, SpecialChance = 0.15f,
                Intro = "Hoy los piden bien cargados. Cada antojo lleva lo suyo: revisa bien el globito.",
                Stars = new[] { 140, 340, 560 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Cero errores", Description = "Regla: un pedido equivocado y no hay estrellas.",
                MaxCustomers = 3, Patience = 46f, MaxExtras = 3, Meats = TodaLaFeria, Rule = MissionRule.NoErrors, PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Cero errores: si entregas algo mal o se te va un cliente, la misión no da estrellas.",
                Stars = new[] { 165, 385, 575 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Antojo de la tarde", Description = "Elotes y tamales: cuida que no se acaben.",
                MaxCustomers = 3, Patience = 44f, MaxExtras = 3, Meats = FeriaDulce, RushChance = 0.2f, PowerUps = P4, Specials = S2, SpecialChance = 0.2f,
                Intro = "Antojo de la tarde: todos quieren elote o tamal. Rellena antes de que se vacíen.",
                Stars = new[] { 160, 370, 615 }, GapMin = 2f, GapMax = 3.5f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Noche de cohetes", Description = "Fuegos artificiales: mucha gente y clientes especiales.",
                MaxCustomers = 3, Patience = 42f, MaxExtras = 3, Meats = TodaLaFeria, RushChance = 0.3f, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Noche de cohetes! Todo el pueblo vino a ver el castillo. Llega el Mariachi con hambre.",
                Stars = new[] { 165, 370, 625 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Maestro de la feria", Description = "Pedidos largos y sin un solo error.",
                MaxCustomers = 3, Patience = 46f, MinExtras = 3, MaxExtras = 4, Meats = TodaLaFeria, Rule = MissionRule.NoErrors, PowerUps = P4, Specials = S3, SpecialChance = 0.2f,
                Intro = "Ahora sí, todo bien preparado y sin errores. Respira hondo.",
                Stars = new[] { 135, 315, 555 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "Último día de feria", Description = "Todos quieren llevarse su antojo antes de que se acabe.",
                MaxCustomers = 3, Patience = 40f, MinExtras = 2, MaxExtras = 4, Meats = TodaLaFeria, RushChance = 0.35f, PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                Intro = "¡Último día de feria! Todos quieren su antojo antes de que se acabe.",
                Stars = new[] { 155, 345, 615 }, GapMin = 2.5f, GapMax = 4f
            },
            new LevelConfig
            {
                Stall = Stall.Feria, Name = "El gran jurado", Description = "Turno de 2 minutos y el jurado pide 4 platos.",
                Duration = 120f, MaxCustomers = 3, Patience = 42f, MinExtras = 2, MaxExtras = 4, RushChance = 0.3f, Meats = TodaLaFeria,
                PowerUps = P4, Specials = S3, SpecialChance = 0.25f,
                BossAt = 35f, BossItems = 4, BossName = "el Gran Jurado de la Feria",
                Intro = "Llegó el Gran Jurado de la Feria: prueba 4 antojos distintos, uno tras otro. ¡Si queda contento, eres el mejor de todos los puestos!",
                Stars = new[] { 320, 670, 945 }, GapMin = 2.5f, GapMax = 4.5f, LateAfter = 80f
            }
        };

        /// <summary>Todas las misiones de la campaña, mundo tras mundo (12 por mundo). El guardado usa este orden.</summary>
        public static readonly LevelConfig[][] Worlds = { Zona1, Garnachas, Elotes, Pozole, Tamales, Feria };

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

        // Cada puesto tiene sus propios cocineros. Van en el mismo orden que los taqueros (mismo lugar = misma ventaja),
        // asi el que elegiste en un mundo es "el mismo" en el siguiente.
        public static readonly TaqueroDef[] Garnacheros =
        {
            new TaqueroDef { Id = "chata", Name = "La Chata", Perk = "Rellena las cazuelas 20 % más rápido.", CookMultiplier = 0.8f },
            new TaqueroDef { Id = "mago", Name = "Doña Mago", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f },
            new TaqueroDef { Id = "beto", Name = "El Beto", Perk = "Cuece 4 masas a la vez en el comal.", ComalSlots = 4 }
        };
        public static readonly TaqueroDef[] Eloteros =
        {
            new TaqueroDef { Id = "yesi", Name = "La Yesi", Perk = "Rellena las ollas 20 % más rápido.", CookMultiplier = 0.8f },
            new TaqueroDef { Id = "cheto", Name = "Don Cheto", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f },
            // aqui no hay comal: el tercero tiene un poco de los dos
            new TaqueroDef { Id = "chuy", Name = "El Chuy", Perk = "Ollas 10 % más rápido y clientes 10 % más pacientes.", CookMultiplier = 0.9f, PatienceMultiplier = 1.1f }
        };

        // Pozole y tamales tampoco tienen comal: el tercero tiene un poco de los dos, como en los elotes
        public static readonly TaqueroDef[] Pozoleros =
        {
            new TaqueroDef { Id = "rosa", Name = "La Rosa", Perk = "Rellena las ollas 20 % más rápido.", CookMultiplier = 0.8f },
            new TaqueroDef { Id = "goyo", Name = "Don Goyo", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f },
            new TaqueroDef { Id = "nacho", Name = "El Nacho", Perk = "Ollas 10 % más rápido y clientes 10 % más pacientes.", CookMultiplier = 0.9f, PatienceMultiplier = 1.1f }
        };
        public static readonly TaqueroDef[] Tamaleros =
        {
            new TaqueroDef { Id = "lencha", Name = "La Lencha", Perk = "La vaporera cuece 20 % más rápido.", CookMultiplier = 0.8f },
            new TaqueroDef { Id = "fili", Name = "Don Fili", Perk = "Los clientes tienen 20 % más paciencia.", PatienceMultiplier = 1.2f },
            new TaqueroDef { Id = "tono", Name = "El Toño", Perk = "Vaporera 10 % más rápido y clientes 10 % más pacientes.", CookMultiplier = 0.9f, PatienceMultiplier = 1.1f }
        };
        /// <summary>En la feria se juntan los mejores de cada puesto (mismo lugar = misma ventaja).</summary>
        public static readonly TaqueroDef[] Feriantes = { Eloteros[0], Garnacheros[1], Eloteros[2] };

        static readonly TaqueroDef[][] ByStall = { Starters, Garnacheros, Eloteros, Pozoleros, Tamaleros, Feriantes };

        public static TaqueroDef[] For(Stall s) => ByStall[(int)s];

        /// <summary>El lugar (0, 1 o 2) de un cocinero en la lista de su puesto.</summary>
        public static int SlotOf(TaqueroDef t)
        {
            foreach (var list in ByStall)
                for (int i = 0; i < list.Length; i++) if (list[i] == t || list[i].Id == t.Id) return i;
            return 0;
        }
    }
}
