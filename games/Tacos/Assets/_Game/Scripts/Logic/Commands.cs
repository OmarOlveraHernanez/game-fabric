namespace Tacos.Logic
{
    /// <summary>
    /// Todo lo que el jugador puede hacer es un comando. La partida offline y la futura partida en linea
    /// usan los mismos comandos: en linea el telefono los envia y el servidor los aplica.
    /// </summary>
    public enum CommandType
    {
        AddTortilla,   // poner una tortilla en el comal
        TakeTortilla,  // Arg = lugar del comal
        StartMeat,     // Arg = (int)Meat
        TakeMeat,      // Arg = lugar de la plancha
        SelectPlate,   // Arg = plato
        ToggleExtra,   // Arg = (int)Extra, al plato elegido
        TrashPlate,    // Arg = plato
        Deliver        // Arg = lugar del cliente
    }

    public readonly struct Command
    {
        public readonly CommandType Type;
        public readonly int Arg;
        public Command(CommandType type, int arg = 0) { Type = type; Arg = arg; }

        public static Command AddTortilla() => new Command(CommandType.AddTortilla);
        public static Command TakeTortilla(int slot) => new Command(CommandType.TakeTortilla, slot);
        public static Command StartMeat(Meat m) => new Command(CommandType.StartMeat, (int)m);
        public static Command TakeMeat(int slot) => new Command(CommandType.TakeMeat, slot);
        public static Command SelectPlate(int plate) => new Command(CommandType.SelectPlate, plate);
        public static Command ToggleExtra(Extra e) => new Command(CommandType.ToggleExtra, (int)e);
        public static Command TrashPlate(int plate) => new Command(CommandType.TrashPlate, plate);
        public static Command Deliver(int customer) => new Command(CommandType.Deliver, customer);

        public override string ToString() => Type + "(" + Arg + ")";
    }

    public enum CommandResult
    {
        Ok,
        NotRunning,
        InvalidArgument,
        NotInThisLevel,   // por ejemplo, tortillas en un turno sin comal
        ComalFull,
        EmptySlot,
        TortillaCold,
        TortillaBurned,   // se tira sola
        NoFreePlate,
        GrillFull,
        MeatRaw,
        MeatBurned,       // se tira sola
        NeedTortillaFirst,
        PlatesFull,
        NeedMeatFirst,
        OneSalsaOnly,
        NoCustomer,
        NoPlateReady,
        Served,
        WrongOrder
    }
}
