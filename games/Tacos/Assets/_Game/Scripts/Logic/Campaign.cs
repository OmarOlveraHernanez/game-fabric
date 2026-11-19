using System;

namespace Tacos.Logic
{
    /// <summary>Mejoras de la tienda. Cada mundo tiene las suyas (se guardan por numero: no reordenar).</summary>
    public enum Upgrade
    {
        Paciencia = 0, ComalRapido = 1, TercerPlato = 2, Plancha3 = 3,             // Taqueria
        GarAguas = 4, GarComal = 5, GarTabla = 6, GarCazuelas = 7                    // Garnachas
    }

    public sealed class UpgradeDef
    {
        public Upgrade Id;
        public Stall World = Stall.Taqueria; // solo funciona en su mundo
        public Upgrade Effect;               // que hace (las de cada mundo hacen lo mismo que una de la taqueria)
        public string Name, Description;
        public int Price;

        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef { Id = Upgrade.Paciencia, Effect = Upgrade.Paciencia, Name = "Radio cumbiera", Description = "Con música los clientes esperan 15 % más.", Price = 250 },
            new UpgradeDef { Id = Upgrade.ComalRapido, Effect = Upgrade.ComalRapido, Name = "Domo de fierro", Description = "Las tortillas se calientan 40 % más rápido.", Price = 300 },
            new UpgradeDef { Id = Upgrade.TercerPlato, Effect = Upgrade.TercerPlato, Name = "Tercer plato", Description = "Un plato más para armar 3 tacos a la vez.", Price = 450 },
            new UpgradeDef { Id = Upgrade.Plancha3, Effect = Upgrade.Plancha3, Name = "Cazos grandes", Description = "Cada cazo lleva 3 porciones más.", Price = 600 },
            // Mundo 2: Garnachas
            new UpgradeDef { Id = Upgrade.GarAguas, World = Stall.Garnachas, Effect = Upgrade.Paciencia, Name = "Aguas frescas", Description = "Con su agüita los clientes esperan 15 % más.", Price = 400 },
            new UpgradeDef { Id = Upgrade.GarComal, World = Stall.Garnachas, Effect = Upgrade.ComalRapido, Name = "Comal de barro", Description = "Las masas se cuecen 40 % más rápido.", Price = 450 },
            new UpgradeDef { Id = Upgrade.GarTabla, World = Stall.Garnachas, Effect = Upgrade.TercerPlato, Name = "Tabla larga", Description = "Un plato más para armar 3 garnachas a la vez.", Price = 600 },
            new UpgradeDef { Id = Upgrade.GarCazuelas, World = Stall.Garnachas, Effect = Upgrade.Plancha3, Name = "Cazuelas hondas", Description = "Cada cazuela lleva 3 porciones más.", Price = 800 }
        };

        public static UpgradeDef Get(Upgrade u) => All[(int)u];

        /// <summary>Las mejoras de la tienda de un mundo, en el orden de los letreros.</summary>
        public static UpgradeDef[] For(Stall world) => Array.FindAll(All, d => d.World == world);

        /// <summary>Aplica la mejora a una copia de las reglas.</summary>
        public static void Apply(KitchenRules r, Upgrade u)
        {
            switch (Get(u).Effect)
            {
                case Upgrade.Paciencia: r.PatienceMultiplier *= 1.15f; break;
                case Upgrade.ComalRapido: r.TortillaHotTime *= 0.6f; break;
                case Upgrade.TercerPlato: r.PlateCount = Math.Max(r.PlateCount, 3); break;
                case Upgrade.Plancha3: r.PotPortions += 3; break; // nombre viejo: se guarda por numero
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

        /// <summary>La primera mision de cada mundo esta abierta (el mundo se abre con estrellas); las demas, con 1 estrella en la anterior.</summary>
        public bool IsUnlocked(int mission) =>
            mission >= 0 && mission < Math.Max(1, stars.Length) && (mission % MissionsPerWorld == 0 || stars[mission - 1] > 0);

        public const int MissionsPerWorld = 12;

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
        /// <summary>Las reglas con las mejoras compradas de ese mundo (las de otro mundo no cuentan).</summary>
        public KitchenRules BuildRules(KitchenRules baseRules = null, Stall world = Stall.Taqueria)
        {
            var r = (baseRules ?? new KitchenRules()).Clone();
            foreach (var def in UpgradeDef.All) if (def.World == world && Owns(def.Id)) UpgradeDef.Apply(r, def.Id);
            return r;
        }
    }
}
