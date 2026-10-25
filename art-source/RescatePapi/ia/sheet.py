"""Procesa una hoja IA con fondo verde -> sprites PNG listos para Unity.
uso: python3 sheet.py hoja.png salida/ nombre_base W H [--flip] [--order 0,1] [--fit WxH] [--trim-shadow] [--anchor bottom|center]
Escala UNICA para todos los cuadros, lienzo WxH, base abajo-centro."""
import sys, argparse, numpy as np
from PIL import Image
from scipy import ndimage
p = argparse.ArgumentParser()
p.add_argument('src'); p.add_argument('out'); p.add_argument('name'); p.add_argument('W', type=int); p.add_argument('H', type=int)
p.add_argument('--fit', default=None, help='caja del cuerpo dentro del lienzo, ej 316x150')
p.add_argument('--flip', action='store_true'); p.add_argument('--order', default=None)
p.add_argument('--trim-shadow', action='store_true'); p.add_argument('--bottom', type=int, default=2, help='px libres abajo')
p.add_argument('--align', default='center', choices=['center','left','right'])
p.add_argument('--rows', action='store_true', help='figuras una encima de otra')
a = p.parse_args()
im = np.asarray(Image.open(a.src).convert('RGB')).astype(np.float32)
r,g,b = im[...,0],im[...,1],im[...,2]
bg = np.median(np.concatenate([im[:20].reshape(-1,3), im[-20:].reshape(-1,3)]), axis=0)  # color del fondo
dist = np.sqrt(((im-bg)**2).sum(axis=2))
alpha = np.clip((dist-60)/80, 0, 1)            # distancia al verde del fondo: <60 transparente, >140 opaco
edge = ndimage.binary_dilation(alpha < 0.5, iterations=3) & (alpha > 0)
g2 = np.where(edge & (g > np.maximum(r,b)), np.maximum(r,b), g)   # quita reflejo verde SOLO en el borde
rgba = np.dstack([r,g2,b,alpha*255]).astype(np.uint8)
mask = ndimage.binary_closing(alpha > 0.5, iterations=3)
lab, n = ndimage.label(mask)
objs = ndimage.find_objects(lab); sizes = ndimage.sum(mask, lab, range(1,n+1))
keep = [i for i in range(n) if sizes[i] > 20000]
keep.sort(key=lambda i: (objs[i][0].start if a.rows else objs[i][1].start))
crops = []
for i in keep:
    sy, sx = objs[i]
    c = rgba[sy, sx].copy()
    c[...,3] = np.where(ndimage.binary_dilation(lab[sy,sx]==i+1, iterations=4), c[...,3], 0)
    if a.trim_shadow:
        # quita la linea de sombra bajo el cuerpo: en cada columna borra lo que este mas abajo
        # que el ultimo pixel NO oscuro + el grosor del contorno
        A = c[...,3] > 128; nd = A & (c[...,:3].max(axis=2) >= 90)
        H = A.shape[0]; t = 14
        for x in range(A.shape[1]):
            ys = np.nonzero(nd[:,x])[0]
            lim = ys.max() + t if len(ys) else H - 30
            c[lim+1:, x, 3] = 0
    crops.append(Image.fromarray(c).crop(Image.fromarray(c).getbbox()))
print('figuras', len(crops), [c.size for c in crops])
if a.order: crops = [crops[int(k)] for k in a.order.split(',')]
fw, fh = map(int, a.fit.split('x')) if a.fit else (a.W-8, a.H-a.bottom-4)
s = min(min(fw/c.width, fh/c.height) for c in crops)
print('escala', round(s,3))
mw = max(round(c.width*s) for c in crops)   # ancho del conjunto: left/right alinean el conjunto centrado
for k,c in enumerate(crops):
    if a.flip: c = c.transpose(Image.FLIP_LEFT_RIGHT)
    c = c.resize((round(c.width*s), round(c.height*s)), Image.LANCZOS)
    cv = Image.new('RGBA', (a.W,a.H), (0,0,0,0))
    x = {'center':(a.W-c.width)//2,'left':(a.W-mw)//2,'right':(a.W+mw)//2-c.width}[a.align]
    cv.alpha_composite(c, (x, a.H-a.bottom-c.height))
    nm = a.name if len(crops)==1 else f'{a.name}_{k}'
    cv.save(f'{a.out}/{nm}.png'); print(nm, c.size)
