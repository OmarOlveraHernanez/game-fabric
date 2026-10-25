import sys, numpy as np
from PIL import Image
from scipy import ndimage
src, out = sys.argv[1], sys.argv[2]
im = np.asarray(Image.open(src).convert('RGB')).astype(np.float32)
r,g,b = im[...,0],im[...,1],im[...,2]
gd = g - np.maximum(r,b)                 # dominancia del verde
a = np.clip(1 - (gd-30)/70, 0, 1)        # 30..100 -> transicion suave
# despill: quita el reflejo verde en bordes
spill = g > np.maximum(r,b)
g2 = np.where(spill, np.maximum(r,b), g)
rgba = np.dstack([r,g2,b,a*255]).astype(np.uint8)
mask = a > 0.5
mask = ndimage.binary_closing(mask, iterations=3)
lab, n = ndimage.label(mask)
objs = ndimage.find_objects(lab)
sizes = ndimage.sum(mask, lab, range(1,n+1))
keep = [i for i in range(n) if sizes[i] > 20000]
boxes = sorted([objs[i] for i in keep], key=lambda s:(s[0].start//400, s[1].start))
print('figuras', len(boxes))
import json
crops=[]
for k,(sy,sx) in enumerate(boxes):
    c = rgba[sy, sx].copy()
    lm = lab[sy, sx]
    idx = [i for i in keep if objs[i]==(sy,sx)][0]+1
    c[...,3] = np.where(ndimage.binary_dilation(lm==idx, iterations=4), c[...,3], 0)  # quita restos de otras figuras
    crops.append(Image.fromarray(c))
    print(k, c.shape[1], c.shape[0])
for k,c in enumerate(crops): c.save(f'{out}/raw_{k}.png')
