using System.Linq;
using RescatePapi.Logic;
using NUnit.Framework;

namespace RescatePapi.Tests
{
    public class RunnerSimulationTests
    {
        const float Dt = 1f / 60f;

        static RunnerSimulation Running(int seed, RunnerConfig c = null)
        {
            var sim = new RunnerSimulation(c ?? new RunnerConfig(), seed);
            sim.Tick(Dt, true, false);
            return sim;
        }

        [Test]
        public void StartsReady_AndTapStartsRun()
        {
            var sim = new RunnerSimulation(new RunnerConfig(), 1);
            Assert.AreEqual(RunState.Ready, sim.State);
            sim.Tick(Dt, false, false);
            Assert.AreEqual(RunState.Ready, sim.State);
            sim.Tick(Dt, true, false);
            Assert.AreEqual(RunState.Running, sim.State);
        }

        [Test]
        public void WithoutJumping_PlayerDies_AndBestScoreIsKept()
        {
            var sim = Running(2);
            bool died = false;
            sim.Died += () => died = true;
            for (int i = 0; i < 60 * 60 && sim.State == RunState.Running; i++) sim.Tick(Dt, false, false);
            Assert.IsTrue(died);
            Assert.That(sim.Score, Is.GreaterThan(0));
            Assert.AreEqual(sim.Score, sim.BestScore);
        }

        [Test]
        public void Restart_RequiresDelay_ThenResetsRun()
        {
            var c = new RunnerConfig();
            var sim = Running(3, c);
            while (sim.State == RunState.Running) sim.Tick(Dt, false, false);
            int best = sim.BestScore;

            sim.Tick(Dt, true, false); // demasiado pronto
            Assert.AreEqual(RunState.Dead, sim.State);

            for (float t = 0; t < c.RestartDelay + 0.1f; t += Dt) sim.Tick(Dt, false, false);
            sim.Tick(Dt, true, false);
            Assert.AreEqual(RunState.Running, sim.State);
            Assert.AreEqual(0, sim.Score);
            Assert.AreEqual(0, sim.Cacao);
            Assert.AreEqual(best, sim.BestScore);
            Assert.AreEqual(0, sim.Hazards.Count);
        }

        [Test]
        public void Speed_IncreasesOverTime_AndIsCapped()
        {
            var c = new RunnerConfig();
            Assert.AreEqual(c.BaseSpeed, Difficulty.SpeedAt(c, 0f), 0.001f);
            Assert.That(Difficulty.SpeedAt(c, 30f), Is.GreaterThan(c.BaseSpeed));
            Assert.AreEqual(c.MaxSpeed, Difficulty.SpeedAt(c, 10000f), 0.001f);
        }

        [Test]
        public void Eagle_DoesNotAppear_AtTheVeryStart()
        {
            var c = new RunnerConfig();
            var gen = new SpawnGenerator(c, 5);
            for (int i = 0; i < 500; i++)
                Assert.AreNotEqual(HazardKind.Eagle, gen.PickKind(0f));
        }

        [Test]
        public void AllEnemyKinds_Appear_AtHighLevel()
        {
            var gen = new SpawnGenerator(new RunnerConfig(), 6);
            var kinds = Enumerable.Range(0, 300).Select(_ => gen.PickKind(1f)).Distinct().ToList();
            Assert.AreEqual(3, kinds.Count);
        }

        [Test]
        public void Shield_AbsorbsOneHit_ThenPlayerCanDie()
        {
            // Solo aguilas (pasan por encima del jugador de pie) y pollitos a ras de suelo
            var c = OnlyEagles();
            c.CacaoChancePerGap = 0f; c.PollitoChancePerGap = 1f; c.PollitoY = 0.45f;
            var sim = Running(7, c);
            bool lost = false;
            sim.ShieldLost += () => lost = true;

            for (int i = 0; i < 60 * 60 && !sim.HasShield && sim.State == RunState.Running; i++) sim.Tick(Dt, false, false);
            Assert.IsTrue(sim.HasShield, "Debio recoger un pollito a ras de suelo");

            c.SerpentWeight = 1f; c.EagleWeight = 0f; c.PollitoChancePerGap = 0f; // ahora enemigos de suelo
            for (int i = 0; i < 60 * 60 && sim.State == RunState.Running && !lost; i++) sim.Tick(Dt, false, false);
            Assert.IsTrue(lost, "El escudo debio absorber un golpe");
            Assert.AreEqual(RunState.Running, sim.State);
            Assert.IsTrue(sim.IsInvulnerable);

            for (int i = 0; i < 60 * 60 && sim.State == RunState.Running; i++) sim.Tick(Dt, false, false);
            Assert.AreEqual(RunState.Dead, sim.State);
        }

        [Test]
        public void GroundCacao_IsCollected_WhileRunning()
        {
            var c = OnlyEagles();
            c.CacaoChancePerGap = 1f; c.PollitoChancePerGap = 0f;
            var sim = Running(8, c);
            for (int i = 0; i < 60 * 15; i++) sim.Tick(Dt, false, false);
            Assert.AreEqual(RunState.Running, sim.State, "De pie, las aguilas pasan por encima");
            Assert.That(sim.Cacao, Is.GreaterThan(5));
        }

        static RunnerConfig OnlyEagles() =>
            new RunnerConfig { SerpentWeight = 0f, JaguarWeight = 0f, EagleWeight = 1f, EagleFromLevel = 0f };

        [Test]
        public void SetBestScore_OnlyRaises()
        {
            var sim = new RunnerSimulation(new RunnerConfig(), 4);
            sim.SetBestScore(100);
            sim.SetBestScore(50);
            Assert.AreEqual(100, sim.BestScore);
        }
    }
}
