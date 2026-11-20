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
        public Extra LabelUp;                // estos llevan el nombre arriba (si abajo hay otra cosa)               // la cazuela del dibujo ya trae comida: se tapa cuando se vacia

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

        public static StallLayout For(Stall s) => s == Stall.Garnachas ? Garnachas : s == Stall.Elotes ? Elotes : Taqueria;
    }
}
