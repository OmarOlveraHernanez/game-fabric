using System;
using System.Collections.Generic;

namespace RescatePapi.Logic
{
    /// <summary>Genera enemigos, huecos y premios segun la dificultad.</summary>
    public sealed class SpawnGenerator
    {
        readonly RunnerConfig _c;
        readonly Random _rng;

        public SpawnGenerator(RunnerConfig config, int seed)
        {
            _c = config;
            _rng = new Random(seed);
        }

        /// <summary>Distancia al siguiente enemigo (definida en tiempo para escalar con la velocidad).</summary>
        public float NextGapDistance(float speed)
        {
            float level = Difficulty.Level(_c, speed);
            float min = MathUtil.Lerp(_c.GapTimeMinEasy, _c.GapTimeMinHard, level);
            float max = MathUtil.Lerp(_c.GapTimeMaxEasy, _c.GapTimeMaxHard, level);
            return speed * MathUtil.Range(_rng, min, max);
        }

        public HazardKind PickKind(float level)
        {
            double wS = _c.SerpentWeight;
            double wJ = _c.JaguarWeight;
            double wE = level >= _c.EagleFromLevel ? _c.EagleWeight : 0.0;
            double r = _rng.NextDouble() * (wS + wJ + wE);
            if (r < wS) return HazardKind.Serpent;
            if (r < wS + wJ) return HazardKind.Jaguar;
            return HazardKind.Eagle;
        }

        public Hazard CreateHazard(float x, float speed)
        {
            float level = Difficulty.Level(_c, speed);
            var h = new Hazard { Kind = PickKind(level), X = x };
            switch (h.Kind)
            {
                case HazardKind.Serpent:
                    h.Width = MathUtil.Range(_rng, _c.SerpentMinWidth, _c.SerpentMaxWidth);
                    h.Height = MathUtil.Range(_rng, _c.SerpentMinHeight, _c.SerpentMaxHeight);
                    break;
                case HazardKind.Jaguar:
                    h.Width = MathUtil.Range(_rng, _c.JaguarMinWidth, _c.JaguarMaxWidth);
                    float maxH = MathUtil.Lerp(_c.JaguarMaxHeightEasy, _c.JaguarMaxHeightHard, level);
                    h.Height = MathUtil.Range(_rng, _c.JaguarMinHeight, maxH);
                    h.WillCharge = level >= _c.JaguarChargeFromLevel && _rng.NextDouble() < _c.JaguarChargeChance;
                    // Compensa la distancia que ganara al embestir: llega al jugador en el mismo tiempo
                    // que uno normal, solo que acelera al final (sorpresa sin ser injusto).
                    if (h.WillCharge) h.X += _c.JaguarChargeSpeed * _c.JaguarChargeDistance / (speed + _c.JaguarChargeSpeed);
                    break;
                case HazardKind.Eagle:
                    h.Width = _c.EagleWidth;
                    h.Height = _c.EagleThickness;
                    h.Bottom = MathUtil.Range(_rng, _c.EagleMinBottom, _c.EagleMaxBottom);
                    break;
            }
            return h;
        }

        /// <summary>Agrega premios asociados al enemigo recien creado y al hueco que le sigue.</summary>
        public void CreatePickups(List<Pickup> into, Hazard h, float gapAfter, bool allowPollito)
        {
            float gapCenter = h.X + gapAfter * 0.5f;
            bool pollito = allowPollito && _rng.NextDouble() < _c.PollitoChancePerGap;
            if (pollito)
                into.Add(new Pickup { Kind = PickupKind.Pollito, X = gapCenter, Y = _c.PollitoY, Size = _c.PollitoSize });

            if (_rng.NextDouble() >= _c.CacaoChancePerGap) return;

            int n = _rng.Next(_c.CacaoMinCount, _c.CacaoMaxCount + 1);
            bool arc = h.Kind == HazardKind.Jaguar && _rng.NextDouble() < _c.CacaoArcOverJaguarChance;
            if (!arc && pollito) return; // no amontonar premios en el mismo hueco

            float center = arc ? h.X : gapCenter;
            float half = (n - 1) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float offset = (i - half) * _c.CacaoSpacing;
                float y = arc
                    ? h.Top + 0.6f + 0.4f * (1f - (half > 0 ? (offset / (half * _c.CacaoSpacing)) * (offset / (half * _c.CacaoSpacing)) : 0f))
                    : _c.CacaoGroundY;
                into.Add(new Pickup { Kind = PickupKind.Cacao, X = center + offset, Y = y, Size = _c.CacaoSize });
            }
        }
    }
}
