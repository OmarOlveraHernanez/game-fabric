using System;

namespace Tacos.Logic
{
    public enum Meat { None = 0, Pastor = 1, Asada = 2 }

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
        Limon = 32
    }

    public static class Extras
    {
        /// <summary>Todos los extras en el orden en que se muestran.</summary>
        public static readonly Extra[] All =
            { Extra.Cebolla, Extra.Cilantro, Extra.Pina, Extra.SalsaVerde, Extra.SalsaRoja, Extra.Limon };

        public const Extra Salsas = Extra.SalsaVerde | Extra.SalsaRoja;

        public static int Count(Extra e)
        {
            int n = 0;
            foreach (var x in All) if ((e & x) != 0) n++;
            return n;
        }

        public static bool Has(Extra set, Extra x) => (set & x) != 0;
    }

    /// <summary>Lo que pide un cliente: una carne y sus extras.</summary>
    public readonly struct Order
    {
        public readonly Meat Meat;
        public readonly Extra Extras;
        public Order(Meat meat, Extra extras) { Meat = meat; Extras = extras; }
        public override string ToString() => Meat + " + " + Extras;
    }

    /// <summary>Un plato de la barra donde se arma el taco.</summary>
    public sealed class Plate
    {
        public bool Tortilla;
        public Meat Meat;
        public Extra Extras;

        public bool HasMeat => Meat != Meat.None;
        public bool IsEmpty => !Tortilla && !HasMeat && Extras == Extra.None;

        public bool Matches(Order o) => Tortilla && Meat == o.Meat && Extras == o.Extras;

        /// <summary>El plato va camino a este pedido: misma carne y ningun extra que el cliente no pidio.</summary>
        public bool IsHeadingTo(Order o) => HasMeat && Meat == o.Meat && (Extras & ~o.Extras) == 0;

        public void Clear(bool withTortilla)
        {
            Tortilla = withTortilla;
            Meat = Meat.None;
            Extras = Extra.None;
        }
    }
}
