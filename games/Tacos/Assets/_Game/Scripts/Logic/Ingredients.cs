using System;

namespace Tacos.Logic
{
    /// <summary>Lo principal del pedido: la carne en la taqueria, el guisado en las garnachas, etc. (ver Stall.cs).</summary>
    public enum Meat
    {
        None = 0, Pastor = 1, Asada = 2, Chorizo = 3, Suadero = 4,
        // Garnachas (guisados en cazuela)
        Chicharron = 5, Tinga = 6, PapaChorizo = 7, Frijoles = 8,
        // Elotes (ollas del carrito)
        Elote = 9, Esquite = 10, EloteAsado = 11, // el asado sale del anafre
        // Pozoleria (ollas de pozole)
        PozoleRojo = 12, PozoleBlanco = 13, PozoleVerde = 14,
        // Tamales (la vaporera)
        TamalVerde = 15, TamalRojo = 16, TamalRajas = 17, TamalDulce = 18
    }

    /// <summary>La base que se calienta en el comal: tortilla en la taqueria; sope, gordita o quesadilla en las garnachas.</summary>
    public enum Base
    {
        None = 0, Tortilla = 1, Sope = 2, Gordita = 3, Quesadilla = 4, Palito = 5, Vaso = 6,
        PlatoChico = 7, PlatoGrande = 8,  // pozole: el grande lleva 2 cucharones
        Hoja = 9, Guajolota = 10          // tamales: en su hoja o en bolillo
    }

    /// <summary>Extras de un taco. Se combinan como banderas (un taco puede llevar varios).</summary>
    [Flags]
    public enum Extra
    {
        None = 0,
        Cebolla = 1,
        Cilantro = 2,
        Pina = 4,
        SalsaVerde = 8,
        SalsaRoja = 16,
        Limon = 32,
        // Garnachas
        Crema = 64,
        Queso = 128,
        Lechuga = 256,
        // Elotes
        Mayonesa = 512,
        ChilePolvo = 1024,
        Picante = 2048,
        // Pozole
        Rabano = 4096,
        Oregano = 8192,
        // Tamales: el atole del combo
        Champurrado = 16384,
        AtoleArroz = 32768,
        AtoleFresa = 65536
    }

    public static class Extras
    {
        /// <summary>Todos los extras en el orden en que se muestran.</summary>
        public static readonly Extra[] All =
            {
                Extra.Cebolla, Extra.Cilantro, Extra.Pina, Extra.SalsaVerde, Extra.SalsaRoja, Extra.Limon,
                Extra.Mayonesa, // antes que el queso y el chile: en los elotes la mayonesa va primero
                Extra.Crema, Extra.Queso, Extra.Lechuga, Extra.ChilePolvo, Extra.Picante,
                Extra.Rabano, Extra.Oregano, Extra.Champurrado, Extra.AtoleArroz, Extra.AtoleFresa
            };

        public const Extra Salsas = Extra.SalsaVerde | Extra.SalsaRoja;

        public static int Count(Extra e)
        {
            int n = 0;
            foreach (var x in All) if ((e & x) != 0) n++;
            return n;
        }

        public static bool Has(Extra set, Extra x) => (set & x) != 0;
    }

    /// <summary>Lo que pide un cliente: su base, lo principal y sus extras.</summary>
    public readonly struct Order
    {
        public readonly Base Base;
        public readonly Meat Meat;
        public readonly Extra Extras;
        public Order(Meat meat, Extra extras) : this(Base.Tortilla, meat, extras) { }
        public Order(Base b, Meat meat, Extra extras) { Base = b; Meat = meat; Extras = extras; }
        public override string ToString() => Base + " " + Meat + " + " + Extras;
    }

    /// <summary>Un plato de la barra donde se arma el taco.</summary>
    public sealed class Plate
    {
        public Base Base;
        /// <summary>Ya tiene su base (tortilla, sope...). En la taqueria es "tiene tortilla".</summary>
        public bool Tortilla { get => Base != Base.None; set => Base = value ? Base.Tortilla : Base.None; }
        public Meat Meat;
        public Extra Extras;
        /// <summary>Cucharones servidos (el plato grande de pozole lleva 2; todo lo demas, 1).</summary>
        public int Scoops;

        public static int ScoopsFor(Base b) => b == Base.PlatoGrande ? 2 : 1;
        public bool HasMeat => Meat != Meat.None;
        /// <summary>Ya tiene lo principal pero le falta otro cucharon.</summary>
        public bool NeedsScoop => HasMeat && Math.Max(1, Scoops) < ScoopsFor(Base);
        public bool IsEmpty => !Tortilla && !HasMeat && Extras == Extra.None;

        public bool Matches(Order o) => Base == o.Base && Meat == o.Meat && Extras == o.Extras && !NeedsScoop;

        /// <summary>El plato va camino a este pedido: misma carne y ningun extra que el cliente no pidio.</summary>
        public bool IsHeadingTo(Order o) => HasMeat && Base == o.Base && Meat == o.Meat && (Extras & ~o.Extras) == 0;

        public void Clear(bool withTortilla) => Clear(withTortilla ? Base.Tortilla : Base.None);

        public void Clear(Base b)
        {
            Base = b;
            Meat = Meat.None;
            Extras = Extra.None;
            Scoops = 0;
        }
    }
}
