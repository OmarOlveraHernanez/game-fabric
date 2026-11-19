"""Musica y efectos de Taquería Neón, compuestos por codigo (100% propios, sin licencias).
Musica: cumbia urbana de noche (bajo tumbao, güiro, cencerro, tarola y un acordeon de neon).
Efectos: plancha, tortilla, campanita, caja registradora, error, cliente, click, quemado.
Salida: WAV mono 44.1 kHz.  uso: python3 synth_tacos.py carpeta_salida"""
import sys, os, numpy as np
from scipy.io import wavfile
from scipy.signal import butter, lfilter

SR = 44100
rng = np.random.default_rng(11)
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)


def t_(d): return np.arange(int(d * SR)) / SR
def band(x, lo, hi):
    b, a = butter(2, [lo / (SR / 2), hi / (SR / 2)], 'band'); return lfilter(b, a, x)
def low(x, hi):
    b, a = butter(2, hi / (SR / 2), 'low'); return lfilter(b, a, x)
def high(x, lo):
    b, a = butter(2, lo / (SR / 2), 'high'); return lfilter(b, a, x)
def noise(n): return rng.standard_normal(n)
def midi(n): return 440.0 * 2 ** ((n - 69) / 12)


def place(buf, snd, at, wrap=True):
    i = int(at * SR); n = len(snd)
    if wrap:
        idx = (np.arange(n) + i) % len(buf); np.add.at(buf, idx, snd)
    else:
        end = min(len(buf), i + n); buf[i:end] += snd[:end - i]


def master(x, peak=0.89): return (x / (np.max(np.abs(x)) + 1e-9) * peak).astype(np.float32)
def save(name, x): wavfile.write(f'{OUT}/{name}.wav', SR, (master(x) * 32767).astype(np.int16))


# ---------- instrumentos ----------
def bajo(f, d, v=1.0):
    t = t_(d + 0.08)
    ph = 2 * np.pi * f * t
    x = np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.12 * np.sin(3 * ph)
    env = np.clip(t / 0.008, 0, 1) * np.where(t < d, np.exp(-t / 0.6), np.exp(-(d) / 0.6) * np.exp(-(t - d) / 0.03))
    return v * low(x, 900) * env


def guiro(d=0.12, v=1.0):
    t = t_(d)
    teeth = 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 70 * t))          # dientes del güiro
    x = band(noise(len(t)), 2500, 7000) * teeth * np.clip(t / 0.01, 0, 1) * np.exp(-t / (d * 0.7))
    return v * x * 0.5


def cencerro(v=1.0):
    t = t_(0.18)
    x = np.sign(np.sin(2 * np.pi * 800 * t)) + 0.8 * np.sign(np.sin(2 * np.pi * 540 * t))
    return v * band(x, 400, 3000) * np.exp(-t / 0.05) * 0.35


def tarola(v=1.0):
    t = t_(0.2)
    x = 0.6 * np.sin(2 * np.pi * 190 * t) * np.exp(-t / 0.04) + high(noise(len(t)), 1800) * np.exp(-t / 0.06)
    return v * x * 0.6


def bombo(v=1.0):
    t = t_(0.3)
    f = 55 + 90 * np.exp(-t / 0.03)
    return v * np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-t / 0.16)


def acordeon(f, d, v=1.0):
    t = t_(d + 0.1)
    vib = 1 + 0.005 * np.sin(2 * np.pi * 6 * t)
    ph = 2 * np.pi * np.cumsum(f * vib) / SR
    ph2 = 2 * np.pi * np.cumsum(f * 1.004 * vib) / SR                  # dos lengüetas desafinadas = acordeon
    x = np.sign(np.sin(ph)) * 0.5 + np.sign(np.sin(ph2)) * 0.5 + 0.4 * np.sin(2 * ph)
    env = np.clip(t / 0.02, 0, 1) * np.where(t < d, 1, np.exp(-(t - d) / 0.05))
    return v * low(x, 3200) * env * 0.35


def marimba(f, v=1.0):
    t = t_(0.5)
    x = np.sin(2 * np.pi * f * t) * np.exp(-t / 0.18) + 0.3 * np.sin(2 * np.pi * f * 4 * t) * np.exp(-t / 0.03)
    return v * x * 0.6


def acorde(root, d, v=1.0):
    """Acorde mayor sostenido suave (pad de neon)."""
    t = t_(d)
    x = sum(np.sin(2 * np.pi * midi(root + k) * t) for k in (0, 4, 7, 12))
    env = np.clip(t / 0.15, 0, 1) * np.clip((d - t) / 0.15, 0, 1)
    return v * x * env * 0.08


# ---------- musica ----------
BPM = 100
BEAT = 60 / BPM
PROG = [57, 52, 57, 52, 50, 52, 57, 52]   # La - Mi - La - Mi - Re - Mi - La - Mi (tonalidad de La mayor; un compas por acorde)


def groove(bars, lead_fn, lead_notes, with_pad=False):
    total = bars * 4 * BEAT
    buf = np.zeros(int(total * SR))
    for bar in range(bars):
        root = PROG[bar % len(PROG)]
        t0 = bar * 4 * BEAT
        # bajo tumbao: tonica en 1, quinta en 3, tonica en el "y" del 4
        place(buf, bajo(midi(root - 12), BEAT * 0.9), t0)
        place(buf, bajo(midi(root - 5), BEAT * 0.9), t0 + 2 * BEAT)
        place(buf, bajo(midi(root - 12), BEAT * 0.4, 0.8), t0 + 3.5 * BEAT)
        for b in range(4):
            place(buf, guiro(BEAT * 0.45, 0.9 if b % 2 == 0 else 0.7), t0 + b * BEAT)       # güiro largo
            place(buf, guiro(BEAT * 0.2, 0.5), t0 + (b + 0.5) * BEAT)                       # güiro corto
            place(buf, cencerro(0.5 if b % 2 else 0.7), t0 + (b + 0.5) * BEAT)
        place(buf, bombo(0.9), t0); place(buf, bombo(0.7), t0 + 2 * BEAT)
        place(buf, tarola(0.6), t0 + 1 * BEAT); place(buf, tarola(0.7), t0 + 3 * BEAT)
        if with_pad:
            place(buf, acorde(root, 4 * BEAT), t0)
    for (beat, note, dur) in lead_notes:
        if note is None: continue
        place(buf, lead_fn(midi(note), dur * BEAT) if lead_fn is acordeon else lead_fn(midi(note)), beat * BEAT)
    return buf


# Melodia de 8 compases (32 tiempos): (tiempo, nota midi, duracion en tiempos)
MELODIA = [
    (0, 69, 1), (1, 73, 1), (2, 76, 1.5), (3.5, 73, 0.5),
    (4, 71, 1), (5, 68, 1), (6, 71, 2),
    (8, 69, 0.5), (8.5, 71, 0.5), (9, 73, 1), (10, 76, 1), (11, 78, 1),
    (12, 76, 2), (14, 73, 1), (15, 71, 1),
    (16, 74, 1), (17, 78, 1), (18, 81, 1.5), (19.5, 78, 0.5),
    (20, 76, 1), (21, 73, 1), (22, 71, 2),
    (24, 73, 0.5), (24.5, 76, 0.5), (25, 78, 1), (26, 76, 1), (27, 73, 1),
    (28, 71, 1), (29, 68, 1), (30, 69, 2),
]

partida = groove(8, acordeon, MELODIA)
partida = partida + 0.15 * low(np.roll(partida, int(0.18 * SR)), 2500)   # eco corto de calle
save('musica_partida', partida)

MARIMBA = [(b, n, 0) for (b, n, _) in MELODIA[::2]] + [(b + 0.5, n + 7, 0) for (b, n, _) in MELODIA[1::3]]
menu = groove(8, marimba, MARIMBA, with_pad=True)
save('musica_menu', menu * 0.85)

# ---------- efectos ----------
def env_ad(t, a, d): return np.clip(t / a, 0, 1) * np.exp(-t / d)

# plancha: chisporroteo al poner la carne
t = t_(0.6)
crackle = np.zeros(len(t))
for _ in range(60):
    i = rng.integers(0, len(t) - 400)
    crackle[i:i + 400] += high(noise(400), 3000) * np.exp(-np.arange(400) / 60) * rng.uniform(0.3, 1)
sizzle = band(noise(len(t)), 3000, 9000) * 0.4 + crackle
save('sfx_plancha', sizzle * env_ad(t, 0.02, 0.35))

# tortilla: palmada suave
t = t_(0.15)
save('sfx_tortilla', low(noise(len(t)), 1800) * env_ad(t, 0.003, 0.03) + 0.4 * np.sin(2 * np.pi * 180 * t) * env_ad(t, 0.002, 0.04))

# extra: "pop" corto (cebolla, cilantro...)
t = t_(0.09)
f = 900 + 600 * np.exp(-t / 0.02)
save('sfx_extra', np.sin(2 * np.pi * np.cumsum(f) / SR) * env_ad(t, 0.002, 0.03))

# carne lista: campanita
t = t_(0.5)
save('sfx_listo', (np.sin(2 * np.pi * 1320 * t) + 0.5 * np.sin(2 * np.pi * 1980 * t)) * env_ad(t, 0.002, 0.18))

# entrega correcta: caja registradora (clic + campana doble)
t = t_(0.7)
reg = high(noise(len(t)), 2000) * env_ad(t, 0.001, 0.015) * 0.6
reg += np.sin(2 * np.pi * 1568 * t) * env_ad(t, 0.002, 0.25)
t2 = np.clip(t - 0.12, 0, None)
reg += np.where(t > 0.12, np.sin(2 * np.pi * 2093 * t2) * env_ad(t2, 0.002, 0.3), 0)
save('sfx_cobro', reg)

# error: dos notas descendentes
t = t_(0.45)
f = np.where(t < 0.2, 330, 247)
x = np.sign(np.sin(2 * np.pi * np.cumsum(f) / SR)) * 0.5
save('sfx_error', low(x, 2000) * np.clip(t / 0.01, 0, 1) * np.exp(-t / 0.3))

# llega cliente: campana de puerta
t = t_(0.6)
save('sfx_cliente', (np.sin(2 * np.pi * 988 * t) * env_ad(t, 0.002, 0.2) +
                     np.where(t > 0.1, np.sin(2 * np.pi * 784 * (t - 0.1)) * env_ad(np.clip(t - 0.1, 0, None), 0.002, 0.25), 0)))

# se quemo: siseo grave
t = t_(0.7)
save('sfx_quemado', band(noise(len(t)), 300, 2500) * env_ad(t, 0.05, 0.3) + 0.3 * np.sin(2 * np.pi * 110 * t) * env_ad(t, 0.01, 0.2))

# click de interfaz
t = t_(0.05)
save('sfx_click', np.sin(2 * np.pi * 1100 * t) * env_ad(t, 0.001, 0.012))

# fin del turno: fanfarria corta de acordeon
fan = np.zeros(int(1.6 * SR))
for i, (n, d) in enumerate([(69, 0.15), (73, 0.15), (76, 0.15), (81, 0.8)]):
    place(fan, acordeon(midi(n), d), i * 0.16, wrap=False)
save('sfx_fin', fan)
print('ok')
