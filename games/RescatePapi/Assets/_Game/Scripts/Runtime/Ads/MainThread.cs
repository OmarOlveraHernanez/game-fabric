using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace RescatePapi.Ads
{
    /// <summary>
    /// Ejecuta en el hilo principal de Unity lo que llega de otros hilos (avisos de AdMob y del consentimiento).
    /// Propio a proposito: el de Google (MobileAdsEventExecutor) no existe hasta MobileAds.Initialize,
    /// y el consentimiento se pide ANTES de inicializar.
    /// </summary>
    public sealed class MainThread : MonoBehaviour
    {
        static MainThread _instance;
        static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        public static void Init()
        {
            if (_instance != null) return;
            var go = new GameObject("[MainThread]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<MainThread>();
        }

        public static void Run(Action action) { if (action != null) Queue.Enqueue(action); }

        static readonly List<(float time, Action action)> Delayed = new List<(float, Action)>();

        /// <summary>Ejecuta despues de unos segundos (tiempo real), en el hilo principal.</summary>
        public static void RunAfter(float seconds, Action action) =>
            Run(() => Delayed.Add((Time.realtimeSinceStartup + seconds, action)));

        void Update()
        {
            float now = Time.realtimeSinceStartup;
            for (int i = Delayed.Count - 1; i >= 0; i--)
            {
                if (Delayed[i].time > now) continue;
                var a = Delayed[i].action;
                Delayed.RemoveAt(i);
                Run(a);
            }
            while (Queue.TryDequeue(out var a))
            {
                try { a(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
