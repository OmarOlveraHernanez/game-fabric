using System;
using System.IO;
using GameFactory.Core.Save;
using NUnit.Framework;

namespace GameFactory.Core.Tests
{
    public class SaveFileTests
    {
        [Serializable]
        public class Data
        {
            public int version = 1;
            public int best;
            public string name = "";
        }

        string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "gf-save-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_dir, true); } catch (Exception) { }
        }

        [Test]
        public void Load_WhenMissing_ReturnsDefaults()
        {
            var f = new SaveFile<Data>("partida", _dir);
            Assert.IsFalse(f.Exists);
            var d = f.Load();
            Assert.AreEqual(1, d.version);
            Assert.AreEqual(0, d.best);
        }

        [Test]
        public void SaveThenLoad_RoundTrips()
        {
            var f = new SaveFile<Data>("partida", _dir);
            f.Save(new Data { best = 42, name = "Papi" });
            var d = new SaveFile<Data>("partida", _dir).Load();
            Assert.AreEqual(42, d.best);
            Assert.AreEqual("Papi", d.name);
            Assert.IsFalse(File.Exists(f.Path + ".tmp"), "No debe quedar el temporal");
        }

        [Test]
        public void Load_WhenCorrupt_ReturnsDefaults_AndKeepsBackup()
        {
            var f = new SaveFile<Data>("partida", _dir);
            File.WriteAllText(f.Path, "{ esto no es json");
            var d = f.Load();
            Assert.AreEqual(0, d.best);
            Assert.IsTrue(File.Exists(f.Path + ".corrupt"));
        }

        [Test]
        public void Load_RecoversFromTemp_IfMainIsMissing()
        {
            var f = new SaveFile<Data>("partida", _dir);
            f.Save(new Data { best = 7 });
            File.Move(f.Path, f.Path + ".tmp"); // simula que la app murio entre escribir y copiar
            Assert.AreEqual(7, f.Load().best);
        }

        [Test]
        public void InvalidName_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SaveFile<Data>("mal/nombre", _dir));
            Assert.Throws<ArgumentException>(() => new SaveFile<Data>("", _dir));
        }
    }
}
