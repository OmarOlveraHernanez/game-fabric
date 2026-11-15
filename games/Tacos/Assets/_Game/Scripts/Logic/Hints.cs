namespace Tacos.Logic
{
    public enum HintKind
    {
        WaitCustomer,
        StartMeat,     // Meat
        WaitMeat,      // Meat
        TakeMeat,      // Slot
        AddTortilla,
        WaitTortilla,
        TakeTortilla,  // Slot
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
                if (s.SelectedPlate != heading)
                    return new Hint(HintKind.SelectPlate, target, Command.SelectPlate(heading), plate: heading);
                foreach (var e in Extras.All)
                    if (Extras.Has(order.Extras, e) && !Extras.Has(s.Plates[heading].Extras, e))
                        return new Hint(HintKind.AddExtra, target, Command.ToggleExtra(e), extra: e);
            }

            // 3. Hace falta un plato sin carne; si no hay, se tira uno que no le sirve a nadie
            int free = -1;
            for (int p = 0; p < s.Plates.Length; p++) if (!s.Plates[p].HasMeat) { free = p; break; }
            if (free < 0)
            {
                int trash = 0;
                for (int p = 0; p < s.Plates.Length; p++) if (!UsefulForSomeone(s, s.Plates[p])) { trash = p; break; }
                return new Hint(HintKind.TrashPlate, target, Command.TrashPlate(trash), plate: trash);
            }

            // 4. La carne: primero ponerla (es lo que mas tarda)
            int grill = -1;
            for (int g = 0; g < s.Grill.Length; g++)
                if (s.Grill[g] != null && s.Grill[g].Meat == order.Meat && s.MeatState(s.Grill[g]) != CookState.Burned) { grill = g; break; }
            if (grill < 0)
            {
                for (int g = 0; g < s.Grill.Length; g++)
                    if (s.Grill[g] != null && s.MeatState(s.Grill[g]) == CookState.Burned)
                        return new Hint(HintKind.TakeMeat, target, Command.TakeMeat(g), slot: g); // tirar la quemada
                if (System.Array.IndexOf(s.Grill, null) < 0) return new Hint(HintKind.WaitMeat, target, meat: order.Meat);
                return new Hint(HintKind.StartMeat, target, Command.StartMeat(order.Meat), meat: order.Meat);
            }

            // 5. La tortilla (solo en turnos con comal)
            bool plateWithTortilla = false;
            foreach (var pl in s.Plates) if (pl.Tortilla && !pl.HasMeat) plateWithTortilla = true;
            if (s.Level.NeedsTortilla && !plateWithTortilla)
            {
                int raw = -1;
                for (int c = 0; c < s.Comal.Length; c++)
                {
                    var t = s.Comal[c];
                    if (t == null) continue;
                    var st = s.TortillaState(t);
                    if (st != CookState.Raw) return new Hint(HintKind.TakeTortilla, target, Command.TakeTortilla(c), slot: c);
                    raw = c;
                }
                if (raw >= 0) return new Hint(HintKind.WaitTortilla, target, slot: raw);
                return new Hint(HintKind.AddTortilla, target, Command.AddTortilla());
            }

            // 6. Pasar la carne al plato cuando este lista
            if (s.MeatState(s.Grill[grill]) == CookState.Ready)
                return new Hint(HintKind.TakeMeat, target, Command.TakeMeat(grill), slot: grill);
            return new Hint(HintKind.WaitMeat, target, meat: order.Meat, slot: grill);
        }

        static bool UsefulForSomeone(KitchenSimulation s, Plate p)
        {
            foreach (var c in s.Customers) if (c != null && p.IsHeadingTo(c.Order)) return true;
            return false;
        }
    }
}
