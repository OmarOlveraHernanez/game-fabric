"""Efectos de la escena y piezas del trompo que gira.
uso: python3 efectos.py <carpeta_ia> <carpeta_salida>
- fx_flama_azul, fx_flama, fx_burbuja, fx_chispa, fx_aceite (hoja magenta 3x2) y fx_vapor (blanco con alfa)
- trompo_carne (tira sin costura que se desliza), trompo_mascara (forma del cono), trompo_sombra (volumen del cono)
  Coordenadas del cono medidas sobre barra_choricera_v2 (rectangulo x 15..242, y 162..560)."""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)
src = open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'procesar.py')).read()
exec(src[src.index('def chroma_alpha'):src.index('def sheet')])
exec(src[src.index('def sheet'):src.index('# Taqueros:')])

# Suciedad que se va quedando en la barra mientras cocinas (KitchenScene.Mess)
sheet('suciedad_hoja.png', 'magenta', 4, 2, ['sucio_salsa_roja', 'sucio_salsa_verde', 'sucio_grasa', 'sucio_cebolla',
                                             'sucio_cilantro', 'sucio_migajas', 'sucio_servilleta', 'sucio_limon'], 140, 140)
sheet('efectos_hoja.png', 'magenta', 3, 2, ['fx_flama_azul', 'fx_flama', None, 'fx_burbuja', 'fx_chispa', 'fx_aceite'], 200, 200)

# Vapor: la celda 3 como blanco puro con alfa (el verde dice que tan blanco es contra el magenta)
im = np.asarray(Image.open(os.path.join(SRC, 'efectos_hoja.png')).convert('RGB')).astype(np.float32)
h, w, _ = im.shape
cell = im[0:h // 2, 2 * w // 3:w]
a = np.clip((cell[..., 1] - 20) / 200, 0, 1)
ys, xs = np.where(a > 0.05)
cell_a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
v = np.dstack([np.full_like(cell_a, 255)] * 3 + [cell_a * 255]).astype(np.uint8)
vap = Image.fromarray(v, 'RGBA'); vap.thumbnail((200, 200), Image.LANCZOS)
vap.save(os.path.join(OUT, 'fx_vapor.png')); print('fx_vapor', vap.size)

# Trompo
X0, Y0, X1, Y1 = 15, 162, 242, 560
bg = Image.open(os.path.join(SRC, 'barra_choricera_v2.png')).convert('RGB')
crop = bg.crop((X0, Y0, X1, Y1))
strip = Image.new('RGB', (crop.width * 2, crop.height))
strip.paste(crop, (0, 0)); strip.paste(crop.transpose(Image.FLIP_LEFT_RIGHT), (crop.width, 0))
strip.save(os.path.join(OUT, 'trompo_carne.jpg'), quality=90); print('trompo_carne', strip.size)
left = [(18, 172), (19, 215), (27, 280), (39, 340), (54, 400), (71, 450), (89, 500), (107, 540), (121, 558)]
right = [(238, 176), (235, 215), (227, 280), (213, 340), (199, 400), (184, 450), (167, 500), (147, 540), (131, 558)]
top = [(40, 166), (90, 163), (128, 162), (170, 163), (215, 167)]
poly = [(x - X0, y - Y0) for x, y in left + right[::-1] + top[::-1]]
S = 4
m = Image.new('L', (crop.width * S, crop.height * S), 0)
ImageDraw.Draw(m).polygon([(x * S, y * S) for x, y in poly], fill=255)
m = m.resize(crop.size, Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.6))
Image.merge('RGBA', [m, m, m, m]).save(os.path.join(OUT, 'trompo_mascara.png')); print('trompo_mascara', m.size)
# volumen: orillas oscuras y un brillo vertical a un tercio
mask = np.asarray(m).astype(np.float32) / 255
H, W = mask.shape
sh = np.zeros((H, W, 4), np.float32)
for y in range(H):
    row = np.where(mask[y] > 0.5)[0]
    if len(row) < 2: continue
    l, r = row.min(), row.max(); c = (l + r) / 2; hw = max((r - l) / 2, 1)
    xs = np.arange(W); u = (xs - c) / hw
    dark = np.clip(np.abs(u), 0, 1) ** 2.2 * 0.62
    shine = np.exp(-((u + 0.35) / 0.16) ** 2) * 0.22
    sh[y, :, 0:3] = np.where(shine[:, None] > dark[:, None], 255, 20)
    sh[y, :, 3] = np.maximum(dark, shine) * 255 * mask[y]
Image.fromarray(sh.clip(0, 255).astype(np.uint8), 'RGBA').save(os.path.join(OUT, 'trompo_sombra.png')); print('trompo_sombra')
