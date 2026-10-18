using System;
using System.Collections.Generic;

namespace ChihuahuaJump.Logic
{
    public enum GameState { Ready, Playing, Dead }
    public enum DeathCause { None, Fell, Hit }

    /// <summary>
    /// Simulacion completa, independiente de Unity (testeable en EditMode).
    /// El chihuahua rebota solo; el jugador mueve el trampolin a izquierda/derecha.
    /// </summary>
    public sealed class BounceSimulation
    {
        readonly GameConfig _c;
        readonly SpawnGenerator _gen;
        readonly List<Hazard> _hazards = new List<Hazard>();
        readonly List<Pickup> _pickups = new List<Pickup>();
        float _hazardTimer, _pickupTimer, _deadTime, _invulnerable;

        public GameConfig Config => _c;
        public Dog Dog { get; } = new Dog();
        public Trampoline Trampoline { get; } = new Trampoline();
        public GameState State { get; private set; }
        public DeathCause LastDeath { get; private set; }
        public float Elapsed { get; private set; }
        public float Level => Difficulty.Level(_c, Elapsed);
        public int Bounces { get; private set; }
        public int Score => Bounces;
        public int BestScore { get; private set; }
        public int Cacao { get; private set; }
        public bool HasShield { get; private set; }
        public bool IsInvulnerable => _invulnerable > 0f;
        public bool CanRestart => State == GameState.Dead && _deadTime >= _c.RestartDelay;
        public IReadOnlyList<Hazard> Hazards => _hazards;
        public IReadOnlyList<Pickup> Pickups => _pickups;

        public event Action Started;
        public event Action Bounced;
        public event Action<Pickup> Collected;
        public event Action ShieldLost;
        public event Action<DeathCause> Died;

        public BounceSimulation(GameConfig config, int seed)
        {
            _c = config;
            _gen = new SpawnGenerator(config, seed);
            Reset();
        }

        public void SetBestScore(int best) { if (best > BestScore) BestScore = best; }

        public void Reset()
        {
            State = GameState.Ready;
            LastDeath = DeathCause.None;
            Elapsed = 0f;
            Bounces = 0;
            Cacao = 0;
            HasShield = false;
            _invulnerable = 0f;
            _deadTime = 0f;
            _hazards.Clear();
            _pickups.Clear();
            _hazardTimer = _c.FirstSpawnDelay;
            _pickupTimer = _c.PickupInterval;
            Trampoline.X = 0f;
            Trampoline.Width = _c.TrampolineWidth;
            Dog.W = Dog.H = _c.DogSize;
            Dog.X = 0f;
            Dog.Y = _c.StartDogY;
            Dog.Vx = 0f;
            Dog.Vy = 0f;
        }

        /// <param name="direction">-1 izquierda, 0 nada, +1 derecha (teclado)</param>
        /// <param name="tapped">toque en este frame (inicia / reinicia)</param>
        /// <param name="targetX">si tiene valor, el trampolin sigue esa x (dedo/raton) e ignora direction</param>
        public void Tick(float dt, int direction, bool tapped, float? targetX = null)
        {
            switch (State)
            {
                case GameState.Ready:
                    MoveTrampoline(dt, direction, targetX);
                    Dog.X = Trampoline.X;
                    if (tapped || direction != 0) { State = GameState.Playing; Started?.Invoke(); }
                    return;
                case GameState.Dead:
                    _deadTime += dt;
                    if (tapped && CanRestart) { Reset(); State = GameState.Playing; Started?.Invoke(); }
                    return;
            }

            Elapsed += dt;
            if (_invulnerable > 0f) _invulnerable -= dt;

            MoveTrampoline(dt, direction, targetX);
            float prevBottom = Dog.Bottom;
            Dog.Step(dt, _c);
            TryBounce(prevBottom);

            if (Dog.Top < 0f) { Die(DeathCause.Fell); return; }

            UpdateHazards(dt);
            UpdatePickups(dt);
            Collect();

            if (!IsInvulnerable && HitsHazard())
            {
                if (HasShield) { HasShield = false; _invulnerable = _c.ShieldInvulnerableTime; ShieldLost?.Invoke(); }
                else Die(DeathCause.Hit);
            }
        }

        void MoveTrampoline(float dt, int direction, float? targetX)
        {
            if (targetX.HasValue) Trampoline.StepTowards(dt, targetX.Value, _c);
            else Trampoline.Step(dt, direction, _c);
        }

        void TryBounce(float prevBottom)
        {
            float surface = _c.TrampolineY;
            bool crossing = Dog.Vy < 0f && prevBottom >= surface && Dog.Bottom <= surface;
            bool overTrampoline = Dog.Right > Trampoline.Left && Dog.Left < Trampoline.Right;
            if (!crossing || !overTrampoline) return;

            float offset = (Dog.X - Trampoline.X) / (Trampoline.Width * 0.5f); // -1 orilla izq ... +1 orilla der
            if (offset < -1f) offset = -1f;
            if (offset > 1f) offset = 1f;
            Dog.Y = surface + Dog.H * 0.5f;
            Dog.Vy = _c.BounceVelocity;
            Dog.Vx = SideSpeedFor(offset, _c);
            Bounces++;
            Bounced?.Invoke();
        }

        /// <summary>Velocidad lateral segun donde pega en el trampolin (-1..1), con zona central recta.</summary>
        public static float SideSpeedFor(float offset, GameConfig c)
        {
            float a = System.Math.Abs(offset);
            if (a <= c.CenterDeadZone) return 0f;
            float t = (a - c.CenterDeadZone) / (1f - c.CenterDeadZone);
            return System.Math.Sign(offset) * t * c.MaxSideSpeed;
        }

        void UpdateHazards(float dt)
        {
            _hazardTimer -= dt;
            int maxHazards = (int)System.Math.Round(MathUtil.Lerp(_c.MaxHazardsEasy, _c.MaxHazardsHard, Level));
            if (_hazardTimer <= 0f && _hazards.Count >= maxHazards) _hazardTimer = 0.2f; // espera a que se libere lugar
            else if (_hazardTimer <= 0f)
            {
                _hazards.Add(_gen.CreateHazard(Level));
                _hazardTimer += _gen.NextHazardInterval(Level);
            }
            for (int i = _hazards.Count - 1; i >= 0; i--)
            {
                var h = _hazards[i];
                h.Step(dt, _c);
                if (h.Top < -1f) _hazards.RemoveAt(i);
            }
        }

        void UpdatePickups(float dt)
        {
            _pickupTimer -= dt;
            if (_pickupTimer <= 0f)
            {
                bool allowPollito = !HasShield && !_pickups.Exists(p => p.Kind == PickupKind.Pollito);
                _gen.CreatePickups(_pickups, allowPollito);
                _pickupTimer += _c.PickupInterval;
            }
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                _pickups[i].Life -= dt;
                if (_pickups[i].Life <= 0f) _pickups.RemoveAt(i);
            }
        }

        void Collect()
        {
            for (int i = _pickups.Count - 1; i >= 0; i--)
            {
                var p = _pickups[i];
                if (!Dog.Overlaps(p, -_c.CollectMargin)) continue; // inset negativo = area mas generosa
                if (p.Kind == PickupKind.Cacao) Cacao++;
                else HasShield = true;
                _pickups.RemoveAt(i);
                Collected?.Invoke(p);
            }
        }

        bool HitsHazard()
        {
            foreach (var h in _hazards)
                if (Dog.Overlaps(h, _c.HitboxInset)) return true;
            return false;
        }

        void Die(DeathCause cause)
        {
            State = GameState.Dead;
            LastDeath = cause;
            _deadTime = 0f;
            if (Score > BestScore) BestScore = Score;
            Died?.Invoke(cause);
        }
    }
}
