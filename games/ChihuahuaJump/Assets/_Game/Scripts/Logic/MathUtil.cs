using System;

namespace ChihuahuaJump.Logic
{
    internal static class MathUtil
    {
        public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Range(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);
    }
}
