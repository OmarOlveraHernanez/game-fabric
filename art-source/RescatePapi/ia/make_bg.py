import sys, numpy as np
from PIL import Image
sys.path.insert(0, '.'); from bg import *
U = sys.argv[1] if len(sys.argv) > 1 else 'ia/'; O = sys.argv[2] if len(sys.argv) > 2 else 'out/'
import os; os.makedirs(O, exist_ok=True)
PPU = 256

# 1) LEJANO: opaco, ancho 4096 (16 u), base en y=-1
far = load(U + 'fondo_lejano_v2.png')
far = seamless(far, 220)
far = far.resize((4096, round(far.height * 4096 / far.width)), Image.LANCZOS)
far.save(O + 'fondo_lejano.png')
top = np.asarray(far)[:12].reshape(-1, 4)[:, :3].mean(0)
print('lejano', far.size, 'alto u', far.height / PPU, 'color cielo arriba', top.round())

# 2) MEDIO: selva sin magenta, apagada, ~4.2 u desde y=-1
mid = key_magenta(load(U + 'fondo_medio_v2.png'))
bb = mid.getbbox(); mid = mid.crop((0, bb[1], mid.width, bb[3]))
mid = mute(mid)
s = 4.2 * PPU / mid.height
mid = mid.resize((round(mid.width * s), round(mid.height * s)), Image.LANCZOS)
mid = seamless(mid, 160)
mid.save(O + 'fondo_medio.png'); print('medio', mid.size, 'alto u', mid.height / PPU)

# 3) SUELO: la linea de pasto solida queda a 512 px del fondo (y=0 si la base esta en y=-2)
g = key_magenta(load(U + 'suelo_v2.png'))
A = np.asarray(g)[..., 3] > 200
frac = A.mean(1)
solid = int(np.argmax(frac > 0.985))       # primera fila casi totalmente opaca = borde del pasto
blades = int(np.argmax(frac > 0.02))
print('suelo: puntas', blades, 'pasto solido', solid, 'alto', g.height)
s = 512 / (g.height - solid)
g = g.crop((0, blades, g.width, g.height))
g = g.resize((round(g.width * s), round(g.height * s)), Image.LANCZOS)
g = seamless(g, 140)
g.save(O + 'suelo_tierra.png'); print('suelo', g.size, 'pasto arriba de y=0:', g.height - 512, 'px')
