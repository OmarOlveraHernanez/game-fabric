using RescatePapi.Logic;
using NUnit.Framework;

namespace RescatePapi.Tests
{
    public class JumpMotorTests
    {
        const float Dt = 1f / 60f;

        static float SimulatePeak(RunnerConfig c, int holdFrames, out int framesToLand)
        {
            var m = new JumpMotor(c);
            float peak = 0f;
            framesToLand = 0;
            for (int f = 0; f < 600; f++)
            {
                bool pressed = f == 0;
                bool held = f < holdFrames;
                m.Tick(Dt, pressed, held);
                if (m.Y > peak) peak = m.Y;
                if (f > 0 && m.IsGrounded) { framesToLand = f; break; }
            }
            return peak;
        }

        [Test]
        public void FullJump_ReachesExpectedHeight_AndLands()
        {
            var c = new RunnerConfig();
            float expected = c.JumpVelocity * c.JumpVelocity / (2f * c.Gravity);
            float peak = SimulatePeak(c, holdFrames: 999, out int land);
            Assert.That(peak, Is.EqualTo(expected).Within(0.2f));
            Assert.That(land, Is.GreaterThan(0), "Debe aterrizar");
        }

        [Test]
        public void ShortTap_JumpsLowerThanHold_ButClearsTallestSerpent()
        {
            var c = new RunnerConfig();
            float tap = SimulatePeak(c, holdFrames: 1, out _);
            float hold = SimulatePeak(c, holdFrames: 999, out _);
            Assert.That(tap, Is.LessThan(hold * 0.8f), "El tap debe ser claramente mas bajo");
            Assert.That(tap - c.HitboxInset, Is.GreaterThan(c.SerpentMaxHeight), "Hasta el tap mas corto debe librar la serpiente mas alta");
        }

        [Test]
        public void PressJustBeforeLanding_IsBuffered()
        {
            var c = new RunnerConfig();
            var m = new JumpMotor(c);
            m.Tick(Dt, true, false);
            // Avanza hasta estar a punto de aterrizar
            int guard = 0;
            while (!(m.VelocityY < 0f && m.Y < 0.3f) && guard++ < 600) m.Tick(Dt, false, false);
            m.Tick(Dt, true, true); // tap en el aire, antes de tocar suelo
            bool jumpedAgain = false;
            for (int i = 0; i < 20; i++)
            {
                m.Tick(Dt, false, true);
                if (m.VelocityY > 0f) { jumpedAgain = true; break; }
            }
            Assert.IsTrue(jumpedAgain, "El tap previo al aterrizaje debe convertirse en salto");
        }

        [Test]
        public void CannotDoubleJump()
        {
            var c = new RunnerConfig();
            var m = new JumpMotor(c);
            m.Tick(Dt, true, true);
            for (int i = 0; i < 5; i++) m.Tick(Dt, false, true);
            float vBefore = m.VelocityY;
            m.Tick(Dt, true, true);
            Assert.That(m.VelocityY, Is.LessThan(vBefore), "En el aire un nuevo tap no debe impulsar");
        }
    }
}
