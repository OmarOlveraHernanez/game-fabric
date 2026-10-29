"""Agrega un contorno claro (estilo estampa) alrededor de un sprite con transparencia."""
import sys, numpy as np
from PIL import Image
from scipy import ndimage
def outline(img, r=5, color=(255, 246, 224)):
    a = np.asarray(img.convert('RGBA')).astype(np.float32)
    al = a[..., 3] / 255
    yy, xx = np.mgrid[-r:r+1, -r:r+1]; disk = (xx**2 + yy**2) <= r*r
    grown = ndimage.grey_dilation(al, footprint=disk)
    grown = ndimage.gaussian_filter(grown, 0.7)          # borde suave
    under = np.dstack([np.broadcast_to(color, al.shape + (3,)), grown[..., None] * 255])
    # componer sprite encima del contorno
    o = under.copy(); fa = al[..., None]
    o[..., :3] = a[..., :3] * fa + under[..., :3] * (1 - fa)
    o[..., 3] = np.maximum(a[..., 3], under[..., 3])
    return Image.fromarray(o.clip(0, 255).astype(np.uint8))
if __name__ == '__main__':
    for src, dst in zip(sys.argv[1::2], sys.argv[2::2]): outline(Image.open(src)).save(dst)
