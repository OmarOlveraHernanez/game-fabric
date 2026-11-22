using System;

namespace Tacos.Ads
{
    /// <summary>
    /// La compra unica "Quitar anuncios" (ID de producto en Play Console: quitar_anuncios, no consumible).
    /// Quita los anuncios entre misiones; los videos con premio siguen (son opcionales).
    /// </summary>
    public interface IRemoveAdsStore
    {
        /// <summary>La tienda ya respondio y se puede comprar.</summary>
        bool Ready { get; }
        /// <summary>Precio como lo da Google (ya con su moneda), o "" si todavia no se sabe.</summary>
        string Price { get; }
        /// <summary>Se conecta a la tienda. onOwned se llama si ya la habia comprado (otro telefono, reinstalo) o al comprarla.</summary>
        void Init(Action onOwned);
        /// <summary>Abre la compra de Google. done(true) si se completo.</summary>
        void Buy(Action<bool> done);
    }

    public static class RemoveAdsStore
    {
        public const string ProductId = "quitar_anuncios";

        /// <summary>La tienda de verdad, si el paquete de compras de Unity esta instalado; si no, una que no vende nada.</summary>
        public static IRemoveAdsStore Create() => Factory != null ? Factory() : new NoStore();

        /// <summary>Lo pone la tienda con Unity IAP al cargar (asi este archivo compila sin el paquete).</summary>
        public static Func<IRemoveAdsStore> Factory;

        sealed class NoStore : IRemoveAdsStore
        {
            public bool Ready => false;
            public string Price => "";
            public void Init(Action onOwned) { }
            public void Buy(Action<bool> done) => done?.Invoke(false);
        }
    }
}
