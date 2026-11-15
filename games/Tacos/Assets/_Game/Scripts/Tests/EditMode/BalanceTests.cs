using System.Collections.Generic;
using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    /// <summary>
    /// Un bot que sigue las pistas con un tiempo de reaccion por toque. Sirve para dos cosas:
    /// comprobar que las pistas llevan a entregar tacos y que cada mision se puede ganar.
    /// </summary>
    public static class HintBot
    {
        public static KitchenSimulation Play(int turno, TaqueroDef taq, int seed, float reaction) =>
            PlayLevel(LevelConfig.Turnos[turno], taq, seed, reaction);

        public static KitchenSimulation PlayLevel(LevelConfig level, TaqueroDef taq, int seed, float reaction, KitchenRules rules = null)
        {
            var s = new KitchenSimulation(level, taq, seed, rules);
            float cooldown = 0f;
            const float dt = 0.05f;
            while (!s.IsFinished)
            {
                s.Tick(dt);
                cooldown -= dt;
                if (cooldown > 0f || s.IsFinished) continue;
                var h = Hints.Next(s);
                if (h.Command.HasValue)
                {
                    s.Apply(h.Command.Value);
                    cooldown = reaction;
                }
            }
            return s;
        }

        public static int MedianStars(int turno, TaqueroDef taq, float reaction, int runs = 21) =>
            MedianStars(LevelConfig.Turnos[turno], taq, reaction, runs);

        public static int MedianStars(LevelConfig level, TaqueroDef taq, float reaction, int runs = 21)
        {
            var stars = new List<int>();
            for (int seed = 1; seed <= runs; seed++) stars.Add(PlayLevel(level, taq, seed, reaction).Stars);
            stars.Sort();
            return stars[runs / 2];
        }
    }

    public class BalanceTests
    {
        [Test]
        public void Pistas_PrimeroPonerLaCarne()
        {
            var s = new KitchenSimulation(LevelConfig.Turnos[0], TaqueroDef.Guero, 5);
            Assert.AreEqual(HintKind.WaitCustomer.ToString(), Hints.Next(s).Kind.ToString());
            for (int i = 0; i < 100 && s.Customers[0] == null; i++) s.Tick(0.05f);
            var h = Hints.Next(s);
            Assert.AreEqual(HintKind.StartMeat.ToString(), h.Kind.ToString());
            Assert.AreEqual(s.Customers[0].Order.Meat.ToString(), h.Meat.ToString());
        }

        [Test]
        public void Pistas_LlevanAEntregarElPrimerTaco()
        {
            var s = new KitchenSimulation(LevelConfig.Turnos[0], TaqueroDef.Tio, 9);
            for (int i = 0; i < 600 && s.Served == 0; i++)
            {
                s.Tick(0.05f);
                var h = Hints.Next(s);
                if (h.Command.HasValue) s.Apply(h.Command.Value);
            }
            Assert.AreEqual(1, s.Served);
            Assert.AreEqual(0, s.Failed);
        }

        [Test]
        public void Pistas_TambienFuncionanConComal()
        {
            var s = HintBot.Play(2, TaqueroDef.Paisa, 4, 0.5f);
            Assert.That(s.Served, Is.GreaterThan(3));
        }

        // Jugador lento (1 s por toque): la primera mision sigue siendo ganable con al menos 1 estrella.
        [TestCase("guero")]
        [TestCase("tio")]
        [TestCase("paisa")]
        public void Mision1_JugadorLentoGanaAlMenosUnaEstrella(string id)
        {
            var taq = System.Array.Find(TaqueroDef.Starters, t => t.Id == id);
            for (int seed = 1; seed <= 10; seed++)
                Assert.That(HintBot.Play(0, taq, seed, 1.0f).Stars, Is.GreaterThan(0), "semilla " + seed);
        }

        // Jugador normal (0.6 s por toque): cada mision da al menos 1 estrella en la mediana, con cualquier taquero.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void CadaMision_EsGanable(int mision)
        {
            var lvl = LevelConfig.Zona1[mision];
            foreach (var taq in TaqueroDef.Starters)
                Assert.That(HintBot.MedianStars(lvl, taq, 0.6f), Is.GreaterThan(0), taq.Name + " en " + lvl.Name);
        }

        // Las 3 estrellas piden jugar rapido: un jugador lento no las saca, uno rapido si.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        public void TresEstrellas_NoSonRegaladasPeroSeAlcanzan(int mision)
        {
            var lvl = LevelConfig.Zona1[mision];
            Assert.That(HintBot.MedianStars(lvl, TaqueroDef.Tio, 1.5f), Is.LessThan(3), lvl.Name + " lento");
            Assert.AreEqual(3, HintBot.MedianStars(lvl, TaqueroDef.Tio, 0.5f), lvl.Name + " rapido");
        }

        // Las mejoras ayudan: con todas compradas, el mismo jugador gana igual o mas monedas.
        [Test]
        public void Mejoras_AyudanEnElJefeDeZona()
        {
            var p = new CampaignProgress();
            p.Normalize(LevelConfig.Zona1.Length, UpgradeDef.All.Length);
            foreach (var u in UpgradeDef.All) p.owned[(int)u.Id] = true;
            var lvl = LevelConfig.Zona1[11];
            int sin = 0, con = 0;
            for (int seed = 1; seed <= 11; seed++)
            {
                sin += HintBot.PlayLevel(lvl, TaqueroDef.Tio, seed, 1.0f).Coins;
                con += HintBot.PlayLevel(lvl, TaqueroDef.Tio, seed, 1.0f, p.BuildRules()).Coins;
            }
            Assert.That(con, Is.GreaterThan(sin));
        }

        // Economia: jugando la campaña a ritmo normal se puede comprar la primera mejora antes de la mision 5.
        [Test]
        public void Economia_PrimeraMejoraAntesDeLaMision5()
        {
            int wallet = 0;
            for (int m = 0; m < 4; m++) wallet += HintBot.PlayLevel(LevelConfig.Zona1[m], TaqueroDef.Tio, 3, 1.0f).Coins;
            Assert.That(wallet, Is.GreaterThan(UpgradeDef.Get(Upgrade.Paciencia).Price));
        }
    }
}
