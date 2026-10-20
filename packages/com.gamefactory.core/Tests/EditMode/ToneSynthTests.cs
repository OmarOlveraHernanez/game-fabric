using GameFactory.Core.Audio;
using NUnit.Framework;

namespace GameFactory.Core.Tests
{
    public class ToneSynthTests
    {
        [TestCase(Wave.Sine)]
        [TestCase(Wave.Square)]
        [TestCase(Wave.Triangle)]
        [TestCase(Wave.Noise)]
        public void Generate_HasExpectedLength_StaysInRange_AndEndsSilent(Wave wave)
        {
            var d = ToneSynth.Generate(440f, 880f, 0.2f, wave, 0.5f, 44100);
            Assert.AreEqual((int)(0.2f * 44100), d.Length);
            float max = 0f;
            foreach (var s in d) max = System.Math.Max(max, System.Math.Abs(s));
            Assert.That(max, Is.LessThan(0.5001f));
            Assert.That(max, Is.GreaterThan(0.05f), "Debe sonar");
            Assert.AreEqual(0f, d[0], 0.001f, "Sin click al inicio");
            Assert.AreEqual(0f, d[d.Length - 1], 0.001f, "Sin click al final");
        }

        [Test]
        public void Generate_IsDeterministic()
        {
            var a = ToneSynth.Generate(300f, 100f, 0.1f, Wave.Noise, 0.4f, 22050, seed: 3);
            var b = ToneSynth.Generate(300f, 100f, 0.1f, Wave.Noise, 0.4f, 22050, seed: 3);
            CollectionAssert.AreEqual(a, b);
        }
    }
}
