"""Cambia el color verde de la serpiente por otro tono, sin tocar el penacho ni el contorno.
uso: python3 recolor.py entrada.png salida.png HUE_CUERPO HUE_PANZA"""
import sys, numpy as np
from PIL import Image
def recolor(img, hue_body, hue_belly):
    a = np.asarray(img.convert('RGBA')).copy()
    hsv = np.asarray(img.convert('RGB').convert('HSV')).astype(np.float32)
    h, s, v = hsv[..., 0] * 360 / 255, hsv[..., 1], hsv[..., 2]
    green = (h > 65) & (h < 165) & (s > 25)               # verdes (el penacho turquesa queda fuera)
    belly = green & (v > 185) & (s < 140)                  # verde claro de la panza
    nh = np.where(belly, hue_belly, hue_body) * 255 / 360
    ns = np.where(belly, np.clip(s * 1.3, 0, 255), np.clip(s * 1.25 + 20, 0, 255))
    out = hsv.copy(); out[..., 0] = np.where(green, nh, hsv[..., 0]); out[..., 1] = np.where(green, ns, s)
    rgb = np.asarray(Image.fromarray(out.astype(np.uint8), 'HSV').convert('RGB'))
    a[..., :3] = rgb
    return Image.fromarray(a)
if __name__ == '__main__':
    recolor(Image.open(sys.argv[1]), float(sys.argv[3]), float(sys.argv[4])).save(sys.argv[2])
