using System.Collections.Generic;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Tienda de mejoras: un puesto de jarceria del mercado. Cada mejora es un letrero de madera colgado
    /// con el dibujo del objeto; se toca el letrero para comprarlo. Lo comprado lleva el sello dorado
    /// y se ve despues en la cocina (radio, domo de fierro, tercer plato, cazos llenos).
    /// </summary>
    public sealed partial class TacosGame
    {
        sealed class ShopCard
        {
            public UpgradeDef Def;
            public RectTransform Root;
            public Image Art, Seal, Coin;
            public Text Price, Name, Desc;
            public float Phase, Bump, Shake, Stamp;
        }

        Stage _shopStage;
        readonly List<ShopCard> _shopCards = new List<ShopCard>();
        Text _shopStars, _shopWallet, _shopInfo, _shopTitle;
        float _shopInfoTime;

        const string ShopHelp = "Toca un letrero para comprar. Las monedas de cada misión se juntan aquí, aunque no ganes estrellas.";
        static readonly float[] ShopCardX = { 363, 633, 903, 1173 };
        const float CardTop = 64f, CardW = 250f, CardH = 500f;

        void BuildShop()
        {
            _shop = UiKit.Stretch(UiKit.NewRect("Mejoras", _root));
            _shopStage = new Stage(_shop, "Jarceria", TacosArt.Get("tienda_fondo"), 1536, 864, Stage.Fit.Cover);
            var st = _shopStage;

            for (int i = 0; i < UpgradeDef.All.Length && i < ShopCardX.Length; i++)
            {
                var def = UpgradeDef.All[i];
                var c = new ShopCard { Def = def, Phase = i * 1.7f };
                var cardRect = new Rect(ShopCardX[i] - CardW / 2f, CardTop, CardW, CardH);

                // sombra del letrero sobre la pared
                st.Img("Sombra " + def.Id, UiKit.GlowSprite, new Color(0f, 0f, 0f, 0.45f), Stage.R(ShopCardX[i] + 10, CardTop + CardH / 2f + 14, CardW + 70, CardH + 40));

                var board = st.Pic("Letrero " + def.Id, TacosArt.Get("ui_tarjeta"), cardRect);
                board.preserveAspect = false;
                if (board.sprite == null) { board.color = new Color(0.45f, 0.27f, 0.14f); board.sprite = UiKit.RoundedSpritePublic; }
                c.Root = board.rectTransform;
                // cuelga de las cuerdas: gira desde arriba
                c.Root.pivot = new Vector2(0.5f, 1f);

                c.Art = Inner(UiKit.Picture(c.Root, "Dibujo", TacosArt.Get(ArtOf(def.Id))), cardRect, Stage.R(ShopCardX[i], CardTop + 150, 190, 140));
                c.Art.preserveAspect = true;

                var name = c.Name = UiKit.Outlined(c.Root, def.Name, 30, UiKit.Maiz);
                FitText(name, 30);
                Inner(name, cardRect, Stage.R(ShopCardX[i], CardTop + 262, 180, 64));
                var desc = c.Desc = UiKit.Outlined(c.Root, def.Description, 22, new Color32(0xFF, 0xF0, 0xD2, 0xFF), TextAnchor.MiddleCenter, 2f);
                FitText(desc, 22);
                Inner(desc, cardRect, Stage.R(ShopCardX[i], CardTop + 345, 176, 96));

                c.Coin = Inner(UiKit.Picture(c.Root, "Moneda", TacosArt.Coin != null ? TacosArt.Coin : UiKit.CircleSprite), cardRect, Stage.R(ShopCardX[i] - 52, CardTop + 438, 44, 44));
                c.Coin.preserveAspect = true;
                c.Price = UiKit.Outlined(c.Root, def.Price.ToString(), 36, UiKit.Maiz, TextAnchor.MiddleLeft);
                FitText(c.Price, 36);
                Inner(c.Price, cardRect, Stage.R(ShopCardX[i] + 22, CardTop + 438, 110, 48));

                c.Seal = Inner(UiKit.Picture(c.Root, "Sello", TacosArt.Get("ui_sello")), cardRect, Stage.R(ShopCardX[i] + 70, CardTop + 200, 96, 96));
                c.Seal.preserveAspect = true;

                var card = c;
                st.Hotspot("Comprar " + def.Id, cardRect, () => TapUpgrade(card));
                _shopCards.Add(c);
            }

            _shopInfo = st.Label(ShopHelp, 26, new Color32(0xFF, 0xF0, 0xD2, 0xFF), Stage.R(768, 770, 1180, 70));
            FitText(_shopInfo, 26);

            // Encabezado (igual que en la calle)
            var back = UiKit.MakeButton(_shop, "VolverTienda", "‹ Calle", new Color(0.08f, 0.07f, 0.12f, 0.88f), UiKit.Cream, 26, ShowZone);
            Corner((RectTransform)back.transform, new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(170f, 62f));
            _removeAdsShop = BuildRemoveAdsButton(_shop, new Vector2(0f, 0f), new Vector2(18f, 18f)); // encima de todo
            var title = UiKit.Panel(_shop, "TituloTienda", new Color(0.08f, 0.07f, 0.12f, 0.88f), true, false);
            Corner(title.rectTransform, new Vector2(0f, 1f), new Vector2(200f, -16f), new Vector2(430f, 62f));
            _shopTitle = UiKit.FillLabel(title.transform, "Jarcería · Mejoras de cocina", 26, UiKit.Maiz, TextAnchor.MiddleCenter, 4f);
            BuildWallet(_shop, out _shopStars, out _shopWallet);
        }

        static string ArtOf(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Paciencia: return "mej_radio";
                case Upgrade.ComalRapido: return "mej_domo";
                case Upgrade.TercerPlato: return "mej_platos";
                case Upgrade.Plancha3: return "mej_cazo";
                case Upgrade.GarAguas: return "mej_aguas";
                case Upgrade.GarComal: return "mej_comal_barro";
                case Upgrade.GarTabla: return "mej_tabla";
                case Upgrade.GarCazuelas: return "mej_cazuela";
                case Upgrade.EloBocina: return "mej_bocina";
                case Upgrade.EloOlla: return "mej_olla_express";
                case Upgrade.EloCarrito: return "mej_carrito";
                case Upgrade.EloOllota: return "mej_ollota";
                case Upgrade.PozRocola: return "mej_rocola";
                case Upgrade.PozFogon: return "mej_fogon";
                case Upgrade.PozMesa: return "mej_mesa";
                case Upgrade.PozOlla: return "mej_olla_pozolera";
                case Upgrade.TamAtole: return "mej_atolito";
                case Upgrade.TamVaporera: return "mej_vaporera";
                case Upgrade.TamCanasta: return "mej_canasta";
                case Upgrade.TamGigante: return "mej_tamalera";
                case Upgrade.FerBanda: return "mej_banda";
                case Upgrade.FerFogones: return "mej_fogones";
                case Upgrade.FerPuesto: return "mej_puesto";
                case Upgrade.FerOllas: return "mej_ollas_feria";
                default: return "mej_cazuela";
            }
        }

        static void FitText(Text t, int max)
        {
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(10, max / 2);
            t.resizeTextMaxSize = max;
        }

        /// <summary>Coloca algo dentro del letrero usando pixeles de la imagen de fondo.</summary>
        static T Inner<T>(T c, Rect card, Rect r) where T : Component
        {
            var rt = (RectTransform)c.transform;
            rt.anchorMin = new Vector2((r.xMin - card.xMin) / card.width, 1f - (r.yMax - card.yMin) / card.height);
            rt.anchorMax = new Vector2((r.xMax - card.xMin) / card.width, 1f - (r.yMin - card.yMin) / card.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return c;
        }

        void TapUpgrade(ShopCard c)
        {
            var u = c.Def.Id;
            if (P.Owns(u))
            {
                c.Bump = 1f;
                ShopMessage("¡Esta ya es tuya! Ya la tienes en tu puesto.");
                return;
            }
            if (P.Buy(u))
            {
                _saveFile.Save(_data);
                Audio.PlaySfx(_snd.Cobro);
                c.Stamp = 1f;
                c.Bump = 1f;
                ShopMessage("¡Comprada! " + ShopWhere(u));
                RefreshShop();
                return;
            }
            Audio.PlaySfx(_snd.Error, 0.5f);
            c.Shake = 1f;
            ShopMessage($"Te faltan {c.Def.Price - P.wallet} monedas. Juega misiones para juntar más.");
        }

        static string ShopWhere(Upgrade u)
        {
            switch (u)
            {
                case Upgrade.Paciencia: return "Vas a ver el radio en la barra de arriba.";
                case Upgrade.ComalRapido: return "Tu domo ahora es de fierro.";
                case Upgrade.TercerPlato: return "Ya tienes otro plato en la tabla.";
                case Upgrade.Plancha3: return "Tus cazos ahora van bien llenos.";
                case Upgrade.GarAguas: return "Tus clientes ya tienen su agüita.";
                case Upgrade.GarComal: return "Tu comal ahora es de barro.";
                case Upgrade.GarTabla: return "Ya tienes otro plato en la tabla.";
                case Upgrade.EloBocina: return "Ya suena la cumbia en el carrito.";
                case Upgrade.EloOlla: return "Tus ollas ahora se rellenan rapidito.";
                case Upgrade.EloCarrito: return "Ya tienes otra charolita.";
                case Upgrade.EloOllota: return "Tus ollas ahora van bien llenas.";
                case Upgrade.PozRocola: return "Ya suenan los boleros en la pozolería.";
                case Upgrade.PozFogon: return "Tus ollas ahora hierven rapidito.";
                case Upgrade.PozMesa: return "Ya tienes otro plato en la mesa.";
                case Upgrade.PozOlla: return "Tus ollas ahora van bien llenas.";
                case Upgrade.TamAtole: return "Tus clientes ya tienen su atolito.";
                case Upgrade.TamVaporera: return "Tu vaporera ahora cuece rapidito.";
                case Upgrade.TamCanasta: return "Ya tienes otro plato en la mesa.";
                case Upgrade.TamGigante: return "Tu vaporera ahora rinde más.";
                case Upgrade.FerBanda: return "Ya toca la banda en la feria.";
                case Upgrade.FerFogones: return "Tus ollas ahora se rellenan rapidito.";
                case Upgrade.FerPuesto: return "Ya tienes otro plato en el puesto.";
                case Upgrade.FerOllas: return "Tus ollas ahora van bien llenas.";
                default: return "Tus cazuelas ahora van bien llenas.";
            }
        }

        void ShopMessage(string text)
        {
            _shopInfo.text = text;
            _shopInfoTime = 3f;
        }

        void RefreshShop()
        {
            // Cada mundo tiene su tienda: los letreros muestran las mejoras de este puesto
            var world = Missions[0].Stall;
            var defs = UpgradeDef.For(world);
            _shopTitle.text = new[]
            {
                "Jarcería · Mejoras de cocina", "Mercado · Mejoras de garnachas", "Plaza · Mejoras del carrito",
                "Mercado · Mejoras de la pozolería", "Tianguis · Mejoras de la tamalería", "La Feria · Mejoras del puesto"
            }[(int)world];
            for (int i = 0; i < _shopCards.Count && i < defs.Length; i++)
            {
                var c = _shopCards[i];
                if (c.Def == defs[i]) continue;
                c.Def = defs[i];
                c.Art.sprite = TacosArt.Get(ArtOf(c.Def.Id));
                c.Name.text = c.Def.Name;
                c.Desc.text = c.Def.Description;
            }
            _shopStars.text = P.TotalStars.ToString();
            _shopWallet.text = P.wallet.ToString();
            foreach (var c in _shopCards)
            {
                bool owned = P.Owns(c.Def.Id);
                bool afford = P.CanBuy(c.Def.Id);
                c.Seal.enabled = owned && c.Seal.sprite != null;
                c.Coin.enabled = !owned;
                c.Price.text = owned ? "¡Ya es tuya!" : c.Def.Price.ToString();
                c.Price.alignment = owned ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                var prt = c.Price.rectTransform;
                // comprada: el texto usa todo el ancho; si no, va a la derecha de la moneda
                var cardRect = new Rect(0, 0, CardW, CardH);
                Inner(c.Price, cardRect, owned ? Stage.R(CardW / 2f, 438, 180, 48) : Stage.R(CardW / 2f + 22, 438, 110, 48));
                c.Price.color = owned ? UiKit.Limon : afford ? UiKit.Maiz : new Color32(0xFF, 0x8A, 0x7A, 0xFF);
                c.Art.color = Color.white;
            }
            if (_shopInfoTime <= 0f) _shopInfo.text = ShopHelp;
        }

        /// <summary>Cada cuadro con la tienda abierta: los letreros se mecen un poquito.</summary>
        void UpdateShop()
        {
            _shopStage.Layout();
            float dt = Time.unscaledDeltaTime;
            if (_shopInfoTime > 0f)
            {
                _shopInfoTime -= dt;
                if (_shopInfoTime <= 0f) _shopInfo.text = ShopHelp;
            }
            float t = Time.unscaledTime;
            foreach (var c in _shopCards)
            {
                c.Bump = Mathf.Max(0f, c.Bump - dt * 1.4f);
                c.Shake = Mathf.Max(0f, c.Shake - dt * 2.5f);
                c.Stamp = Mathf.Max(0f, c.Stamp - dt * 3.5f);
                float sway = 1.2f * Mathf.Sin(t * 1.3f + c.Phase)
                             + 7f * c.Bump * Mathf.Sin(c.Bump * 14f)
                             + 5f * c.Shake * Mathf.Sin(t * 60f);
                c.Root.localRotation = Quaternion.Euler(0f, 0f, sway);
                // el sello cae como estampa
                float s = 1f + 0.8f * c.Stamp * c.Stamp;
                c.Seal.rectTransform.localScale = new Vector3(s, s, 1f);
                c.Seal.color = new Color(1f, 1f, 1f, 1f - c.Stamp * 0.6f);
            }
        }
    }
}
