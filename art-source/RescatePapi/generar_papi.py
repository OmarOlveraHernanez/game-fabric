import math, os, sys
sys.argv=['x']
exec(open('gen.py').read().split("# ---------- Dueño")[0])  # reutiliza Canvas, paleta, helpers
P['CANELA']=(214,160,96); P['CANELA_S']=(176,122,66); P['ROSA']=(232,150,150)

def papi(name, legs_phase, mode='run'):
    c = Canvas(256, 256)
    body_y = 176 if mode != 'jump' else 166
    # cola enroscada
    c.lineseg([(54,body_y-6),(30,body_y-30),(40,body_y-56),(60,body_y-50)], 'INK', 22)
    c.lineseg([(54,body_y-6),(30,body_y-30),(40,body_y-56),(60,body_y-50)], 'CANELA', 12)
    # patas traseras/delanteras (fondo)
    if mode == 'jump':
        sets_back = [(78, 60), (150, -60)]; sets_front=[(96,50),(168,-50)]
    elif mode == 'hit':
        sets_back = [(78, -30), (150, 30)]; sets_front=[(96,-20),(168,20)]
    else:
        a = 34*math.sin(legs_phase); sets_back=[(78, a), (150, -a)]; sets_front=[(96,-a),(168,a)]
    for x, ang in sets_back: c.limb(x, body_y+12, ang, 56, 18, 'CANELA_S')
    # cuerpo
    c.ell(122, body_y, 70, 38, 'CANELA')
    c.ell(132, body_y+14, 40, 18, 'AMATE', 0)  # panza clara
    for x, ang in sets_front: c.limb(x, body_y+14, ang, 56, 18, 'CANELA')
    # collar de jade
    c.rrect(168, body_y-44, 196, body_y-8, 8, 'JADE', 5)
    c.ell(184, body_y-6, 8, 8, 'JADE_L', 4)
    # cabeza grande
    hx, hy = 196, body_y-62
    # orejas enormes
    c.poly([(hx-30,hy-14),(hx-48,hy-78),(hx-6,hy-34)], 'CANELA', 6)
    c.poly([(hx-36,hy-22),(hx-44,hy-62),(hx-16,hy-34)], 'ROSA', 0)
    c.poly([(hx+4,hy-30),(hx+20,hy-92),(hx+30,hy-26)], 'CANELA', 6)
    c.poly([(hx+10,hy-34),(hx+20,hy-76),(hx+24,hy-32)], 'ROSA', 0)
    c.ell(hx, hy, 40, 36, 'CANELA')
    c.ell(hx+30, hy+14, 20, 14, 'AMATE', 5)   # hocico
    c.ell(hx+46, hy+8, 7, 6, 'INK', 0)         # nariz
    eye(c, hx+8, hy-4, 13, 1, dead=(mode=='hit'))
    if mode != 'hit': c.lineseg([(hx+28,hy+24),(hx+40,hy+22)], 'INK', 4)
    else: c.ell(hx+34, hy+26, 6, 5, 'GRANA', 3)
    c.save(name)

for i in range(4): papi(f'papi_correr_{i}', i*math.pi/2)
papi('papi_salto', 0, 'jump'); papi('papi_golpe', 0, 'hit')

# Dueño atrapado: reusa el dibujo del dueño con los brazos arriba (pide ayuda), 2 cuadros
src = open('gen.py').read()
owner_code = src.split("# ---------- Dueño (256x256, mira a la derecha) ----------")[1].split("runs = [")[0]
exec(owner_code)
owner('dueno_atrapado_0', (6,-6), (170,150), 'jump')
owner('dueno_atrapado_1', (6,-6), (150,175), 'jump')
print('ok')
