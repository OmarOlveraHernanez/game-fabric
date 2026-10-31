using System;
using System.Collections.Generic;

namespace RescatePapi.Logic
{
    public enum RunState { Ready, Running, Dead }

    /// <summary>
    /// Simulacion completa del runner, independiente de Unity (testeable en EditMode).
    /// La vista solo llama Tick() con el input y dibuja el estado.
    /// </summary>
    public sealed class RunnerSimulation
    {
        readonly RunnerConfig _c;
        readonly SpawnGenerator _gen;
        readonly List<Hazard> _hazards = new List<Hazard>();
        readonly List<Pickup> _pickups = new List<Pickup>();
        float _distanceToNextSpawn;
        float _deadTime;
        float _invulnerable;

        public RunnerConfig Config => _c;
        public JumpMotor Jump { get; }
        public RunState State { get; private set; }
        public float Speed { get; private set; }
        public float Distance { get; private set; }
        public float Elapsed { get; private set; }
        public int Score => (int)(Distance * _c.ScorePerUnit);
        /// <summary>0..1: sube con el tiempo de carrera y decide que enemigos y comportamientos aparecen.</summary>
        public float Level => Difficulty.LevelAt(_c, Elapsed);
        public int BestScore { get; private set; }
        public int Cacao { get; private set; }               // cacao de esta carrera
        public bool HasShield { get; private set; }
        public bool IsInvulnerable => _invulnerable > 0f;
        public bool CanRestart => State == RunState.Dead && _deadTime >= _c.RestartDelay;
        public IReadOnlyList<Hazard> Hazards => _hazards;
        public IReadOnlyList<Pickup> Pickups => _pickups;

        public event Action Started;
        public event Action Died;
        public event Action<Pickup> Collected;
        public event Action ShieldLost;
        /// <summary>Un enemigo empieza un comportamiento especial (la serpiente salta, el aguila se lanza en picada).</summary>
        public event Action<Hazard> EnemyAction;

        public RunnerSimulation(RunnerConfig config, int seed)
        {
            _c = config;
            _gen = new SpawnGenerator(config, seed);
            Jump = new JumpMotor(config);
            ResetRun();
        }

        /// <summary>Permite fijar el record (p. ej. cargado de disco).</summary>
        public void SetBestScore(int best) { if (best > BestScore) BestScore = best; }

        public void ResetRun()
        {
            State = RunState.Ready;
            Speed = _c.BaseSpeed;
            Distance = 0f;
            Elapsed = 0f;
            Cacao = 0;
            HasShield = false;
            _invulnerable = 0f;
            _deadTime = 0f;
            _hazards.Clear();
            _pickups.Clear();
            Jump.Reset();
            _distanceToNextSpawn = _c.BaseSpeed * _c.FirstHazardDelay;
        }

        /// <summary>Empieza la carrera desde la pantalla de inicio (boton Jugar o tocar la pantalla).</summary>
        public void Begin()
        {
            if (State != RunState.Ready) return;
            State = RunState.Running;
            Started?.Invoke();
        }

        /// <summary>Nueva carrera tras morir (boton Otra vez). False si aun no pasa RestartDelay.</summary>
        public bool TryRestart()
        {
            if (!CanRestart) return false;
            ResetRun();
            State = RunState.Running;
            Started?.Invoke();
            return true;
        }

        public void Tick(float dt, bool pressedThisFrame, bool held)
        {
            switch (State)
            {
                case RunState.Ready:
                    if (pressedThisFrame) Begin();
                    return;

                case RunState.Dead:
                    _deadTime += dt;
                    if (pressedThisFrame) TryRestart();
                    return;
            }

            // --- Running ---
            Elapsed += dt;
            Speed = Difficulty.SpeedAt(_c, Elapsed);
            float dx = Speed * dt;
            Distance += dx;
            if (_invulnerable > 0f) _invulnerable -= dt;

            Jump.Tick(dt, pressedThisFrame, held);

            MoveHazards(dt, dx);
            MovePickups(dx);
            SpawnIfNeeded(dx);
            CollectPickups();

            if (HitsAnyHazard() && !IsInvulnerable)
            {
                if (HasShield)
                {
                    HasShield = false;
                    _invulnerable = _c.ShieldInvulnerableTime;
                    ShieldLost?.Invoke();
                }
                else Die();
            }
        }

        void MoveHazards(float dt, float dx)
        {
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                var h = _hazards[i];
                if (h.WillCharge && !h.IsCharging && h.Left - _c.PlayerX < _c.JaguarChargeDistance)
                    h.IsCharging = true;
                h.X -= dx + (h.IsCharging ? _c.JaguarChargeSpeed * dt : 0f);
                if (h.WillHop) UpdateHop(h, dt);
                if (h.WillDive) UpdateDive(h, dt);
                if (h.Right < _c.DespawnX) _hazards.RemoveAt(i);
            }
        }

        /// <summary>Serpiente: salta en arco de modo que el punto mas alto coincide con el jugador.</summary>
        void UpdateHop(Hazard h, float dt)
        {
            if (h.HopTime < 0f)
            {
                float timeToPlayer = (h.X - _c.PlayerX) / Speed;
                if (timeToPlayer > _c.SerpentHopDuration * 0.5f) return;
                h.HopTime = 0f;
                EnemyAction?.Invoke(h);
            }
            h.HopTime += dt;
            float u = Math.Min(1f, h.HopTime / _c.SerpentHopDuration);
            h.Bottom = 4f * _c.SerpentHopHeight * u * (1f - u);
        }

        /// <summary>Aguila: a cierta distancia baja a ras de suelo; despues de pasar al jugador vuelve a subir.</summary>
        void UpdateDive(Hazard h, float dt)
        {
            if (!h.IsDiving)
            {
                if (h.Left - _c.PlayerX > _c.EagleDiveDistance) return;
                h.IsDiving = true;
                EnemyAction?.Invoke(h);
            }
            bool passed = h.Right < _c.PlayerX - _c.PlayerWidth;
            h.Bottom = passed ? h.Bottom + _c.EagleDiveSpeed * dt
                              : Math.Max(_c.EagleDiveBottom, h.Bottom - _c.EagleDiveSpeed * dt);
        }

        void MovePickups(float dx)
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                _pickups[i].X -= dx;
                if (_pickups[i].Right < _c.DespawnX) _pickups.RemoveAt(i);
            }
        }

        void SpawnIfNeeded(float dx)
        {
            _distanceToNextSpawn -= dx;
            if (_distanceToNextSpawn > 0f) return;

            float level = Level;
            var h = _gen.CreateHazard(_c.SpawnX, Speed, level);
            _hazards.Add(h);
            float gap = _gen.NextGapDistance(Speed, level);
            bool allowPollito = !HasShield && !_pickups.Exists(p => p.Kind == PickupKind.Pollito);
            _gen.CreatePickups(_pickups, h, gap, allowPollito);
            _distanceToNextSpawn += gap;
        }

        void CollectPickups()
        {
            float pLeft = _c.PlayerX - _c.PlayerWidth * 0.5f;
            float pRight = _c.PlayerX + _c.PlayerWidth * 0.5f;
            float pBottom = Jump.Y;
            float pTop = Jump.Y + _c.PlayerHeight;

            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                var p = _pickups[i];
                if (pRight > p.Left && pLeft < p.Right && pTop > p.Bottom && pBottom < p.Top)
                {
                    if (p.Kind == PickupKind.Cacao) Cacao++;
                    else if (p.Kind == PickupKind.Pollito) HasShield = true;
                    _pickups.RemoveAt(i);
                    Collected?.Invoke(p);
                }
            }
        }

        bool HitsAnyHazard()
        {
            float inset = _c.HitboxInset;
            float pLeft = _c.PlayerX - _c.PlayerWidth * 0.5f + inset;
            float pRight = _c.PlayerX + _c.PlayerWidth * 0.5f - inset;
            float pBottom = Jump.Y + inset;
            float pTop = Jump.Y + _c.PlayerHeight - inset;

            foreach (var h in _hazards)
            {
                if (pRight > h.Left && pLeft < h.Right && pTop > h.Bottom && pBottom < h.Top)
                    return true;
            }
            return false;
        }

        void Die()
        {
            State = RunState.Dead;
            _deadTime = 0f;
            if (Score > BestScore) BestScore = Score;
            Died?.Invoke();
        }
    }
}
