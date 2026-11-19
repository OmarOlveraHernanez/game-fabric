using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    public class PowerUpTests
    {
        static LevelConfig Level(PowerUp[] powerUps = null, CustomerKind[] specials = null, float chance = 0f, int customers = 1) =>
            new LevelConfig
            {
                Name = "prueba", Duration = 900f, MaxCustomers = customers, Patience = 40f, MaxExtras = 1, Stars = new[] { 1, 2, 3 },
                GapMin = 0.5f, GapMax = 0.5f, PowerUps = powerUps ?? LevelConfig.NoPowerUps,
                Specials = specials ?? LevelConfig.NoSpecials, SpecialChance = chance
            };

        static readonly PowerUp[] Todos = PowerUps.All;

        static void Advance(KitchenSimulation s, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.05f) s.Tick(0.05f);
        }

        static void UntilCustomer(KitchenSimulation s, int slot = 0)
        {
            for (int i = 0; i < 400 && s.Customers[slot] == null; i++) s.Tick(0.05f);
            Assert.IsTrue(s.Customers[slot] != null, "llego el cliente");
        }

        /// <summary>Arma el pedido del cliente y se lo entrega.</summary>
        static CommandResult ServeCustomer(KitchenSimulation s, int slot = 0)
        {
            var o = s.Customers[slot].Order;
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ServeMeat(o.Meat)));
            foreach (var e in Extras.All) if (Extras.Has(o.Extras, e)) s.Apply(Command.ToggleExtra(e));
            return s.Apply(Command.Deliver(slot));
        }

        [Test]
        public void Racha_CargaPowerUps_YSeRompeConUnError()
        {
            var s = new KitchenSimulation(Level(Todos), TaqueroDef.Tio, 3);
            int gained = 0;
            s.PowerUpGained += (p, pickup) => gained++;
            for (int i = 0; i < 3; i++)
            {
                UntilCustomer(s);
                Assert.AreEqual(CommandResult.Served, ServeCustomer(s));
            }
            Assert.AreEqual(0, gained, "con 3 tacos todavia no");
            UntilCustomer(s);
            ServeCustomer(s);
            Assert.AreEqual(4, s.Streak);
            Assert.AreEqual(1, gained, "4 tacos cargan uno");
            Assert.AreEqual(1, s.Stock(PowerUp.Cebollitas), "el primero es el primero de la lista");

            UntilCustomer(s);
            s.Apply(Command.ServeMeat(s.Customers[0].Order.Meat == Meat.Pastor ? Meat.Asada : Meat.Pastor));
            int broke = 0;
            s.StreakBroken += n => broke = n;
            Assert.AreEqual(CommandResult.WrongOrder, s.Apply(Command.Deliver(0)));
            Assert.AreEqual(0, s.Streak);
            Assert.AreEqual(4, broke);
            Assert.AreEqual(4, s.BestStreak);
        }

        [Test]
        public void SinPowerUpsEnLaMision_NoSeCargaNada()
        {
            var s = new KitchenSimulation(Level(), TaqueroDef.Tio, 3);
            for (int i = 0; i < 5; i++) { UntilCustomer(s); ServeCustomer(s); }
            Assert.AreEqual(0f, s.Charge);
            foreach (var p in Todos) Assert.AreEqual(0, s.Stock(p));
            Advance(s, 40f);
            Assert.IsFalse(s.Pickup.HasValue, "tampoco aparecen en la barra");
        }

        [Test]
        public void PowerUpEnLaBarra_SeAgarraOSeVa()
        {
            var s = new KitchenSimulation(Level(Todos), TaqueroDef.Tio, 5);
            Assert.AreEqual(CommandResult.NoPickup, s.Apply(Command.TakePickup()));
            for (int i = 0; i < 2000 && !s.Pickup.HasValue; i++) s.Tick(0.05f);
            Assert.IsTrue(s.Pickup.HasValue, "aparece uno");
            var p = s.Pickup.Value;
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakePickup()));
            Assert.AreEqual(1, s.Stock(p));
            Assert.IsFalse(s.Pickup.HasValue);

            for (int i = 0; i < 2000 && !s.Pickup.HasValue; i++) s.Tick(0.05f);
            bool expired = false;
            s.PickupExpired += () => expired = true;
            Advance(s, s.Rules.PickupLife + 0.2f);
            Assert.IsTrue(expired, "si no lo agarras se va");
        }

        [Test]
        public void Cebollitas_AtiendenSolasAlMasDesesperado()
        {
            var s = new KitchenSimulation(Level(Todos, customers: 2), TaqueroDef.Tio, 9);
            Assert.AreEqual(CommandResult.NoPowerUp, s.Apply(Command.UsePowerUp(PowerUp.Cebollitas)));
            UntilCustomer(s, 0); UntilCustomer(s, 1);
            GiveAll(s);
            s.Customers[0].Patience = 40f;
            s.Customers[1].Patience = 5f;
            int coins = s.Coins;
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.UsePowerUp(PowerUp.Cebollitas)));
            Assert.IsTrue(s.Customers[1] == null, "atendio al que tenia menos paciencia");
            Assert.IsTrue(s.Customers[0] != null);
            Assert.That(s.Coins, Is.GreaterThan(coins));
        }

        [Test]
        public void Nopal_CongelaLaPaciencia_Salsa_DoblaLaPropina()
        {
            var s = new KitchenSimulation(Level(Todos), TaqueroDef.Tio, 4);
            GiveAll(s);
            UntilCustomer(s);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.UsePowerUp(PowerUp.Nopal)));
            Assert.AreEqual(CommandResult.PowerUpActive, s.Apply(Command.UsePowerUp(PowerUp.Nopal)));
            float before = s.Customers[0].Patience;
            Advance(s, 3f);
            Assert.AreEqual(before, s.Customers[0].Patience, 0.001);
            Advance(s, s.Rules.NopalTime);
            Assert.That(s.Customers[0].Patience, Is.LessThan(before));

            int tipNormal = -1, tipSalsa = -1;
            s.OrderServed += (slot, pay, tip) => { if (tipNormal < 0) tipNormal = tip; else tipSalsa = tip; };
            s.Customers[0].Patience = s.Customers[0].MaxPatience;
            ServeCustomer(s);
            UntilCustomer(s);
            s.Apply(Command.UsePowerUp(PowerUp.Salsa));
            s.Customers[0].Patience = s.Customers[0].MaxPatience;
            ServeCustomer(s);
            Assert.AreEqual(tipNormal * 2, tipSalsa);
        }

        [Test]
        public void TrompoEnLlamas_LlenaCazosYCalientaTortillas()
        {
            var lvl = Level(Todos); lvl.NeedsTortilla = true;
            var s = new KitchenSimulation(lvl, TaqueroDef.Tio, 4);
            GiveAll(s);
            s.Pot(Meat.Asada).Portions = 0;
            s.Apply(Command.RefillMeat(Meat.Asada));
            s.Apply(Command.AddTortilla());
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.UsePowerUp(PowerUp.Trompo)));
            Assert.AreEqual(s.Rules.PotPortions, s.Pot(Meat.Asada).Portions);
            Assert.IsFalse(s.Pot(Meat.Asada).IsRefilling);
            foreach (var t in s.Comal) Assert.AreEqual(CookState.Ready, s.TortillaState(t));
        }

        [Test]
        public void Mariachi_PideTresYSeQuedaHastaTenerlos()
        {
            var s = new KitchenSimulation(Level(specials: new[] { CustomerKind.Mariachi }, chance: 1f), TaqueroDef.Tio, 2);
            UntilCustomer(s);
            var c = s.Customers[0];
            Assert.AreEqual(CustomerKind.Mariachi, c.Kind);
            int coins = 0;
            s.OrderServed += (slot, pay, tip) => coins += pay;
            Assert.AreEqual(CommandResult.Served, ServeCustomer(s));
            Assert.IsTrue(s.Customers[0] == c, "sigue esperando");
            Assert.AreEqual(2, c.Remaining);
            ServeCustomer(s);
            ServeCustomer(s);
            Assert.IsTrue(s.Customers[0] == null, "con 3 tacos se va");
            Assert.AreEqual(3, s.Served);
            Assert.That(coins, Is.GreaterThan(3 * s.Rules.BasePay + s.Rules.MariachiBonus - 1));
        }

        [Test]
        public void Critico_PagaTriple()
        {
            var normal = new KitchenSimulation(Level(), TaqueroDef.Tio, 2);
            var critico = new KitchenSimulation(Level(specials: new[] { CustomerKind.Critico }, chance: 1f), TaqueroDef.Tio, 2);
            UntilCustomer(normal); UntilCustomer(critico);
            Assert.AreEqual(CustomerKind.Critico, critico.Customers[0].Kind);
            ServeCustomer(normal); ServeCustomer(critico);
            Assert.AreEqual(normal.Coins * 3, critico.Coins);
        }

        [Test]
        public void Borrachito_CambiaSuPedidoUnaVez()
        {
            var s = new KitchenSimulation(Level(specials: new[] { CustomerKind.Borrachito }, chance: 1f), TaqueroDef.Tio, 11);
            UntilCustomer(s);
            var c = s.Customers[0];
            int changed = 0;
            s.OrderChanged += slot => changed++;
            for (int i = 0; i < 2000 && changed == 0; i++) s.Tick(0.1f);
            Assert.AreEqual(1, changed);
            Assert.IsTrue(c.Changed);
            Assert.That(c.PatienceRatio, Is.GreaterThan(0.75f), "le da tiempo de pedir otra vez");
            while (c.PatienceRatio >= s.Rules.BorrachitoChangeAt) s.Tick(0.1f);
            s.Tick(0.1f);
            Assert.AreEqual(1, changed, "solo una vez");
        }

        [Test]
        public void PausaDelTutorial_NoBajaLaPaciencia()
        {
            var s = new KitchenSimulation(Level(), TaqueroDef.Tio, 2);
            UntilCustomer(s);
            s.PausePatience = true;
            float p = s.Customers[0].Patience;
            Advance(s, 5f);
            Assert.AreEqual(p, s.Customers[0].Patience, 0.001);
        }

        [Test]
        public void Misiones_IntroducenUnaCosaALaVez()
        {
            var z = LevelConfig.Zona1;
            Assert.AreEqual(0, z[0].PowerUps.Length, "la mision 1 es solo aprender");
            for (int i = 1; i < z.Length; i++)
            {
                Assert.That(z[i].PowerUps.Length, Is.GreaterThanOrEqualTo(z[i - 1].PowerUps.Length), z[i].Name);
                Assert.IsTrue(!string.IsNullOrEmpty(z[i].Intro), z[i].Name + " explica lo nuevo");
            }
            Assert.AreEqual(4, z[11].PowerUps.Length);
            Assert.AreEqual(3, z[11].Specials.Length);
        }

        static void GiveAll(KitchenSimulation s)
        {
            // Agarra power-ups de la barra hasta tener uno de cada uno
            for (int guard = 0; guard < 200; guard++)
            {
                bool all = true;
                foreach (var p in PowerUps.All) if (s.Stock(p) == 0) all = false;
                if (all) return;
                for (int i = 0; i < 2000 && !s.Pickup.HasValue; i++)
                {
                    s.Tick(0.05f);
                    foreach (var c in s.Customers) if (c != null) c.Patience = c.MaxPatience;
                }
                s.Apply(Command.TakePickup());
            }
            Assert.IsTrue(false, "no junto todos los power-ups");
        }
    }
}
