"""Genera el aura dorada del escudo (tamal): flamas que suben, estilo 'super guerrero'.
Lienzo 384x448 (1.5 x 1.75 unidades), pivote abajo-centro: se coloca en los pies de Papi."""
import numpy as np, sys
from PIL import Image
W, H, PPU = 384, 448, 256
FRAMES = 4
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
x = (xx - W / 2) / PPU                    # unidades, 0 = centro
y = (H - yy) / PPU                        # unidades, 0 = pies
cx, cy = 0.0, 0.44                        # centro del cuerpo de Papi
dx, dy = x - cx, (y - cy)
ang = np.arctan2(dy, dx)                  # pi/2 = arriba
rad = np.sqrt((dx / 0.56) ** 2 + (dy / 0.50) ** 2)   # elipse base
def smooth(e0, e1, v):
    t = np.clip((v - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)
out_dir = sys.argv[1]
rng = np.random.default_rng(7)
for f in range(FRAMES):
    ph = f / FRAMES * 2 * np.pi
    up = np.clip(np.sin(ang), 0, 1) ** 1.6                 # las flamas crecen hacia arriba
    spikes = (np.abs(np.sin(5 * ang + ph)) ** 3 * 0.55 +
              np.abs(np.sin(9 * ang - 1.7 * ph + 1.3)) ** 4 * 0.35 +
              np.abs(np.sin(13 * ang + 2.3 * ph + 0.4)) ** 6 * 0.25)
    reach = 1.0 + up * (0.28 + 0.72 * spikes) + 0.04 * np.sin(17 * ang + ph)
    d = rad / reach                                         # <1 dentro del aura
    # vetas verticales que suben (dan la sensacion de energia)
    streak = 0.5 + 0.5 * np.sin(x * 38 + np.sin(y * 3 + ph) * 2) * np.sin(y * 9 - ph * 2)
    body = smooth(1.02, 0.80, d)                           # forma principal con borde suave
    rim = smooth(0.55, 0.84, d) * body                     # mas fuerte en el borde, casi vacia al centro
    glow = smooth(1.22, 0.95, d) * 0.22                    # halo externo tenue
    a = np.clip(rim * (0.85 + 0.35 * streak) + glow * (1 - body), 0, 1)
    a *= smooth(-0.05, 0.12, y)                            # se desvanece en el suelo
    # color: blanco-amarillo por dentro del borde, dorado, naranja hacia afuera
    t = np.clip((d - 0.62) / 0.45, 0, 1)
    c0, c1, c2 = np.array([255, 252, 215.]), np.array([255, 214, 60.]), np.array([255, 150, 20.])
    col = np.where(t[..., None] < 0.5, c0 + (c1 - c0) * (t[..., None] / 0.5), c1 + (c2 - c1) * ((t[..., None] - 0.5) / 0.5))
    img = np.dstack([col, a[..., None] * 250]).clip(0, 255).astype(np.uint8)
    # chispas
    im = Image.fromarray(img).copy()
    px = im.load()
    for _ in range(14):
        sx = rng.uniform(-0.5, 0.5); sy = rng.uniform(0.25, 1.25) + (f * 0.07) % 0.3
        X, Y = int(W / 2 + sx * PPU), int(H - sy * PPU)
        for ox in range(-2, 3):
            for oy in range(-6, 7):
                if 0 <= X + ox < W and 0 <= Y + oy < H and abs(ox) + abs(oy) / 3 <= 2:
                    px[X + ox, Y + oy] = (255, 250, 220, 230)
    im.save(f'{out_dir}/aura_{f}.png')
print('ok')
