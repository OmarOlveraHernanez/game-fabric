using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    public class CampaignTests
    {
        static CampaignProgress NewProgress()
        {
            var p = new CampaignProgress();
            p.Normalize(LevelConfig.Zona1.Length, UpgradeDef.All.Length);
            return p;
        }

        static void Advance(KitchenSimulation s, float seconds)
        {
            for (float t = 0; t < seconds; t += 0.05f) s.Tick(0.05f);
        }

        [Test]
        public void Zona1_TieneDoceMisiones_LasPrimerasSonLosTurnos()
        {
            Assert.AreEqual(12, LevelConfig.Zona1.Length);
            Assert.IsTrue(ReferenceEquals(LevelConfig.Zona1[0], LevelConfig.Turnos[0]));
            Assert.AreEqual(120.0, LevelConfig.Zona1[11].Duration, 0.01);
        }

        [Test]
        public void Misiones_SeAbrenConUnaEstrellaEnLaAnterior()
        {
            var p = NewProgress();
            Assert.IsTrue(p.IsUnlocked(0));
            Assert.IsFalse(p.IsUnlocked(1));
            p.Record(0, 1, 70);
            Assert.IsTrue(p.IsUnlocked(1));
            Assert.IsFalse(p.IsUnlocked(2));
        }

        [Test]
        public void Record_SumaMonedasSiempre_YSoloMejoraEstrellas()
        {
            var p = NewProgress();
            Assert.IsFalse(p.Record(0, 0, 40), "sin estrellas no es record");
            Assert.AreEqual(40, p.wallet);
            Assert.IsTrue(p.Record(0, 2, 150));
            Assert.IsFalse(p.Record(0, 1, 80), "una estrella menos no baja el record");
            Assert.AreEqual(2, p.stars[0]);
            Assert.AreEqual(270, p.wallet);
            Assert.AreEqual(2, p.TotalStars);
        }

        [Test]
        public void Mejoras_SeCompranUnaVez_ConMonedasSuficientes()
        {
            var p = NewProgress();
            Assert.IsFalse(p.Buy(Upgrade.Paciencia));
            p.wallet = 260;
            Assert.IsTrue(p.Buy(Upgrade.Paciencia));
            Assert.AreEqual(10, p.wallet);
            Assert.IsTrue(p.Owns(Upgrade.Paciencia));
            p.wallet = 1000;
            Assert.IsFalse(p.Buy(Upgrade.Paciencia), "no se compra dos veces");
        }

        [Test]
        public void BuildRules_AplicaMejoras_SinTocarLasReglasBase()
        {
            var p = NewProgress();
            foreach (var u in UpgradeDef.All) p.owned[(int)u.Id] = true;
            var baseRules = new KitchenRules();
            var r = p.BuildRules(baseRules);
            Assert.AreEqual(3, r.PlateCount);
            Assert.AreEqual(8, r.PotPortions);
            Assert.AreEqual(1.2, r.TortillaHotTime, 0.001);
            Assert.AreEqual(1.15, r.PatienceMultiplier, 0.001);
            Assert.AreEqual(2, baseRules.PlateCount, "la base no cambia");

            var s = new KitchenSimulation(LevelConfig.Zona1[2], TaqueroDef.Guero, 1, r);
            Assert.AreEqual(3, s.Plates.Length);
            Assert.AreEqual(8, s.Pots[0].Portions);
        }

        [Test]
        public void Normalize_AjustaGuardadosViejos()
        {
            var p = new CampaignProgress { stars = new[] { 3, 2, 1 }, owned = null };
            p.Normalize(12, 4);
            Assert.AreEqual(12, p.stars.Length);
            Assert.AreEqual(4, p.owned.Length);
            Assert.AreEqual(1, p.stars[2]);
        }

        [Test]
        public void ClienteApurado_MenosPaciencia_PropinaDoble()
        {
            var lvl = new LevelConfig { MaxCustomers = 1, Patience = 50f, MaxExtras = 1, RushChance = 1f, Stars = new[] { 1, 2, 3 }, GapMin = 1f, GapMax = 1f };
            var s = new KitchenSimulation(lvl, TaqueroDef.Guero, 3);
            for (int i = 0; i < 100 && s.Customers[0] == null; i++) s.Tick(0.05f);
            var c = s.Customers[0];
            Assert.IsTrue(c.Rush);
            Assert.AreEqual(30.0, c.MaxPatience, 0.01);
            s.Apply(Command.ServeMeat(c.Order.Meat));
            foreach (var e in Extras.All) if (Extras.Has(c.Order.Extras, e)) s.Apply(Command.ToggleExtra(e));
            int baseTip = (int)System.Math.Round(5 * c.PatienceRatio, System.MidpointRounding.AwayFromZero);
            int tip = -1;
            s.OrderServed += (slot, pay, t) => tip = t;
            Assert.AreEqual(CommandResult.Served, s.Apply(Command.Deliver(0)));
            Assert.AreEqual(baseTip * 2, tip);
        }

        [Test]
        public void CeroErrores_UnaFallaQuitaLasEstrellas()
        {
            var lvl = LevelConfig.Zona1[5];
            Assert.AreEqual(MissionRule.NoErrors, lvl.Rule);
            var s = new KitchenSimulation(lvl, TaqueroDef.Guero, 5);
            for (int i = 0; i < 200 && s.Customers[0] == null; i++) s.Tick(0.05f);
            Advance(s, 60f); // el cliente se cansa
            Assert.That(s.Failed, Is.GreaterThan(0));
            Assert.IsTrue(s.RuleBroken);
            Assert.AreEqual(0, s.Stars);
        }

        [Test]
        public void SinQuemar_QuemarTortillaRompeLaRegla()
        {
            var s = new KitchenSimulation(LevelConfig.Zona1[9], TaqueroDef.Guero, 5);
            Assert.AreEqual(MissionRule.NoBurn, s.Level.Rule);
            int burned = 0;
            s.ItemBurned += () => burned++;
            s.Apply(Command.AddTortilla());
            Advance(s, s.Rules.TortillaBurnTime + 0.2f);
            Assert.AreEqual(1, burned, "se cuenta una sola vez");
            Assert.AreEqual(1, s.Burned);
            Assert.IsTrue(s.RuleBroken);
        }

        [Test]
        public void SoloPastor_YClienteExigente_RespetanSusPedidos()
        {
            var pastor = new KitchenSimulation(LevelConfig.Zona1[10], TaqueroDef.Tio, 8);
            for (int i = 0; i < 300; i++) Assert.AreEqual(Meat.Pastor, pastor.NewOrder().Meat);
            var exigente = new KitchenSimulation(LevelConfig.Zona1[8], TaqueroDef.Tio, 8);
            for (int i = 0; i < 300; i++)
            {
                int n = Extras.Count(exigente.NewOrder().Extras);
                Assert.IsTrue(n >= 3 && n <= 4, "extras: " + n);
            }
        }

        [Test]
        public void TodasLasMisiones_PedidosValidos()
        {
            for (int m = 0; m < LevelConfig.Zona1.Length; m++)
            {
                var lvl = LevelConfig.Zona1[m];
                var s = new KitchenSimulation(lvl, TaqueroDef.Tio, 20 + m);
                for (int i = 0; i < 200; i++)
                {
                    var o = s.NewOrder();
                    int n = Extras.Count(o.Extras);
                    Assert.IsTrue(n >= 1 && n <= lvl.MaxExtras, lvl.Name + ": " + o);
                    Assert.IsFalse((o.Extras & Extras.Salsas) == Extras.Salsas, lvl.Name + " dos salsas");
                    if (Extras.Has(o.Extras, Extra.Pina)) Assert.AreEqual(Meat.Pastor, o.Meat);
                }
                Assert.AreEqual(3, lvl.Stars.Length, lvl.Name);
                Assert.IsTrue(lvl.Stars[0] < lvl.Stars[1] && lvl.Stars[1] < lvl.Stars[2], lvl.Name + " estrellas en orden");
            }
        }
    }
}
