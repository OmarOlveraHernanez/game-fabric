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
        public bool InstantBase;        // la base (plato, hoja, bolillo) se toma de su pila y va directo al plato: no hay comal
        public float PotTimeScale = 1f; // la olla tarda esto por el tiempo normal en rellenarse (la vaporera de tamales tarda mas)
        public int ExtraPortions;       // porciones de mas en cada olla (la vaporera rinde mas)
        public StallDef[] Parts;        // puesto mixto (la feria): cada familia de lo principal trae sus propios extras y reglas

        /// <summary>Hay comal donde se calienta la base (taqueria y garnachas).</summary>
        public bool HasComal => !AutoBase && !InstantBase;

        /// <summary>Cuando la base sale de lo principal: elote y asado en palito, esquite en vaso, y en la feria cada cosa en lo suyo.</summary>
        public Base BaseFor(Meat m)
        {
            switch (m)
            {
                case Meat.Elote: case Meat.EloteAsado: return Base.Palito;
                case Meat.Esquite: return Base.Vaso;
                case Meat.Pastor: case Meat.Asada: case Meat.Chorizo: case Meat.Suadero: return Base.Tortilla;
                case Meat.PozoleRojo: case Meat.PozoleBlanco: case Meat.PozoleVerde: return Base.PlatoChico;
                case Meat.TamalVerde: case Meat.TamalRojo: case Meat.TamalRajas: case Meat.TamalDulce: return Base.Hoja;
                default: return Bases[0];
            }
        }

        /// <summary>Las reglas de extras que tocan a ese principal (en un puesto mixto, las de su familia).</summary>
        public StallDef PartFor(Meat m)
        {
            if (Parts == null) return this;
            foreach (var p in Parts) if (Array.IndexOf(p.Mains, m) >= 0) return p;
            return Parts[0];
        }

        /// <summary>Todos los extras del puesto en el orden en que se muestran.</summary>
        public Extra[] AllExtras
        {
            get
            {
                if (Parts != null)
                {
                    var mask = Extra.None;
                    foreach (var p in Parts) foreach (var e in p.AllExtras) mask |= e;
                    return Array.FindAll(Extras.All, e => (mask & e) != 0);
                }
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

        // (va antes de los puestos que la usan: los campos estaticos se inicializan en orden)
        static readonly Extra[] Atoles = { Extra.Champurrado, Extra.AtoleArroz, Extra.AtoleFresa };

        /// <summary>Mundo 4: lo nuevo es EL TAMAÑO. El plato grande lleva 2 cucharones. Los platos se toman de su pila (no hay comal).</summary>
        public static readonly StallDef Pozole = new StallDef
        {
            Id = Stall.Pozole, Name = "Pozolería",
            Bases = new[] { Base.PlatoChico, Base.PlatoGrande },
            Mains = new[] { Meat.PozoleRojo, Meat.PozoleBlanco, Meat.PozoleVerde },
            Common = new[] { Extra.Lechuga, Extra.Rabano, Extra.Cebolla, Extra.Oregano, Extra.ChilePolvo, Extra.Limon },
            OneOf = new Extra[0],
            InstantBase = true
        };

        /// <summary>
        /// Mundo 5: lo nuevo es EL COMBO (tamal con su atole) y la vaporera, que rinde mas pero tarda en cocer:
        /// hay que rellenar antes de que se acabe. El tamal dulce no lleva crema ni queso.
        /// </summary>
        public static readonly StallDef Tamales = new StallDef
        {
            Id = Stall.Tamales, Name = "Tamales",
            Bases = new[] { Base.Hoja, Base.Guajolota },
            Mains = new[] { Meat.TamalVerde, Meat.TamalRojo, Meat.TamalRajas, Meat.TamalDulce },
            Common = new[] { Extra.Crema, Extra.Queso },
            OneOf = Atoles,
            InstantBase = true, PotTimeScale = 1.6f, ExtraPortions = 3,
            Parts = new[]
            {
                new StallDef { Id = Stall.Tamales, Name = "Salados", Bases = new[] { Base.Hoja }, Mains = new[] { Meat.TamalVerde, Meat.TamalRojo, Meat.TamalRajas },
                               Common = new[] { Extra.Crema, Extra.Queso }, OneOf = Atoles },
                new StallDef { Id = Stall.Tamales, Name = "Dulce", Bases = new[] { Base.Hoja }, Mains = new[] { Meat.TamalDulce },
                               Common = new Extra[0], OneOf = Atoles }
            }
        };

        /// <summary>
        /// Mundo 6: TODO JUNTO. Un antojo de cada puesto, cada uno con sus propias reglas
        /// (las salsas solo al taco, mayonesa primero en el elote, el champurrado con el tamal). La base sale sola.
        /// </summary>
        public static readonly StallDef Feria = new StallDef
        {
            Id = Stall.Feria, Name = "La Feria",
            Bases = new[] { Base.Tortilla, Base.Palito, Base.PlatoChico, Base.Hoja },
            Mains = new[] { Meat.Pastor, Meat.Elote, Meat.PozoleBlanco, Meat.TamalVerde },
            Common = new Extra[0], OneOf = new Extra[0],
            AutoBase = true,
            Parts = new[]
            {
                new StallDef { Id = Stall.Feria, Name = "Taco", Bases = new[] { Base.Tortilla }, Mains = new[] { Meat.Pastor },
                               Common = new[] { Extra.Cebolla, Extra.Cilantro }, OneOf = new[] { Extra.SalsaVerde, Extra.SalsaRoja } },
                new StallDef { Id = Stall.Feria, Name = "Elote", Bases = new[] { Base.Palito }, Mains = new[] { Meat.Elote },
                               Common = new[] { Extra.Mayonesa, Extra.Queso, Extra.ChilePolvo, Extra.Picante }, OneOf = new Extra[0],
                               First = Extra.Mayonesa, NeedsFirst = Extra.Queso | Extra.ChilePolvo },
                new StallDef { Id = Stall.Feria, Name = "Pozole", Bases = new[] { Base.PlatoChico }, Mains = new[] { Meat.PozoleBlanco },
                               Common = new[] { Extra.Cebolla, Extra.ChilePolvo, Extra.Picante }, OneOf = new Extra[0] },
                new StallDef { Id = Stall.Feria, Name = "Tamal", Bases = new[] { Base.Hoja }, Mains = new[] { Meat.TamalVerde },
                               Common = new[] { Extra.Queso }, OneOf = new[] { Extra.Champurrado } }
            }
        };

        public static readonly StallDef[] AllStalls = { Taqueria, Garnachas, Elotes, Pozole, Tamales, Feria };

        public static StallDef Get(Stall s) => AllStalls[(int)s];
    }
}
