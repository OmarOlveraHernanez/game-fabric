"""Titulo y logo de neon con el nombre nuevo, Antojitos Neon (21 nov. 2026).
uso: python3 titulos.py carpeta_salida ruta_poppins_bold_italic"""
import sys, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter
OUT, FONT = sys.argv[1], sys.argv[2]
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ia', 'tienda')
ROSA, TURQ = (255, 60, 170), (40, 225, 225)

def neon_layer(size, lines):
    """lines: (texto, tamano, (x, y), color del centro, color del brillo). Fondo transparente."""
    im = Image.new('RGBA', size, (0, 0, 0, 0))
    for text, fs, xy, core, glow in lines:
        font = ImageFont.truetype(FONT, fs)
        g = Image.new('RGBA', size, (0, 0, 0, 0))
        ImageDraw.Draw(g).text(xy, text, font=font, fill=glow + (255,), stroke_width=max(4, fs // 16), stroke_fill=glow + (255,))
        for r in (fs // 5, fs // 10):
            im.alpha_composite(g.filter(ImageFilter.GaussianBlur(r)))
        ImageDraw.Draw(im).text(xy, text, font=font, fill=core + (255,), stroke_width=max(2, fs // 30), stroke_fill=glow + (255,))
    return im

def centered(fs, text, width):
    f = ImageFont.truetype(FONT, fs)
    l, t, r, b = f.getbbox(text)
    return (width - (r - l)) // 2 - l

# Titulo del mapa de mundos (803 x 380): dos renglones centrados
W, H = 803, 380
t = neon_layer((W, H), [('ANTOJITOS', 118, (centered(118, 'ANTOJITOS', W), 30), (255, 238, 248), ROSA),
                        ('NEÓN', 130, (centered(130, 'NEÓN', W), 190), (230, 255, 252), TURQ)])
t.save(os.path.join(OUT, 'titulo.png'))

# Logo (1160 x 543): igual, con el taco de neon del logo anterior junto a NEON
W, H = 1160, 543
lg = neon_layer((W, H), [('ANTOJITOS', 170, (centered(170, 'ANTOJITOS', W), 20), (255, 238, 248), ROSA),
                         ('NEÓN', 185, (centered(185, 'NEÓN', W) - 110, 250), (230, 255, 252), TURQ)])
old = Image.open(os.path.join(SRC, 'logo_viejo.png')).convert('RGBA')
taco = old.crop((820, 300, 1100, 543))
import numpy as np
tp = np.asarray(taco).astype(float)
lum = tp[..., :3].max(axis=2) * tp[..., 3] / 255  # brillo real (el logo viejo guarda el color sin multiplicar)
tp[..., 3] = np.minimum(tp[..., 3], np.clip(lum * 1.2, 0, 255))  # sin la caja oscura de alrededor
hh, ww = tp.shape[:2]
yy, xx = np.mgrid[0:hh, 0:ww]
edge = np.minimum.reduce([xx, ww - 1 - xx, yy, hh - 1 - yy]).astype(float)
tp[..., 3] *= np.clip(edge / 28, 0, 1)  # el brillo se desvanece antes de la orilla del recorte (si no, se ve la caja)
taco = Image.fromarray(tp.astype(np.uint8), 'RGBA')
taco = taco.crop(taco.getchannel('A').point(lambda v: 255 if v > 20 else 0).getbbox())
taco = taco.resize((int(taco.width * 1.05), int(taco.height * 1.05)), Image.LANCZOS)
f = ImageFont.truetype(FONT, 185); l, tp, r, b = f.getbbox('NEÓN')
x = centered(185, 'NEÓN', W) - 110 + (r - l) + 50
lg.alpha_composite(taco, (min(x, W - taco.width - 5), 300))
lg.save(os.path.join(OUT, 'logo.png'))
print('listo')
