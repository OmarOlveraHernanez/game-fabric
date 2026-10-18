using ChihuahuaJump.Logic;
using NUnit.Framework;

namespace ChihuahuaJump.Tests
{
    public class PhysicsTests
    {
        const float Dt = 1f / 60f;

        static BounceSimulation StartedNoSpawns(GameConfig c = null)
        {
            c ??= new GameConfig();
            c.FirstSpawnDelay = 9999f;
            c.PickupInterval = 9999f;
            var sim = new BounceSimulation(c, 1);
            sim.Tick(Dt, 0, true);
            return sim;
        }

        [Test]
        public void CenterHit_BouncesStraightUp_ToExpectedHeight()
        {
            var sim = StartedNoSpawns();
            var c = sim.Config;
            while (sim.Bounces == 0) sim.Tick(Dt, 0, false);
            Assert.AreEqual(0f, sim.Dog.Vx, 0.001f);
            float peak = 0f;
            for (int i = 0; i < 200; i++) { sim.Tick(Dt, 0, false); if (sim.Dog.Bottom > peak) peak = sim.Dog.Bottom; }
            float expected = c.TrampolineY + c.BounceVelocity * c.BounceVelocity / (2f * c.Gravity);
            Assert.That(peak, Is.EqualTo(expected).Within(0.3f));
        }

        [Test]
        public void EdgeHit_SendsDogSideways()
        {
            var sim = StartedNoSpawns();
            var c = sim.Config;
            // Mover el trampolin a la izquierda justo antes del primer rebote: el perro pega en la orilla derecha
            while (sim.Dog.Bottom > c.TrampolineY + 0.6f) sim.Tick(Dt, 0, false);
            while (sim.Bounces == 0) sim.Tick(Dt, -1, false);
            Assert.That(sim.Dog.Vx, Is.GreaterThan(0.5f), "Pegar en la orilla derecha debe mandarlo a la derecha");
        }

        [Test]
        public void NearCenterHit_StillBouncesStraight()
        {
            var c = new GameConfig();
            Assert.AreEqual(0f, BounceSimulation.SideSpeedFor(c.CenterDeadZone * 0.9f, c));
            Assert.AreEqual(0f, BounceSimulation.SideSpeedFor(-c.CenterDeadZone * 0.9f, c));
            Assert.AreEqual(c.MaxSideSpeed, BounceSimulation.SideSpeedFor(1f, c), 0.001f);
            Assert.That(BounceSimulation.SideSpeedFor(-0.6f, c), Is.LessThan(0f));
        }

        [Test]
        public void Dog_BouncesOffSideWalls()
        {
            var c = new GameConfig();
            var d = new Dog { W = c.DogSize, H = c.DogSize, X = c.HalfWidth - 0.4f, Y = 5f, Vx = 5f, Vy = 0f };
            for (int i = 0; i < 30; i++) d.Step(Dt, c);
            Assert.That(d.Vx, Is.LessThan(0f));
            Assert.That(d.Right, Is.LessThan(c.HalfWidth + 0.001f));
        }

        [Test]
        public void Trampoline_StaysInsideField()
        {
            var c = new GameConfig();
            var t = new Trampoline { Width = c.TrampolineWidth };
            for (int i = 0; i < 300; i++) t.Step(Dt, 1, c);
            Assert.That(t.Right, Is.EqualTo(c.HalfWidth + c.TrampolineOverhang).Within(0.001f));
        }
    }
}
