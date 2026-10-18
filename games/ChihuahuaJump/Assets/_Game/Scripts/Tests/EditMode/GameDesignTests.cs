using ChihuahuaJump.Logic;
using NUnit.Framework;

namespace ChihuahuaJump.Tests
{
    /// <summary>Garantizan que el juego siga siendo justo al ajustar GameConfig.</summary>
    public class GameDesignTests
    {
        const float Dt = 1f / 60f;

        static float AirTime(GameConfig c) => 2f * c.BounceVelocity / c.Gravity;

        [Test]
        public void Trampoline_CanCrossTheField_InHalfAnArc()
        {
            var c = new GameConfig();
            float cross = 2f * (c.HalfWidth - c.TrampolineWidth * 0.5f) / c.TrampolineSpeed;
            Assert.That(cross, Is.LessThan(AirTime(c) * 0.5f), $"Cruce {cross:0.00}s vs arco {AirTime(c):0.00}s");
        }

        [Test]
        public void DogAgainstAWall_CanAlwaysBeSentBackInside()
        {
            var c = new GameConfig();
            var t = new Trampoline { Width = c.TrampolineWidth };
            float dogX = c.HalfWidth - c.DogSize * 0.5f;            // pegado a la pared derecha
            float offset = (dogX - t.MaxCenter(c)) / (c.TrampolineWidth * 0.5f);
            Assert.That(offset, Is.LessThan(-0.5f), "Debe poder rebotar hacia el centro con buen angulo");
            Assert.That(BounceSimulation.SideSpeedFor(offset, c), Is.LessThan(-1.5f));
        }

        [Test]
        public void EveryHazard_GivesAtLeastOneFullBounceOfWarning()
        {
            var c = new GameConfig();
            Assert.That(c.HazardLeadTime, Is.GreaterThan(AirTime(c) + 0.4f));
            var gen = new SpawnGenerator(c, 9);
            for (int i = 0; i < 500; i++)
            {
                var h = gen.CreateHazard(i % 2 == 0 ? 1f : 0.5f);
                float t = (h.Bottom - c.DogPeakY) / System.Math.Abs(h.Vy);
                Assert.That(t, Is.GreaterThan(AirTime(c)), $"{h.Kind} llega en {t:0.00}s");
            }
        }

        [Test]
        public void StraightBounce_IsEasyToHold()
        {
            // La zona recta debe ser mas ancha que el paso del trampolin en un frame (precision humana)
            var c = new GameConfig();
            float straightZone = 2f * c.CenterDeadZone * c.TrampolineWidth * 0.5f;
            Assert.That(straightZone, Is.GreaterThan(3f * c.TrampolineSpeed / 60f));
        }

        [Test]
        public void Peak_StaysOnScreen_BelowSpawnLine()
        {
            var c = new GameConfig();
            Assert.That(c.DogPeakY, Is.LessThan(c.VisibleTopY - 3f));
        }

        /// <summary>
        /// Un jugador habil (bot) en 40 partidas: casi siempre supera el inicio (45 s) y la mitad llega lejos.
        /// La dificultad sube hasta que pierdes: el objetivo no es sobrevivir siempre, sino que perder sea culpa tuya.
        /// </summary>
        [Test]
        public void SkilledBot_SurvivesTheStart_AndGoesFar()
        {
            const int runs = 40;
            var times = new System.Collections.Generic.List<float>();
            for (int s = 1; s <= runs; s++)
            {
                var c = new GameConfig { PollitoChance = 0f };
                var sim = new BounceSimulation(c, s * 7919);
                var bot = new Bot();
                sim.Tick(Dt, 0, true);
                for (int i = 0; i < 60 * 100 && sim.State == GameState.Playing; i++)
                    sim.Tick(Dt, bot.Decide(sim), false);
                times.Add(sim.Elapsed);
            }
            times.Sort();
            int passStart = times.FindAll(t => t >= 45f).Count;
            float median = times[runs / 2];
            Assert.That(passStart, Is.GreaterThan(runs * 0.85f - 0.01f), $"Solo {passStart}/{runs} superan 45 s");
            Assert.That(median, Is.GreaterThan(90f - 0.01f), $"Mediana {median:0} s");
        }
    }
}
