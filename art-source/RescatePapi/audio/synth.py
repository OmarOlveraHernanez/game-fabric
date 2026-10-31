"""Musica y efectos de Rescate Papi, compuestos por codigo (100% propios, sin licencias).
Instrumentos de inspiracion prehispanica: huehuetl (tambor grave), teponaztli (tambor de madera de 2 tonos),
sonajas, flauta de barro (pentatonica) y caracol. Salida: WAV mono 44.1 kHz.
uso: python3 synth.py carpeta_salida"""
import sys, os, numpy as np
from scipy.io import wavfile
from scipy.signal import butter, lfilter
SR = 44100
rng = np.random.default_rng(7)
OUT = sys.argv[1]; os.makedirs(OUT, exist_ok=True)

def t_(d): return np.arange(int(d * SR)) / SR
def band(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], 'band'); return lfilter(b, a, x)
def low(x, hi):
    b, a = butter(2, hi / (SR / 2), 'low'); return lfilter(b, a, x)

# ---------- instrumentos ----------
def huehuetl(v=1.0):
    t = t_(0.7)
    f = 52 + 70 * np.exp(-t / 0.035)
    ph = 2 * np.pi * np.cumsum(f) / SR
    body = np.sin(ph) * np.exp(-t / 0.22)
    hit = low(rng.standard_normal(len(t)), 900) * np.exp(-t / 0.012) * 0.6
    return v * (body + hit)

def teponaztli(f, v=1.0):
    t = t_(0.45)
    x = (np.sin(2 * np.pi * f * t) * np.exp(-t / 0.13)
         + 0.45 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / 0.045)
         + 0.2 * np.sin(2 * np.pi * f * 5.4 * t) * np.exp(-t / 0.02))
    x += band(rng.standard_normal(len(t)), 1500, 5000) * np.exp(-t / 0.004) * 0.5
    return v * x

def sonaja(v=1.0):
    t = t_(0.12); x = np.zeros(len(t))
    for k in range(5):                                   # semillas que chocan
        o = int(rng.uniform(0, 0.025) * SR)
        n = band(rng.standard_normal(len(t) - o), 3500, 11000) * np.exp(-t[:len(t) - o] / 0.018)
        x[o:] += n * rng.uniform(0.4, 1)
    return v * 0.5 * x

def flauta(f, d, v=1.0, scoop=True):
    t = t_(d + 0.25)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.2 * t) * np.clip((t - 0.15) / 0.2, 0, 1)
    fs = f * vib * (1 - (0.018 * np.exp(-t / 0.04) if scoop else 0))
    ph = 2 * np.pi * np.cumsum(fs) / SR
    tone = np.sin(ph) + 0.22 * np.sin(2 * ph) + 0.07 * np.sin(3 * ph)
    env = np.clip(t / 0.05, 0, 1) * np.where(t < d, 1, np.exp(-(t - d) / 0.07))
    breath = band(rng.standard_normal(len(t)), 1800, 4500) * 0.05
    return v * env * (tone + breath)

def caracol(f, d, v=1.0):
    t = t_(d + 0.3)
    fs = f * (1 + 0.04 * (1 - np.exp(-t / 0.15)))
    ph = 2 * np.pi * np.cumsum(fs) / SR
    x = sum(np.sin(n * ph) / n ** 1.3 for n in range(1, 9))
    env = np.clip(t / 0.12, 0, 1) * np.where(t < d, 1, np.exp(-(t - d) / 0.12))
    return v * low(x, 1800) * env * 0.6

# ---------- utilidades ----------
def place(buf, snd, at, wrap=True):
    i = int(at * SR); n = len(snd)
    if wrap:
        idx = (np.arange(n) + i) % len(buf); np.add.at(buf, idx, snd)
    else:
        end = min(len(buf), i + n); buf[i:end] += snd[:end - i]

def reverb_circular(x, secs=0.9, mix=0.2):
    n = len(x); ir = np.zeros(n); L = int(secs * SR)
    ir[:L] = rng.standard_normal(L) * np.exp(-np.arange(L) / SR / (secs / 4)); ir = low(ir, 5000)
    wet = np.real(np.fft.ifft(np.fft.fft(x) * np.fft.fft(ir)))
    wet *= np.std(x) / (np.std(wet) + 1e-9)
    return x + mix * wet

def master(x, peak=0.89):
    return (x / (np.max(np.abs(x)) + 1e-9) * peak).astype(np.float32)

def save(name, x): wavfile.write(f'{OUT}/{name}.wav', SR, (master(x) * 32767).astype(np.int16))

# Pentatonica de La menor
N = dict(A3=220.0, C4=261.63, D4=293.66, E4=329.63, G4=392.0, A4=440.0, C5=523.25, D5=587.33, E5=659.25, G5=783.99, A5=880.0)

# ---------- musica de carrera (120 bpm, 16 compases = 32 s, loop perfecto) ----------
def tema_carrera():
    bpm = 120; s16 = 60 / bpm / 4; bars = 16; L = bars * 16 * s16
    buf = np.zeros(int(L * SR))
    hue = [0, 6, 8, 11]; tep = {0: 'b', 2: 'a', 4: 'b', 5: 'a', 7: 'b', 8: 'b', 10: 'a', 12: 'b', 13: 'a', 15: 'a'}
    for b in range(bars):
        base = b * 16 * s16
        for st in hue: place(buf, huehuetl(0.9 if st in (0, 8) else 0.6), base + st * s16)
        for st, k in tep.items():
            place(buf, teponaztli(185 if k == 'b' else 247, 0.35 if st % 4 else 0.5), base + st * s16)
        for st in range(0, 16, 2): place(buf, sonaja(0.35 if st % 4 else 0.2), base + st * s16 + 0.01)
    A = [('E5', 0, 4), ('D5', 4, 2), ('C5', 6, 2), ('A4', 8, 6), ('G4', 14, 2), ('A4', 16, 4), ('C5', 20, 4), ('D5', 24, 6), ('E5', 30, 2)]
    B = [('D5', 0, 4), ('E5', 4, 2), ('D5', 6, 2), ('C5', 8, 4), ('A4', 12, 4), ('G4', 16, 6), ('A4', 22, 2), ('C5', 24, 4), ('A4', 28, 4)]
    B2 = [('G5', 0, 4), ('E5', 4, 2), ('D5', 6, 2), ('E5', 8, 4), ('G5', 12, 4), ('A5', 16, 8), ('G5', 24, 4), ('E5', 28, 4)]
    C = [('A4', 0, 2), ('C5', 4, 2), ('A4', 8, 2), ('G4', 12, 2), ('A4', 16, 2), ('C5', 20, 2), ('D5', 24, 4)]
    for start, ph in [(4, A), (6, B), (8, A), (10, B2), (12, C), (14, C)]:
        for n, st, d in ph:
            place(buf, flauta(N[n], d * s16 * 0.92, 0.55), start * 16 * s16 + st * s16)
    return reverb_circular(buf, 0.8, 0.22)

# ---------- musica de menu (84 bpm, 8 compases ~ 23 s, tranquila) ----------
def tema_menu():
    bpm = 84; beat = 60 / bpm; bars = 8; L = bars * 4 * beat
    buf = np.zeros(int(L * SR))
    for b in range(bars):
        base = b * 4 * beat
        place(buf, huehuetl(0.5), base)
        for k in range(4): place(buf, teponaztli(185 if k % 2 == 0 else 247, 0.25), base + k * beat + (beat / 2 if k == 3 else 0))
        place(buf, sonaja(0.15), base + 2.5 * beat)
    mel = [('A4', 0, 2), ('C5', 2, 1), ('D5', 3, 1), ('E5', 4, 3), ('D5', 7, 1), ('C5', 8, 2), ('A4', 10, 2), ('G4', 12, 4),
           ('A4', 16, 2), ('C5', 18, 1), ('E5', 19, 1), ('G5', 20, 3), ('E5', 23, 1), ('D5', 24, 2), ('C5', 26, 2), ('A4', 28, 4)]
    for n, b_, d in mel: place(buf, flauta(N[n], d * beat * 0.95, 0.6), b_ * beat)
    return reverb_circular(buf, 1.4, 0.32)

save('musica_carrera', tema_carrera())
save('musica_menu', tema_menu())

# ---------- efectos ----------
def cat(*parts):
    tot = max(at + len(s) / SR for s, at in parts); buf = np.zeros(int(tot * SR) + 1)
    for s, at in parts: place(buf, s, at, wrap=False)
    return buf
sfx = {
    'sfx_salto': cat((teponaztli(330, 0.8), 0), (sonaja(0.3), 0.0)),
    'sfx_moneda': cat((flauta(N['E5'] * 2, 0.06, 0.6, False), 0), (flauta(N['A5'] * 2, 0.12, 0.6, False), 0.06)),
    'sfx_tamal': cat(*[(flauta(N[n], 0.09, 0.7, False), i * 0.07) for i, n in enumerate(['A4', 'C5', 'D5', 'E5', 'A5'])]),
    'sfx_escudo_roto': cat((huehuetl(1.0), 0), (sonaja(0.8), 0.0), (teponaztli(160, 0.6), 0.05)),
    'sfx_golpe': cat((huehuetl(1.0), 0), (flauta(N['E5'], 0.12, 0.6), 0.05), (flauta(N['C5'], 0.12, 0.6), 0.18), (flauta(N['A4'], 0.35, 0.6), 0.31)),
    'sfx_inicio': cat((caracol(110, 0.55, 1.0), 0), (huehuetl(0.8), 0.0)),
    'sfx_click': teponaztli(420, 0.6)[: int(0.15 * SR)],
    'sfx_aguila': cat((band(rng.standard_normal(int(0.45 * SR)), 2500, 6000) * np.exp(-t_(0.45) / 0.15) * 0.5, 0),
                      (flauta(1500, 0.3, 0.7, False) * 0 + np.sin(2 * np.pi * np.cumsum(np.linspace(1900, 900, int(0.55 * SR))) / SR) * np.exp(-t_(0.55) / 0.2) * 0.6, 0)),
    'sfx_serpiente': cat((sonaja(1.0), 0), (sonaja(0.8), 0.06), (sonaja(0.9), 0.12), (teponaztli(220, 0.5), 0.15)),
}
for k, v in sfx.items(): save(k, v)
print('ok')
