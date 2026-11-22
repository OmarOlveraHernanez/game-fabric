"""Icono e imagen destacada de la ficha de Play (Antojitos Neon). uso: python3 tienda.py carpeta_salida ruta_fuente_poppins_bold_italic"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
OUT, FONT = sys.argv[1], sys.argv[2]
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ia', 'tienda')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

def key_magenta(im):
    a = np.asarray(im.convert('RGB')).astype(float)
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    d = np.minimum(r, b) - g          # que tan magenta es
    alpha = np.clip((150 - d) / 70, 0, 1)
    # quitar el tinte magenta de las orillas
    spill = np.clip(np.minimum(r, b) - g - 40, 0, None) * (1 - alpha)
    a[..., 0] -= spill * 0.6; a[..., 2] -= spill * 0.6
    out = np.dstack([np.clip(a, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')

fg = key_magenta(Image.open(os.path.join(SRC, 'icono_frente_ia.png')))
fg = fg.crop(fg.getbbox())
bg = Image.open(os.path.join(SRC, 'icono_fondo_ia.png')).convert('RGBA')

def fit(img, box):
    k = box / max(img.size)
    return img.resize((int(img.width * k), int(img.height * k)), Image.LANCZOS)

# Icono adaptable de Android: fondo y frente de 432; el frente dentro de la zona segura (66 %)
bg.resize((432, 432), Image.LANCZOS).convert('RGB').save(os.path.join(OUT, 'icono_fondo_432.png'))
front = Image.new('RGBA', (432, 432), (0, 0, 0, 0))
f = fit(fg, 300); front.alpha_composite(f, ((432 - f.width) // 2, (432 - f.height) // 2))
front.save(os.path.join(OUT, 'icono_frente_432.png'))
# Icono completo (Play y el de siempre)
for size in (1024, 512, 192):
    ic = bg.resize((size, size), Image.LANCZOS)
    f = fit(fg, int(size * 0.84)); ic.alpha_composite(f, ((size - f.width) // 2, (size - f.height) // 2))
    ic.convert('RGB').save(os.path.join(OUT, f'icono_{size}.png'))

# Imagen destacada 1024 x 500 con el titulo a la izquierda, en neon
ban = Image.open(os.path.join(SRC, 'destacada_ia.png')).convert('RGB')
W, H = ban.size; h = int(W / 2.048); y0 = (H - h) // 2
ban = ban.crop((0, y0, W, y0 + h)).resize((1024, 500), Image.LANCZOS).convert('RGBA')
shade = Image.new('RGBA', ban.size, (0, 0, 0, 0)); sd = ImageDraw.Draw(shade)
for x in range(520):  # oscurece un poco la izquierda para que se lea el titulo
    sd.line([(x, 0), (x, 500)], fill=(10, 4, 24, int(150 * (1 - x / 520))))
ban.alpha_composite(shade)
def neon(text, size, xy, core, glow):
    font = ImageFont.truetype(FONT, size)
    lay = Image.new('RGBA', ban.size, (0, 0, 0, 0)); d = ImageDraw.Draw(lay)
    d.text(xy, text, font=font, fill=glow + (255,), stroke_width=6, stroke_fill=glow + (255,))
    g = lay.filter(ImageFilter.GaussianBlur(14))
    ban.alpha_composite(g); ban.alpha_composite(g)
    d2 = ImageDraw.Draw(ban)
    d2.text(xy, text, font=font, fill=core, stroke_width=3, stroke_fill=glow)
neon('Antojitos', 96, (44, 120), (255, 244, 250), (255, 60, 170))
neon('Neón', 112, (70, 222), (235, 255, 252), (40, 220, 220))
d = ImageDraw.Draw(ban)
d.text((50, 378), '¡Cocina los antojitos de la calle!', font=ImageFont.truetype(FONT, 30), fill=(255, 225, 120), stroke_width=3, stroke_fill=(40, 12, 30))
ban.convert('RGB').save(os.path.join(OUT, 'destacada_1024x500.png'))
print('listo')
