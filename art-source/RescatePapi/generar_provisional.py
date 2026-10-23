# Genera el arte PROVISIONAL de Rescate Papi (estilo codice simple) y las plantillas.
# Uso: python generar_provisional.py  (requiere Pillow). Ajustar OUT antes de correr en Windows.
import math, os
from PIL import Image, ImageDraw, ImageFont

OUT = '/mnt/user-data/outputs/art'
SPR = OUT + '/sprites'; TPL = OUT + '/plantillas'
os.makedirs(SPR, exist_ok=True); os.makedirs(TPL, exist_ok=True)
SS = 2  # supersampling

P = dict(INK=(26,20,16), AMATE=(242,230,204), AMATE_D=(217,194,154), TURQ=(31,138,138), TURQ_L=(79,183,176),
         JADE=(62,142,79), JADE_L=(125,190,106), OCRE=(217,150,43), OCRE_L=(242,193,78), GRANA=(176,48,42),
         GRANA_L=(217,87,74), MAYA=(58,110,165), CAFE=(122,74,42), CAFE_D=(74,44,26), HUESO=(250,245,234),
         PIEL=(198,138,94), PIEL_S=(160,106,68))
LINE = 6  # px de contorno a 256 PPU

class Canvas:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.im = Image.new('RGBA', (w*SS, h*SS), (0,0,0,0)); s.d = ImageDraw.Draw(s.im)
    def S(s, pts): return [(x*SS, y*SS) for x, y in pts]
    def poly(s, pts, fill, line=LINE):
        s.d.polygon(s.S(pts), fill=P[fill] if isinstance(fill,str) else fill, outline=P['INK'], width=line*SS if line else 0)
    def ell(s, cx, cy, rx, ry, fill, line=LINE):
        s.d.ellipse([ (cx-rx)*SS, (cy-ry)*SS, (cx+rx)*SS, (cy+ry)*SS ], fill=P[fill] if isinstance(fill,str) else fill, outline=P['INK'], width=line*SS if line else 0)
    def rrect(s, x0, y0, x1, y1, r, fill, line=LINE):
        s.d.rounded_rectangle([x0*SS, y0*SS, x1*SS, y1*SS], radius=r*SS, fill=P[fill], outline=P['INK'], width=line*SS if line else 0)
    def lineseg(s, pts, color, width):
        s.d.line(s.S(pts), fill=P[color], width=int(width*SS), joint='curve')
    def limb(s, x, y, ang_deg, length, thick, fill):
        a = math.radians(ang_deg); dx, dy = math.sin(a)*length, math.cos(a)*length
        nx, ny = math.cos(a)*thick/2, -math.sin(a)*thick/2
        pts = [(x+nx, y+ny), (x+dx+nx, y+dy+ny), (x+dx-nx, y+dy-ny), (x-nx, y-ny)]
        s.poly(pts, fill)
        return x+dx, y+dy
    def save(s, name):
        s.im.resize((s.w, s.h), Image.LANCZOS).save(f'{SPR}/{name}.png')

def eye(c, cx, cy, r, look=1, dead=False):
    c.ell(cx, cy, r, r, 'HUESO', 4)
    if dead:
        c.lineseg([(cx-r*0.6, cy-r*0.6), (cx+r*0.6, cy+r*0.6)], 'INK', 4); c.lineseg([(cx-r*0.6, cy+r*0.6), (cx+r*0.6, cy-r*0.6)], 'INK', 4)
    else:
        c.ell(cx + look*r*0.35, cy, r*0.45, r*0.5, 'INK', 0)

# ---------- Dueño (256x256, mira a la derecha) ----------
def owner(name, legs, arms, mode='run'):
    c = Canvas(256, 256)
    hipx, hipy = 124, 168
    tilt = -12 if mode == 'hit' else 0
    # pierna trasera, brazo trasero
    fx, fy = c.limb(hipx-6, hipy, legs[1], 62, 26, 'CAFE_D')
    c.ell(fx+6, fy+4, 16, 8, 'CAFE')
    ax, ay = c.limb(118, 112, arms[1], 46, 18, 'PIEL_S')
    # torso turquesa con greca ocre
    c.poly([(98+tilt,96),(152+tilt,96),(150,172),(100,172)], 'TURQ')
    for i in range(4):
        x0 = 104 + i*11 + tilt*0.4
        c.poly([(x0,132),(x0+6,124),(x0+11,132),(x0+6,140)], 'OCRE', 3)
    # pierna delantera
    fx, fy = c.limb(hipx+8, hipy, legs[0], 62, 26, 'CAFE')
    c.ell(fx+6, fy+4, 16, 8, 'CAFE_D')
    # cabeza
    hx, hy = 130+tilt, 62
    c.ell(hx, hy, 32, 32, 'PIEL')
    c.d.pieslice([(hx-34)*SS, (hy-36)*SS, (hx+34)*SS, (hy+30)*SS], 180, 350, fill=P['INK'])  # pelo
    eye(c, hx+12, hy+4, 10, 1, dead=(mode=='hit'))
    c.lineseg([(hx+18, hy+22), (hx+28, hy+20)], 'INK', 4)  # boca
    # paliacate rojo
    c.poly([(hx-22,hy+28),(hx+20,hy+28),(hx-2,hy+48)], 'GRANA', 4)
    # brazo delantero
    c.limb(140+tilt, 112, arms[0], 46, 18, 'PIEL')
    c.save(name)

runs = [((28,-24),(-30,30)), ((8,-8),(-10,10)), ((-24,28),(30,-30)), ((-8,8),(10,-10))]
for i,(l,a) in enumerate(runs): owner(f'dueno_correr_{i}', l, a)
owner('dueno_salto', (55,10), (150,120), 'jump')
owner('dueno_golpe', (-40,40), (160,-160), 'hit')

# ---------- Serpiente (320x160, mira a la izquierda) ----------
def serpent(name, phase):
    c = Canvas(320, 160)
    pts = []
    for i in range(0, 41):
        x = 300 - i*6.5
        y = 112 + math.sin(i*0.45 + phase)*16
        pts.append((x, y))
    c.lineseg(pts, 'INK', 44); c.lineseg(pts, 'JADE', 32); c.lineseg(pts, 'JADE_L', 8)
    hx, hy = pts[-1]
    c.ell(hx-6, hy-10, 30, 24, 'JADE')
    # penacho de plumas (estilo codice)
    for k,(dx,col) in enumerate([(10,'TURQ'),(22,'OCRE'),(34,'GRANA')]):
        c.poly([(hx+dx-14, hy-28), (hx+dx+6, hy-62+k*4), (hx+dx+12, hy-26)], col, 4)
    eye(c, hx-14, hy-16, 8, -1)
    c.lineseg([(hx-34, hy-4), (hx-48, hy-8)], 'GRANA', 5); c.lineseg([(hx-48, hy-8), (hx-56, hy-14)], 'GRANA', 4); c.lineseg([(hx-48, hy-8), (hx-56, hy-2)], 'GRANA', 4)
    c.save(name)
serpent('serpiente_0', 0.0); serpent('serpiente_1', 1.6)

# ---------- Jaguar (384x384, mira a la izquierda) ----------
def jaguar(name, step):
    c = Canvas(384, 384)
    # cola
    c.lineseg([(320,250),(350,200),(345,150),(360,120)], 'INK', 26); c.lineseg([(320,250),(350,200),(345,150),(360,120)], 'OCRE', 16)
    # patas traseras/delanteras (alternan)
    for x, ang in [(285, 12*step), (110, -12*step)]:
        c.limb(x, 300, ang, 80, 34, 'OCRE')
    for x, ang in [(255, -12*step), (140, 12*step)]:
        c.limb(x, 300, ang, 80, 34, 'OCRE_L')
    # cuerpo
    c.ell(200, 270, 130, 62, 'OCRE')
    for (sx, sy) in [(150,255),(195,240),(240,262),(285,250),(175,290),(225,295),(120,280)]:
        c.ell(sx, sy, 12, 9, 'CAFE_D', 0); c.ell(sx, sy, 5, 4, 'OCRE', 0)
    # cabeza
    c.ell(96, 190, 62, 56, 'OCRE')
    c.poly([(70,140),(84,112),(100,140)], 'OCRE', 5); c.poly([(110,140),(126,114),(136,142)], 'OCRE', 5)
    c.ell(58, 214, 28, 20, 'OCRE_L', 5)  # hocico
    c.ell(40, 206, 8, 6, 'INK', 0)
    eye(c, 78, 178, 12, -1)
    c.lineseg([(36,226),(66,232)], 'INK', 4)
    for (sx, sy) in [(110,170),(125,200),(100,225)]:
        c.ell(sx, sy, 7, 5, 'CAFE_D', 0)
    c.save(name)
jaguar('jaguar_0', 1); jaguar('jaguar_1', -1)

# ---------- Águila (384x192, mira a la izquierda) ----------
def eagle(name, up):
    c = Canvas(384, 192)
    wy = 30 if up else 160
    c.poly([(170,100),(300,wy),(250,110)], 'CAFE', 5)               # ala trasera
    c.ell(200, 108, 100, 34, 'CAFE')                                   # cuerpo
    c.poly([(290,96),(360,80),(360,130),(292,118)], 'CAFE_D', 5)       # cola
    c.ell(102, 92, 34, 30, 'OCRE')                                     # cabeza dorada (aguila real, no aguila calva)
    c.poly([(74,86),(44,100),(76,108)], 'OCRE_L', 4)                  # pico
    eye(c, 96, 84, 8, -1)
    c.poly([(150,96),(240,wy+(14 if up else -14)),(215,108)], 'CAFE_D', 5)  # ala delantera
    c.save(name)
eagle('aguila_0', True); eagle('aguila_1', False)

# ---------- Cacao (128x128) ----------
c = Canvas(128, 128)
c.ell(64, 64, 34, 52, 'CAFE'); c.lineseg([(64,22),(60,64),(64,106)], 'CAFE_D', 5); c.ell(50, 44, 7, 12, 'OCRE_L', 0)
c.save('cacao')

# ---------- Pollito (160x160) ----------
def chick(name, wing):
    c = Canvas(160, 160)
    c.ell(84, 104, 46, 40, 'OCRE_L'); c.ell(58, 58, 30, 28, 'OCRE_L')
    c.poly([(32,60),(14,66),(32,72)], 'GRANA_L', 4); eye(c, 54, 52, 7, -1)
    c.poly([(84,96),(124,96-wing),(112,120)], 'OCRE', 4)
    c.lineseg([(74,142),(74,154)], 'GRANA', 5); c.lineseg([(94,142),(94,154)], 'GRANA', 5)
    c.save(name)
chick('pollito_0', 18); chick('pollito_1', -6)

# ---------- Suelo (512x256, repetible) ----------
c = Canvas(512, 256)
c.d.rectangle([0, 0, 512*SS, 256*SS], fill=P['CAFE'])
c.d.rectangle([0, 0, 512*SS, 26*SS], fill=P['JADE']); c.d.rectangle([0, 22*SS, 512*SS, 30*SS], fill=P['INK'])
# greca escalonada (periodo 64 px)
for x0 in range(0, 512, 64):
    pts = [(x0,70),(x0+16,70),(x0+16,54),(x0+48,54),(x0+48,86),(x0+32,86),(x0+32,70),(x0+40,70)]
    c.lineseg(pts, 'OCRE', 7)
c.d.rectangle([0, 104*SS, 512*SS, 110*SS], fill=P['CAFE_D'])
for (x, y) in [(40,150),(140,200),(250,165),(360,215),(450,175),(90,232),(300,236)]:
    c.ell(x, y, 16, 10, 'CAFE_D', 0)
c.save('suelo_tierra')

# ---------- Fondo lejano (2048x2048 = 8x8 u; base en y=-1) ----------
c = Canvas(2048, 2048)
def pyramid(cx, base, w, h, col):
    steps = 5
    for k in range(steps):
        ww = w*(1-k/steps); y1 = base - k*h/steps; y0 = y1 - h/steps
        c.d.rectangle([(cx-ww/2)*SS, y0*SS, (cx+ww/2)*SS, y1*SS], fill=P[col])
    c.d.rectangle([(cx-w*0.08)*SS, (base-h-40)*SS, (cx+w*0.08)*SS, (base-h)*SS], fill=P[col])
c.ell(1500, 1050, 120, 120, 'OCRE_L', 0)
pyramid(560, 1800, 900, 520, 'AMATE_D')
pyramid(1500, 1800, 600, 330, 'AMATE_D')
c.d.rectangle([0, 1790*SS, 2048*SS, 2048*SS], fill=P['AMATE_D'])
c.save('fondo_lejano')

# ---------- Fondo medio (2048x1024 = 8x4 u; selva) ----------
c = Canvas(2048, 1024)
import random; random.seed(4)
for layer, col in [(0,'JADE'),(1,'JADE_L')]:
    for i in range(14):
        x = i*160 + (80 if layer else 0); r = random.randint(90, 150)
        for dx in (0, 2048, -2048):  # repetible en los bordes
            c.ell(x+dx, 1024-60-layer*40, r, r*0.8, col, 0)
c.d.rectangle([0, 960*SS, 2048*SS, 1024*SS], fill=P['JADE'])
c.save('fondo_medio')

# ---------- Plantillas (para dibujar encima) ----------
try:
    font = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf', 13)
    fontb = ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf', 15)
except Exception:
    font = fontb = ImageFont.load_default()

def template(name, w, h, hitbox, note, preview=None):
    im = Image.new('RGBA', (w, h), (255,255,255,255)); d = ImageDraw.Draw(im)
    for x in range(0, w, 32): d.line([(x,0),(x,h)], fill=(232,232,232), width=1)
    for y in range(0, h, 32): d.line([(0,y),(w,y)], fill=(232,232,232), width=1)
    if preview:
        pv = Image.open(f'{SPR}/{preview}.png').convert('RGBA'); a = pv.split()[3].point(lambda v: int(v*0.18)); pv.putalpha(a); im.alpha_composite(pv)
    x0,y0,x1,y1 = hitbox
    for i in range(int(x0), int(x1), 10): d.line([(i,y0),(min(i+5,x1),y0)], fill=(220,40,40), width=2); d.line([(i,y1-1),(min(i+5,x1),y1-1)], fill=(220,40,40), width=2)
    for j in range(int(y0), int(y1), 10): d.line([(x0,j),(x0,min(j+5,y1))], fill=(220,40,40), width=2); d.line([(x1-1,j),(x1-1,min(j+5,y1))], fill=(220,40,40), width=2)
    px = w//2; d.line([(px-10,h-2),(px+10,h-2)], fill=(30,90,220), width=3); d.line([(px,h-14),(px,h)], fill=(30,90,220), width=3)
    d.rectangle([0,0,w-1,h-1], outline=(120,120,120))
    d.text((6,4), name, fill=(40,40,40), font=fontb)
    d.text((6,22), f'{w}x{h}px = {w/256:g}x{h/256:g} u', fill=(90,90,90), font=font)
    if note: d.text((6,38), note, fill=(90,90,90), font=font)
    im.save(f'{TPL}/{name}_plantilla.png')

template('dueno', 256, 256, (13,26,243,256), 'rojo: caja de choque', 'dueno_correr_0')
template('serpiente', 320, 160, (6,19,313,160), 'mira a la izquierda', 'serpiente_0')
template('jaguar', 384, 384, (22,26,362,384), 'mira a la izquierda', 'jaguar_0')
template('aguila', 384, 192, (13,19,371,173), 'mira a la izquierda', 'aguila_0')
template('cacao', 128, 128, (6,7,122,122), '', 'cacao')
template('pollito', 160, 160, (3,0,157,154), '', 'pollito_0')
template('suelo_tierra', 512, 256, (0,0,0,0), 'repetible: borde izq = borde der', 'suelo_tierra')

# Paleta
names = list(P.keys()); sw = Image.new('RGB', (4*180, ((len(names)+3)//4)*90), (255,255,255)); d = ImageDraw.Draw(sw)
for i, n in enumerate(names):
    x, y = (i%4)*180, (i//4)*90
    d.rectangle([x+8, y+8, x+80, y+80], fill=P[n], outline=(0,0,0))
    d.text((x+88, y+22), n, fill=(0,0,0), font=fontb); d.text((x+88, y+44), '#%02X%02X%02X' % P[n], fill=(60,60,60), font=font)
sw.save(f'{OUT}/paleta.png')

# Hoja de vista previa (todo el set sobre amate)
sheet = Image.new('RGBA', (1280, 720), P['AMATE']+(255,))
x = 20
for n in ['dueno_correr_0','dueno_correr_1','dueno_correr_2','dueno_correr_3','dueno_salto','dueno_golpe']:
    sp = Image.open(f'{SPR}/{n}.png'); sheet.alpha_composite(sp, (x, 20)); x += 206
y = 300; x = 20
for n in ['serpiente_0','jaguar_0','aguila_0','cacao','pollito_0']:
    sp = Image.open(f'{SPR}/{n}.png'); sheet.alpha_composite(sp, (x, y + (384 - sp.height)//2 - 40)); x += sp.width + 20
sheet.convert('RGB').save(f'{OUT}/vista_previa.png')
print('ok', sorted(os.listdir(SPR)))
