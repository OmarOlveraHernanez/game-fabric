"""Niveles de las ollas de caldo (20 nov.): la olla a la mitad y casi vacia, hechas del mismo fondo.
El caldo se recorre hacia abajo dentro de la boca y arriba se ve la pared de adentro (barro o lamina); el cucharon se queda.
uso: BARRA_ELOTES=<ruta de barra_elotes.jpg> python3 niveles.py carpeta_salida"""
import sys, os, numpy as np, cv2
from PIL import Image
OUT = sys.argv[1]
PAD = 40
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'ia', 'puestos') + os.sep
BARRO, LAMINA = (40, 62, 120), (104, 112, 122)
_cache = {}
def load(path):
    if path not in _cache:
        im = cv2.cvtColor(np.asarray(Image.open(path).convert('RGB')), cv2.COLOR_RGB2BGR)
        _cache[path] = cv2.copyMakeBorder(im, PAD, PAD, PAD, PAD, cv2.BORDER_REPLICATE)
    return _cache[path]
# (nombre, centro, semiejes de la boca, cucharon (x0,y0,x1,y1))
ELOTES = os.environ.get('BARRA_ELOTES') or os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'games', 'Tacos', 'Assets', '_Game', 'Art', 'Resources', 'Sprites', 'barra_elotes.jpg')
OLLAS = [(P + 'cocina_pozole.png', 'pozole_0', (118, 509), (121, 39), (262, 438, 203, 512), BARRO),
         (P + 'cocina_pozole.png', 'pozole_1', (422, 510), (123, 38), (562, 438, 500, 510), BARRO),
         (P + 'cocina_pozole.png', 'pozole_2', (728, 510), (122, 39), (847, 438, 798, 510), BARRO),
         (ELOTES, 'elotes_1', (500, 462), (150, 62), (670, 343, 614, 446), LAMINA),
         (P + 'barra_feria_v2.png', 'feria_2', (1013, 361), (122, 33), None, BARRO)]
rng = np.random.default_rng(3)
for path, nm, (cx, cy), (ax, ay), lad, wallc in OLLAS:
    img = load(path)
    x0, y0, x1, y1 = cx - ax - 6, cy - ay - 6, cx + ax + 6, cy + ay + 6
    crop = img[y0 + PAD:y1 + PAD, x0 + PAD:x1 + PAD].copy()
    h, w = crop.shape[:2]
    ccx, ccy = cx - x0, cy - y0
    mouth = np.zeros((h, w), np.uint8); cv2.ellipse(mouth, (ccx, ccy), (ax - 2, ay - 2), 0, 0, 360, 255, -1)
    ladle = np.zeros((h, w), np.uint8)
    if lad: cv2.line(ladle, (lad[0] - x0, lad[1] - y0), (lad[2] - x0, lad[3] - y0), 255, 11)
    # lo que flota (maiz, carne): lo que resalta del caldo
    lab = cv2.cvtColor(crop, cv2.COLOR_BGR2LAB).astype(np.float32)
    med = cv2.medianBlur(crop, 31)
    labm = cv2.cvtColor(med, cv2.COLOR_BGR2LAB).astype(np.float32)
    diff = np.abs(lab - labm).sum(2)
    items = ((diff > 28) & (mouth > 0) & (ladle == 0)).astype(np.uint8) * 255
    items = cv2.dilate(items, np.ones((5, 5), np.uint8))
    n, comp = cv2.connectedComponents(items)
    order = rng.permutation(np.arange(1, n))
    for lvl, e in (('medio', 0.5), ('bajo', 0.85)):
        # menos cosas flotando: se mezcla con el mismo caldo bien difuminado (sin manchas de otro color)
        soup = cv2.GaussianBlur(cv2.medianBlur(crop, 21), (0, 0), 6)
        k = 0.0 if e < 0.6 else 0.5
        broth = cv2.addWeighted(crop, 1 - k, soup, k, 0)
        broth = cv2.addWeighted(broth, 1 - 0.12 * e, np.zeros_like(broth), 0, 0)  # mas abajo, mas sombra
        # baja el nivel: se ve la pared de adentro de la olla arriba del caldo
        d = int(2 * ay * 0.32 * e)
        newm = np.zeros((h, w), np.uint8)
        nax, nay = int((ax - 2) * (1 - 0.07 * e)), (2 * (ay - 2) - d) // 2
        cv2.ellipse(newm, (ccx, ccy + d // 2), (nax, nay), 0, 0, 360, 255, -1)
        # el caldo se "baja": se desplaza hacia abajo dentro de la boca
        M = np.float32([[1 - 0.07 * e, 0, ccx * 0.07 * e], [0, (nay * 2) / (2 * (ay - 2)), ccy + d // 2 - ccy * (nay * 2) / (2 * (ay - 2))]])
        broth = cv2.warpAffine(broth, M, (w, h), borderMode=cv2.BORDER_REPLICATE)
        # pared: barro de adentro, mas oscuro hacia el caldo
        wall = np.zeros_like(crop)
        base = np.array(wallc, np.float32)  # pared de adentro: barro o lamina
        yy = np.arange(h)[:, None].astype(np.float32)
        t = np.clip((yy - (ccy - ay)) / max(1, d + 4), 0, 1)
        wall[:] = (base * (1.0 - 0.45 * t))[..., None, :].reshape(h, 1, 3).repeat(w, 1).astype(np.uint8) if False else 0
        for c in range(3): wall[..., c] = np.clip(base[c] * (1.05 - 0.5 * t), 0, 255).astype(np.uint8)[:, 0][:, None]
        out = np.where(newm[..., None] > 0, broth, wall)
        # sombra suave en la orilla del caldo
        edge = cv2.GaussianBlur(newm, (0, 0), 3).astype(np.float32) / 255
        out = (out.astype(np.float32) * (0.75 + 0.25 * edge[..., None])).astype(np.uint8)
        # el cucharon se queda: se pone encima tal cual estaba en el dibujo
        lm = cv2.GaussianBlur(ladle, (0, 0), 1.5).astype(np.float32)[..., None] / 255
        out = (out.astype(np.float32) * (1 - lm) + crop.astype(np.float32) * lm).astype(np.uint8)
        alpha = cv2.GaussianBlur(mouth, (0, 0), 1.2)
        rgba = np.dstack([cv2.cvtColor(out, cv2.COLOR_BGR2RGB), alpha])
        Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, f'nivel_{nm}_{lvl}.png'))
        print(nm, lvl, x0 + w / 2, y0 + h / 2, w, h)
