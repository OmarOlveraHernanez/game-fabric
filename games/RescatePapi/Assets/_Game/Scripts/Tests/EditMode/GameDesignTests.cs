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

        [Test]
        public void HoppingSerpent_PassesOverStandingPlayer_WithMargin()
        {
            // Mientras la serpiente cruza al jugador, su base debe ir por encima de la cabeza (de pie)
            var c = new RunnerConfig();
            float halfOverlap = (c.SerpentMaxWidth + c.PlayerWidth - 2 * c.HitboxInset) * 0.5f / c.BaseSpeed;
            float u = 0.5f - halfOverlap / c.SerpentHopDuration;          // el peor momento: al entrar/salir
            float bottom = 4f * c.SerpentHopHeight * u * (1f - u);
            float head = c.PlayerHeight - c.HitboxInset;
            Assert.That(bottom, Is.GreaterThan(head + 0.15f), $"base de la serpiente {bottom:0.00} vs cabeza {head:0.00}");
        }

        [Test]
        public void HoppingSerpents_StandingPlayerSurvives_JumpingPlayerDies()
        {
            var c = new RunnerConfig { JaguarWeight = 0f, EagleWeight = 0f, SerpentHopFromLevel = 0f, SerpentHopChance = 1f,
                                       PollitoChancePerGap = 0f };
            var sim = new RunnerSimulation(c, 31);
            sim.Begin();
            for (int f = 0; f < 60 * 30 && sim.State == RunState.Running; f++) sim.Tick(Dt, false, false);
            Assert.AreEqual(RunState.Running, sim.State, "De pie, las serpientes saltarinas pasan por encima");

            // El reflejo de siempre (tap como a una serpiente normal) choca: el truco es no saltar
            var sim2 = new RunnerSimulation(c, 31);
            sim2.Begin();
            float tapPeak = TapPeakTime(c);
            for (int f = 0; f < 60 * 30 && sim2.State == RunState.Running; f++)
            {
                bool press = sim2.Jump.IsGrounded && sim2.Hazards.Any(h =>
                    h.X > c.PlayerX && (h.X - c.PlayerX) / sim2.Speed <= tapPeak);
                sim2.Tick(Dt, press, false);
            }
            Assert.AreEqual(RunState.Dead, sim2.State);
        }

        [Test]
        public void DivingEagles_MustBeJumped_AndAreClearable()
        {
            var c = new RunnerConfig { SerpentWeight = 0f, JaguarWeight = 0f, EagleFromLevel = 0f,
                                       EagleDiveFromLevel = 0f, EagleDiveChance = 1f, PollitoChancePerGap = 0f };
            var still = new RunnerSimulation(c, 41);
            still.Begin();
            for (int f = 0; f < 60 * 30 && still.State == RunState.Running; f++) still.Tick(Dt, false, false);
            Assert.AreEqual(RunState.Dead, still.State, "Quedarse de pie ante un aguila en picada debe chocar");

            var bot = new RunnerSimulation(c, 41);
            bot.Begin();
            RunBot(bot, 60 * 60);
            Assert.AreEqual(RunState.Running, bot.State, $"El bot murio contra {LastHit(bot)}");
        }

        [Test]
        public void DivingEagle_GivesTimeToReact()
        {
            // Desde que se lanza hasta que hay que saltar debe quedar tiempo de reaccion humano (~0.35 s)
            var c = new RunnerConfig();
            float timeToPlayer = (c.EagleDiveDistance + c.EagleWidth * 0.5f) / c.BaseSpeed;
            float jumpLead = c.JumpVelocity / c.Gravity;                   // hay que despegar antes del pico
            Assert.That(timeToPlayer - jumpLead, Is.GreaterThan(0.45f), $"tiempo para decidir {timeToPlayer - jumpLead:0.00}s");
        }

        /// <summary>
        /// Un bot con buena tecnica sobrevive 2 minutos (toda la curva de dificultad, aguilas, embestidas,
        /// serpientes saltarinas y aguilas en picada): salto alto para jaguares y aguilas en picada,
        /// tap para serpientes, no salta bajo aguilas normales ni serpientes saltarinas.
        /// </summary>
        [TestCase(11)]
        [TestCase(222)]
        [TestCase(3333)]
        [TestCase(44444)]
        public void Bot_WithGoodTechnique_Survives_TwoMinutes(int seed)
        {
            var c = new RunnerConfig { PollitoChancePerGap = 0f }; // sin escudos: debe sobrevivir por tecnica
            var sim = new RunnerSimulation(c, seed);
            sim.Begin();
            RunBot(sim, 60 * 150);                                 // 2.5 min: pasa toda la rampa de dificultad
            Assert.AreEqual(RunState.Running, sim.State,
                $"El bot murio a los {sim.Elapsed:0.0}s (puntos {sim.Score}) contra {LastHit(sim)}");
        }

        /// <summary>
        /// Juega como un humano atento: solo usa lo que se ve en pantalla (la serpiente saltarina se
        /// distingue porque rebota nerviosa; el aguila en picada solo cuando ya se lanzo).
        /// </summary>
        static void RunBot(RunnerSimulation sim, int frames)
        {
            var c = sim.Config;
            float playerLeft = c.PlayerX - c.PlayerWidth * 0.5f;
            bool holding = false;
            for (int f = 0; f < frames && sim.State == RunState.Running; f++)
            {
                var next = sim.Hazards.Where(h => h.Right > playerLeft && NeedsJump(h))
                                      .OrderBy(h => h.X).FirstOrDefault();
                bool press = false;
                if (next != null && sim.Jump.IsGrounded && !DangerAbove(sim))
                {
                    float v = sim.Speed + (next.IsCharging || next.WillCharge ? c.JaguarChargeSpeed : 0f);
                    float timeToCenter = (next.X - c.PlayerX) / v;
                    bool full = next.Kind != HazardKind.Serpent;
                    float peakTime = full ? c.JumpVelocity / c.Gravity : TapPeakTime(c);
                    if (timeToCenter <= peakTime) { press = true; holding = full; }
                }
                sim.Tick(Dt, press, holding);
                if (sim.Jump.IsGrounded && !press) holding = false;
            }
        }

        /// <summary>Lo que hay que saltar: serpiente normal, jaguar y aguila ya en picada.</summary>
        static bool NeedsJump(Hazard h) => h.Kind switch
        {
            HazardKind.Serpent => !h.WillHop,
            HazardKind.Jaguar => true,
            _ => h.IsDiving
        };

        /// <summary>No despegar si un aguila normal o una serpiente saltarina esta por cruzar encima.</summary>
        static bool DangerAbove(RunnerSimulation sim)
        {
            var c = sim.Config;
            foreach (var h in sim.Hazards)
            {
                bool overhead = (h.Kind == HazardKind.Eagle && !h.IsDiving) || h.WillHop;
                if (!overhead) continue;
                float t = (h.Left - (c.PlayerX + c.PlayerWidth * 0.5f)) / sim.Speed;
                if (h.Right > c.PlayerX - c.PlayerWidth * 0.5f && t < 0.75f) return true;
            }
            return false;
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
            return h == null ? "?" : $"{h.Kind} (h={h.Height:0.00}, w={h.Width:0.00}, base={h.Bottom:0.00}, embiste={h.IsCharging}, salta={h.WillHop}, picada={h.IsDiving}) jugadorY={sim.Jump.Y:0.00} nivel={sim.Level:0.00}";
        }
    }
}
