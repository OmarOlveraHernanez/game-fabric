"""Fondos IA -> capas repetibles para ScrollingStrip (256 px = 1 unidad).
Funciones: key_magenta (quita el fondo #FF00FF y su reflejo), seamless (funde los bordes para repetir sin costura)."""
import numpy as np
from PIL import Image, ImageEnhance
from scipy import ndimage

def load(p): return Image.open(p).convert('RGBA')

def key_magenta(img):
    a = np.asarray(img).astype(np.float32); rgb = a[..., :3]
    d = np.sqrt(((rgb - [255, 0, 255]) ** 2).sum(2))
    alpha = np.clip((d - 90) / 90, 0, 1)
    edge = ndimage.binary_dilation(alpha < 0.5, iterations=3) & (alpha > 0)
    m = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, None) * edge   # reflejo magenta
    rgb[..., 0] -= m; rgb[..., 2] -= m
    return Image.fromarray(np.dstack([rgb, alpha * 255]).clip(0, 255).astype(np.uint8))

def seamless(img, band):
    a = np.asarray(img).astype(np.float32); W = a.shape[1]; T = W - band
    w = np.linspace(0, 1, band)[None, :, None]
    al, ar = a[:, :band, 3:4] / 255, a[:, T:, 3:4] / 255
    # mezcla premultiplicada para no crear halos en zonas transparentes
    rgb_l, rgb_r = a[:, :band, :3], a[:, T:, :3]
    al_mix = (1 - w) * ar + w * al
    rgb_mix = ((1 - w) * ar * rgb_r + w * al * rgb_l) / np.maximum(al_mix, 1e-4)
    out = a[:, :T].copy()
    out[:, :band, :3] = rgb_mix; out[:, :band, 3:4] = al_mix * 255
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))

def mute(img, sat=0.75, bright=0.82):
    rgb = img.convert('RGB'); a = img.getchannel('A')
    rgb = ImageEnhance.Color(rgb).enhance(sat); rgb = ImageEnhance.Brightness(rgb).enhance(bright)
    rgb.putalpha(a); return rgb
