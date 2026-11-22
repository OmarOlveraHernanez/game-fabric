"""Lo del dibujo que va ENFRENTE de los clientes (cucharones, vasos que sobresalen de la barra).
Saca esos pedazos del fondo a un PNG transparente del mismo tamano; el juego lo pone encima de los clientes.
uso: python3 frente.py carpeta_salida"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = sys.argv[1]
SRC = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ia', 'puestos')
JUEGO = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'games', 'Tacos', 'Assets', '_Game', 'Art', 'Resources', 'Sprites')

# (fondo, nombre de salida, trazos (x0, y0, x1, y1, grueso) y poligonos), en pixeles de la imagen de 1536 x 864
PIEZAS = {
    'tamales': ('cocina_tamales_v2.png', 'frente_tamales', [
        (819, 259, 776, 336, 10),   # cucharon del champurrado
        (1016, 268, 978, 336, 10),  # cucharon del atole de arroz
        (1205, 279, 1168, 336, 10), # cucharon del atole de fresa
    ], [
        [(1222, 284), (1271, 284), (1277, 302), (1216, 302)],  # la pila de vasos
        [(281, 282), (313, 284), (347, 287), (380, 294), (399, 300), (281, 300)],  # la orilla de la vaporera
    ]),
    'feria': ('barra_feria_v2.png', 'frente_feria', [], [
        # lo de arriba de las ollas (elotes, tamales y la orilla del pozole) va enfrente de la gente
        [(300, 288), (330, 284), (360, 282), (400, 290), (440, 305), (455, 313), (480, 320), (492, 330), (498, 346), (300, 346)],
        [(503, 346), (510, 332), (525, 320), (545, 300), (580, 287), (596, 281), (625, 287), (650, 270), (670, 261), (690, 264),
         (712, 272), (740, 280), (762, 292), (790, 310), (815, 325), (833, 338), (838, 346)],
        [(858, 346), (880, 332), (930, 322), (980, 319), (1030, 319), (1080, 324), (1130, 334), (1162, 346)],
    ]),
    'pozole': ('cocina_pozole.png', 'frente_pozole', [], [
        # la canasta de tostadas de la repisa (la gente se para detras de ella)
        [(411, 293), (437, 279), (472, 265), (495, 257), (528, 253), (555, 254), (574, 263), (600, 271), (627, 282), (652, 295),
         (662, 316), (669, 354), (411, 354)],
    ]),
    'elotes': (os.path.join(JUEGO, 'barra_elotes.jpg') if os.path.exists(os.path.join(JUEGO, 'barra_elotes.jpg')) else os.environ.get('BARRA_ELOTES', ''), 'frente_elotes', [], [
        # la orilla derecha de la olla grande de elotes
        [(240, 297), (290, 304), (315, 312), (340, 322), (361, 334), (371, 345), (377, 361), (381, 390), (240, 390)],
    ]),
}

for nm, (fondo, salida, trazos, polis) in PIEZAS.items():
    im = Image.open(os.path.join(SRC, fondo)).convert('RGBA')
    m = Image.new('L', im.size, 0)
    d = ImageDraw.Draw(m)
    for x0, y0, x1, y1, w in trazos:
        d.line([(x0, y0), (x1, y1)], fill=255, width=w)
        for x, y in ((x0, y0), (x1, y1)):
            d.ellipse([x - w / 2, y - w / 2, x + w / 2, y + w / 2], fill=255)
    for p in polis:
        d.polygon(p, fill=255)
    m = m.filter(ImageFilter.GaussianBlur(0.8))  # orilla suave
    im.putalpha(m)
    a = np.asarray(im).copy()
    a[a[:, :, 3] == 0] = 0  # lo transparente en negro: el PNG pesa mucho menos
    im = Image.fromarray(a, 'RGBA')
    im.save(os.path.join(OUT, salida + '.png'), optimize=True)
    print(salida)
