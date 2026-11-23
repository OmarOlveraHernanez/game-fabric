using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Tacos.Ads
{
    public enum RewardedSlot { MonedasX2, PowerUp }

    /// <summary>
    /// Anuncios con Google Mobile Ads (AdMob): consentimiento (UMP), 2 videos con premio e intersticial.
    /// Basado en el de Rescate Papi. En el editor y en builds de desarrollo usa los ID DE PRUEBA de Google
    /// (anuncios "Test Ad"): nunca tocar anuncios reales propios, Google puede suspender la cuenta.
    /// Los ID reales solo se usan en el build de lanzamiento, y solo cuando ya esten puestos abajo.
    /// Con "Quitar anuncios" comprado no se carga el intersticial; los videos con premio siguen (son opcionales).
    /// </summary>
    public sealed class AdsService
    {
        // ID de bloques de Antojitos Neon en AdMob (cuenta Ollinix Games, pub-6064533911049856; app ~7644806167).
        // Llegaron en el orden en que se pidieron: monedas_x2, powerup, entre_misiones.
        const string RealMonedasX2 = "ca-app-pub-6064533911049856/7852514716";
        const string RealPowerUp = "ca-app-pub-6064533911049856/7825302391";
        const string RealInterstitial = "ca-app-pub-6064533911049856/7021989636";
        // ID de prueba oficiales de Google (Android)
        const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
        const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";

        static bool UseTestAds => Debug.isDebugBuild || RealMonedasX2.Length == 0;

        RewardedAd _x2, _power;
        InterstitialAd _interstitial;
        bool _initialized;
        int _failsX2, _failsPower, _failsInterstitial;

        /// <summary>Si ya compro "Quitar anuncios": no hay intersticial.</summary>
        public bool NoInterstitial { get; set; }
        public bool IsShowing { get; private set; }
        public string Status { get; private set; } = "sin iniciar";

        static float RetryDelay(int fails) => Mathf.Min(60f, 5f * Mathf.Pow(2f, Mathf.Max(0, fails - 1)));

        public void Initialize()
        {
            MainThread.Init();
            Status = "consentimiento...";
            ConsentInformation.Update(new ConsentRequestParameters(), updateError => OnMain(() =>
            {
                if (updateError != null)
                {
                    Debug.LogWarning($"[Ads] Consentimiento: {updateError.Message}");
                    StartAds();
                    return;
                }
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => OnMain(() =>
                {
                    if (formError != null) Debug.LogWarning($"[Ads] Formulario: {formError.Message}");
                    if (ConsentInformation.CanRequestAds()) StartAds();
                    else Status = "sin consentimiento";
                }));
            }));
            if (ConsentInformation.CanRequestAds()) StartAds();
        }

        void StartAds()
        {
            if (_initialized) return;
            _initialized = true;
            Status = "iniciando AdMob...";
            MobileAds.Initialize(_ => OnMain(() =>
            {
                Status = "cargando anuncios...";
                LoadRewarded(RewardedSlot.MonedasX2);
                LoadRewarded(RewardedSlot.PowerUp);
                LoadInterstitial();
            }));
        }

        // ---------- videos con premio ----------

        static bool Ready(RewardedAd ad) => ad != null && ad.CanShowAd();

        /// <summary>Hay video (si el bloque propio no cargo, se usa el otro: los dos son videos con premio).</summary>
        public bool IsRewardedReady(RewardedSlot slot) => Ready(_x2) || Ready(_power);

        /// <summary>Muestra el video. onReward solo si lo vio completo; onClosed siempre al cerrar.</summary>
        public bool ShowRewarded(RewardedSlot slot, Action onReward, Action onClosed)
        {
            if (!IsRewardedReady(slot)) return false;
            var own = slot == RewardedSlot.MonedasX2 ? _x2 : _power;
            var used = Ready(own) ? slot : (slot == RewardedSlot.MonedasX2 ? RewardedSlot.PowerUp : RewardedSlot.MonedasX2);
            var ad = used == RewardedSlot.MonedasX2 ? _x2 : _power;
            if (used == RewardedSlot.MonedasX2) _x2 = null; else _power = null;
            bool rewarded = false, finished = false;
            void Finish()
            {
                if (finished) return;
                finished = true;
                IsShowing = false;
                ad.Destroy();
                onClosed?.Invoke();
                LoadRewarded(used);
            }
            ad.OnAdFullScreenContentClosed += () => OnMain(Finish);
            ad.OnAdFullScreenContentFailed += e => OnMain(() => { Debug.LogWarning($"[Ads] Video fallo: {e.GetMessage()}"); Finish(); });
            IsShowing = true;
            ad.Show(_ => OnMain(() => { if (rewarded) return; rewarded = true; onReward?.Invoke(); }));
            return true;
        }

        void LoadRewarded(RewardedSlot slot)
        {
            string id = UseTestAds ? TestRewarded : slot == RewardedSlot.MonedasX2 ? RealMonedasX2 : RealPowerUp;
            RewardedAd.Load(id, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (error != null || ad == null)
                {
                    int fails = slot == RewardedSlot.MonedasX2 ? ++_failsX2 : ++_failsPower;
                    Debug.LogWarning($"[Ads] No cargo video {slot} (intento {fails}): {error?.GetMessage()}");
                    MainThread.RunAfter(RetryDelay(fails), () => LoadRewarded(slot));
                    return;
                }
                if (slot == RewardedSlot.MonedasX2) { _x2 = ad; _failsX2 = 0; } else { _power = ad; _failsPower = 0; }
                Status = "listo";
            }));
        }

        // ---------- intersticial ----------

        public bool IsInterstitialReady => !NoInterstitial && _interstitial != null && _interstitial.CanShowAd();

        public bool ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady) return false;
            var ad = _interstitial;
            _interstitial = null;
            bool finished = false;
            void Finish()
            {
                if (finished) return;
                finished = true;
                IsShowing = false;
                ad.Destroy();
                onClosed?.Invoke();
                LoadInterstitial();
            }
            ad.OnAdFullScreenContentClosed += () => OnMain(Finish);
            ad.OnAdFullScreenContentFailed += e => OnMain(() => { Debug.LogWarning($"[Ads] Intersticial fallo: {e.GetMessage()}"); Finish(); });
            IsShowing = true;
            ad.Show();
            return true;
        }

        void LoadInterstitial()
        {
            if (NoInterstitial) return; // compro "Quitar anuncios"
            InterstitialAd.Load(UseTestAds ? TestInterstitial : RealInterstitial, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (error != null || ad == null)
                {
                    _failsInterstitial++;
                    Debug.LogWarning($"[Ads] No cargo intersticial (intento {_failsInterstitial}): {error?.GetMessage()}");
                    MainThread.RunAfter(RetryDelay(_failsInterstitial), LoadInterstitial);
                    return;
                }
                _interstitial = ad;
                _failsInterstitial = 0;
            }));
        }

        /// <summary>Al comprar "Quitar anuncios": se suelta el intersticial que ya estaba cargado.</summary>
        public void DropInterstitial()
        {
            NoInterstitial = true;
            _interstitial?.Destroy();
            _interstitial = null;
        }

        static void OnMain(Action action) => MainThread.Run(action);
    }
}
