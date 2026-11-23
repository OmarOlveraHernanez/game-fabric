using System;
using Tacos.Ads;
using Tacos.Logic;
using Tacos.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tacos
{
    /// <summary>
    /// Anuncios y la compra "Quitar anuncios":
    /// - Video con premio para duplicar las monedas de la mision (en resultados).
    /// - Video con premio para empezar la mision con un power-up (arriba de la tarjeta de mision).
    /// - Anuncio entre misiones: cada 3 misiones, nunca en las 3 primeras del juego; se quita con la compra.
    /// </summary>
    public sealed partial class TacosGame
    {
        const int MissionsPerInterstitial = 3;

        AdsService _ads;
        IRemoveAdsStore _store;
        bool _doubled, _bonusPowerUp;
        RectTransform _x2Group, _powerOffer;
        Text _x2Label, _powerOfferLabel;
        GameObject _removeAdsWorld, _removeAdsShop;
        float _adsUiTimer;

        void InitMonetization()
        {
            _ads = new AdsService { NoInterstitial = _data.noAds };
            _ads.Initialize();
            _store = RemoveAdsStore.Create();
            _store.Init(() =>
            {
                // Ya la tenia (Google la recuerda en su cuenta) o la acaba de comprar
                if (!_data.noAds) { _data.noAds = true; _saveFile.Save(_data); }
                _ads.DropInterstitial();
                RefreshAdsUi();
            });
            RefreshAdsUi();
        }

        // ---------- anuncio entre misiones ----------

        /// <summary>Al salir de resultados: si toca, primero el anuncio y luego a donde iba.</summary>
        void AfterResults(Action next)
        {
            bool due = !_data.noAds && _data.missionsSinceAd >= MissionsPerInterstitial && P.TotalStars > 0 && G(_mission) >= 3;
            if (due && _ads != null && _ads.ShowInterstitial(() => { AudioListener.pause = false; next(); }))
            {
                AudioListener.pause = true;
                _data.missionsSinceAd = 0;
                _saveFile.Save(_data);
                return;
            }
            next();
        }

        // ---------- duplicar monedas ----------

        void BuildDoubleCoins(Stage st)
        {
            _x2Group = UiKit.NewRect("DuplicarMonedas", st.Root);
            UiKit.Stretch(_x2Group);
            var img = st.Pic("Fondo", TacosArt.Get("btn_verde"), Stage.R(860, 676, 420, 70), _x2Group);
            img.preserveAspect = false;
            if (img.sprite == null) { img.sprite = UiKit.RoundedSpritePublic; img.color = UiKit.Limon; }
            _x2Label = st.Label("▶ Ver video: ¡monedas x2!", 28, Color.white, Stage.R(860, 672, 380, 52), _x2Group);
            _x2Label.font = UiKit.Display;
            FitText(_x2Label, 28);
            st.Hotspot("Tocar", Stage.R(860, 676, 420, 70), () => { UiKit.Clicked?.Invoke(); WatchForDoubleCoins(); }, _x2Group);
            _x2Group.gameObject.SetActive(false);
        }

        void WatchForDoubleCoins()
        {
            if (_doubled || _sim == null || _ads == null) return;
            int coins = _sim.Coins;
            AudioListener.pause = true;
            bool shown = _ads.ShowRewarded(RewardedSlot.MonedasX2, () =>
            {
                _doubled = true;
                P.wallet += coins;
                P.coinsEarned += coins;
                _saveFile.Save(_data);
                _endCoins.text = (coins * 2).ToString();
                _endInfo.text = $"¡Monedas al doble! Ganaste {coins} más. Ahora tienes {P.wallet} monedas para mejoras.";
                Audio.PlaySfx(_snd.Estrella);
            }, () => { AudioListener.pause = false; RefreshAdsUi(); });
            if (!shown) AudioListener.pause = false;
        }

        // ---------- empezar con power-up ----------

        void BuildPowerUpOffer()
        {
            var b = UiKit.MakeButton(_zone, "VideoPowerUp", "", new Color(0.1f, 0.5f, 0.3f, 0.95f), Color.white, 24, WatchForPowerUp);
            _powerOffer = (RectTransform)b.transform;
            // justo arriba de la tarjeta de mision
            Corner(_powerOffer, new Vector2(1f, 0f), new Vector2(-18f, 18f + 380f + 12f), new Vector2(640f, 62f));
            _powerOfferLabel = b.GetComponentInChildren<Text>();
            _powerOffer.gameObject.SetActive(false);
        }

        void WatchForPowerUp()
        {
            if (_bonusPowerUp || _ads == null) return;
            AudioListener.pause = true;
            bool shown = _ads.ShowRewarded(RewardedSlot.PowerUp, () =>
            {
                _bonusPowerUp = true;
                Audio.PlaySfx(_snd.PowerUp);
            }, () => { AudioListener.pause = false; RefreshAdsUi(); });
            if (!shown) AudioListener.pause = false;
        }

        /// <summary>Al empezar la mision: el power-up que se gano con el video.</summary>
        void GiveBonusPowerUp()
        {
            if (!_bonusPowerUp || _sim == null || _sim.Level.PowerUps.Length == 0) return;
            _bonusPowerUp = false;
            var p = _sim.Level.PowerUps[UnityEngine.Random.Range(0, _sim.Level.PowerUps.Length)];
            _sim.GiftPowerUp(p);
            Toast($"¡Empiezas con {PowerUps.Name(p, _sim.Level.Stall)}!", UiKit.Limon);
        }

        // ---------- quitar anuncios ----------

        GameObject BuildRemoveAdsButton(RectTransform parent, Vector2 anchor, Vector2 pos)
        {
            var b = UiKit.MakeButton(parent, "QuitarAnuncios", "Quitar anuncios", new Color(0.45f, 0.12f, 0.42f, 0.95f), Color.white, 22, BuyRemoveAds);
            Corner((RectTransform)b.transform, anchor, pos, new Vector2(270f, 62f));
            b.gameObject.SetActive(false);
            return b.gameObject;
        }

        void BuyRemoveAds()
        {
            if (_store == null || !_store.Ready) return;
            _store.Buy(ok =>
            {
                if (ok) Toast("¡Listo! Ya no hay anuncios entre misiones. ¡Gracias!", UiKit.Limon);
                RefreshAdsUi();
            });
        }

        /// <summary>Que botones se ven: segun lo que ya cargo y si ya compro.</summary>
        void RefreshAdsUi()
        {
            bool canBuy = !_data.noAds && _store != null && _store.Ready;
            string label = string.IsNullOrEmpty(_store?.Price) ? "Quitar anuncios" : $"Quitar anuncios · {_store.Price}";
            foreach (var b in new[] { _removeAdsWorld, _removeAdsShop })
            {
                if (b == null) continue;
                b.SetActive(canBuy);
                var t = b.GetComponentInChildren<Text>(); if (t != null) t.text = label;
            }
            if (_x2Group != null)
                _x2Group.gameObject.SetActive(_end != null && _end.gameObject.activeSelf && !_doubled && _sim != null && _sim.Coins > 0
                                              && _ads != null && _ads.IsRewardedReady(RewardedSlot.MonedasX2));
            if (_powerOffer != null && _zone != null)
            {
                bool hasPowerUps = _mission < Missions.Length && Missions[_mission].PowerUps.Length > 0;
                _powerOffer.gameObject.SetActive(hasPowerUps && (_bonusPowerUp || (_ads != null && _ads.IsRewardedReady(RewardedSlot.PowerUp))));
                _powerOfferLabel.text = _bonusPowerUp ? "✓ ¡Empiezas con un power-up de regalo!" : "▶ Ver video: empieza con un power-up";
            }
        }

        /// <summary>Los anuncios cargan solos con el tiempo: se revisa dos veces por segundo.</summary>
        void UpdateAdsUi()
        {
            _adsUiTimer -= Time.unscaledDeltaTime;
            if (_adsUiTimer > 0f) return;
            _adsUiTimer = 0.5f;
            RefreshAdsUi();
        }
    }
}
