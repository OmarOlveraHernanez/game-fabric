using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    public class KitchenSimulationTests
    {
        static KitchenSimulation Sim(int turno = 0, TaqueroDef taq = null, int seed = 7) =>
            new KitchenSimulation(LevelConfig.Turnos[turno], taq ?? TaqueroDef.Tio, seed);

        /// <summary>Avanza hasta que llegue el primer cliente.</summary>
        static void UntilCustomer(KitchenSimulation s)
        {
            for (int i = 0; i < 200 && s.Customers[0] == null; i++) s.Tick(0.05f);
            Assert.IsTrue(s.Customers[0] != null, "debio llegar un cliente");
        }

        static void Advance(KitchenSimulation s, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.05f) s.Tick(0.05f);
        }

        /// <summary>Arma en el plato exactamente el pedido del cliente 0 (turno sin comal).</summary>
        static void BuildOrderFor(KitchenSimulation s, int customer)
        {
            var o = s.Customers[customer].Order;
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.StartMeat(o.Meat)));
            Advance(s, s.CookTime + 0.1f);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakeMeat(0)));
            foreach (var e in Extras.All)
                if (Extras.Has(o.Extras, e)) Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(e)));
        }

        [Test]
        public void SinComal_LosPlatosYaTraenTortilla()
        {
            Assert.IsTrue(Sim(0).Plates[0].Tortilla, "turno 1");
            Assert.IsTrue(Sim(1).Plates[1].Tortilla, "turno 2");
            Assert.IsFalse(Sim(2).Plates[0].Tortilla, "turno 3 con comal");
        }

        [Test]
        public void Carne_CrudaNoSePuedeTomar_ListaSiYQuemadaSeTira()
        {
            var s = Sim(0);
            s.Apply(Command.StartMeat(Meat.Pastor));
            Advance(s, 1f);
            Assert.AreEqual(CommandResult.MeatRaw, s.Apply(Command.TakeMeat(0)));
            Advance(s, s.CookTime);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakeMeat(0)));
            Assert.AreEqual(Meat.Pastor, s.Plates[s.SelectedPlate].Meat);

            s.Apply(Command.StartMeat(Meat.Asada));
            Advance(s, s.Rules.MeatBurnTime + 0.1f);
            Assert.AreEqual(CommandResult.MeatBurned, s.Apply(Command.TakeMeat(0)));
            Assert.IsTrue(s.Grill[0] == null, "la carne quemada se tira");
        }

        [Test]
        public void ElGuero_CocinaVeintePorCientoMasRapido()
        {
            var s = Sim(0, TaqueroDef.Guero);
            Assert.AreEqual(3.2, s.CookTime, 0.001);
            s.Apply(Command.StartMeat(Meat.Asada));
            Advance(s, 3.3f);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakeMeat(0)));
        }

        [Test]
        public void ElTio_ClientesConMasPaciencia()
        {
            var s = Sim(0, TaqueroDef.Tio);
            UntilCustomer(s);
            Assert.AreEqual(72.0, s.Customers[0].MaxPatience, 0.01);
        }

        [Test]
        public void ElPaisa_ComalDeCuatro()
        {
            var s = Sim(2, TaqueroDef.Paisa);
            for (int i = 0; i < 4; i++) Assert.AreEqual(CommandResult.Ok, s.Apply(Command.AddTortilla()));
            Assert.AreEqual(CommandResult.ComalFull, s.Apply(Command.AddTortilla()));
            Assert.AreEqual(2, Sim(2, TaqueroDef.Guero).Comal.Length, "los demás tienen comal de 2");
        }

        [Test]
        public void Tortilla_SoloEnTurnoConComal_FriaNoCalienteSi()
        {
            Assert.AreEqual(CommandResult.NotInThisLevel, Sim(0).Apply(Command.AddTortilla()));
            var s = Sim(2);
            s.Apply(Command.AddTortilla());
            Assert.AreEqual(CommandResult.TortillaCold, s.Apply(Command.TakeTortilla(0)));
            Advance(s, s.Rules.TortillaHotTime + 0.1f);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakeTortilla(0)));
            Assert.IsTrue(s.Plates[0].Tortilla);
        }

        [Test]
        public void Turno3_CarneSinTortillaEnElPlato_NoSePuede()
        {
            var s = Sim(2);
            s.Apply(Command.StartMeat(Meat.Pastor));
            Advance(s, s.CookTime + 0.1f);
            Assert.AreEqual(CommandResult.NeedTortillaFirst, s.Apply(Command.TakeMeat(0)));
        }

        [Test]
        public void Extras_NecesitanCarne_SeQuitanConOtroToque_YUnaSolaSalsa()
        {
            var s = Sim(0);
            Assert.AreEqual(CommandResult.NeedMeatFirst, s.Apply(Command.ToggleExtra(Extra.Cebolla)));
            s.Apply(Command.StartMeat(Meat.Pastor));
            Advance(s, s.CookTime + 0.1f);
            s.Apply(Command.TakeMeat(0));
            s.Apply(Command.ToggleExtra(Extra.Cebolla));
            Assert.AreEqual(Extra.Cebolla, s.Plates[s.SelectedPlate].Extras);
            s.Apply(Command.ToggleExtra(Extra.Cebolla));
            Assert.AreEqual(Extra.None, s.Plates[s.SelectedPlate].Extras);
            s.Apply(Command.ToggleExtra(Extra.SalsaVerde));
            Assert.AreEqual(CommandResult.OneSalsaOnly, s.Apply(Command.ToggleExtra(Extra.SalsaRoja)));
        }

        [Test]
        public void EntregaCorrecta_PagaBaseMasExtrasMasPropina()
        {
            var s = Sim(0);
            UntilCustomer(s);
            BuildOrderFor(s, 0);
            var c = s.Customers[0];
            int tip = (int)System.Math.Round(5 * c.PatienceRatio, System.MidpointRounding.AwayFromZero);
            int expected = 10 + 2 * Extras.Count(c.Order.Extras) + tip;
            Assert.AreEqual(CommandResult.Served, s.Apply(Command.Deliver(0)));
            Assert.AreEqual(expected, s.Coins);
            Assert.AreEqual(1, s.Served);
            Assert.IsTrue(s.Customers[0] == null);
            Assert.IsTrue(s.Plates[0].Tortilla && !s.Plates[0].HasMeat, "el plato vuelve limpio con tortilla");
        }

        [Test]
        public void EntregaEquivocada_CuentaComoFalla()
        {
            var s = Sim(0);
            UntilCustomer(s);
            var o = s.Customers[0].Order;
            var otra = o.Meat == Meat.Pastor ? Meat.Asada : Meat.Pastor;
            s.Apply(Command.StartMeat(otra));
            Advance(s, s.CookTime + 0.1f);
            s.Apply(Command.TakeMeat(0));
            Assert.AreEqual(CommandResult.WrongOrder, s.Apply(Command.Deliver(0)));
            Assert.AreEqual(1, s.Failed);
            Assert.AreEqual(0, s.Coins);
        }

        [Test]
        public void Entrega_UsaElPlatoQueCoincideAunqueNoEsteElegido()
        {
            var s = Sim(0);
            UntilCustomer(s);
            BuildOrderFor(s, 0);
            int listo = s.SelectedPlate;
            s.Apply(Command.SelectPlate(1 - listo));
            Assert.IsTrue(s.HasPlateFor(0));
            Assert.AreEqual(CommandResult.Served, s.Apply(Command.Deliver(0)));
        }

        [Test]
        public void SinPlatoListo_NoSeEntregaNada()
        {
            var s = Sim(0);
            UntilCustomer(s);
            Assert.AreEqual(CommandResult.NoPlateReady, s.Apply(Command.Deliver(0)));
            Assert.IsTrue(s.Customers[0] != null, "el cliente sigue esperando");
        }

        [Test]
        public void SeAcabaLaPaciencia_ElClienteSeVa()
        {
            var s = Sim(0, TaqueroDef.Guero);
            UntilCustomer(s);
            int left = 0;
            s.CustomerLeft += _ => left++;
            Advance(s, 61f);
            Assert.AreEqual(1, left);
            Assert.That(s.Failed, Is.GreaterThan(0));
        }

        [TestCase(0, 1)]
        [TestCase(1, 2)]
        [TestCase(2, 3)]
        public void NuncaHayMasClientesQueLosDelTurno(int turno, int max)
        {
            var s = Sim(turno, seed: 3);
            int peak = 0;
            for (int i = 0; i < 1800; i++)
            {
                s.Tick(0.05f);
                int n = 0;
                foreach (var c in s.Customers) if (c != null) n++;
                if (n > peak) peak = n;
            }
            Assert.AreEqual(max, peak);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void Pedidos_RespetanReglas(int turno)
        {
            var s = Sim(turno, seed: 11);
            for (int i = 0; i < 500; i++)
            {
                var o = s.NewOrder();
                int n = Extras.Count(o.Extras);
                Assert.IsTrue(n >= 1 && n <= LevelConfig.Turnos[turno].MaxExtras, "extras fuera de rango: " + o);
                Assert.IsFalse((o.Extras & Extras.Salsas) == Extras.Salsas, "dos salsas: " + o);
                if (Extras.Has(o.Extras, Extra.Pina)) Assert.AreEqual(Meat.Pastor, o.Meat, "piña sin pastor");
            }
        }

        [Test]
        public void ElTurnoTerminaALos90Segundos_YYaNoAceptaComandos()
        {
            var s = Sim(0);
            int fin = 0;
            s.Finished += () => fin++;
            Advance(s, 90.1f);
            Assert.IsTrue(s.IsFinished);
            Assert.AreEqual(1, fin);
            Assert.AreEqual(CommandResult.NotRunning, s.Apply(Command.StartMeat(Meat.Pastor)));
        }

        [Test]
        public void MismaSemilla_MismosPedidos()
        {
            var a = Sim(2, seed: 42);
            var b = Sim(2, seed: 42);
            for (int i = 0; i < 20; i++) Assert.AreEqual(a.NewOrder().ToString(), b.NewOrder().ToString());
        }

        [Test]
        public void Estrellas_SegunMonedas()
        {
            var s = Sim(0);
            Assert.AreEqual(0, s.StarsFor(59));
            Assert.AreEqual(1, s.StarsFor(60));
            Assert.AreEqual(2, s.StarsFor(140));
            Assert.AreEqual(3, s.StarsFor(200));
        }
    }
}
