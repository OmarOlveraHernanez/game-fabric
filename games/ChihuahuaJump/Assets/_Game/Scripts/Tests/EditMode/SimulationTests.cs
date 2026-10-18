using System.Linq;
using ChihuahuaJump.Logic;
using NUnit.Framework;

namespace ChihuahuaJump.Tests
{
    public class SimulationTests
    {
        const float Dt = 1f / 60f;

        [Test]
        public void StartsReady_TapStarts()
        {
            var sim = new BounceSimulation(new GameConfig(), 1);
            Assert.AreEqual(GameState.Ready, sim.State);
            sim.Tick(Dt, 0, false);
            Assert.AreEqual(GameState.Ready, sim.State);
            sim.Tick(Dt, 0, true);
            Assert.AreEqual(GameState.Playing, sim.State);
        }

        [Test]
        public void WithoutEnemies_StandingStill_KeepsBouncingForever()
        {
            var c = new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 9999f };
            var sim = new BounceSimulation(c, 2);
            sim.Tick(Dt, 0, true);
            for (int i = 0; i < 60 * 120; i++) sim.Tick(Dt, 0, false);
            Assert.AreEqual(GameState.Playing, sim.State);
            Assert.That(sim.Bounces, Is.GreaterThan(50));
        }

        [Test]
        public void MissingTheDog_EndsTheGame_AsFall()
        {
            var c = new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 9999f };
            var sim = new BounceSimulation(c, 3);
            sim.Tick(Dt, 0, true);
            for (int i = 0; i < 60 * 5 && sim.State == GameState.Playing; i++) sim.Tick(Dt, 1, false); // se va a la derecha
            Assert.AreEqual(GameState.Dead, sim.State);
            Assert.AreEqual(DeathCause.Fell, sim.LastDeath);
        }

        [Test]
        public void Restart_RequiresDelay_ThenResets()
        {
            var c = new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 9999f };
            var sim = new BounceSimulation(c, 4);
            sim.Tick(Dt, 0, true);
            while (sim.State == GameState.Playing) sim.Tick(Dt, 1, false);
            sim.Tick(Dt, 0, true);
            Assert.AreEqual(GameState.Dead, sim.State, "Reinicio demasiado pronto");
            for (float t = 0; t < c.RestartDelay + 0.1f; t += Dt) sim.Tick(Dt, 0, false);
            sim.Tick(Dt, 0, true);
            Assert.AreEqual(GameState.Playing, sim.State);
            Assert.AreEqual(0, sim.Score);
            Assert.AreEqual(0, sim.Hazards.Count);
        }

        [Test]
        public void EnemyKinds_UnlockWithLevel()
        {
            var gen = new SpawnGenerator(new GameConfig(), 5);
            Assert.IsTrue(Enumerable.Range(0, 300).All(_ => gen.PickKind(0f) == HazardKind.Serpent));
            Assert.AreEqual(3, Enumerable.Range(0, 300).Select(_ => gen.PickKind(1f)).Distinct().Count());
        }

        [Test]
        public void Shield_AbsorbsOneHit()
        {
            var c = new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 9999f };
            var sim = new BounceSimulation(c, 6);
            sim.Tick(Dt, 0, true);
            // Darle escudo con un pollito puesto sobre el perro
            var field = typeof(BounceSimulation).GetField("_pickups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pickups = (System.Collections.Generic.List<Pickup>)field.GetValue(sim);
            pickups.Add(new Pickup { Kind = PickupKind.Pollito, X = sim.Dog.X, Y = sim.Dog.Y, W = 1, H = 1, Life = 10f });
            sim.Tick(Dt, 0, false);
            Assert.IsTrue(sim.HasShield);

            var hf = typeof(BounceSimulation).GetField("_hazards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hazards = (System.Collections.Generic.List<Hazard>)hf.GetValue(sim);
            hazards.Add(new Hazard { Kind = HazardKind.Serpent, X = sim.Dog.X, Y = sim.Dog.Y, W = 1, H = 1 });
            sim.Tick(Dt, 0, false);
            Assert.AreEqual(GameState.Playing, sim.State);
            Assert.IsFalse(sim.HasShield);
            Assert.IsTrue(sim.IsInvulnerable);
        }

        [Test]
        public void Trampoline_FollowsFinger_WithoutOvershooting()
        {
            var c = new GameConfig();
            var t = new Trampoline { Width = c.TrampolineWidth };
            for (int i = 0; i < 120; i++) t.StepTowards(Dt, 1.37f, c);
            Assert.AreEqual(1.37f, t.X, 0.0001f);
            for (int i = 0; i < 2; i++) t.StepTowards(Dt, -3f, c);
            Assert.AreEqual(1.37f - 2 * c.TrampolineSpeed * Dt, t.X, 0.001f);
        }

        [Test]
        public void FloatingPickups_AreAlwaysWithinReach_AndExpire()
        {
            var c = new GameConfig();
            var gen = new SpawnGenerator(c, 12);
            var list = new System.Collections.Generic.List<Pickup>();
            for (int i = 0; i < 300; i++) gen.CreatePickups(list, true);
            foreach (var p in list)
            {
                Assert.That(p.Top, Is.LessThan(c.DogPeakY), "El perro debe poder alcanzarlo");
                Assert.That(p.Bottom, Is.GreaterThan(c.TrampolineY + 1f));
                Assert.That(System.Math.Abs(p.X) + p.W * 0.5f, Is.LessThan(c.HalfWidth + 0.001f));
            }
            var sim = new BounceSimulation(new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 0.1f }, 13);
            sim.Tick(Dt, 0, true);
            for (int i = 0; i < 60 * 20; i++) sim.Tick(Dt, 0, false, 3.4f); // trampolin lejos del perro? no: el perro cae
            Assert.That(sim.Pickups.Count, Is.LessThan(c.PickupLifetime / 0.1f * c.CacaoGroupMax + 1));
        }

        [Test]
        public void Cacao_IsCollectedByTheDog()
        {
            var c = new GameConfig { FirstSpawnDelay = 9999f, PickupInterval = 9999f };
            var sim = new BounceSimulation(c, 7);
            sim.Tick(Dt, 0, true);
            var field = typeof(BounceSimulation).GetField("_pickups", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pickups = (System.Collections.Generic.List<Pickup>)field.GetValue(sim);
            pickups.Add(new Pickup { Kind = PickupKind.Cacao, X = 0.5f, Y = 6f, W = c.CacaoSize, H = c.CacaoSize, Life = 10f }); // un poco de lado: el area generosa lo atrapa
            for (int i = 0; i < 120; i++) sim.Tick(Dt, 0, false);
            Assert.AreEqual(1, sim.Cacao);
        }
    }
}
