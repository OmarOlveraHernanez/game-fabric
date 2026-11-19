namespace Tacos.Logic
{
    /// <summary>
    /// Todo lo que el jugador puede hacer es un comando. La partida offline y la futura partida en linea
    /// usan los mismos comandos: en linea el telefono los envia y el servidor los aplica.
    /// </summary>
    public enum CommandType
    {
        AddTortilla,   // Arg = (int)Base: poner una base en el comal (0 = la de siempre del puesto)
        TakeTortilla,  // Arg = lugar del comal
        RefillMeat,    // Arg = (int)Meat: rellenar su cazo (tarda CookTime)
        ServeMeat,     // Arg = (int)Meat: una porcion del cazo al plato
        SelectPlate,   // Arg = plato
        ToggleExtra,   // Arg = (int)Extra, al plato elegido
        TrashPlate,    // Arg = plato
        Deliver,       // Arg = lugar del cliente
        TakePickup,    // agarrar el power-up que cayo del cielo
        UsePowerUp,    // Arg = (int)PowerUp
        FlipItem       // Arg = lugar del comal: voltear la base (garnachas)
    }

    public readonly struct Command
    {
        public readonly CommandType Type;
        public readonly int Arg;
        public Command(CommandType type, int arg = 0) { Type = type; Arg = arg; }

        public static Command AddTortilla(Base b = Base.None) => new Command(CommandType.AddTortilla, (int)b);
        public static Command FlipItem(int slot) => new Command(CommandType.FlipItem, slot);
        public static Command TakeTortilla(int slot) => new Command(CommandType.TakeTortilla, slot);
        public static Command RefillMeat(Meat m) => new Command(CommandType.RefillMeat, (int)m);
        public static Command ServeMeat(Meat m) => new Command(CommandType.ServeMeat, (int)m);
        public static Command SelectPlate(int plate) => new Command(CommandType.SelectPlate, plate);
        public static Command ToggleExtra(Extra e) => new Command(CommandType.ToggleExtra, (int)e);
        public static Command TrashPlate(int plate) => new Command(CommandType.TrashPlate, plate);
        public static Command Deliver(int customer) => new Command(CommandType.Deliver, customer);
        public static Command TakePickup() => new Command(CommandType.TakePickup);
        public static Command UsePowerUp(PowerUp p) => new Command(CommandType.UsePowerUp, (int)p);

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
        PotEmpty,         // el cazo no tiene porciones: hay que rellenarlo
        PotRefilling,     // el cazo se esta rellenando
        PotFull,          // el cazo ya esta lleno
        NeedTortillaFirst,
        PlatesFull,
        NeedMeatFirst,
        OneSalsaOnly,
        NoCustomer,
        NoPlateReady,
        Served,
        WrongOrder,
        NoPickup,         // no hay power-up flotando (o ya se fue)
        NoPowerUp,        // no tienes ese power-up cargado
        PowerUpActive,    // ese power-up ya esta funcionando
        NeedsFlip,        // la base tiene un lado listo: hay que voltearla antes de servirla
        AlreadyFlipped
    }
}
