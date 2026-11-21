"""Huecos organicos que van apareciendo en las ollas al servir (cucharonazos).
De ia/huecos_ollas.png (6 texturas: caldo rojo, blanco, verde, agua de elote, fondo de barro, fondo de vaporera)
saca el centro de cada una y le pone una orilla irregular suave con una sombrita adentro, para que parezca hundido.
uso: python3 huecos.py carpeta_salida"""
import sys, os
import numpy as np
from PIL import Image, ImageFilter

OUT = sys.argv[1]
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ia', 'huecos_ollas.png')
NOMBRES = ['hueco_rojo', 'hueco_blanco', 'hueco_verde', 'hueco_agua', 'hueco_barro', 'hueco_vaporera']
S = 256
rng = np.random.default_rng(7)

im = Image.open(SRC).convert('RGB')
W, H = im.size
cw, ch = W / 3, H / 2
yy, xx = np.mgrid[0:S, 0:S]
cx = cy = (S - 1) / 2
ang = np.arctan2(yy - cy, xx - cx)
dist = np.hypot(xx - cx, yy - cy) / (S / 2)

for i, nm in enumerate(NOMBRES):
    c, r = i % 3, i // 3
    mx, my = (c + 0.5) * cw, (r + 0.5) * ch
    side = min(cw, ch) * 0.46  # solo el centro: sin la orilla de florecita
    tex = im.crop((int(mx - side / 2), int(my - side / 2), int(mx + side / 2), int(my + side / 2))).resize((S, S), Image.LANCZOS)
    # orilla irregular: radio con ondas suaves al azar
    rad = 0.78 + sum(rng.uniform(0.02, 0.06) * np.sin(k * ang + rng.uniform(0, 6.28)) for k in (2, 3, 5, 7))
    m = np.clip((rad - dist) / 0.10, 0, 1)
    alpha = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3))
    # sombrita en la orilla de adentro: se ve hundido
    a = np.asarray(alpha).astype(float) / 255
    edge = np.clip(1 - (rad - dist) / 0.22, 0, 1) * (a > 0)
    t = np.asarray(tex).astype(float)
    if nm == 'hueco_blanco':  # el caldo blanco salia muy amarillo: mas crema, como el de la olla
        t = t * 0.55 + np.array([226, 214, 186]) * 0.45
    t = t * 0.88 * (1 - 0.45 * edge[..., None])  # un poco mas oscuro: el nivel bajo
    out = np.dstack([np.clip(t, 0, 255), a * 255]).astype(np.uint8)
    out[out[..., 3] == 0] = 0
    Image.fromarray(out, 'RGBA').save(os.path.join(OUT, nm + '.png'), optimize=True)
    print(nm)
