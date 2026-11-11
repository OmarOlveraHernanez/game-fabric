using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace RescatePapi.Ads
{
    public enum RewardedSlot { Continue, DoubleCoins }

    /// <summary>
    /// Anuncios con Google Mobile Ads (AdMob): consentimiento (UMP), videos con premio e intersticial.
    /// En el editor y en builds de desarrollo usa los ID DE PRUEBA de Google (anuncios "Test Ad"):
    /// nunca tocar anuncios reales propios, Google puede suspender la cuenta.
    /// Los ID reales solo se usan en el build de lanzamiento (Release AAB).
    /// </summary>
    public sealed class AdsService
    {
        // ID de bloques de anuncios de Rescate Papi en AdMob (cuenta Ollinix Games, pub-6064533911049856)
        const string RealContinue = "ca-app-pub-6064533911049856/8675625300";
        const string RealDoubleCoins = "ca-app-pub-6064533911049856/3343232565";
        const string RealInterstitial = "ca-app-pub-6064533911049856/4639856573";
        // ID de prueba oficiales de Google (Android)
        const string TestRewarded = "ca-app-pub-3940256099942544/5224354917";
        const string TestInterstitial = "ca-app-pub-3940256099942544/1033173712";

        static bool UseTestAds => Debug.isDebugBuild;

        RewardedAd _continue, _doubleCoins;
        InterstitialAd _interstitial;
        bool _initialized;
        // Reintentos si una carga falla (sin red, sin anuncio disponible): 5 s, 10 s, 20 s... hasta 60 s
        int _failsContinue, _failsDouble, _failsInterstitial;
        static float RetryDelay(int fails) => Mathf.Min(60f, 5f * Mathf.Pow(2f, Mathf.Max(0, fails - 1)));

        public bool IsShowing { get; private set; }
        /// <summary>Estado para diagnostico (se ve en builds de desarrollo).</summary>
        public string Status { get; private set; } = "sin iniciar";

        public void Initialize()
        {
            MainThread.Init();
            Status = "consentimiento...";
            // Consentimiento (obligatorio en la UE/Reino Unido; en Mexico normalmente no muestra nada)
            ConsentInformation.Update(new ConsentRequestParameters(), updateError => OnMain(() =>
            {
                if (updateError != null)
                {
                    // Sin respuesta del servicio de consentimiento (p. ej. sin mensaje configurado en AdMob):
                    // fuera de la UE no hace falta; se intenta igual para no quedarse sin anuncios.
                    Debug.LogWarning($"[Ads] Consentimiento: {updateError.Message}");
                    Status = "consentimiento con error: " + updateError.Message;
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
            // Si ya habia consentimiento de otra sesion, se puede empezar sin esperar
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
                LoadRewarded(RewardedSlot.Continue);
                LoadRewarded(RewardedSlot.DoubleCoins);
                LoadInterstitial();
            }));
        }

        // ---------- videos con premio ----------

        public bool IsRewardedReady(RewardedSlot slot)
        {
            var ad = slot == RewardedSlot.Continue ? _continue : _doubleCoins;
            return ad != null && ad.CanShowAd();
        }

        /// <summary>Muestra el video. onReward solo si lo vio completo; onClosed siempre al cerrar.</summary>
        public bool ShowRewarded(RewardedSlot slot, Action onReward, Action onClosed)
        {
            if (!IsRewardedReady(slot)) return false;
            var ad = slot == RewardedSlot.Continue ? _continue : _doubleCoins;
            // Ya se usa: el siguiente se pide en cuanto se cierre
            if (slot == RewardedSlot.Continue) _continue = null; else _doubleCoins = null;
            bool rewarded = false, finished = false;
            void Finish()
            {
                if (finished) return;
                finished = true;
                IsShowing = false;
                ad.Destroy();
                onClosed?.Invoke();
                LoadRewarded(slot);                      // el siguiente queda listo
            }
            ad.OnAdFullScreenContentClosed += () => OnMain(Finish);
            ad.OnAdFullScreenContentFailed += e => OnMain(() => { Debug.LogWarning($"[Ads] Video fallo: {e.GetMessage()}"); Finish(); });
            IsShowing = true;
            // El premio se da cuando Google confirma que lo vio (puede llegar antes o despues del cierre)
            ad.Show(_ => OnMain(() => { if (rewarded) return; rewarded = true; onReward?.Invoke(); }));
            return true;
        }

        void LoadRewarded(RewardedSlot slot)
        {
            string id = UseTestAds ? TestRewarded : slot == RewardedSlot.Continue ? RealContinue : RealDoubleCoins;
            RewardedAd.Load(id, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (error != null || ad == null)
                {
                    int fails = slot == RewardedSlot.Continue ? ++_failsContinue : ++_failsDouble;
                    Debug.LogWarning($"[Ads] No cargo video {slot} (intento {fails}): {error?.GetMessage()}");
                    Status = $"video {slot} no cargo, reintento: {error?.GetMessage()}";
                    MainThread.RunAfter(RetryDelay(fails), () => LoadRewarded(slot));
                    return;
                }
                if (slot == RewardedSlot.Continue) { _continue = ad; _failsContinue = 0; }
                else { _doubleCoins = ad; _failsDouble = 0; }
                Status = "listo";
            }));
        }

        // ---------- intersticial ----------

        public bool IsInterstitialReady => _interstitial != null && _interstitial.CanShowAd();

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
                _interstitial = null;
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
            InterstitialAd.Load(UseTestAds ? TestInterstitial : RealInterstitial, new AdRequest(), (ad, error) => OnMain(() =>
            {
                if (error != null || ad == null)
                {
                    _failsInterstitial++;
                    Debug.LogWarning($"[Ads] No cargo intersticial (intento {_failsInterstitial}): {error?.GetMessage()}");
                    Status = $"intersticial no cargo, reintento: {error?.GetMessage()}";
                    MainThread.RunAfter(RetryDelay(_failsInterstitial), LoadInterstitial);
                    return;
                }
                _interstitial = ad;
                _failsInterstitial = 0;
            }));
        }

        /// <summary>Los avisos de AdMob pueden llegar desde otro hilo; Unity solo se toca en el principal.</summary>
        static void OnMain(Action action) => MainThread.Run(action);
    }
}
