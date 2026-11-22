"""(22 nov.) uso: python3 repinta_vaporera.py ia/puestos/cocina_tamales.png ia/puestos/cocina_tamales_v2.png
Repinta en la vaporera de tamales los verdes (verde vivo) y los de rajas (masa blanca con rajas verdes), como sus iconos nuevos.
Mapa de degradado sobre la luz de cada pixel: se conserva el dibujo y la sombra, cambia solo el color."""
import numpy as np, cv2, sys
from PIL import Image
SRC, OUT = sys.argv[1], sys.argv[2]
im = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32) / 255
h, w = im.shape[:2]
L = im @ np.array([0.3, 0.59, 0.11])
yy, xx = np.mgrid[0:h, 0:w]
inner = np.zeros((h, w), np.uint8); cv2.ellipse(inner, (254, 406), (232, 103), 0, 0, 360, 255, -1)
inner = cv2.GaussianBlur(inner, (0, 0), 4).astype(np.float32) / 255
left = np.clip((256 - xx) / 5, 0, 1)
# la frontera entre verdes y rajas no es recta: los de rajas de atras llegan a y ~ 400 en medio, ~ 412 a la izquierda
edge_y = 398 + (256 - xx) * 0.06
low = np.clip((yy - edge_y) / 8, 0, 1)
# la tapa de lamina (gris azulado) y el borde de la olla no se tocan
sat = im.max(2) - im.min(2)
food = np.clip((sat - 0.10) / 0.08, 0, 1)
food = cv2.GaussianBlur(food, (0, 0), 1.2)
reg_r = inner * left * low * food
reg_v = inner * left * (1 - low) * food

def grad(stops):
    xs = np.array([s[0] for s in stops]); cs = np.array([s[1] for s in stops], np.float32) / 255
    return np.dstack([np.interp(L, xs, cs[:, c]) for c in range(3)])
verde = grad([(0.0, (22, 30, 10)), (0.15, (68, 84, 32)), (0.32, (112, 132, 62)), (0.55, (165, 178, 112)), (1.0, (228, 232, 200))])  # verde de tomate verde, no chillante
rajas = grad([(0.0, (18, 28, 12)), (0.10, (38, 64, 28)), (0.22, (78, 108, 52)), (0.34, (205, 200, 172)), (0.55, (236, 232, 214)), (1.0, (252, 250, 242))])  # masa blanca de maiz con rajas verdes
out = im * (1 - reg_v - reg_r)[..., None] + verde * reg_v[..., None] + rajas * reg_r[..., None]
Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(OUT)
