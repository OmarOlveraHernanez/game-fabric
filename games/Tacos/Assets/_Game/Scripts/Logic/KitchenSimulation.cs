using System;
using System.Collections.Generic;

namespace Tacos.Logic
{
    public enum CookState { Raw, Ready, Burned, Flip } // Flip: un lado listo, hay que voltearla (garnachas)

    public sealed class Customer
    {
        public int Id;
        public Order Order;
        public float Patience;
        public float MaxPatience;
        public bool Rush; // apurado: menos paciencia, propina doble
        public CustomerKind Kind;
        public int Remaining = 1;  // tacos que faltan (el mariachi pide 3)
        public bool Changed;       // el borrachito ya cambio su pedido
        public Queue<Order> Next;  // jefe final: los platos que pide despues del actual
        public float PatienceRatio => MaxPatience <= 0f ? 0f : Math.Max(0f, Patience / MaxPatience);
    }

    public sealed class ComalItem
    {
        public float Age;           // segundos en el lado que esta cocinandose
        public Base Base = Base.Tortilla;
        public bool Flipped;        // ya se volteo (solo en puestos con Flip)
    }
    /// <summary>Un cazo de carne en la choricera (o el trompo). Siempre caliente; se vacia al servir.</summary>
    public sealed class MeatPot
    {
        public Meat Meat;
        public int Portions;
        public float Refill; // segundos que faltan para terminar de rellenar (0 = no se esta rellenando)
        public bool IsRefilling => Refill > 0f;
    }

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
        /// <summary>Tortillas que se quemaron (aunque no se hayan tirado).</summary>
        public int Burned { get; private set; }
        public int SelectedPlate { get; private set; }

        // ---- racha y power-ups ----
        /// <summary>Tacos bien entregados seguidos (se rompe con un error, un cliente que se va o algo quemado).</summary>
        public int Streak { get; private set; }
        public int BestStreak { get; private set; }
        /// <summary>Carga del siguiente power-up, de 0 a 1.</summary>
        public float Charge { get; private set; }
        readonly int[] _stock = new int[PowerUps.All.Length];
        public int Stock(PowerUp p) => _stock[(int)p];
        public float NopalLeft { get; private set; }
        public float SalsaLeft { get; private set; }
        /// <summary>Power-up que cayo del cielo (null si no hay) y cuanto le queda antes de irse.</summary>
        public PowerUp? Pickup { get; private set; }
        public float PickupLeft { get; private set; }
        float _pickupTimer;
        int _nextGrant;
        /// <summary>La vista lo pone mientras el tutorial explica algo: la paciencia no baja.</summary>
        public bool PausePatience;

        public readonly Customer[] Customers;
        public readonly ComalItem[] Comal;
        public readonly MeatPot[] Pots;
        public readonly Plate[] Plates;

        public float CookTime => Rules.CookTime * Taquero.CookMultiplier;
        /// <summary>La regla especial de la mision se rompio: la mision no da estrellas.</summary>
        public bool RuleBroken =>
            (Level.Rule == MissionRule.NoErrors && Failed > 0) || (Level.Rule == MissionRule.NoBurn && Burned > 0) || BossFailed;

        // ---- jefe final ----
        bool _bossSpawned;
        public bool BossDone { get; private set; }
        public bool BossFailed { get; private set; }
        /// <summary>Lugar donde se para el jefe (el de en medio si cabe).</summary>
        public int BossSlot => Math.Min(1, Math.Max(0, Level.MaxCustomers - 1));
        public bool BossWaiting => _bossSpawned && !BossDone && !BossFailed;
        /// <summary>Llego el jefe final (lugar).</summary>
        public event Action<int> BossArrived;
        public event Action BossServed;
        public int Stars => RuleBroken ? 0 : StarsFor(Coins);

        /// <summary>Llego un cliente (lugar).</summary>
        public event Action<int> CustomerArrived;
        /// <summary>Taco correcto entregado (lugar, pago total, propina).</summary>
        public event Action<int, int, int> OrderServed;
        /// <summary>Taco equivocado (lugar).</summary>
        public event Action<int> OrderWrong;
        /// <summary>Un cliente se fue por esperar demasiado (lugar).</summary>
        public event Action<int> CustomerLeft;
        /// <summary>Se quemo una tortilla en el domo.</summary>
        public event Action ItemBurned;
        /// <summary>Un cazo termino de rellenarse (carne).</summary>
        public event Action<Meat> PotRefilled;
        public event Action Finished;
        /// <summary>Se cargo o se agarro un power-up (cual, si vino de la barra).</summary>
        public event Action<PowerUp, bool> PowerUpGained;
        /// <summary>Se uso un power-up.</summary>
        public event Action<PowerUp> PowerUpUsed;
        public event Action<PowerUp> PickupSpawned;
        public event Action PickupExpired;
        /// <summary>Se rompio una racha de al menos 3 (cuantos llevaba).</summary>
        public event Action<int> StreakBroken;
        /// <summary>El borrachito cambio su pedido (lugar).</summary>
        public event Action<int> OrderChanged;

        public KitchenSimulation(LevelConfig level, TaqueroDef taquero, int seed, KitchenRules rules = null)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            Taquero = taquero ?? throw new ArgumentNullException(nameof(taquero));
            Rules = rules ?? new KitchenRules();
            _rng = new Random(seed);
            Customers = new Customer[Rules.MaxCustomerSlots];
            Comal = new ComalItem[Taquero.ComalSlots];
            Pots = new MeatPot[Level.Meats.Length];
            for (int i = 0; i < Pots.Length; i++)
                Pots[i] = new MeatPot { Meat = Level.Meats[i], Portions = Rules.PotPortions };
            Plates = new Plate[Rules.PlateCount];
            for (int i = 0; i < Plates.Length; i++)
            {
                Plates[i] = new Plate();
                ClearPlate(Plates[i]); // sin comal, los platos ya traen tortilla
            }
            _spawnTimer = Rules.FirstCustomerDelay;
            _pickupTimer = NextPickupGap();
        }

        float NextPickupGap() => Rules.PickupGapMin + (float)_rng.NextDouble() * (Rules.PickupGapMax - Rules.PickupGapMin);

        public int StarsFor(int coins)
        {
            int n = 0;
            foreach (var s in Level.Stars) if (coins >= s) n++;
            return n;
        }

        /// <summary>Segundos para que un lado de la base este listo (la tortilla de siempre, mas en las masas gruesas).</summary>
        public float HotTime => Rules.TortillaHotTime * Level.StallDef.CookScale;

        public CookState TortillaState(ComalItem t)
        {
            var def = Level.StallDef;
            float hot = HotTime;
            if (!def.Flip)
                return t.Age >= Rules.TortillaBurnTime * def.CookScale ? CookState.Burned : t.Age >= hot ? CookState.Ready : CookState.Raw;
            // Con voltear: el primer lado se quema si no la volteas a tiempo; el segundo aguanta el doble
            float burn = hot + def.FlipWindow * (t.Flipped ? 2f : 1f);
            if (t.Age >= burn) return CookState.Burned;
            if (t.Age >= hot) return t.Flipped ? CookState.Ready : CookState.Flip;
            return CookState.Raw;
        }

        public MeatPot Pot(Meat m)
        {
            foreach (var p in Pots) if (p.Meat == m) return p;
            return null;
        }

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
                bool was = TortillaState(t) == CookState.Burned;
                t.Age += dt;
                if (!was && TortillaState(t) == CookState.Burned) { Burned++; ItemBurned?.Invoke(); BreakStreak(); }
            }
            foreach (var p in Pots)
            {
                if (!p.IsRefilling) continue;
                p.Refill -= dt;
                if (p.Refill <= 0f) { p.Refill = 0f; p.Portions = Rules.PotPortions; PotRefilled?.Invoke(p.Meat); }
            }

            if (NopalLeft > 0f) NopalLeft = Math.Max(0f, NopalLeft - dt);
            if (SalsaLeft > 0f) SalsaLeft = Math.Max(0f, SalsaLeft - dt);
            bool frozen = NopalLeft > 0f || PausePatience;
            for (int i = 0; i < Customers.Length; i++)
            {
                var c = Customers[i];
                if (c == null || frozen) continue;
                c.Patience -= dt;
                if (c.Kind == CustomerKind.Borrachito && !c.Changed && c.PatienceRatio < Rules.BorrachitoChangeAt)
                {
                    c.Changed = true;
                    c.Order = NewOrder();
                    c.Patience = Math.Max(c.Patience, c.MaxPatience * 0.8f); // le da tiempo de pedir otra vez
                    OrderChanged?.Invoke(i);
                }
                if (c.Patience <= 0f)
                {
                    if (c.Kind == CustomerKind.Jefe) BossFailed = true;
                    Customers[i] = null;
                    Failed++;
                    CustomerLeft?.Invoke(i);
                    BreakStreak();
                }
            }

            if (Level.PowerUps.Length > 0)
            {
                if (Pickup.HasValue)
                {
                    PickupLeft -= dt;
                    if (PickupLeft <= 0f) { Pickup = null; PickupLeft = 0f; PickupExpired?.Invoke(); }
                }
                else
                {
                    _pickupTimer -= dt;
                    if (_pickupTimer <= 0f)
                    {
                        Pickup = Level.PowerUps[_rng.Next(Level.PowerUps.Length)];
                        PickupLeft = Rules.PickupLife;
                        _pickupTimer = NextPickupGap();
                        PickupSpawned?.Invoke(Pickup.Value);
                    }
                }
            }

            if (Level.BossAt > 0f && !_bossSpawned && Time >= Level.BossAt && Customers[BossSlot] == null) SpawnBoss();

            _spawnTimer -= dt;
            if (_spawnTimer <= 0f)
            {
                if (Time < Level.Duration) TrySpawn(); // en tiempo extra (esperando al jefe) ya no llega nadie
                _spawnTimer = NextGap();
            }

            // Si se acaba el tiempo con el jefe esperando, sigue hasta que lo atiendas o se vaya
            if (Time >= Level.Duration && !BossWaiting)
            {
                if (Level.BossAt > 0f && !BossDone) BossFailed = true;
                IsFinished = true;
                Finished?.Invoke();
            }
        }

        float NextGap() => Level.GapMin + (float)_rng.NextDouble() * (Level.GapMax - Level.GapMin);

        void TrySpawn()
        {
            int slot = -1;
            bool keepBossSlot = Level.BossAt > 0f && !_bossSpawned && Time >= Level.BossAt - 6f; // se le aparta su lugar
            for (int i = 0; i < Level.MaxCustomers && i < Customers.Length; i++)
                if (Customers[i] == null && !(keepBossSlot && i == BossSlot)) { slot = i; break; }
            if (slot < 0) return;
            var kind = CustomerKind.Normal;
            if (Level.Specials.Length > 0 && _rng.NextDouble() < Level.SpecialChance)
                kind = Level.Specials[_rng.Next(Level.Specials.Length)];
            bool rush = kind == CustomerKind.Normal && Level.RushChance > 0f && _rng.NextDouble() < Level.RushChance;
            float max = Level.Patience * Taquero.PatienceMultiplier * Rules.PatienceMultiplier
                        * (Time > Level.LateAfter ? Level.LatePatienceFactor : 1f) * (rush ? Rules.RushPatienceFactor : 1f)
                        * (kind == CustomerKind.Mariachi ? Rules.MariachiPatience : 1f);
            Customers[slot] = new Customer
            {
                Id = _nextId++, Order = NewOrder(), Patience = max, MaxPatience = max, Rush = rush, Kind = kind,
                Remaining = kind == CustomerKind.Mariachi ? Rules.MariachiTacos : 1
            };
            CustomerArrived?.Invoke(slot);
        }

        void SpawnBoss()
        {
            _bossSpawned = true;
            float max = Level.Patience * Taquero.PatienceMultiplier * Rules.PatienceMultiplier * Rules.BossPatience;
            int n = Math.Max(1, Level.BossItems);
            var next = new Queue<Order>();
            for (int i = 1; i < n; i++) next.Enqueue(NewOrder());
            Customers[BossSlot] = new Customer
            {
                Id = _nextId++, Order = NewOrder(), Patience = max, MaxPatience = max, Kind = CustomerKind.Jefe, Remaining = n, Next = next
            };
            CustomerArrived?.Invoke(BossSlot);
            BossArrived?.Invoke(BossSlot);
        }

        /// <summary>Pedido al azar: carne + de MinExtras a MaxExtras extras, a lo mas una salsa y pina solo con pastor.</summary>
        public Order NewOrder()
        {
            var meat = Level.OnlyMeat != Meat.None ? Level.OnlyMeat : Level.Meats[_rng.Next(Level.Meats.Length)];
            var def = Level.StallDef;
            var pool = new List<Extra>(def.Common);
            if (def.Special != Extra.None && meat == def.SpecialMeat) pool.Add(def.Special);
            if (def.OneOf.Length == 2) pool.Add(_rng.NextDouble() < 0.5 ? def.OneOf[0] : def.OneOf[1]);
            else if (def.OneOf.Length > 0) pool.Add(def.OneOf[_rng.Next(def.OneOf.Length)]);
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
            if ((extras & def.NeedsFirst) != 0) extras |= def.First; // nadie pide queso sin su mayonesa
            var bases = Level.OrderBases;
            var b = def.AutoBase ? def.BaseFor(meat)
                  : bases.Length == 1 ? bases[0] : bases[_rng.Next(bases.Length)]; // con una sola base no se gasta azar (la taqueria queda igual)
            return new Order(b, meat, extras);
        }

        // ---------------- comandos ----------------

        public CommandResult Apply(Command cmd)
        {
            if (IsFinished) return CommandResult.NotRunning;
            switch (cmd.Type)
            {
                case CommandType.AddTortilla: return AddTortilla((Base)cmd.Arg);
                case CommandType.FlipItem: return FlipItem(cmd.Arg);
                case CommandType.TakeTortilla: return TakeTortilla(cmd.Arg);
                case CommandType.RefillMeat: return RefillMeat((Meat)cmd.Arg);
                case CommandType.ServeMeat: return ServeMeat((Meat)cmd.Arg);
                case CommandType.SelectPlate:
                    if (!ValidIndex(cmd.Arg, Plates.Length)) return CommandResult.InvalidArgument;
                    SelectedPlate = cmd.Arg;
                    return CommandResult.Ok;
                case CommandType.ToggleExtra: return ToggleExtra((Extra)cmd.Arg);
                case CommandType.TrashPlate:
                    if (!ValidIndex(cmd.Arg, Plates.Length)) return CommandResult.InvalidArgument;
                    ClearPlate(Plates[cmd.Arg]);
                    return CommandResult.Ok;
                case CommandType.Deliver: return Deliver(cmd.Arg);
                case CommandType.TakePickup: return TakePickup();
                case CommandType.UsePowerUp: return UsePowerUp(cmd.Arg);
                default: return CommandResult.InvalidArgument;
            }
        }

        static bool ValidIndex(int i, int length) => i >= 0 && i < length;

        CommandResult AddTortilla(Base b)
        {
            if (!Level.NeedsTortilla) return CommandResult.NotInThisLevel;
            if (b == Base.None) b = Level.OrderBases[0];
            if (Array.IndexOf(Level.StallDef.Bases, b) < 0) return CommandResult.InvalidArgument;
            int i = Array.IndexOf(Comal, null);
            if (i < 0) return CommandResult.ComalFull;
            Comal[i] = new ComalItem { Base = b };
            return CommandResult.Ok;
        }

        CommandResult FlipItem(int slot)
        {
            if (!Level.StallDef.Flip) return CommandResult.NotInThisLevel;
            if (!ValidIndex(slot, Comal.Length)) return CommandResult.InvalidArgument;
            var t = Comal[slot];
            if (t == null) return CommandResult.EmptySlot;
            if (t.Flipped) return CommandResult.AlreadyFlipped;
            var state = TortillaState(t);
            if (state == CookState.Burned) { Comal[slot] = null; return CommandResult.TortillaBurned; }
            if (state == CookState.Raw) return CommandResult.TortillaCold;
            t.Flipped = true;
            t.Age = 0f;
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
            if (state == CookState.Flip) return CommandResult.NeedsFlip;
            int p = Destination(pl => !pl.Tortilla);
            if (p < 0) return CommandResult.NoFreePlate;
            Plates[p].Base = t.Base;
            SelectedPlate = p;
            Comal[slot] = null;
            return CommandResult.Ok;
        }

        CommandResult RefillMeat(Meat meat)
        {
            var pot = Pot(meat);
            if (pot == null) return CommandResult.InvalidArgument;
            if (pot.IsRefilling) return CommandResult.PotRefilling;
            if (pot.Portions >= Rules.PotPortions) return CommandResult.PotFull;
            pot.Refill = CookTime;
            return CommandResult.Ok;
        }

        /// <summary>Plato vacio: con tortilla si no hay comal; en los elotes la base la pone lo principal.</summary>
        void ClearPlate(Plate p)
        {
            if (Level.StallDef.AutoBase) p.Clear(Base.None);
            else p.Clear(!Level.NeedsTortilla);
        }

        CommandResult ServeMeat(Meat meat)
        {
            var pot = Pot(meat);
            if (pot == null) return CommandResult.InvalidArgument;
            if (pot.IsRefilling) return CommandResult.PotRefilling;
            if (pot.Portions <= 0) return CommandResult.PotEmpty;
            bool auto = Level.StallDef.AutoBase;
            int p = Destination(pl => (pl.Tortilla || auto) && !pl.HasMeat);
            if (p < 0)
                return FindPlate(pl => !pl.HasMeat) >= 0 ? CommandResult.NeedTortillaFirst : CommandResult.PlatesFull;
            if (auto) Plates[p].Base = Level.StallDef.BaseFor(meat);
            Plates[p].Meat = meat;
            SelectedPlate = p;
            pot.Portions--;
            return CommandResult.Ok;
        }

        CommandResult ToggleExtra(Extra e)
        {
            var def = Level.StallDef;
            if (Array.IndexOf(def.AllExtras, e) < 0) return CommandResult.InvalidArgument;
            var pl = Plates[SelectedPlate];
            if (!pl.HasMeat) return CommandResult.NeedMeatFirst;
            if ((pl.Extras & e) != 0)
            {
                // la mayonesa no se quita si ya tiene queso o chile encima
                if (e == def.First && (pl.Extras & def.NeedsFirst) != 0) return CommandResult.FirstMayo;
                pl.Extras &= ~e;
                return CommandResult.Ok;
            }
            if ((e & def.NeedsFirst) != 0 && (pl.Extras & def.First) == 0) return CommandResult.FirstMayo;
            var one = def.OneOfMask;
            if ((e & one) != 0 && (pl.Extras & one) != 0) return CommandResult.OneSalsaOnly;
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
            ClearPlate(Plates[p]);
            if (correct)
            {
                c.Remaining--;
                bool done = c.Remaining <= 0;
                ServeOne(slot, c, done);
                if (!done)
                {
                    c.Patience = Math.Min(c.MaxPatience, c.Patience + c.MaxPatience * 0.25f);
                    if (c.Next != null && c.Next.Count > 0) { c.Order = c.Next.Dequeue(); OrderChanged?.Invoke(slot); }
                }
                return CommandResult.Served;
            }
            if (c.Kind == CustomerKind.Jefe)
            {
                // Al jefe no se le corre: se queda, pero se desespera
                c.Patience -= c.MaxPatience * Rules.BossWrongPenalty;
                Failed++;
                OrderWrong?.Invoke(slot);
                BreakStreak();
                return CommandResult.WrongOrder;
            }
            Customers[slot] = null; // con el taco equivocado se va (el critico tambien)
            _spawnTimer = Math.Min(_spawnTimer, Level.GapMin);
            Failed++;
            OrderWrong?.Invoke(slot);
            BreakStreak();
            return CommandResult.WrongOrder;
        }

        /// <summary>Cobra un taco correcto, sube la racha y carga power-ups. last = el cliente ya se va contento.</summary>
        void ServeOne(int slot, Customer c, bool last)
        {
            int tip = (int)Math.Round(Rules.MaxTip * c.PatienceRatio, MidpointRounding.AwayFromZero)
                      * (c.Rush ? Rules.RushTipMultiplier : 1) * (SalsaLeft > 0f ? 2 : 1);
            int pay = Rules.BasePay + Rules.PayPerExtra * Extras.Count(c.Order.Extras) + tip;
            if (c.Kind == CustomerKind.Critico) pay *= Rules.CriticoMultiplier;
            if (c.Kind == CustomerKind.Mariachi && last) pay += Rules.MariachiBonus;
            if (c.Kind == CustomerKind.Jefe && last) { pay += Rules.BossBonus; BossDone = true; }
            Coins += pay;
            Served++;
            if (last)
            {
                Customers[slot] = null;
                _spawnTimer = Math.Min(_spawnTimer, Level.GapMin);
            }
            Streak++;
            BestStreak = Math.Max(BestStreak, Streak);
            if (Level.PowerUps.Length > 0)
            {
                Charge += (Streak >= 3 ? 1.5f : 1f) / Rules.ServesPerPowerUp;
                if (Charge >= 1f)
                {
                    Charge -= 1f;
                    var p = Level.PowerUps[_nextGrant++ % Level.PowerUps.Length];
                    Grant(p, false);
                }
            }
            OrderServed?.Invoke(slot, pay, tip);
            if (c.Kind == CustomerKind.Jefe && last) BossServed?.Invoke();
        }

        void Grant(PowerUp p, bool fromPickup)
        {
            _stock[(int)p] = Math.Min(Rules.MaxPowerUpStock, _stock[(int)p] + 1);
            PowerUpGained?.Invoke(p, fromPickup);
        }

        void BreakStreak()
        {
            int was = Streak;
            Streak = 0;
            if (was >= 3) StreakBroken?.Invoke(was);
        }

        CommandResult TakePickup()
        {
            if (!Pickup.HasValue) return CommandResult.NoPickup;
            var p = Pickup.Value;
            Pickup = null;
            PickupLeft = 0f;
            Grant(p, true);
            return CommandResult.Ok;
        }

        CommandResult UsePowerUp(int arg)
        {
            if (arg < 0 || arg >= _stock.Length) return CommandResult.InvalidArgument;
            var p = (PowerUp)arg;
            if ((p == PowerUp.Nopal && NopalLeft > 0f) || (p == PowerUp.Salsa && SalsaLeft > 0f)) return CommandResult.PowerUpActive;
            if (_stock[arg] <= 0) return CommandResult.NoPowerUp;
            switch (p)
            {
                case PowerUp.Cebollitas:
                {
                    // El taquero atiende solo al cliente con menos paciencia (al mariachi le da todos sus tacos)
                    int best = -1;
                    for (int i = 0; i < Customers.Length; i++)
                        if (Customers[i] != null && (best < 0 || Customers[i].PatienceRatio < Customers[best].PatienceRatio)) best = i;
                    if (best < 0) return CommandResult.NoCustomer;
                    _stock[arg]--;
                    PowerUpUsed?.Invoke(p);
                    var c = Customers[best];
                    while (Customers[best] == c) { c.Remaining--; ServeOne(best, c, c.Remaining <= 0); }
                    return CommandResult.Ok;
                }
                case PowerUp.Nopal:
                    NopalLeft = Rules.NopalTime;
                    break;
                case PowerUp.Salsa:
                    SalsaLeft = Rules.SalsaTime;
                    break;
                case PowerUp.Trompo:
                    foreach (var pot in Pots) { pot.Refill = 0f; pot.Portions = Rules.PotPortions; }
                {
                    // Todo lo del comal queda listo (y ya volteado), y los lugares vacios se llenan
                    bool flip = Level.StallDef.Flip;
                    foreach (var t in Comal)
                        if (t != null && TortillaState(t) != CookState.Burned && (flip ? !t.Flipped || t.Age < HotTime : t.Age < HotTime))
                        { t.Flipped = flip; t.Age = HotTime; }
                    if (Level.NeedsTortilla)
                    {
                        var bases = Level.OrderBases;
                        for (int i = 0; i < Comal.Length; i++)
                            if (Comal[i] == null) Comal[i] = new ComalItem { Age = HotTime, Base = bases[i % bases.Length], Flipped = flip };
                    }
                }
                    break;
            }
            _stock[arg]--;
            PowerUpUsed?.Invoke(p);
            return CommandResult.Ok;
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
