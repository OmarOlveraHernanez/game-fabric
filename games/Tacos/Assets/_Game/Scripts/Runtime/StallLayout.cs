using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;

namespace Tacos
{
    /// <summary>
    /// Donde esta cada cosa en el dibujo de cada puesto (pixeles de la imagen de 1536 x 864).
    /// KitchenScene es la misma para todos los puestos; esto es lo unico que cambia.
    /// </summary>
    public sealed class StallLayout
    {
        public Stall Stall;
        public string Background;
        public float CropBottom = 110f;      // franja de flores de abajo que se puede recortar
        public bool Videos;                  // puesto vivo en video con 3 capas (taqueria)
        public string Video;                 // o un solo video de todo el puesto (Resources), si existe

        public bool Trompo;                  // el pastor sale del trompo
        public Rect TrompoArea;
        public Vector2 TrompoBadge;

        public Vector2[] Bowls;              // cazos o cazuelas de lo principal
        public Meat[] BowlMeat;
        public Vector2 HeapSize = new Vector2(106, 70);
        public Vector2[] HeapSizes;          // tamano de lo de cada olla (si cambia por olla); se estira a la boca de la olla
        public Vector2 PlateMeat = new Vector2(88, 46); // lo principal ya servido en el plato (masas, elotes)
        public bool BowlCover;
        public bool PaintedFull;             // las ollas ya vienen llenas en el dibujo (y en el video): solo se tapan al vaciarse
        public string FullPots;              // prefijo del dibujo de la cazuela completa ("cazuela_" + guisado), null = monton encima
        public bool ExtraLabels;             // nombre abajo de cada cazuelita de extras
        public Extra LabelUp;                // estos llevan el nombre arriba (si abajo hay otra cosa)
        public Vector2[] NameAt, BadgeAt;    // donde va el nombre y el numero de cada olla (null = abajo y a la derecha)
        public Meat[] PaintOver;
        public string Front;
        public string[] Levels;              // por olla: "nivel_<x>_medio/_bajo" (el caldo baja y se ve la pared de adentro); null = sin niveles
        public Rect[] LevelRects;            // donde va cada dibujo de nivel (centro y tamano, pixeles de la imagen)
        public float BowlHotH;               // alto del lugar para tomar de cada olla (0 = el normal); baja hacia el cuerpo de la olla                 // pedazos del dibujo que van ENFRENTE de los clientes (cucharones, vasos); ver art-source/frente.py             // con PaintedFull: estas ollas se dibujan encima (el dibujo trae otra cosa en esa olla)

        public (Base Base, Rect Area, string Art)[] Stacks; // de donde se toman las bases (Art null = ya viene en el dibujo)
        public Rect RawTray;                 // charola de carne cruda (ancho 0 = no hay)

        public Vector2[] Comal2, Comal4;     // lugares en el domo o el comal (2 o 4)
        public Rect ComalDrop;               // donde se suelta una base
        public Vector2 BaseSize = new Vector2(110, 64);
        public Vector2 PlateBase = new Vector2(112, 64);  // la base ya servida en el plato
        public bool BaseKeepsAspect;         // las masas guardan su forma; la tortilla se estira
        public Vector2 HotLabel;

        public Vector2[] PlateSpots;
        public string PlateArt;              // plato dibujado encima (null = ya viene en el dibujo)
        public float[] CustomerX = { 470, 760, 1040 };
        public float CounterTop;
        public Rect TrashArea;
        public bool TrashArt = true;         // dibujar el bote encima

        public Dictionary<Extra, Vector2> Extras;
        public (string, float, float, float, float)[] Garnish = new (string, float, float, float, float)[0];
        public bool Upgrades;                // radio y domo de fierro (dibujados para la taqueria)
        public Vector2 FxDome, FxBase = new Vector2(-1, -1);
        public (Vector2 At, float Width, int Bowl)[] AmbientSteam; // vapor continuo por codigo (si no hay video)
        public Rect Embers;                                        // brasas que titilan (ancho 0 = no hay)

        static Rect R(float cx, float cy, float w, float h) => Stage.R(cx, cy, w, h);

        public static readonly StallLayout Taqueria = new StallLayout
        {
            Stall = Stall.Taqueria, Background = "barra_choricera", Videos = true,
            Trompo = true, TrompoArea = R(125, 330, 230, 520), TrompoBadge = new Vector2(222, 248),
            Bowls = new[] { new Vector2(443, 388), new Vector2(612, 372), new Vector2(778, 400) },
            BowlMeat = new[] { Meat.Asada, Meat.Chorizo, Meat.Suadero },
            Stacks = new[] { (Base.Tortilla, R(968, 400, 110, 105), (string)null) },
            RawTray = R(1090, 418, 155, 75),
            Comal2 = new[] { new Vector2(545, 480), new Vector2(628, 480) },
            Comal4 = new[] { new Vector2(522, 492), new Vector2(585, 458), new Vector2(650, 492), new Vector2(585, 520) },
            ComalDrop = R(585, 478, 300, 160), HotLabel = new Vector2(585, 548),
            PlateSpots = new[] { new Vector2(1058, 505), new Vector2(1065, 577), new Vector2(1071, 657) },
            CounterTop = 342f, // orilla de la barra en el dibujo: la gente se para detras (los cazos empiezan en 350)
            TrashArea = R(1470, 712, 74, 92),
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Cebolla, new Vector2(1238, 520) }, { Extra.Cilantro, new Vector2(1355, 520) }, { Extra.Pina, new Vector2(1468, 520) },
                { Extra.SalsaVerde, new Vector2(1255, 618) }, { Extra.SalsaRoja, new Vector2(1375, 618) }, { Extra.Limon, new Vector2(1488, 618) }
            },
            // La orilla del comal bien llena (adorno, no se toca): chorizos, carne doradita, tripa, nopales y el manojo de cebollas
            Garnish = new[]
            {
                ("com_cabeza", 255f, 560f, 135f, 105f), ("com_salchichas", 275f, 450f, 160f, 112f), ("com_nopales", 400f, 470f, 100f, 78f),
                ("com_suadero", 845f, 470f, 150f, 105f), ("com_espiral", 800f, 560f, 140f, 110f), ("com_tripa3", 660f, 575f, 120f, 90f),
                ("com_chorizo_suelto", 560f, 575f, 96f, 72f), ("com_nopal", 742f, 505f, 78f, 64f),
                ("com_manojo", 712f, 545f, 135f, 64f) // cebollitas entre la tripa y el nopal (al final: va encima)
            },
            Upgrades = true, FxDome = new Vector2(585, 478)
        };

        /// <summary>Mundo 2: el comal plano grande, las 3 pilas de masa a la izquierda y las 4 cazuelas de guisado atras.</summary>
        public static readonly StallLayout Garnachas = new StallLayout
        {
            Stall = Stall.Garnachas, Background = "barra_garnachas", CropBottom = 120f, Video = "Video/garnachas_vivo",
            Bowls = new[] { new Vector2(272, 350), new Vector2(480, 350), new Vector2(680, 348), new Vector2(878, 350) },
            BowlMeat = new[] { Meat.Chicharron, Meat.Tinga, Meat.PapaChorizo, Meat.Frijoles },
            HeapSize = new Vector2(150, 62), FullPots = "cazuela_", ExtraLabels = true,
            // Pilas de masa sobre su servilleta (la de abajo se dibuja al ultimo: va enfrente)
            Stacks = new[]
            {
                (Base.Sope, R(86, 382, 150, 150), "pila_sope"), (Base.Gordita, R(82, 486, 150, 150), "pila_gordita"),
                (Base.Quesadilla, R(76, 592, 150, 150), "pila_quesadilla")
            },
            Comal2 = new[] { new Vector2(385, 528), new Vector2(580, 528) },
            Comal4 = new[] { new Vector2(305, 508), new Vector2(480, 462), new Vector2(655, 508), new Vector2(480, 598) },
            ComalDrop = R(480, 530, 620, 230), BaseSize = new Vector2(176, 130), PlateBase = new Vector2(132, 92), BaseKeepsAspect = true, HotLabel = new Vector2(480, 418),
            PlateSpots = new[] { new Vector2(1015, 468), new Vector2(1015, 548), new Vector2(1015, 628) }, PlateArt = "plato",
            CounterTop = 296f,
            TrashArea = R(1440, 745, 170, 200), TrashArt = false,
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Crema, new Vector2(1140, 372) }, { Extra.Queso, new Vector2(1280, 376) }, { Extra.Lechuga, new Vector2(1435, 380) },
                { Extra.SalsaVerde, new Vector2(1243, 492) }, { Extra.SalsaRoja, new Vector2(1427, 497) }
            },
            FxDome = new Vector2(480, 530),
            // Para que el puesto no se vea pobre: papel picado con foquitos, letrero de neon, aguas frescas,
            // prensa, canasta de masa, y en la orilla del comal chiles toreados y nopales asandose (adorno, no se toca)
            Garnish = new[]
            {
                ("gar_papel", 250f, 46f, 470f, 150f), ("gar_papel", 768f, 40f, 470f, 150f), ("gar_papel", 1290f, 46f, 470f, 150f),
                ("gar_neon", 1365f, 170f, 200f, 150f),
                ("gar_canasta", 1268f, 604f, 150f, 110f), // (la prensa de tortillas se quito: el usuario no la quiso)
                // (los chiles con cebollitas y los nopales del comal se quitaron: el usuario prefirio el comal limpio)
            }
        };

        /// <summary>Mundo 3: el carrito de elotes. Olla de elotes, cazo de esquite y anafre de asados, ya pintados llenos; no hay comal.</summary>
        public static readonly StallLayout Elotes = new StallLayout
        {
            Stall = Stall.Elotes, Background = "barra_elotes", CropBottom = 110f,
            Bowls = new[] { new Vector2(178, 420), new Vector2(500, 466), new Vector2(815, 488) },
            BowlMeat = new[] { Meat.Elote, Meat.Esquite, Meat.EloteAsado },
            // El dibujo ya trae las ollas llenas: cuando se vacian se tapa la boca con su fondo (agua, caldo, carbon)
            HeapSizes = new[] { new Vector2(360, 160), new Vector2(290, 118), new Vector2(250, 160) }, PaintedFull = true,
            Stacks = new (Base, Rect, string)[0],
            Comal2 = new[] { new Vector2(-400, -400), new Vector2(-400, -400) },
            Comal4 = new[] { new Vector2(-400, -400), new Vector2(-400, -400), new Vector2(-400, -400), new Vector2(-400, -400) },
            ComalDrop = R(-400, -400, 10, 10), HotLabel = new Vector2(-400, -400), BaseKeepsAspect = true,
            PlateSpots = new[] { new Vector2(1088, 478), new Vector2(1088, 568), new Vector2(1088, 658) }, PlateArt = "plato",
            PlateMeat = new Vector2(170, 96),
            CounterTop = 386f, // la orilla del cazo y del anafre: la gente se para detras
            Front = "frente_elotes", // la orilla de la olla grande va enfrente de la primera persona (los lugares quedan como siempre)
            Levels = new[] { null, "elotes_1", null }, // el esquite baja; los elotes enteros y los asados todavia no
            LevelRects = new[] { default(Rect), R(500, 462, 312, 136), default(Rect) },
            LabelUp = Extra.Queso, // su nombre arriba de la charola (abajo estan los limones)
            TrashArea = R(1430, 800, 200, 140), TrashArt = false,
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Mayonesa, new Vector2(1196, 448) }, { Extra.Queso, new Vector2(1360, 435) }, { Extra.ChilePolvo, new Vector2(1252, 538) },
                { Extra.Limon, new Vector2(1380, 552) }, { Extra.Picante, new Vector2(1495, 490) }
            },
            ExtraLabels = true, FxDome = new Vector2(-400, -400),
            // Animado por codigo (el usuario prefirio esto a pagar Weave): vapor de las ollas y brasas del anafre
            AmbientSteam = new[] { (new Vector2(175, 390), 260f, 0), (new Vector2(500, 440), 200f, 1), (new Vector2(815, 440), 150f, 2) },
            Embers = R(812, 548, 300, 70)
        };

        static readonly Vector2 Off = new Vector2(-400, -400); // fuera de la pantalla: estos puestos no tienen comal
        static Vector2[] Offs(int n) { var a = new Vector2[n]; for (int i = 0; i < n; i++) a[i] = Off; return a; }

        /// <summary>
        /// Mundo 4: la pozoleria (ia/puestos/cocina_pozole.png). Las 3 ollas de barro ya pintadas llenas, las pilas de platos
        /// en la repisa de atras (el grande a la izquierda), la tabla con 3 huecos y las 6 cazuelitas de lo de encima.
        /// </summary>
        public static readonly StallLayout Pozole = new StallLayout
        {
            Stall = Stall.Pozole, Background = "barra_pozole", CropBottom = 100f,
            Bowls = new[] { new Vector2(125, 505), new Vector2(420, 505), new Vector2(725, 510) },
            BowlMeat = new[] { Meat.PozoleRojo, Meat.PozoleBlanco, Meat.PozoleVerde },
            HeapSizes = new[] { new Vector2(235, 80), new Vector2(235, 78), new Vector2(235, 78) }, PaintedFull = true,
            Stacks = new[] { (Base.PlatoGrande, R(180, 340, 190, 120), (string)null), (Base.PlatoChico, R(333, 345, 150, 110), (string)null) },
            Comal2 = Offs(2), Comal4 = Offs(4), ComalDrop = R(-400, -400, 10, 10), HotLabel = Off, FxDome = Off,
            BaseKeepsAspect = true, PlateBase = new Vector2(150, 108), PlateMeat = new Vector2(150, 112),
            PlateSpots = new[] { new Vector2(997, 478), new Vector2(997, 560), new Vector2(1008, 655) },
            // Separados como en la taqueria (si se juntan, los globitos tapan al de al lado). La canasta de tostadas
            // de la repisa va enfrente de la primera persona.
            CustomerX = new float[] { 720, 990, 1260 }, Front = "frente_pozole",
            CounterTop = 352f,
            TrashArea = R(1430, 800, 210, 150), TrashArt = false,
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Lechuga, new Vector2(1157, 440) }, { Extra.Rabano, new Vector2(1338, 445) }, { Extra.Cebolla, new Vector2(1194, 522) },
                { Extra.Oregano, new Vector2(1355, 525) }, { Extra.ChilePolvo, new Vector2(1210, 618) }, { Extra.Limon, new Vector2(1366, 618) }
            },
            ExtraLabels = true, LabelUp = Extra.Lechuga | Extra.Rabano,
            // El pozole se va acabando de verdad: baja el caldo (art-source/niveles.py)
            Levels = new[] { "pozole_0", "pozole_1", "pozole_2" },
            LevelRects = new[] { R(118, 509, 254, 90), R(422, 510, 258, 88), R(728, 510, 256, 90) },
            AmbientSteam = new[] { (new Vector2(125, 470), 200f, 0), (new Vector2(420, 470), 200f, 1), (new Vector2(725, 475), 200f, 2) },
            Embers = R(440, 690, 860, 40)
        };

        /// <summary>
        /// Mundo 5: la tamaleria (ia/puestos/cocina_tamales.png). La vaporera con sus 4 tamales, las 3 ollas de atole,
        /// la canasta de bolillos, la pila de hojas, la crema y el queso, y la tabla con 3 huecos.
        /// </summary>
        public static readonly StallLayout Tamales = new StallLayout
        {
            Stall = Stall.Tamales, Background = "barra_tamales", CropBottom = 100f,
            Bowls = new[] { new Vector2(150, 360), new Vector2(365, 365), new Vector2(140, 462), new Vector2(362, 452) },
            BowlMeat = new[] { Meat.TamalVerde, Meat.TamalRojo, Meat.TamalRajas, Meat.TamalDulce },
            HeapSizes = new[] { new Vector2(210, 90), new Vector2(200, 90), new Vector2(210, 90), new Vector2(200, 88) }, PaintedFull = true,
            NameAt = new[] { new Vector2(150, 318), new Vector2(365, 318), new Vector2(140, 512), new Vector2(362, 506) },
            BadgeAt = new[] { new Vector2(48, 338), new Vector2(478, 345), new Vector2(40, 452), new Vector2(478, 440) },
            Stacks = new[] { (Base.Guajolota, R(598, 555, 300, 200), (string)null), (Base.Hoja, R(836, 635, 120, 96), "pila_hojas") },
            Comal2 = Offs(2), Comal4 = Offs(4), ComalDrop = R(-400, -400, 10, 10), HotLabel = Off, FxDome = Off,
            BaseKeepsAspect = true, PlateBase = new Vector2(132, 92), PlateMeat = new Vector2(140, 100),
            PlateSpots = new[] { new Vector2(1005, 505), new Vector2(1020, 588), new Vector2(1037, 672) },
            CounterTop = 298f,
            CustomerX = new float[] { 500, 790, 1080 }, // separados como en la taqueria
            Front = "frente_tamales",                   // la orilla de la vaporera, los cucharones del atole y los vasos van enfrente de la gente
            TrashArea = R(1450, 770, 180, 190), TrashArt = false,
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Champurrado, new Vector2(715, 345) }, { Extra.AtoleArroz, new Vector2(920, 348) }, { Extra.AtoleFresa, new Vector2(1115, 352) },
                { Extra.Crema, new Vector2(1236, 580) }, { Extra.Queso, new Vector2(1240, 680) }
            },
            ExtraLabels = true, LabelUp = Extra.Crema,
            // la crema y el queso no venian en el dibujo: sus cazuelitas van encima
            Garnish = new[] { ("cazuelita_crema", 1236f, 580f, 112f, 104f), ("cazuelita_queso", 1240f, 680f, 116f, 92f) },
            AmbientSteam = new[] { (new Vector2(250, 320), 300f, -1), (new Vector2(715, 310), 120f, -1), (new Vector2(920, 315), 120f, -1), (new Vector2(1115, 318), 120f, -1) }
        };

        /// <summary>
        /// Mundo 6: la feria (ia/puestos/barra_feria_v2.png, pintada otra vez el 20 de noviembre con el estilo de los demas).
        /// Trompo de pastor, olla de elotes, olla de tamales, pozole blanco y champurrado en las de barro, 8 cazuelitas y la tabla.
        /// </summary>
        public static readonly StallLayout Feria = new StallLayout
        {
            Stall = Stall.Feria, Background = "barra_feria", CropBottom = 90f,
            Trompo = true, TrompoArea = R(85, 300, 170, 400), TrompoBadge = new Vector2(172, 150),
            Bowls = new[] { new Vector2(330, 330), new Vector2(672, 325), new Vector2(1008, 350) },
            BowlMeat = new[] { Meat.Elote, Meat.TamalVerde, Meat.PozoleBlanco },
            HeapSizes = new[] { new Vector2(300, 80), new Vector2(310, 90), new Vector2(250, 52) }, PaintedFull = true,
            BowlHotH = 190f,
            Stacks = new (Base, Rect, string)[0],
            Comal2 = Offs(2), Comal4 = Offs(4), ComalDrop = R(-400, -400, 10, 10), HotLabel = Off, FxDome = Off,
            BaseKeepsAspect = true, PlateBase = new Vector2(150, 100), PlateMeat = new Vector2(175, 112),
            PlateSpots = new[] { new Vector2(405, 715), new Vector2(745, 715), new Vector2(1088, 715) },
            CounterTop = 320f, Front = "frente_feria", // lo de arriba de las ollas va enfrente de la gente
            Levels = new[] { null, null, "feria_2" }, // el pozole baja; elotes y tamales todavia no
            LevelRects = new[] { default(Rect), default(Rect), R(1013, 361, 256, 78) },
            TrashArea = R(1455, 775, 170, 180), TrashArt = false,
            Extras = new Dictionary<Extra, Vector2>
            {
                { Extra.Cebolla, new Vector2(220, 568) }, { Extra.Cilantro, new Vector2(390, 568) }, { Extra.SalsaRoja, new Vector2(562, 568) },
                { Extra.SalsaVerde, new Vector2(735, 568) }, { Extra.Picante, new Vector2(905, 568) }, { Extra.Mayonesa, new Vector2(1077, 568) },
                { Extra.Queso, new Vector2(1248, 568) }, { Extra.ChilePolvo, new Vector2(1412, 568) }, { Extra.Champurrado, new Vector2(1330, 400) }
            },
            ExtraLabels = true, LabelUp = Extra.None,
            AmbientSteam = new[] { (new Vector2(330, 300), 260f, 0), (new Vector2(672, 290), 260f, 1), (new Vector2(1008, 325), 200f, 2), (new Vector2(1330, 325), 200f, -1) }
        };

        static readonly StallLayout[] ByStall = { Taqueria, Garnachas, Elotes, Pozole, Tamales, Feria };

        public static StallLayout For(Stall s) => ByStall[(int)s];
    }
}
