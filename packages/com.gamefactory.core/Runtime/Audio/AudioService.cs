using System;
using System.Collections.Generic;
using GameFactory.Core.Save;
using UnityEngine;

namespace GameFactory.Core.Audio
{
    [Serializable]
    public class AudioSettingsData
    {
        public float sfxVolume = 1f;
        public float musicVolume = 0.7f;
        public bool muted;
    }

    /// <summary>
    /// Servicio de audio global (se crea solo la primera vez que se usa y sobrevive cambios de escena).
    /// Efectos con un pool de AudioSources (permite tono distinto por sonido) y una fuente de musica.
    /// Volumen y silencio se guardan en "audio-settings.json".
    /// Uso: AudioService.Instance.PlaySfx(clip, 0.8f, 1.1f);
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        const int PoolSize = 8;
        static AudioService _instance;

        readonly List<AudioSource> _sfx = new List<AudioSource>();
        AudioSource _music;
        int _next;
        SaveFile<AudioSettingsData> _file;
        AudioSettingsData _settings;

        public static AudioService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[AudioService]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<AudioService>();
                }
                return _instance;
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            _file = new SaveFile<AudioSettingsData>("audio-settings");
            _settings = _file.Load();
            for (int i = 0; i < PoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                _sfx.Add(s);
            }
            _music = gameObject.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
            ApplyMusicVolume();
        }

        void OnDestroy() { if (_instance == this) _instance = null; }

        public float SfxVolume
        {
            get => _settings.sfxVolume;
            set { _settings.sfxVolume = Mathf.Clamp01(value); Persist(); }
        }

        public float MusicVolume
        {
            get => _settings.musicVolume;
            set { _settings.musicVolume = Mathf.Clamp01(value); ApplyMusicVolume(); Persist(); }
        }

        public bool Muted
        {
            get => _settings.muted;
            set { _settings.muted = value; ApplyMusicVolume(); Persist(); }
        }

        public void PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null || _settings.muted || _settings.sfxVolume <= 0f) return;
            var s = _sfx[_next];
            _next = (_next + 1) % _sfx.Count;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * _settings.sfxVolume;
            s.pitch = pitch;
            s.Play();
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            _music.clip = clip;
            ApplyMusicVolume();
            _music.Play();
        }

        public void StopMusic() => _music.Stop();

        void ApplyMusicVolume()
        {
            if (_music != null) _music.volume = _settings.muted ? 0f : _settings.musicVolume;
        }

        void Persist()
        {
            try { _file.Save(_settings); }
            catch (Exception e) { Debug.LogWarning($"[AudioService] No se pudo guardar la configuracion: {e.Message}"); }
        }
    }
}
