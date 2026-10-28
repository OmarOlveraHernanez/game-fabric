import sys, numpy as np
from PIL import Image, ImageEnhance
sys.path.insert(0, '.')
U = sys.argv[1]
from bg import key_magenta, seamless, load
mid = key_magenta(load(U))
bb = mid.getbbox(); mid = mid.crop((0, bb[1], mid.width, bb[3]))
rgb = ImageEnhance.Color(mid.convert('RGB')).enhance(0.6)
a = np.asarray(rgb).astype(np.float32)
sky = np.array([200, 228, 200], np.float32)                 # neblina verde clara (cercana al cielo)
h = a.shape[0]; t = np.linspace(0.42, 0.28, h)[:, None, None]  # mas neblina arriba que abajo
a = a * (1 - t) + sky * t
out = Image.fromarray(a.clip(0, 255).astype(np.uint8)); out.putalpha(mid.getchannel('A'))
s = 4.2 * 256 / out.height
out = out.resize((round(out.width * s), round(out.height * s)), Image.LANCZOS)
seamless(out, 160).save(sys.argv[2])
