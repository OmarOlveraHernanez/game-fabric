using System.Collections.Generic;
using ChihuahuaJump.Logic;

namespace ChihuahuaJump.Tests
{
    /// <summary>
    /// Jugador automatico para pruebas de diseno: predice donde caera el chihuahua, coloca el
    /// trampolin y elige el angulo de rebote que evita a los enemigos en el siguiente arco.
    /// </summary>
    public sealed class Bot
    {
        const float Step = 1f / 60f;
        static readonly float[] Offsets = { 0f, -0.45f, 0.45f, -0.65f, 0.65f, -0.85f, 0.85f };

        public int LastBestHits { get; private set; }
        float _lockedTarget = float.NaN;
        int _lockedForBounce = -1;

        public int Decide(BounceSimulation sim)
        {
            var c = sim.Config;
            var d = sim.Dog;
            // 1) Donde y cuando aterriza el arco actual
            float x = d.X, y = d.Y, vx = d.Vx, vy = d.Vy, t = 0f;
            float half = d.W * 0.5f;
            while (t < 5f)
            {
                float prevBottom = y - half;
                vy -= c.Gravity * Step; x += vx * Step; y += vy * Step; t += Step;
                if (x - half < -c.HalfWidth) { x = -c.HalfWidth + half; vx = System.Math.Abs(vx); }
                if (x + half > c.HalfWidth) { x = c.HalfWidth - half; vx = -System.Math.Abs(vx); }
                if (vy < 0 && prevBottom >= c.TrampolineY && y - half <= c.TrampolineY) break;
            }
            float landX = x, landT = t;

            // A punto de aterrizar: mantener el plan (como haria una persona)
            if (landT < 0.25f && _lockedForBounce == sim.Bounces && !float.IsNaN(_lockedTarget))
                return MoveTowards(sim, _lockedTarget);

            // 2) Elegir el offset cuyo siguiente arco no choque con enemigos (prediccion lineal)
            float limit = sim.Trampoline.MaxCenter(c);
            float halfT = c.TrampolineWidth * 0.5f;
            float reach = c.TrampolineSpeed * System.Math.Max(0f, landT - 0.05f); // hasta donde alcanza a llegar
            float minX = System.Math.Max(-limit, sim.Trampoline.X - reach);
            float maxX = System.Math.Min(limit, sim.Trampoline.X + reach);
            var future = Advance(sim.Hazards, landT, c);
            float target = 0f; int bestHits = int.MaxValue;
            foreach (var off in Offsets)
            {
                // Cerca de las paredes el trampolin no puede colocarse donde quisieramos: usar el offset real
                // Posiciones que el trampolin alcanza exactamente (avanza TrampolineSpeed*Step por frame)
                float stepX = c.TrampolineSpeed * Step;
                float desired = System.Math.Max(minX, System.Math.Min(maxX, landX - off * halfT));
                float tgt = sim.Trampoline.X + (float)System.Math.Round((desired - sim.Trampoline.X) / stepX) * stepX;
                tgt = System.Math.Max(-limit, System.Math.Min(limit, tgt));
                float effective = System.Math.Max(-1f, System.Math.Min(1f, (landX - tgt) / halfT));
                if (System.Math.Abs(landX - tgt) > halfT + c.DogSize * 0.5f) continue; // no alcanza a atraparlo
                int hits = ArcHits(future, c, landX, effective, out float nextLand, out float arcT) * 10;
                if (hits == 0 && !HasSafeFollowUp(future, c, nextLand, arcT, limit)) hits = 1; // evitar callejones sin salida
                if (hits < bestHits) { bestHits = hits; target = tgt; if (hits == 0) break; }
            }
            LastBestHits = bestHits >= 10 ? 1 : 0;
            _lockedTarget = target;
            _lockedForBounce = sim.Bounces;
            return MoveTowards(sim, target);
        }

        static int MoveTowards(BounceSimulation sim, float target)
        {
            float diff = target - sim.Trampoline.X;
            if (System.Math.Abs(diff) < sim.Config.TrampolineSpeed * Step * 0.5f) return 0;
            return diff > 0 ? 1 : -1;
        }

        static List<Hazard> Advance(IReadOnlyList<Hazard> hazards, float time, GameConfig c)
        {
            var list = new List<Hazard>();
            foreach (var h in hazards)
            {
                var k = h.Clone();
                for (float t = 0f; t < time; t += Step) k.Step(Step, c);
                list.Add(k);
            }
            return list;
        }

        /// <summary>Simula un arco desde el trampolin; devuelve 1 si choca. landX/arcT: donde y cuando vuelve a caer.</summary>
        static int ArcHits(List<Hazard> hazardsAtLanding, GameConfig c, float startX, float offset, out float landX, out float arcT)
        {
            float half = c.DogSize * 0.5f;
            float x = startX, y = c.TrampolineY + half, vx = BounceSimulation.SideSpeedFor(offset, c), vy = c.BounceVelocity;
            var hz = new List<Hazard>();
            foreach (var h in hazardsAtLanding) hz.Add(h.Clone());
            float dt = Step * 2, t = 0f;
            int hit = 0;
            while (t < 3f)
            {
                vy -= c.Gravity * dt; x += vx * dt; y += vy * dt; t += dt;
                if (x - half < -c.HalfWidth) { x = -c.HalfWidth + half; vx = System.Math.Abs(vx); }
                if (x + half > c.HalfWidth) { x = c.HalfWidth - half; vx = -System.Math.Abs(vx); }
                foreach (var h in hz)
                {
                    h.Step(dt, c);
                    if (hit == 0 && System.Math.Abs(h.X - x) < h.W * 0.5f + half + 0.12f &&
                        System.Math.Abs(h.Y - y) < h.H * 0.5f + half + 0.12f) hit = 1;
                }
                if (vy < 0f && y - half <= c.TrampolineY) break;
            }
            landX = x; arcT = t;
            return hit;
        }

        /// <summary>Hay algun offset seguro para el arco que empieza en landX tras arcT segundos.</summary>
        static bool HasSafeFollowUp(List<Hazard> hazardsAtLanding, GameConfig c, float landX, float arcT, float limit)
        {
            var later = Advance(hazardsAtLanding, arcT, c);
            float halfT = c.TrampolineWidth * 0.5f;
            foreach (var off in Offsets)
            {
                float tgt = System.Math.Max(-limit, System.Math.Min(limit, landX - off * halfT));
                float eff = System.Math.Max(-1f, System.Math.Min(1f, (landX - tgt) / halfT));
                if (ArcHits(later, c, landX, eff, out _, out _) == 0) return true;
            }
            return false;
        }
    }
}
