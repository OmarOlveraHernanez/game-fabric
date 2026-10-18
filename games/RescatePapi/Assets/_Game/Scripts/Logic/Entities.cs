namespace RescatePapi.Logic
{
    public enum HazardKind { Serpent, Jaguar, Eagle }

    /// <summary>Enemigo. X = centro horizontal; Bottom = altura de su base (0 = suelo).</summary>
    public sealed class Hazard
    {
        public HazardKind Kind;
        public float X, Width, Bottom, Height;
        public bool WillCharge;   // jaguar que embestira
        public bool IsCharging;   // embistiendo ahora
        public float Left => X - Width * 0.5f;
        public float Right => X + Width * 0.5f;
        public float Top => Bottom + Height;
        public bool IsAirborne => Kind == HazardKind.Eagle;
    }

    public enum PickupKind { Cacao, Pollito }

    /// <summary>Premio. (X, Y) = centro.</summary>
    public sealed class Pickup
    {
        public PickupKind Kind;
        public float X, Y, Size;
        public float Left => X - Size * 0.5f;
        public float Right => X + Size * 0.5f;
        public float Bottom => Y - Size * 0.5f;
        public float Top => Y + Size * 0.5f;
    }
}
