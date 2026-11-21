namespace Tacos.Logic
{
    /// <summary>
    /// Power-ups: se cargan con la racha (clientes bien atendidos seguidos) o se agarran cuando caen del cielo (se tocan).
    /// </summary>
    public enum PowerUp
    {
        Cebollitas = 0, // el taquero atiende solo al cliente con menos paciencia
        Nopal = 1,      // congela la paciencia de todos un rato
        Salsa = 2,      // "Salsa de la abuela": propina doble un rato
        Trompo = 3      // "Trompo en llamas": cazos llenos y tortillas calientes al instante
    }

    /// <summary>Clientes especiales (como los distintos zombies): cada uno cambia un poco el reto.</summary>
    public enum CustomerKind
    {
        Normal = 0,
        Mariachi = 1,   // pide 3 tacos iguales; se queda hasta recibir los 3
        Critico = 2,    // paga triple si le das bien; si te equivocas se va
        Borrachito = 3, // a media espera cambia su pedido (una vez)
        Jefe = 4        // jefe final (mision 12): pide 3 platos distintos, uno tras otro; si se va, no hay estrellas
    }

    public static class PowerUps
    {
        public static readonly PowerUp[] All = { PowerUp.Cebollitas, PowerUp.Nopal, PowerUp.Salsa, PowerUp.Trompo };

        /// <summary>Nombre segun el puesto: hacen lo mismo en todos los mundos, pero cada puesto tiene los suyos.</summary>
        public static string Name(PowerUp p, Stall s)
        {
            if (s == Stall.Garnachas)
                switch (p)
                {
                    case PowerUp.Cebollitas: return "Papitas fritas";
                    case PowerUp.Nopal: return "Nopalitos";
                    case PowerUp.Salsa: return "Salsa de molcajete";
                    case PowerUp.Trompo: return "Comal al rojo";
                }
            if (s == Stall.Elotes)
                switch (p)
                {
                    case PowerUp.Cebollitas: return "Elote dorado";
                    case PowerUp.Nopal: return "Raspado de limón";
                    case PowerUp.Salsa: return "Chilito de la suerte";
                    case PowerUp.Trompo: return "Olla hirviendo";
                }
            if (s == Stall.Pozole)
                switch (p)
                {
                    case PowerUp.Cebollitas: return "Tostadas crujientes";
                    case PowerUp.Nopal: return "Agua de jamaica";
                    case PowerUp.Salsa: return "Piquín de la suerte";
                    case PowerUp.Trompo: return "Olla al hervor";
                }
            if (s == Stall.Tamales)
                switch (p)
                {
                    case PowerUp.Cebollitas: return "Tamal de regalo";
                    case PowerUp.Nopal: return "Café de olla";
                    case PowerUp.Salsa: return "Rosca de la suerte";
                    case PowerUp.Trompo: return "Vaporera a todo vapor";
                }
            if (s == Stall.Feria)
                switch (p)
                {
                    case PowerUp.Cebollitas: return "Algodón de azúcar";
                    case PowerUp.Nopal: return "Rueda de la fortuna";
                    case PowerUp.Salsa: return "Lotería";
                    case PowerUp.Trompo: return "Castillo de cohetes";
                }
            return Name(p);
        }

        public static string Effect(PowerUp p, Stall s) =>
            p != PowerUp.Trompo || s == Stall.Taqueria ? Effect(p)
            : s == Stall.Garnachas ? "cazuelas llenas y masas listas al instante"
            : s == Stall.Tamales ? "la vaporera llena al instante"
            : "todas las ollas llenas al instante";

        public static string Name(PowerUp p)
        {
            switch (p)
            {
                case PowerUp.Cebollitas: return "Cebollitas asadas";
                case PowerUp.Nopal: return "Nopal";
                case PowerUp.Salsa: return "Salsa de la abuela";
                case PowerUp.Trompo: return "Trompo en llamas";
                default: return "";
            }
        }

        public static string Effect(PowerUp p)
        {
            switch (p)
            {
                case PowerUp.Cebollitas: return "atiende solo al cliente más desesperado";
                case PowerUp.Nopal: return "congela la paciencia de todos";
                case PowerUp.Salsa: return "propina doble por un rato";
                case PowerUp.Trompo: return "cazos llenos y tortillas calientes al instante";
                default: return "";
            }
        }

        public static string KindName(CustomerKind k)
        {
            switch (k)
            {
                case CustomerKind.Mariachi: return "Mariachi";
                case CustomerKind.Critico: return "Crítico";
                case CustomerKind.Borrachito: return "Borrachito";
                case CustomerKind.Jefe: return "¡Jefe final!";
                default: return "";
            }
        }
    }
}
