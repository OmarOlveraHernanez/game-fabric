using System;
using System.Collections.Generic;

namespace Tacos.Logic
{
    /// <summary>Un puesto por mundo (ver claude/mundos-puestos.md). Mismo motor y mismos gestos; cambia lo que se cocina.</summary>
    public enum Stall { Taqueria = 0, Garnachas = 1, Elotes = 2, Pozole = 3, Tamales = 4, Feria = 5 }

    /// <summary>
    /// Lo que distingue a un puesto: sus bases, lo principal, sus extras y su mecanica especial.
    /// La taqueria queda exactamente como antes (mismas reglas y mismo azar), lo vigilan las pruebas.
    /// </summary>
    public sealed class StallDef
    {
        public Stall Id;
        public string Name;
        public Base[] Bases;            // las que se pueden pedir (la primera es la de siempre)
        public Meat[] Mains;            // todo lo principal que existe en el puesto
        public Extra[] Common;          // extras que entran en cualquier pedido
        public Extra[] OneOf;           // de estos, a lo mas uno por pedido (las salsas)
        public Extra Special;           // extra que solo va con SpecialMeat (la pina con el pastor)
        public Meat SpecialMeat;
        public bool Flip;               // hay que voltear la base a media coccion (garnachas)
        public float CookScale = 1f;    // la base tarda esto por el tiempo de la tortilla (por lado, si se voltea)
        public float FlipWindow = 6f;   // segundos para voltear antes de que se queme el primer lado
        public bool AutoBase;           // la base sale de lo principal (elote en palito, esquite en vaso); no hay comal
        public Extra First;             // extra que va antes que los demas (la mayonesa en los elotes)
        public Extra NeedsFirst;        // los que solo se pegan si ya tiene First (queso y chile en polvo)

        /// <summary>En los elotes cada cosa tiene su base: el elote y el asado en palito, el esquite en vaso.</summary>
        public Base BaseFor(Meat m) => m == Meat.Elote || m == Meat.EloteAsado ? Base.Palito : m == Meat.Esquite ? Base.Vaso : Bases[0];

        /// <summary>Todos los extras del puesto en el orden en que se muestran.</summary>
        public Extra[] AllExtras
        {
            get
            {
                var l = new List<Extra>(Common);
                if (Special != Extra.None) l.Add(Special);
                l.AddRange(OneOf);
                return l.ToArray();
            }
        }

        public Extra OneOfMask
        {
            get { var m = Extra.None; foreach (var e in OneOf) m |= e; return m; }
        }

        public static readonly StallDef Taqueria = new StallDef
        {
            Id = Stall.Taqueria, Name = "Taquería",
            Bases = new[] { Base.Tortilla },
            Mains = new[] { Meat.Pastor, Meat.Asada, Meat.Chorizo, Meat.Suadero },
            Common = new[] { Extra.Cebolla, Extra.Cilantro, Extra.Limon },
            OneOf = new[] { Extra.SalsaVerde, Extra.SalsaRoja },
            Special = Extra.Pina, SpecialMeat = Meat.Pastor
        };

        public static readonly StallDef Garnachas = new StallDef
        {
            Id = Stall.Garnachas, Name = "Garnachas",
            Bases = new[] { Base.Sope, Base.Gordita, Base.Quesadilla },
            Mains = new[] { Meat.Chicharron, Meat.Tinga, Meat.PapaChorizo, Meat.Frijoles },
            Common = new[] { Extra.Crema, Extra.Queso, Extra.Lechuga },
            OneOf = new[] { Extra.SalsaVerde, Extra.SalsaRoja },
            Flip = true, CookScale = 1.25f, FlipWindow = 5f
        };

        /// <summary>Mundo 3: lo nuevo es EL ORDEN. Primero la mayonesa, para que se peguen el queso y el chile.</summary>
        public static readonly StallDef Elotes = new StallDef
        {
            Id = Stall.Elotes, Name = "Elotes",
            Bases = new[] { Base.Palito, Base.Vaso },
            Mains = new[] { Meat.Elote, Meat.Esquite, Meat.EloteAsado },
            Common = new[] { Extra.Mayonesa, Extra.Queso, Extra.ChilePolvo, Extra.Limon, Extra.Picante },
            OneOf = new Extra[0],
            AutoBase = true, First = Extra.Mayonesa, NeedsFirst = Extra.Queso | Extra.ChilePolvo
        };

        public static StallDef Get(Stall s) => s == Stall.Garnachas ? Garnachas : s == Stall.Elotes ? Elotes : Taqueria;
    }
}
