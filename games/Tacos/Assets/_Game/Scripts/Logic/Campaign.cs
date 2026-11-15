using System;

namespace Tacos.Logic
{
    public enum Upgrade { Paciencia = 0, ComalRapido = 1, TercerPlato = 2, Plancha3 = 3 }

    public sealed class UpgradeDef
    {
        public Upgrade Id;
        public string Name, Description;
        public int Price;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef { Id = Upgrade.Paciencia, Name = "Clientes pacientes", Description = "Todos los clientes esperan 15 % más.", Price = 250 },
            new UpgradeDef { Id = Upgrade.ComalRapido, Name = "Comal de fierro", Description = "Las tortillas se calientan 40 % más rápido.", Price = 300 },
            new UpgradeDef { Id = Upgrade.TercerPlato, Name = "Tercer plato", Description = "Un plato más en la barra para armar 3 tacos a la vez.", Price = 450 },
            new UpgradeDef { Id = Upgrade.Plancha3, Name = "Plancha grande", Description = "La plancha cocina 3 carnes a la vez.", Price = 600 }
        };

        public static UpgradeDef Get(Upgrade u) => All[(int)u];

        /// <summary>Aplica la mejora a una copia de las reglas.</summary>
        public static void Apply(KitchenRules r, Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Paciencia: r.PatienceMultiplier *= 1.15f; break;
                case Upgrade.ComalRapido: r.TortillaHotTime *= 0.6f; break;
                case Upgrade.TercerPlato: r.PlateCount = Math.Max(r.PlateCount, 3); break;
                case Upgrade.Plancha3: r.GrillSlots = Math.Max(r.GrillSlots, 3); break;
            }
        }
    }

    /// <summary>
    /// Avance de la campaña. Se guarda tal cual en JSON (campos publicos, sin Unity).
    /// Las monedas de cada mision se suman a la cartera aunque no se ganen estrellas, para poder juntar y mejorar.
    /// </summary>
    [Serializable]
    public sealed class CampaignProgress
    {
        public int[] stars = new int[0];
        public bool[] owned = new bool[0];
        public int wallet;
        public int coinsEarned;

        /// <summary>Ajusta los arreglos al numero actual de misiones y mejoras (por si el guardado es de otra version).</summary>
        public void Normalize(int missions, int upgrades)
        {
            if (stars == null || stars.Length != missions) Array.Resize(ref stars, missions);
            if (owned == null || owned.Length != upgrades) Array.Resize(ref owned, upgrades);
            if (wallet < 0) wallet = 0;
        }

        public bool IsUnlocked(int mission) => mission == 0 || (mission > 0 && mission < stars.Length && stars[mission - 1] > 0);

        public int TotalStars
        {
            get { int n = 0; foreach (var s in stars) n += s; return n; }
        }

        /// <summary>Guarda el resultado de una mision. Devuelve true si mejoro su record de estrellas.</summary>
        public bool Record(int mission, int earnedStars, int coins)
        {
            if (coins > 0) { wallet += coins; coinsEarned += coins; }
            if (earnedStars <= stars[mission]) return false;
            stars[mission] = earnedStars;
            return true;
        }

        public bool Owns(Upgrade u) => owned[(int)u];

        public bool CanBuy(Upgrade u) => !Owns(u) && wallet >= UpgradeDef.Get(u).Price;

        public bool Buy(Upgrade u)
        {
            if (!CanBuy(u)) return false;
            wallet -= UpgradeDef.Get(u).Price;
            owned[(int)u] = true;
            return true;
        }

        /// <summary>Reglas de cocina con las mejoras compradas.</summary>
        public KitchenRules BuildRules(KitchenRules baseRules = null)
        {
            var r = (baseRules ?? new KitchenRules()).Clone();
            foreach (var def in UpgradeDef.All) if (Owns(def.Id)) UpgradeDef.Apply(r, def.Id);
            return r;
        }
    }
}
