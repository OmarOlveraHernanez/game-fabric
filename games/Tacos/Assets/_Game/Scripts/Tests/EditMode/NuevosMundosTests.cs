using NUnit.Framework;
using Tacos.Logic;

namespace Tacos.Tests
{
    /// <summary>Mundos 4, 5 y 6: pozoleria (el tamaño), tamales (el combo y la vaporera) y la feria (todo junto).</summary>
    public class NuevosMundosTests
    {
        static KitchenSimulation Sim(LevelConfig[] world, int mission) => new KitchenSimulation(world[mission], TaqueroDef.Guero, 1);

        [Test]
        public void Pozole_ElPlatoVaDirectoDeLaPila_SinComal()
        {
            var s = Sim(LevelConfig.Pozole, 2);
            Assert.AreEqual(Base.None, s.Plates[0].Base, "la tabla empieza vacia");
            Assert.AreEqual(CommandResult.NeedTortillaFirst, s.Apply(Command.ServeMeat(Meat.PozoleRojo)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.AddTortilla(Base.PlatoGrande)));
            Assert.AreEqual(Base.PlatoGrande, s.Plates[0].Base);
            Assert.IsTrue(System.Array.TrueForAll(s.Comal, c => c == null), "nada en el comal");
        }

        [Test]
        public void Pozole_ElGrandeLlevaDosCucharones()
        {
            var s = Sim(LevelConfig.Pozole, 2);
            var o = new Order(Base.PlatoGrande, Meat.PozoleRojo, Extra.Limon);
            s.Apply(Command.AddTortilla(Base.PlatoGrande));
            s.Apply(Command.ServeMeat(Meat.PozoleRojo));
            s.Apply(Command.ToggleExtra(Extra.Limon));
            Assert.IsTrue(s.Plates[0].NeedsScoop);
            Assert.IsFalse(s.Plates[0].Matches(o), "con un cucharon no se acepta");
            Assert.IsTrue(s.Plates[0].IsHeadingTo(o));
            s.Apply(Command.ServeMeat(Meat.PozoleRojo));
            Assert.AreEqual(2, s.Plates[0].Scoops);
            Assert.IsTrue(s.Plates[0].Matches(o));
            Assert.AreEqual(s.PotSize - 2, s.Pot(Meat.PozoleRojo).Portions, "cada cucharon gasta una porcion");
            // el chico lleva uno: el siguiente cucharon va a otro plato
            s.Apply(Command.SelectPlate(1));
            s.Apply(Command.AddTortilla(Base.PlatoChico));
            s.Apply(Command.ServeMeat(Meat.PozoleRojo));
            Assert.IsTrue(s.Plates[1].Matches(new Order(Base.PlatoChico, Meat.PozoleRojo, Extra.None)));
        }

        [Test]
        public void Tamales_LaVaporeraRindeMasPeroTarda()
        {
            var s = Sim(LevelConfig.Tamales, 1);
            Assert.AreEqual(s.Rules.PotPortions + 3, s.PotSize);
            Assert.IsTrue(s.CookTime > s.Rules.CookTime * TaqueroDef.Guero.CookMultiplier);
        }

        [Test]
        public void Tamales_ElDulceNoLlevaCremaNiQueso()
        {
            var s = Sim(LevelConfig.Tamales, 4);
            s.Apply(Command.AddTortilla(Base.Hoja));
            s.Apply(Command.ServeMeat(Meat.TamalDulce));
            Assert.AreEqual(CommandResult.NotForThis, s.Apply(Command.ToggleExtra(Extra.Crema)));
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.AtoleFresa)));
            Assert.AreEqual(CommandResult.OneSalsaOnly, s.Apply(Command.ToggleExtra(Extra.Champurrado)), "un solo atole");
            foreach (var lvl in LevelConfig.Tamales)
            {
                var t = new KitchenSimulation(lvl, TaqueroDef.Tio, 3);
                for (int i = 0; i < 60; i++)
                {
                    var o = t.NewOrder();
                    if (o.Meat == Meat.TamalDulce) Assert.AreEqual(Extra.None, o.Extras & (Extra.Crema | Extra.Queso), lvl.Name);
                    Assert.IsTrue(System.Array.IndexOf(lvl.OrderBases, o.Base) >= 0, lvl.Name);
                }
            }
        }

        [Test]
        public void Feria_CadaAntojoConSuBaseYSusReglas()
        {
            var s = Sim(LevelConfig.Feria, 2);
            s.Apply(Command.ServeMeat(Meat.Elote));
            Assert.AreEqual(Base.Palito, s.Plates[0].Base);
            Assert.AreEqual(CommandResult.FirstMayo, s.Apply(Command.ToggleExtra(Extra.Queso)));
            Assert.AreEqual(CommandResult.NotForThis, s.Apply(Command.ToggleExtra(Extra.SalsaRoja)), "la salsa no va en el elote");
            s.Apply(Command.SelectPlate(1));
            s.Apply(Command.ServeMeat(Meat.Pastor));
            Assert.AreEqual(Base.Tortilla, s.Plates[1].Base);
            Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(Extra.SalsaRoja)));
            foreach (var lvl in LevelConfig.Feria)
            {
                var t = new KitchenSimulation(lvl, TaqueroDef.Tio, 5);
                for (int i = 0; i < 60; i++)
                {
                    var o = t.NewOrder();
                    var part = lvl.StallDef.PartFor(o.Meat);
                    Assert.AreEqual(lvl.StallDef.BaseFor(o.Meat), o.Base, lvl.Name);
                    foreach (var e in Extras.All)
                        if ((o.Extras & e) != 0) Assert.IsTrue(System.Array.IndexOf(part.AllExtras, e) >= 0, lvl.Name + " " + o);
                }
            }
        }

        [Test]
        public void CadaMundoNuevo_TieneDoce_Jefe_Mejoras_YCocineros()
        {
            foreach (var w in new[] { LevelConfig.Pozole, LevelConfig.Tamales, LevelConfig.Feria })
            {
                Assert.AreEqual(12, w.Length);
                Assert.IsTrue(w[11].BossAt > 0f);
                Assert.AreEqual(4, UpgradeDef.For(w[0].Stall).Length);
                Assert.AreEqual(3, TaqueroDef.For(w[0].Stall).Length);
                for (int i = 0; i < 12; i++)
                {
                    Assert.AreEqual(w[0].Stall, w[i].Stall);
                    Assert.IsTrue(w[i].Stars[0] < w[i].Stars[1] && w[i].Stars[1] < w[i].Stars[2], w[i].Name);
                }
            }
            Assert.AreEqual(6, LevelConfig.Worlds.Length);
        }

        [Test]
        public void ElBot_TerminaBienCadaMision()
        {
            // con reaccion de 0.6 s el bot debe sacar al menos 1 estrella en todas (si no, la mision esta rota)
            foreach (var w in new[] { LevelConfig.Pozole, LevelConfig.Tamales, LevelConfig.Feria })
                foreach (var lvl in w)
                    Assert.IsTrue(HintBot.MedianStars(lvl, TaqueroDef.Tio, 0.6f, 7) >= 1, lvl.Stall + " " + lvl.Name);
        }
    
        [Test]
        public void ArmarElPedidoAMano_SeEntregaBien_EnLosTresMundos()
        {
            // Como lo hace el jugador: base de su pila (o sola), lo principal y cada extra; luego tocar al cliente.
            foreach (var w in new[] { LevelConfig.Pozole, LevelConfig.Tamales, LevelConfig.Feria })
                foreach (var lvl in w)
                {
                    var s = new KitchenSimulation(lvl, TaqueroDef.Tio, 11);
                    int served = 0;
                    for (int step = 0; step < 4000 && served < 15 && !s.IsFinished; step++)
                    {
                        s.Tick(0.05f);
                        for (int c = 0; c < s.Customers.Length; c++)
                        {
                            var cu = s.Customers[c];
                            if (cu == null) continue;
                            var o = cu.Order;
                            foreach (var pot in s.Pots) if (pot.Portions <= 0 && !pot.IsRefilling) s.Apply(Command.RefillMeat(pot.Meat));
                            if (s.Pot(o.Meat).Portions < Plate.ScoopsFor(o.Base)) continue;
                            s.Apply(Command.SelectPlate(0));
                            if (s.Plates[0].HasMeat || s.Plates[0].Tortilla) s.Apply(Command.TrashPlate(0));
                            if (lvl.StallDef.InstantBase) Assert.AreEqual(CommandResult.Ok, s.Apply(Command.AddTortilla(o.Base)), lvl.Name);
                            for (int k = 0; k < Plate.ScoopsFor(o.Base); k++) Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ServeMeat(o.Meat)), lvl.Name + " " + o);
                            foreach (var e in Extras.All)
                                if ((o.Extras & e) != 0) Assert.AreEqual(CommandResult.Ok, s.Apply(Command.ToggleExtra(e)), lvl.Name + " " + o + " " + e);
                            Assert.IsTrue(s.Plates[0].Matches(o), lvl.Name + " " + o + " plato " + s.Plates[0].Base + " " + s.Plates[0].Meat + " " + s.Plates[0].Extras);
                            Assert.AreEqual(CommandResult.Served, s.Apply(Command.Deliver(c)), lvl.Name + " " + o);
                            served++;
                        }
                    }
                    Assert.IsTrue(served > 0, lvl.Name);
                }
        }
}
}
