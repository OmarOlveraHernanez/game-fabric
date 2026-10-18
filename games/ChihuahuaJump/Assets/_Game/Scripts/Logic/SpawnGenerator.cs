using System;

namespace ChihuahuaJump.Logic
{
    /// <summary>Crea enemigos y premios segun la dificultad.</summary>
    public sealed class SpawnGenerator
    {
        readonly GameConfig _c;
        readonly Random _rng;

        public SpawnGenerator(GameConfig config, int seed)
        {
            _c = config;
            _rng = new Random(seed);
        }

        public float NextHazardInterval(float level) =>
            MathUtil.Lerp(_c.SpawnIntervalEasy, _c.SpawnIntervalHard, level) * MathUtil.Range(_rng, 0.8f, 1.2f);

        public HazardKind PickKind(float level)
        {
            double wS = _c.SerpentWeight;
            double wJ = level >= _c.JaguarFromLevel ? _c.JaguarWeight : 0.0;
            double wE = level >= _c.EagleFromLevel ? _c.EagleWeight : 0.0;
            double r = _rng.NextDouble() * (wS + wJ + wE);
            if (r < wS) return HazardKind.Serpent;
            if (r < wS + wJ) return HazardKind.Jaguar;
            return HazardKind.Eagle;
        }

        public Hazard CreateHazard(float level)
        {
            var h = new Hazard { Kind = PickKind(level) };
            float fall = MathUtil.Lerp(_c.SerpentFallEasy, _c.SerpentFallHard, level);
            switch (h.Kind)
            {
                case HazardKind.Serpent:
                    h.W = _c.SerpentWidth; h.H = _c.SerpentHeight;
                    h.X = RandomX(h.W); h.Vy = -fall; h.Y = FallingStartY(h);
                    break;
                case HazardKind.Jaguar:
                    h.W = _c.JaguarWidth; h.H = _c.JaguarHeight;
                    h.X = RandomX(h.W); h.Vy = -fall * _c.JaguarFallMultiplier; h.Y = FallingStartY(h);
                    break;
                case HazardKind.Eagle:
                    h.W = _c.EagleWidth; h.H = _c.EagleHeight;
                    h.X = RandomX(h.W);
                    h.Vx = (_rng.NextDouble() < 0.5 ? -1f : 1f) * _c.EagleSideSpeed;
                    h.Vy = -fall * _c.EagleFallMultiplier;
                    h.Y = FallingStartY(h);
                    break;
            }
            return h;
        }

        /// <summary>Crea un grupo de premios flotando a una altura alcanzable (1 pollito o 1-3 granos de cacao).</summary>
        public void CreatePickups(System.Collections.Generic.List<Pickup> into, bool allowPollito)
        {
            float minY = _c.TrampolineY + 2f;
            float maxY = _c.DogPeakY - _c.DogSize - 0.3f;
            float y = MathUtil.Range(_rng, minY, maxY);

            if (allowPollito && _rng.NextDouble() < _c.PollitoChance)
            {
                into.Add(new Pickup { Kind = PickupKind.Pollito, W = _c.PollitoSize, H = _c.PollitoSize, X = RandomX(_c.PollitoSize), Y = y, Life = _c.PickupLifetime });
                return;
            }

            int n = _rng.Next(1, _c.CacaoGroupMax + 1);
            float groupWidth = (n - 1) * _c.CacaoSpacing + _c.CacaoSize;
            float center = RandomX(groupWidth);
            for (int i = 0; i < n; i++)
            {
                float x = center + (i - (n - 1) * 0.5f) * _c.CacaoSpacing;
                into.Add(new Pickup { Kind = PickupKind.Cacao, W = _c.CacaoSize, H = _c.CacaoSize, X = x, Y = y, Life = _c.PickupLifetime });
            }
        }

        /// <summary>Altura inicial para que tarde HazardLeadTime en llegar a la zona del perro (nunca debajo de TopY).</summary>
        float FallingStartY(Hazard h)
        {
            float y = _c.DogPeakY + h.H * 0.5f + _c.HazardLeadTime * System.Math.Abs(h.Vy);
            return System.Math.Max(y, _c.TopY + h.H);
        }

        float RandomX(float width)
        {
            float limit = _c.HalfWidth - width * 0.5f;
            return MathUtil.Range(_rng, -limit, limit);
        }
    }
}
