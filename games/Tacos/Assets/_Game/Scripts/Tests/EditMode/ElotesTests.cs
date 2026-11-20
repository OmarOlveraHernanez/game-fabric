using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    /// <summary>Mundo 3: los elotes. Lo nuevo es el orden: primero la mayonesa, luego el queso y el chile.</summary>
    public class ElotesTests
    {
        static KitchenSimulation Sim(int mission = 1) => new KitchenSimulation(LevelConfig.Elotes[mission], TaqueroDef.Guero, 1);

        [Test]
        public void ElElote_VaEnPalito_YElEsquiteEnVaso()
        {
            var s = Sim(4);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ServeMeat(Meat.Elote)));
            Assert.AreEqual(Base.Palito, s.Plates[s.SelectedPlate].Base);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.SelectPlate(1)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ServeMeat(Meat.Esquite)));
            Assert.AreEqual(Base.Vaso, s.Plates[1].Base);
            Assert.AreEqual(Base.Palito, LevelConfig.Elotes[4].StallDef.BaseFor(Meat.EloteAsado), "el asado tambien va en palito");
        }

        [Test]
        public void SinMayonesa_NoSePegaElQueso()
        {
            var s = Sim();
            s.Apply(Command.ServeMeat(Meat.Elote));
            Assert.AreEqual(CommandResult.FirstMayo, s.Apply(Command.ToggleExtra(Extra.Queso)));
            Assert.AreEqual(CommandResult.FirstMayo, s.Apply(Command.ToggleExtra(Extra.ChilePolvo)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.Limon)), "el limon va cuando sea");
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.Mayonesa)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.Queso)));
            Assert.AreEqual(CommandResult.FirstMayo, s.Apply(Command.ToggleExtra(Extra.Mayonesa)), "no se quita la mayonesa con queso encima");
        }

        [Test]
        public void NadiePideQuesoSinMayonesa()
        {
            foreach (var lvl in LevelConfig.Elotes)
            {
                var s = new KitchenSimulation(lvl, TaqueroDef.Tio, 7);
                for (int i = 0; i < 80; i++)
                {
                    var o = s.NewOrder();
                    if ((o.Extras & (Extra.Queso | Extra.ChilePolvo)) != 0)
                        Assert.IsTrue((o.Extras & Extra.Mayonesa) != 0, lvl.Name + " " + o);
                    Assert.AreEqual(lvl.StallDef.BaseFor(o.Meat), o.Base, lvl.Name);
                    Assert.IsTrue(System.Array.IndexOf(lvl.Meats, o.Meat) >= 0, lvl.Name);
                }
            }
        }

        [Test]
        public void TieneSusMejoras_SuJefa_YSusMisiones()
        {
            Assert.AreEqual(12, LevelConfig.Elotes.Length);
            Assert.IsTrue(LevelConfig.Elotes[11].BossAt > 0f);
            Assert.AreEqual(4, UpgradeDef.For(Stall.Elotes).Length);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)]
        public void CadaMision_EsGanableYLas3PidenRapidez(int mision)
        {
            var lvl = LevelConfig.Elotes[mision];
            foreach (var taq in TaqueroDef.Starters)
                Assert.That(HintBot.MedianStars(lvl, taq, 0.6f), Is.GreaterThan(0), taq.Name + " en " + lvl.Name);
            Assert.That(HintBot.MedianStars(lvl, TaqueroDef.Tio, 1.5f), Is.LessThan(3), lvl.Name + " lento");
            Assert.AreEqual(3, HintBot.MedianStars(lvl, TaqueroDef.Tio, 0.5f), lvl.Name + " rapido");
        }
    }
}
