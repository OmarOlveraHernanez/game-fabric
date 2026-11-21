namespace Tacos.Logic
{
    public enum HintKind
    {
        WaitCustomer,
        RefillMeat,    // Meat: el cazo esta vacio
        WaitMeat,      // Meat: el cazo se esta rellenando
        ServeMeat,     // Meat
        AddTortilla,
        WaitTortilla,
        TakeTortilla,  // Slot
        FlipItem,      // Slot: voltear la base (garnachas)
        SelectPlate,   // Plate
        AddExtra,      // Extra
        TrashPlate,    // Plate
        Deliver        // Customer
    }

    /// <summary>Siguiente paso sugerido. Command es lo que haria el jugador (null si solo hay que esperar).</summary>
    public readonly struct Hint
    {
        public readonly HintKind Kind;
        public readonly int Customer;
        public readonly Meat Meat;
        public readonly Extra Extra;
        public readonly int Slot;
        public readonly int Plate;
        public readonly Command? Command;

        public Hint(HintKind kind, int customer, Command? command = null, Meat meat = Meat.None,
                    Extra extra = Extra.None, int slot = -1, int plate = -1)
        {
            Kind = kind; Customer = customer; Command = command; Meat = meat; Extra = extra; Slot = slot; Plate = plate;
        }
    }

    /// <summary>
    /// Pistas paso a paso para el cliente con menos paciencia. Las usa la pista del Turno 1
    /// y el bot de las pruebas de equilibrio (si el bot gana con ellas, el turno es justo).
    /// </summary>
    public static class Hints
    {
        public static Hint Next(KitchenSimulation s)
        {
            int target = -1;
            for (int i = 0; i < s.Customers.Length; i++)
            {
                var c = s.Customers[i];
                if (c != null && (target < 0 || c.Patience < s.Customers[target].Patience)) target = i;
            }
            if (target < 0) return new Hint(HintKind.WaitCustomer, -1);
            var order = s.Customers[target].Order;

            // 1. Ya hay un plato listo para este cliente
            if (s.HasPlateFor(target)) return new Hint(HintKind.Deliver, target, Command.Deliver(target));

            // 2. Un plato va camino a este pedido: faltan extras
            int heading = -1;
            for (int p = 0; p < s.Plates.Length; p++) if (s.Plates[p].IsHeadingTo(order)) { heading = p; break; }
            if (heading >= 0)
            {
                if (s.Plates[heading].NeedsScoop)
                {
                    // plato grande de pozole: falta el segundo cucharon
                    var hp = s.Pot(order.Meat);
                    if (hp != null && hp.Portions <= 0 && !hp.IsRefilling)
                        return new Hint(HintKind.RefillMeat, target, Command.RefillMeat(order.Meat), meat: order.Meat);
                    if (hp == null || hp.IsRefilling) return new Hint(HintKind.WaitMeat, target, meat: order.Meat);
                    return new Hint(HintKind.ServeMeat, target, Command.ServeMeat(order.Meat), meat: order.Meat);
                }
                if (s.SelectedPlate != heading)
                    return new Hint(HintKind.SelectPlate, target, Command.SelectPlate(heading), plate: heading);
                foreach (var e in Extras.All)
                    if (Extras.Has(order.Extras, e) && !Extras.Has(s.Plates[heading].Extras, e))
                        return new Hint(HintKind.AddExtra, target, Command.ToggleExtra(e), extra: e);
            }

            // 3. Hace falta un plato sin carne (y sin otra base); si no hay, se tira uno que no le sirve a nadie
            int free = -1;
            for (int p = 0; p < s.Plates.Length; p++)
            {
                var pl = s.Plates[p];
                if (!pl.HasMeat && (!pl.Tortilla || pl.Base == order.Base)) { free = p; break; }
            }
            if (free < 0)
            {
                int trash = 0;
                for (int p = 0; p < s.Plates.Length; p++) if (!UsefulForSomeone(s, s.Plates[p])) { trash = p; break; }
                return new Hint(HintKind.TrashPlate, target, Command.TrashPlate(trash), plate: trash);
            }

            // 4. Si el cazo de esa carne esta vacio, rellenarlo primero (es lo que mas tarda)
            var pot = s.Pot(order.Meat);
            if (pot != null && pot.Portions <= 0 && !pot.IsRefilling)
                return new Hint(HintKind.RefillMeat, target, Command.RefillMeat(order.Meat), meat: order.Meat);

            // 5. La base (solo en turnos con comal): la que pidio el cliente
            bool plateWithBase = false;
            foreach (var pl in s.Plates) if (pl.Base == order.Base && !pl.HasMeat) plateWithBase = true;
            if (s.Level.NeedsTortilla && !plateWithBase && s.Level.StallDef.InstantBase)
                return new Hint(HintKind.AddTortilla, target, Command.AddTortilla(order.Base)); // directo de la pila al plato
            if (s.Level.NeedsTortilla && !plateWithBase)
            {
                int raw = -1;
                for (int c = 0; c < s.Comal.Length; c++)
                {
                    var t = s.Comal[c];
                    if (t == null || t.Base != order.Base) continue;
                    var st = s.TortillaState(t);
                    if (st == CookState.Flip) return new Hint(HintKind.FlipItem, target, Command.FlipItem(c), slot: c);
                    if (st != CookState.Raw) return new Hint(HintKind.TakeTortilla, target, Command.TakeTortilla(c), slot: c);
                    raw = c;
                }
                if (raw >= 0) return new Hint(HintKind.WaitTortilla, target, slot: raw);
                // Si el comal esta lleno de bases que no pidio este cliente, se despeja la mas avanzada
                if (System.Array.IndexOf(s.Comal, null) < 0)
                {
                    int old = 0;
                    for (int c = 1; c < s.Comal.Length; c++) if (s.Comal[c].Age > s.Comal[old].Age) old = c;
                    var st = s.TortillaState(s.Comal[old]);
                    if (st == CookState.Flip) return new Hint(HintKind.FlipItem, target, Command.FlipItem(old), slot: old);
                    if (st == CookState.Raw) return new Hint(HintKind.WaitTortilla, target, slot: old);
                    return new Hint(HintKind.TakeTortilla, target, Command.TakeTortilla(old), slot: old);
                }
                return new Hint(HintKind.AddTortilla, target, Command.AddTortilla(order.Base));
            }

            // 6. Servir la carne del cazo
            if (pot == null || pot.IsRefilling) return new Hint(HintKind.WaitMeat, target, meat: order.Meat);
            return new Hint(HintKind.ServeMeat, target, Command.ServeMeat(order.Meat), meat: order.Meat);
        }

        static bool UsefulForSomeone(KitchenSimulation s, Plate p)
        {
            foreach (var c in s.Customers) if (c != null && p.IsHeadingTo(c.Order)) return true;
            return false;
        }
    }
}
