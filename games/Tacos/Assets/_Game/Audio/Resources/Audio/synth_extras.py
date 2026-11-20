"""Efectos que faltaban (power-ups, jefe final, estrellas de resultados), compuestos por codigo, sin licencias.
Mismo estilo que synth_tacos.py.  uso: python3 synth_extras.py carpeta_salida"""
import sys, os, numpy as np
from scipy.io import wavfile

SR = 44100
rng = np.random.default_rng(23)
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)


def t_(d): return np.arange(int(d * SR)) / SR
def env_ad(t, a, d): return np.clip(t / a, 0, 1) * np.exp(-t / d)
def midi(n): return 440.0 * 2 ** ((n - 69) / 12)
def master(x, peak=0.89): return (x / (np.max(np.abs(x)) + 1e-9) * peak).astype(np.float32)
def save(name, x): wavfile.write(f'{OUT}/{name}.wav', SR, (master(x) * 32767).astype(np.int16))


def place(buf, snd, at):
    i = int(at * SR)
    n = min(len(snd), len(buf) - i)
    if n > 0: buf[i:i + n] += snd[:n]


def campanita(f, d=0.35, v=1.0):
    t = t_(d)
    return v * (np.sin(2 * np.pi * f * t) + 0.35 * np.sin(2 * np.pi * 2.76 * f * t)) * env_ad(t, 0.002, d / 3)


def metal(f, d, v=1.0):
    """Trompeta de mariachi sencilla: diente de sierra suavizado con vibrato."""
    t = t_(d)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip(t / 0.15, 0, 1)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    x = sum(np.sin(k * ph) / k for k in range(1, 9))
    return v * x * np.clip(t / 0.02, 0, 1) * np.exp(-t / (d * 0.9))


# Power-up que cae del cielo: brillitos que bajan
x = np.zeros(int(0.9 * SR))
for i, n in enumerate([96, 93, 89, 86, 84]):
    place(x, campanita(midi(n), 0.4, 0.8), i * 0.09)
save('sfx_powerup_cae', x)

# Usar un power-up: arpegio que sube y un brillo
x = np.zeros(int(1.0 * SR))
for i, n in enumerate([72, 76, 79, 84, 88]):
    place(x, campanita(midi(n), 0.45, 0.9), i * 0.06)
t = t_(0.6)
place(x, 0.25 * rng.standard_normal(len(t)) * env_ad(t, 0.05, 0.12) * np.sin(2 * np.pi * 9000 * t), 0.3)
save('sfx_powerup', x)

# Llega el jefe final: fanfarria de trompetas (do mayor) con redoble
x = np.zeros(int(1.8 * SR))
for at, notas, d in [(0.0, [60, 64, 67], 0.18), (0.22, [60, 64, 67], 0.18), (0.44, [65, 69, 72], 0.9)]:
    for n in notas: place(x, metal(midi(n), d, 0.5), at)
t = t_(0.5)
redoble = rng.standard_normal(len(t)) * (0.5 + 0.5 * np.sin(2 * np.pi * 28 * t) ** 2) * np.clip(t / 0.3, 0, 1) * 0.25
place(x, redoble, 0.0)
save('sfx_jefe', x)

# Estrella que cae en su hueco (resultados)
x = np.zeros(int(0.6 * SR))
place(x, campanita(midi(88), 0.5, 1.0), 0.0)
place(x, campanita(midi(95), 0.4, 0.6), 0.05)
save('sfx_estrella', x)
print('listo')
