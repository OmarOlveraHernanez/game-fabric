using System.Linq;
using RescatePapi.Logic;
using NUnit.Framework;

namespace RescatePapi.Tests
{
    /// <summary>
    /// Pruebas de diseno: garantizan que, al ajustar RunnerConfig, el juego siga siendo
    /// justo (siempre se puede sobrevivir con buena tecnica).
    /// </summary>
    public class GameDesignTests
    {
        const float Dt = 1f / 60f;

        static float FullJumpAirTime(RunnerConfig c)
        {
            var m = new JumpMotor(c);
            m.Tick(Dt, true, true);
            int frames = 1;
            while (!m.IsGrounded && frames < 1000) { m.Tick(Dt, false, true); frames++; }
            return frames * Dt;
        }

        static float TimeAbove(RunnerConfig c, float height, int holdFrames)
        {
            var m = new JumpMotor(c);
            float above = 0f;
            m.Tick(Dt, true, holdFrames > 0);
            for (int f = 1; !m.IsGrounded && f < 1000; f++)
            {
                if (m.Y > height) above += Dt;
                m.Tick(Dt, false, f < holdFrames);
            }
            return above;
        }

        [Test]
        public void MinimumGap_LeavesRoomToLandAndReact()
        {
            var c = new RunnerConfig();
            float air = FullJumpAirTime(c);
            Assert.That(c.GapTimeMinHard, Is.GreaterThan(air * 1.25f),
                $"Hueco minimo {c.GapTimeMinHard}s vs tiempo en el aire {air:0.00}s");
        }

        [Test]
        public void TallestJaguar_IsClearable_AtMaxSpeed_EvenCharging()
        {
            var c = new RunnerConfig();
            float above = TimeAbove(c, c.JaguarMaxHeightHard - c.HitboxInset, 999);
            float overlap = (c.JaguarMaxWidth + c.PlayerWidth - 2 * c.HitboxInset) / (c.MaxSpeed + c.JaguarChargeSpeed);
            Assert.That(above, Is.GreaterThan(overlap * 1.5f), $"Sobre el jaguar {above:0.000}s vs necesario {overlap:0.000}s");
        }

        [Test]
        public void ShortTap_ClearsLongestSerpent_EvenAtBaseSpeed()
        {
            // Peor caso para el tap: velocidad baja = mas tiempo encima de la serpiente
            var c = new RunnerConfig();
            float above = TimeAbove(c, c.SerpentMaxHeight - c.HitboxInset, 1);
            float overlap = (c.SerpentMaxWidth + c.PlayerWidth - 2 * c.HitboxInset) / c.BaseSpeed;
            Assert.That(above, Is.GreaterThan(overlap * 1.1f), $"Tap sobre serpiente {above:0.000}s vs necesario {overlap:0.000}s");
        }

        [Test]
        public void StandingPlayer_PassesUnderEagle()
        {
            var c = new RunnerConfig();
            Assert.That(c.EagleMinBottom, Is.GreaterThan(c.PlayerHeight - c.HitboxInset + 0.1f));
        }

        [Test]
        public void AnyJump_HitsEagle()
        {
            var c = new RunnerConfig();
            // Incluso el tap mas corto sube la cabeza por encima de la base del aguila mas alta
            var m = new JumpMotor(c);
            float peak = 0f;
            m.Tick(Dt, true, false);
            while (!m.IsGrounded) { peak = System.Math.Max(peak, m.Y); m.Tick(Dt, false, false); }
            Assert.That(peak + c.PlayerHeight - c.HitboxInset, Is.GreaterThan(c.EagleMaxBottom + 0.2f));
        }

        /// <summary>
        /// Un bot con buena tecnica sobrevive 2 minutos (incluye velocidad maxima, aguilas y embestidas):
        /// salto alto para jaguares, tap para serpientes, nunca salta bajo un aguila.
        /// </summary>
        [TestCase(11)]
        [TestCase(222)]
        [TestCase(3333)]
        [TestCase(44444)]
        public void Bot_WithGoodTechnique_Survives_TwoMinutes(int seed)
        {
            var c = new RunnerConfig { PollitoChancePerGap = 0f }; // sin escudos: debe sobrevivir por tecnica
            var sim = new RunnerSimulation(c, seed);
            sim.Tick(Dt, true, false);
            float playerLeft = c.PlayerX - c.PlayerWidth * 0.5f;
            bool holding = false;

            for (int f = 0; f < 60 * 120 && sim.State == RunState.Running; f++)
            {
                var next = sim.Hazards.Where(h => !h.IsAirborne && h.Right > playerLeft)
                                      .OrderBy(h => h.X).FirstOrDefault();
                bool press = false;
                if (next != null && sim.Jump.IsGrounded)
                {
                    float v = sim.Speed + (next.IsCharging || next.WillCharge ? c.JaguarChargeSpeed : 0f);
                    float timeToCenter = (next.X - c.PlayerX) / v;
                    bool full = next.Kind == HazardKind.Jaguar;
                    float peakTime = full ? c.JumpVelocity / c.Gravity : TapPeakTime(c);
                    if (timeToCenter <= peakTime) { press = true; holding = full; }
                }
                sim.Tick(Dt, press, holding);
                if (sim.Jump.IsGrounded && !press) holding = false;
            }
            Assert.AreEqual(RunState.Running, sim.State,
                $"El bot murio a los {sim.Elapsed:0.0}s (puntos {sim.Score}) contra {LastHit(sim)}");
        }

        static float TapPeakTime(RunnerConfig c)
        {
            var m = new JumpMotor(c);
            m.Tick(Dt, true, false);
            float t = Dt, peak = m.Y, tPeak = t;
            while (!m.IsGrounded) { m.Tick(Dt, false, false); t += Dt; if (m.Y > peak) { peak = m.Y; tPeak = t; } }
            return tPeak;
        }

        static string LastHit(RunnerSimulation sim)
        {
            var c = sim.Config;
            var h = sim.Hazards.OrderBy(x => System.Math.Abs(x.X - c.PlayerX)).FirstOrDefault();
            return h == null ? "?" : $"{h.Kind} (h={h.Height:0.00}, w={h.Width:0.00}, base={h.Bottom:0.00}, embiste={h.IsCharging}) jugadorY={sim.Jump.Y:0.00}";
        }
    }
}
