"""Suaviza capas de fondo para reducir el mareo: desenfoque horizontal (como desenfoque de movimiento)
que da la vuelta en los bordes (la capa sigue repitiendose sin costura) y menos contraste en patrones repetidos.
uso: python3 calm.py entrada.png salida.png RADIO_X [FILA_DESDE] [CONTRASTE]"""
import sys, numpy as np
from PIL import Image
from scipy import ndimage
src, dst, rx = sys.argv[1], sys.argv[2], int(sys.argv[3])
y0 = int(sys.argv[4]) if len(sys.argv) > 4 else 0
contrast = float(sys.argv[5]) if len(sys.argv) > 5 else 1.0
a = np.asarray(Image.open(src).convert('RGBA')).astype(np.float32)
al = a[..., 3:4] / 255
pm = a[..., :3] * al                                   # premultiplicado: sin halos en lo transparente
part = slice(y0, None)
k = 2 * rx + 1
pm[part] = ndimage.uniform_filter1d(pm[part], k, axis=1, mode='wrap')
al_b = al.copy(); al_b[part] = ndimage.uniform_filter1d(al[part], k, axis=1, mode='wrap')
rgb = pm / np.maximum(al_b, 1e-4)
if contrast != 1.0:
    m = rgb[part].reshape(-1, 3)[al_b[part].reshape(-1) > 0.5].mean(0)
    rgb[part] = m + (rgb[part] - m) * contrast
out = np.dstack([rgb, al_b * 255]).clip(0, 255).astype(np.uint8)
Image.fromarray(out).save(dst)
