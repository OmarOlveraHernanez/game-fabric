using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    /// <summary>Mundo 2: las garnachas. Lo nuevo es voltear la masa; ademas cada cliente pide su masa.</summary>
    public class GarnachasTests
    {
        static KitchenSimulation Sim(int mission = 0) => new KitchenSimulation(LevelConfig.Garnachas[mission], TaqueroDef.Guero, 1);

        static void Run(KitchenSimulation s, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.05f) s.Tick(0.05f);
        }

        [Test]
        public void SinVoltear_NoSePuedeServir()
        {
            var s = Sim();
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.AddTortilla(Base.Sope)));
            Run(s, s.HotTime + 0.1f);
            Assert.AreEqual(CookState.Flip, s.TortillaState(s.Comal[0]));
            Assert.AreEqual(CommandResult.NeedsFlip, s.Apply(Command.TakeTortilla(0)));
        }

        [Test]
        public void Volteada_QuedaLista()
        {
            var s = Sim();
            s.Apply(Command.AddTortilla(Base.Sope));
            Assert.AreEqual(CommandResult.TortillaCold, s.Apply(Command.FlipItem(0)), "muy pronto");
            Run(s, s.HotTime + 0.1f);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.FlipItem(0)));
            Assert.AreEqual(CommandResult.AlreadyFlipped, s.Apply(Command.FlipItem(0)));
            Assert.AreEqual(CookState.Raw, s.TortillaState(s.Comal[0]), "el otro lado empieza crudo");
            Run(s, s.HotTime + 0.1f);
            Assert.AreEqual(CookState.Ready, s.TortillaState(s.Comal[0]));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.TakeTortilla(0)));
            Assert.AreEqual(Base.Sope, s.Plates[s.SelectedPlate].Base);
        }

        [Test]
        public void SiNoLaVolteas_SeQuema()
        {
            var s = Sim();
            s.Apply(Command.AddTortilla(Base.Sope));
            Run(s, s.HotTime + LevelConfig.Garnachas[0].StallDef.FlipWindow + 0.2f);
            Assert.AreEqual(CookState.Burned, s.TortillaState(s.Comal[0]));
            Assert.AreEqual(1, s.Burned);
        }

        [Test]
        public void LaMasaEquivocada_NoCuenta()
        {
            var plate = new Plate { Base = Base.Gordita, Meat = Meat.Tinga, Extras = Extra.Crema };
            Assert.IsFalse(plate.Matches(new Order(Base.Sope, Meat.Tinga, Extra.Crema)));
            Assert.IsTrue(plate.Matches(new Order(Base.Gordita, Meat.Tinga, Extra.Crema)));
        }

        [Test]
        public void SoloExtrasDelPuesto()
        {
            var s = Sim(1);
            s.Apply(Command.SelectPlate(0));
            s.Plates[0].Base = Base.Sope;
            s.Plates[0].Meat = Meat.Frijoles;
            Assert.AreEqual(CommandResult.InvalidArgument, s.Apply(Command.ToggleExtra(Extra.Pina)), "la pina es de la taqueria");
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.Crema)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.SalsaVerde)));
            Assert.AreEqual(CommandResult.OneSalsaOnly, s.Apply(Command.ToggleExtra(Extra.SalsaRoja)));
        }

        [Test]
        public void Pedidos_SoloConLoDeLaMision()
        {
            foreach (var lvl in LevelConfig.Garnachas)
            {
                var s = new KitchenSimulation(lvl, TaqueroDef.Tio, 3);
                var mask = Extra.None;
                foreach (var e in lvl.StallDef.AllExtras) mask |= e;
                for (int i = 0; i < 60; i++)
                {
                    var o = s.NewOrder();
                    Assert.IsTrue(System.Array.IndexOf(lvl.OrderBases, o.Base) >= 0, lvl.Name);
                    Assert.IsTrue(System.Array.IndexOf(lvl.Meats, o.Meat) >= 0, lvl.Name);
                    Assert.AreEqual(Extra.None, o.Extras & ~mask, lvl.Name);
                }
            }
        }

        [Test]
        public void ElTrompoEnLlamas_DejaLasMasasListas()
        {
            var s = Sim(11);
            s.Apply(Command.AddTortilla(Base.Gordita));
            var t = typeof(KitchenSimulation).GetMethod("Grant", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            t.Invoke(s, new object[] { PowerUp.Trompo, false });
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.UsePowerUp(PowerUp.Trompo)));
            foreach (var c in s.Comal) Assert.AreEqual(CookState.Ready, s.TortillaState(c));
        }

        [Test]
        public void CadaMundoTieneSusMejoras_YSoloCuentanAhi()
        {
            Assert.AreEqual(4, UpgradeDef.For(Stall.Taqueria).Length);
            Assert.AreEqual(4, UpgradeDef.For(Stall.Garnachas).Length);
            var p = new CampaignProgress();
            p.Normalize(24, UpgradeDef.All.Length);
            p.owned[(int)Upgrade.GarComal] = true;
            var basic = new KitchenRules();
            Assert.AreEqual(basic.TortillaHotTime, p.BuildRules(null, Stall.Taqueria).TortillaHotTime, "en la taqueria no cuenta");
            Assert.IsTrue(p.BuildRules(null, Stall.Garnachas).TortillaHotTime < basic.TortillaHotTime, "en las garnachas si");
        }

        // Equilibrio, igual que en el mundo 1: un jugador lento saca al menos 1 estrella y las 3 piden rapidez.
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void CadaMision_EsGanableYLas3PidenRapidez(int mision)
        {
            var lvl = LevelConfig.Garnachas[mision];
            foreach (var taq in TaqueroDef.Starters)
                Assert.That(HintBot.MedianStars(lvl, taq, 0.6f), Is.GreaterThan(0), taq.Name + " en " + lvl.Name);
            Assert.That(HintBot.MedianStars(lvl, TaqueroDef.Tio, 1.5f), Is.LessThan(3), lvl.Name + " lento");
            Assert.AreEqual(3, HintBot.MedianStars(lvl, TaqueroDef.Tio, 0.5f), lvl.Name + " rapido");
        }
    }
}
