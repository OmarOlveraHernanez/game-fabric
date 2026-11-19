# Hace que un adorno se vea "metido" en la grasa: tono calido, base hundida en aceite dorado y orilla suave
import sys, numpy as np
from PIL import Image, ImageFilter
src, dst = sys.argv[1], sys.argv[2]
im = Image.open(src).convert('RGBA'); a = np.asarray(im).astype(np.float32) / 255
h, w = a.shape[:2]
rgb, al = a[..., :3], a[..., 3]
oil = np.array([0.86, 0.55, 0.16])
# 1) todo un poco mas dorado y menos saturado (como bañado en grasa)
gray = rgb.mean(axis=2, keepdims=True)
rgb = rgb * 0.8 + gray * 0.2
rgb = rgb * np.array([1.0, 0.9, 0.72])
# 2) la mitad de abajo de cada pieza se hunde: se mezcla con el color del aceite
ys = np.linspace(0, 1, h)[:, None, None]
# por columna: la base real de la pieza
sink = np.zeros((h, w, 1))
for x in range(w):
    col = np.where(al[:, x] > 0.2)[0]
    if len(col) < 2: continue
    top, bot = col[0], col[-1]
    t = (np.arange(h) - top) / max(1, bot - top)
    sink[:, x, 0] = np.clip((t - 0.55) / 0.45, 0, 1)
rgb = rgb * (1 - 0.55 * sink) + oil * 0.55 * sink
al = al * (1 - 0.35 * sink[..., 0])
# 3) orilla suave (sin recorte duro)
A = Image.fromarray((al * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2))
out = np.dstack([np.clip(rgb, 0, 1) * 255, np.asarray(A)]).astype(np.uint8)
Image.fromarray(out, 'RGBA').save(dst, optimize=True)
