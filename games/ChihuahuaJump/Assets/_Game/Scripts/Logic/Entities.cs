namespace ChihuahuaJump.Logic
{
    /// <summary>Caja con centro (X, Y) y tamano (W, H) que se mueve con velocidad (Vx, Vy).</summary>
    public abstract class Body
    {
        public float X, Y, W, H, Vx, Vy;
        public float Left => X - W * 0.5f;
        public float Right => X + W * 0.5f;
        public float Bottom => Y - H * 0.5f;
        public float Top => Y + H * 0.5f;

        public bool Overlaps(Body o, float inset = 0f) =>
            Right - inset > o.Left && Left + inset < o.Right && Top - inset > o.Bottom && Bottom + inset < o.Top;

        public void Move(float dt) { X += Vx * dt; Y += Vy * dt; }
    }

    public enum HazardKind { Serpent, Jaguar, Eagle }
    public sealed class Hazard : Body
    {
        public HazardKind Kind;

        /// <summary>Avanza; el aguila rebota en las paredes laterales.</summary>
        public void Step(float dt, GameConfig c)
        {
            Move(dt);
            if (Kind != HazardKind.Eagle) return;
            if (Left < -c.HalfWidth) { X = -c.HalfWidth + W * 0.5f; Vx = System.Math.Abs(Vx); }
            if (Right > c.HalfWidth) { X = c.HalfWidth - W * 0.5f; Vx = -System.Math.Abs(Vx); }
        }

        public Hazard Clone() => new Hazard { Kind = Kind, X = X, Y = Y, W = W, H = H, Vx = Vx, Vy = Vy };
    }

    public enum PickupKind { Cacao, Pollito }
    public sealed class Pickup : Body
    {
        public PickupKind Kind;
        public float Life; // segundos que le quedan
    }

    /// <summary>El chihuahua: cae con gravedad y rebota en las paredes laterales.</summary>
    public sealed class Dog : Body
    {
        public void Step(float dt, GameConfig c)
        {
            Vy -= c.Gravity * dt;
            Move(dt);
            if (Left < -c.HalfWidth) { X = -c.HalfWidth + W * 0.5f; Vx = System.Math.Abs(Vx); }
            if (Right > c.HalfWidth) { X = c.HalfWidth - W * 0.5f; Vx = -System.Math.Abs(Vx); }
        }
    }

    /// <summary>El trampolin que carga el dueno. X = centro.</summary>
    public sealed class Trampoline
    {
        public float X;
        public float Width;
        public float Left => X - Width * 0.5f;
        public float Right => X + Width * 0.5f;

        public float MaxCenter(GameConfig c) => c.HalfWidth - Width * 0.5f + c.TrampolineOverhang;

        /// <summary>Se mueve hacia targetX sin pasarse (control de seguir el dedo).</summary>
        public void StepTowards(float dt, float targetX, GameConfig c)
        {
            float limit = MaxCenter(c);
            if (targetX < -limit) targetX = -limit;
            if (targetX > limit) targetX = limit;
            float maxMove = c.TrampolineSpeed * dt;
            float diff = targetX - X;
            X += System.Math.Abs(diff) <= maxMove ? diff : System.Math.Sign(diff) * maxMove;
        }

        /// <summary>direction: -1 izquierda, 0 quieto, +1 derecha.</summary>
        public void Step(float dt, int direction, GameConfig c)
        {
            float limit = MaxCenter(c);
            X += direction * c.TrampolineSpeed * dt;
            if (X < -limit) X = -limit;
            if (X > limit) X = limit;
        }
    }
}
