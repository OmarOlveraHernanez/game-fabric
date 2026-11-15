using System;
using System.Collections.Generic;

namespace Tacos.Logic
{
    public enum CookState { Raw, Ready, Burned }

    public sealed class Customer
    {
        public int Id;
        public Order Order;
        public float Patience;
        public float MaxPatience;
        public bool Rush; // apurado: menos paciencia, propina doble
        public float PatienceRatio => MaxPatience <= 0f ? 0f : Math.Max(0f, Patience / MaxPatience);
    }

    public sealed class ComalItem { public float Age; }
    public sealed class GrillItem { public Meat Meat; public float Age; }

    /// <summary>
    /// Un turno completo en la taqueria, sin Unity (se prueba en EditMode).
    /// La vista llama Tick() cada cuadro y Apply() con lo que toca el jugador; despues solo dibuja el estado.
    /// </summary>
    public sealed class KitchenSimulation
    {
        readonly Random _rng;
        float _spawnTimer;
        int _nextId = 1;

        public KitchenRules Rules { get; }
        public LevelConfig Level { get; }
        public TaqueroDef Taquero { get; }

        public float Time { get; private set; }
        public float TimeLeft => Math.Max(0f, Level.Duration - Time);
        public bool IsFinished { get; private set; }
        public int Coins { get; private set; }
        public int Served { get; private set; }
        public int Failed { get; private set; }
        /// <summary>Tortillas o carnes que se quemaron (aunque no se hayan tirado).</summary>
        public int Burned { get; private set; }
        public int SelectedPlate { get; private set; }

        public readonly Customer[] Customers;
        public readonly ComalItem[] Comal;
        public readonly GrillItem[] Grill;
        public readonly Plate[] Plates;

        public float CookTime => Rules.CookTime * Taquero.CookMultiplier;
        /// <summary>La regla especial de la mision se rompio: la mision no da estrellas.</summary>
        public bool RuleBroken =>
            (Level.Rule == MissionRule.NoErrors && Failed > 0) || (Level.Rule == MissionRule.NoBurn && Burned > 0);
        public int Stars => RuleBroken ? 0 : StarsFor(Coins);

        /// <summary>Llego un cliente (lugar).</summary>
        public event Action<int> CustomerArrived;
        /// <summary>Taco correcto entregado (lugar, pago total, propina).</summary>
        public event Action<int, int, int> OrderServed;
        /// <summary>Taco equivocado (lugar).</summary>
        public event Action<int> OrderWrong;
        /// <summary>Un cliente se fue por esperar demasiado (lugar).</summary>
        public event Action<int> CustomerLeft;
        /// <summary>Algo se quemo en el comal o la plancha.</summary>
        public event Action ItemBurned;
        public event Action Finished;

        public KitchenSimulation(LevelConfig level, TaqueroDef taquero, int seed, KitchenRules rules = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Taquero = taquero ?? throw new ArgumentNullException(nameof(taquero));
            Rules = rules ?? new KitchenRules();
            _rng = new Random(seed);
            Customers = new Customer[Rules.MaxCustomerSlots];
            Comal = new ComalItem[Taquero.ComalSlots];
            Grill = new GrillItem[Rules.GrillSlots];
            Plates = new Plate[Rules.PlateCount];
            for (int i = 0; i < Plates.Length; i++)
            {
                Plates[i] = new Plate();
                Plates[i].Clear(!Level.NeedsTortilla); // sin comal, los platos ya traen tortilla
            }
            _spawnTimer = Rules.FirstCustomerDelay;
        }

        public int StarsFor(int coins)
        {
            int n = 0;
            foreach (var s in Level.Stars) if (coins >= s) n++;
            return n;
        }

        public CookState TortillaState(ComalItem t) =>
            t.Age >= Rules.TortillaBurnTime ? CookState.Burned : t.Age >= Rules.TortillaHotTime ? CookState.Ready : CookState.Raw;

        public CookState MeatState(GrillItem m) =>
            m.Age >= Rules.MeatBurnTime ? CookState.Burned : m.Age >= CookTime ? CookState.Ready : CookState.Raw;

        /// <summary>Hay algun plato que ya coincide con el pedido de ese cliente.</summary>
        public bool HasPlateFor(int customer)
        {
            var c = Customers[customer];
            return c != null && FindPlate(p => p.Matches(c.Order)) >= 0;
        }

        // ---------------- tiempo ----------------

        public void Tick(float dt)
        {
            if (IsFinished || dt <= 0f) return;
            Time += dt;
            foreach (var t in Comal)
            {
                if (t == null) continue;
                bool was = t.Age >= Rules.TortillaBurnTime;
                t.Age += dt;
                if (!was && t.Age >= Rules.TortillaBurnTime) { Burned++; ItemBurned?.Invoke(); }
            }
            foreach (var m in Grill)
            {
                if (m == null) continue;
                bool was = m.Age >= Rules.MeatBurnTime;
                m.Age += dt;
                if (!was && m.Age >= Rules.MeatBurnTime) { Burned++; ItemBurned?.Invoke(); }
            }

            for (int i = 0; i < Customers.Length; i++)
            {
                var c = Customers[i];
                if (c == null) continue;
                c.Patience -= dt;
                if (c.Patience <= 0f)
                {
                    Customers[i] = null;
                    Failed++;
                    CustomerLeft?.Invoke(i);
                }
            }

            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                TrySpawn();
                _spawnTimer = NextGap();
            }

            if (Time >= Level.Duration)
            {
                IsFinished = true;
                Finished?.Invoke();
            }
        }

        float NextGap() => Level.GapMin + (float)_rng.NextDouble() * (Level.GapMax - Level.GapMin);

        void TrySpawn()
        {
            int slot = -1;
            for (int i = 0; i < Level.MaxCustomers && i < Customers.Length; i++)
                if (Customers[i] == null) { slot = i; break; }
            if (slot < 0) return;
            bool rush = Level.RushChance > 0f && _rng.NextDouble() < Level.RushChance;
            float max = Level.Patience * Taquero.PatienceMultiplier * Rules.PatienceMultiplier
                        * (Time > Level.LateAfter ? Level.LatePatienceFactor : 1f) * (rush ? Rules.RushPatienceFactor : 1f);
            Customers[slot] = new Customer { Id = _nextId++, Order = NewOrder(), Patience = max, MaxPatience = max, Rush = rush };
            CustomerArrived?.Invoke(slot);
        }

        /// <summary>Pedido al azar: carne + de MinExtras a MaxExtras extras, a lo mas una salsa y pina solo con pastor.</summary>
        public Order NewOrder()
        {
            var meat = Level.OnlyMeat != Meat.None ? Level.OnlyMeat : _rng.NextDouble() < 0.55 ? Meat.Pastor : Meat.Asada;
            var pool = new List<Extra> { Extra.Cebolla, Extra.Cilantro, Extra.Limon };
            if (meat == Meat.Pastor) pool.Add(Extra.Pina);
            pool.Add(_rng.NextDouble() < 0.5 ? Extra.SalsaVerde : Extra.SalsaRoja);
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
            }
            int max = Math.Max(1, Math.Min(Level.MaxExtras, pool.Count));
            int min = Math.Max(1, Math.Min(Level.MinExtras, max));
            int k = min + _rng.Next(max - min + 1);
            var extras = Extra.None;
            for (int i = 0; i < k; i++) extras |= pool[i];
            return new Order(meat, extras);
        }

        // ---------------- comandos ----------------

        public CommandResult Apply(Command cmd)
        {
            if (IsFinished) return CommandResult.NotRunning;
            switch (cmd.Type)
            {
                case CommandType.AddTortilla: return AddTortilla();
                case CommandType.TakeTortilla: return TakeTortilla(cmd.Arg);
                case CommandType.StartMeat: return StartMeat((Meat)cmd.Arg);
                case CommandType.TakeMeat: return TakeMeat(cmd.Arg);
                case CommandType.SelectPlate:
                    if (!ValidIndex(cmd.Arg, Plates.Length)) return CommandResult.InvalidArgument;
                    SelectedPlate = cmd.Arg;
                    return CommandResult.Ok;
                case CommandType.ToggleExtra: return ToggleExtra((Extra)cmd.Arg);
                case CommandType.TrashPlate:
                    if (!ValidIndex(cmd.Arg, Plates.Length)) return CommandResult.InvalidArgument;
                    Plates[cmd.Arg].Clear(!Level.NeedsTortilla);
                    return CommandResult.Ok;
                case CommandType.Deliver: return Deliver(cmd.Arg);
                default: return CommandResult.InvalidArgument;
            }
        }

        static bool ValidIndex(int i, int length) => i >= 0 && i < length;

        CommandResult AddTortilla()
        {
            if (!Level.NeedsTortilla) return CommandResult.NotInThisLevel;
            int i = Array.IndexOf(Comal, null);
            if (i < 0) return CommandResult.ComalFull;
            Comal[i] = new ComalItem();
            return CommandResult.Ok;
        }

        CommandResult TakeTortilla(int slot)
        {
            if (!ValidIndex(slot, Comal.Length)) return CommandResult.InvalidArgument;
            var t = Comal[slot];
            if (t == null) return CommandResult.EmptySlot;
            var state = TortillaState(t);
            if (state == CookState.Burned) { Comal[slot] = null; return CommandResult.TortillaBurned; }
            if (state == CookState.Raw) return CommandResult.TortillaCold;
            int p = Destination(pl => !pl.Tortilla);
            if (p < 0) return CommandResult.NoFreePlate;
            Plates[p].Tortilla = true;
            SelectedPlate = p;
            Comal[slot] = null;
            return CommandResult.Ok;
        }

        CommandResult StartMeat(Meat meat)
        {
            if (meat != Meat.Pastor && meat != Meat.Asada) return CommandResult.InvalidArgument;
            int i = Array.IndexOf(Grill, null);
            if (i < 0) return CommandResult.GrillFull;
            Grill[i] = new GrillItem { Meat = meat };
            return CommandResult.Ok;
        }

        CommandResult TakeMeat(int slot)
        {
            if (!ValidIndex(slot, Grill.Length)) return CommandResult.InvalidArgument;
            var m = Grill[slot];
            if (m == null) return CommandResult.EmptySlot;
            var state = MeatState(m);
            if (state == CookState.Burned) { Grill[slot] = null; return CommandResult.MeatBurned; }
            if (state == CookState.Raw) return CommandResult.MeatRaw;
            int p = Destination(pl => pl.Tortilla && !pl.HasMeat);
            if (p < 0)
                return FindPlate(pl => !pl.HasMeat) >= 0 ? CommandResult.NeedTortillaFirst : CommandResult.PlatesFull;
            Plates[p].Meat = m.Meat;
            SelectedPlate = p;
            Grill[slot] = null;
            return CommandResult.Ok;
        }

        CommandResult ToggleExtra(Extra e)
        {
            if (Array.IndexOf(Extras.All, e) < 0) return CommandResult.InvalidArgument;
            var pl = Plates[SelectedPlate];
            if (!pl.HasMeat) return CommandResult.NeedMeatFirst;
            if ((pl.Extras & e) != 0) { pl.Extras &= ~e; return CommandResult.Ok; }
            if ((e & Extras.Salsas) != 0 && (pl.Extras & Extras.Salsas) != 0) return CommandResult.OneSalsaOnly;
            pl.Extras |= e;
            return CommandResult.Ok;
        }

        /// <summary>
        /// Entrega al cliente el plato que coincide con su pedido, aunque no este elegido.
        /// Si ninguno coincide, entrega el plato elegido (si tiene carne) y cuenta como error.
        /// </summary>
        CommandResult Deliver(int slot)
        {
            if (!ValidIndex(slot, Customers.Length)) return CommandResult.InvalidArgument;
            var c = Customers[slot];
            if (c == null) return CommandResult.NoCustomer;
            int p = FindPlate(pl => pl.Matches(c.Order));
            bool correct = p >= 0;
            if (!correct)
            {
                if (!Plates[SelectedPlate].HasMeat) return CommandResult.NoPlateReady;
                p = SelectedPlate;
            }
            Customers[slot] = null;
            Plates[p].Clear(!Level.NeedsTortilla);
            _spawnTimer = Math.Min(_spawnTimer, Level.GapMin);
            if (correct)
            {
                int tip = (int)Math.Round(Rules.MaxTip * c.PatienceRatio, MidpointRounding.AwayFromZero)
                          * (c.Rush ? Rules.RushTipMultiplier : 1);
                int pay = Rules.BasePay + Rules.PayPerExtra * Extras.Count(c.Order.Extras) + tip;
                Coins += pay;
                Served++;
                OrderServed?.Invoke(slot, pay, tip);
                return CommandResult.Served;
            }
            Failed++;
            OrderWrong?.Invoke(slot);
            return CommandResult.WrongOrder;
        }

        /// <summary>El plato elegido si cumple; si no, el primero que cumpla.</summary>
        int Destination(Predicate<Plate> ok) => ok(Plates[SelectedPlate]) ? SelectedPlate : FindPlate(ok);

        int FindPlate(Predicate<Plate> ok)
        {
            for (int i = 0; i < Plates.Length; i++) if (ok(Plates[i])) return i;
            return -1;
        }
    }
}
