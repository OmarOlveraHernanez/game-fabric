using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    /// <summary>Jefe final de la mision 12 de cada mundo: llega a media mision y pide 3 platos, uno tras otro.</summary>
    public class BossTests
    {
        static KitchenSimulation Boss(out LevelConfig lvl)
        {
            lvl = LevelConfig.Zona1[11];
            return new KitchenSimulation(lvl, TaqueroDef.Guero, 5);
        }

        static void Run(KitchenSimulation s, float seconds) { for (float t = 0f; t < seconds; t += 0.05f) s.Tick(0.05f); }

        /// <summary>Corre hasta que llega el jefe (su lugar se libera cuando se va quien estaba ahi).</summary>
        static void UntilBoss(KitchenSimulation s)
        {
            for (float t = 0f; t < 200f && !s.BossWaiting; t += 0.05f)
            {
                // se atiende al que ocupa el lugar del jefe para que lo libere
                var c = s.Customers[s.BossSlot];
                if (c != null && c.Kind != CustomerKind.Jefe && s.Time >= s.Level.BossAt) Serve(s, s.BossSlot);
                s.Tick(0.05f);
            }
            Assert.IsTrue(s.BossWaiting, "el jefe llego");
        }

        static void Serve(KitchenSimulation s, int slot)
        {
            var o = s.Customers[slot].Order;
            int p = s.SelectedPlate;
            s.Plates[p].Base = o.Base; s.Plates[p].Meat = o.Meat; s.Plates[p].Extras = o.Extras;
            Assert.AreEqual(CommandResult.Served, s.Apply(Command.Deliver(slot)));
        }

        [Test]
        public void LasMisiones12TienenJefe()
        {
            foreach (var w in LevelConfig.Worlds)
            {
                Assert.IsTrue(w[11].BossAt > 0f, w[11].Name);
                Assert.IsFalse(string.IsNullOrEmpty(w[11].BossName));
                for (int i = 0; i < 11; i++) Assert.AreEqual(0f, w[i].BossAt, w[i].Name);
            }
        }

        [Test]
        public void LlegaASuHora_YSeLeApartaSuLugar()
        {
            var s = Boss(out var lvl);
            bool arrived = false;
            s.BossArrived += slot => arrived = true;
            Run(s, lvl.BossAt - 1f);
            Assert.IsFalse(arrived, "todavia no es su hora");
            UntilBoss(s);
            Assert.IsTrue(arrived);
            Assert.AreEqual(CustomerKind.Jefe, s.Customers[s.BossSlot].Kind);
        }

        [Test]
        public void Pide3Platos_YAlFinalPagaElBono()
        {
            var s = Boss(out var lvl);
            UntilBoss(s);
            int slot = s.BossSlot;
            int coins = s.Coins;
            Serve(s, slot);
            Assert.IsTrue(s.Customers[slot] != null, "sigue ahi: faltan 2");
            Serve(s, slot);
            Assert.IsTrue(s.Customers[slot] != null);
            Serve(s, slot);
            Assert.IsTrue(s.Customers[slot] == null);
            Assert.IsTrue(s.BossDone);
            Assert.IsTrue(s.Coins - coins >= s.Rules.BossBonus);
        }

        [Test]
        public void SiSeVa_NoHayEstrellas()
        {
            var s = Boss(out var lvl);
            UntilBoss(s);
            Run(s, lvl.Patience * s.Rules.BossPatience * 2f + 5f);
            Assert.IsTrue(s.BossFailed);
            Assert.IsTrue(s.RuleBroken);
            Assert.AreEqual(0, s.Stars);
        }

        [Test]
        public void ConElJefeEsperando_HayTiempoExtra()
        {
            var s = Boss(out var lvl);
            Run(s, lvl.Duration - 1f);
            if (s.BossWaiting)
            {
                Run(s, 3f);
                Assert.IsFalse(s.IsFinished, "no se acaba mientras el jefe espera");
            }
        }

        [Test]
        public void SiLeDasAlgoMal_NoSeVa()
        {
            var s = Boss(out var lvl);
            UntilBoss(s);
            int slot = s.BossSlot;
            var p = s.Plates[s.SelectedPlate];
            p.Base = Base.Tortilla; p.Meat = s.Customers[slot].Order.Meat == Meat.Pastor ? Meat.Asada : Meat.Pastor;
            Assert.AreEqual(CommandResult.WrongOrder, s.Apply(Command.Deliver(slot)));
            Assert.IsTrue(s.Customers[slot] != null);
        }
    }
}
