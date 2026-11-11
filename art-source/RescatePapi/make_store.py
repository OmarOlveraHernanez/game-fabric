import sys, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
sys.path.insert(0, '/home/claude/work/papi')
from outline import outline
IA = '/mnt/user-data/uploads/game-fabric/art-source/RescatePapi/ia/'
FAR = '/home/claude/work/papi/out5/fondo_lejano.png'
FONT = '/usr/share/fonts/truetype/google-fonts/Poppins-Bold.ttf'

def key_green(img):
    a = np.asarray(img.convert('RGB')).astype(np.float32)
    bg = np.median(np.concatenate([a[:20].reshape(-1, 3), a[-20:].reshape(-1, 3)]), 0)
    d = np.sqrt(((a - bg) ** 2).sum(2)); al = np.clip((d - 60) / 80, 0, 1)
    from scipy import ndimage
    edge = ndimage.binary_dilation(al < 0.5, iterations=3) & (al > 0)
    g = a[..., 1]; m = np.maximum(a[..., 0], a[..., 2]); a[..., 1] = np.where(edge & (g > m), m, g)
    return Image.fromarray(np.dstack([a, al * 255]).astype(np.uint8))

papi = key_green(Image.open(IA + 'Bchibi.png'))
far = Image.open(FAR).convert('RGBA')

# ---------- icono 512 (Play) ----------
S = 512
# fondo: cielo y piramide del juego, recortado alrededor de la piramide
fw, fh = far.size
crop = far.crop((fw // 2 - 700, fh - 1750, fw // 2 + 700, fh - 350)).resize((S, S), Image.LANCZOS)
bg = crop.filter(ImageFilter.GaussianBlur(2.5))
warm = Image.new('RGBA', (S, S), (255, 196, 90, 70)); bg = Image.alpha_composite(bg, warm)
# cabeza y paliacate de Papi (diseno B): contorno ANTES de recortar y el recorte sale por la orilla
full = outline(papi, r=14)
head = full.crop((380, 200, 1760, 1460))
def bleed(canvas, img, width, top, dx=0):
    k = width / img.width
    im = img.resize((int(img.width * k), int(img.height * k)), Image.LANCZOS)
    x = (canvas.width - im.width) // 2 + dx                  # dx<0: Papi mira a la derecha, se corre a la izquierda
    y = max(top, canvas.height - im.height + int(0.06 * canvas.height))  # y por abajo
    canvas.alpha_composite(im, (x, y)); return canvas
icon = bleed(bg.copy(), head, 540, 40, dx=-38)
icon.convert('RGB').save('icono_512.png')

# capas adaptativas para Android (432x432; el circulo visible es el 66% central)
A = 432
fg = Image.new('RGBA', (A, A), (0, 0, 0, 0))
tall = full.crop((380, 200, 1760, 1720))                    # mas alto: el corte queda fuera de la vista
k = 330 / tall.width
t2 = tall.resize((int(tall.width * k), int(tall.height * k)), Image.LANCZOS)
face = (770 * k, 600 * k)                                   # centro de la cara dentro del recorte
fg.alpha_composite(t2, (int(A / 2 - face[0] + 8), int(A / 2 - face[1] + 6)))
fg.save('icono_frente_432.png')
bg.resize((A, A), Image.LANCZOS).convert('RGB').save('icono_fondo_432.png')

# ---------- imagen destacada 1024x500 ----------
W, H = 1024, 500
fcrop = far.crop((fw // 2 - 1500, fh - 1500, fw // 2 + 1500, fh - 36))
fcrop = fcrop.resize((W, int(fcrop.height * W / fcrop.width)), Image.LANCZOS)
feat = fcrop.crop((0, fcrop.height - H, W, fcrop.height)).copy()
body = outline(papi.crop(papi.getbbox()), r=12)
k = 420 / body.height; body = body.resize((int(body.width * k), int(body.height * k)), Image.LANCZOS)
feat.alpha_composite(body, (W - body.width - 40, H - body.height - 20))
d = ImageDraw.Draw(feat)
f1 = ImageFont.truetype(FONT, 104); f2 = ImageFont.truetype(FONT, 40)
def text(xy, s, font, fill, stroke):
    d.text(xy, s, font=font, fill=fill, stroke_width=stroke, stroke_fill=(26, 20, 16))
text((48, 70), 'Rescate', f1, (242, 193, 78), 9)
text((48, 190), 'Papi', f1, (242, 193, 78), 9)
text((52, 330), '¡Salva a tu dueño!', f2, (255, 255, 255), 5)
feat.convert('RGB').save('destacada_1024x500.png')
print('ok')
