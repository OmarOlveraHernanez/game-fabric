"""Procesa las hojas IA de Taquería Neón -> sprites PNG con transparencia para Unity.

Fondo: verde (#00FF00) o magenta (#FF00FF). Solo se quita el color de fondo que toca el borde de la hoja,
asi lo verde/morado DENTRO de una figura (sueter, cilantro, rebozo) no se borra.
Cada figura se asigna a una celda de la cuadricula (columnas x filas) segun su centro; las piezas sueltas
de una misma celda (sarten, tortillas en el aire) se juntan.

uso: python3 procesar.py <carpeta_ia> <carpeta_salida>
"""
import sys, os
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUT = sys.argv[1], sys.argv[2]
os.makedirs(OUT, exist_ok=True)


def chroma_alpha(rgb, mode):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    if mode == 'gray':
        # fondo gris claro parejo (algunas hojas de Figma salen asi aunque se pida magenta): distancia al color de las esquinas
        h, w = rgb.shape[:2]
        bgc = np.median(np.concatenate([rgb[:8, :8].reshape(-1, 3), rgb[:8, -8:].reshape(-1, 3), rgb[-8:, :8].reshape(-1, 3), rgb[-8:, -8:].reshape(-1, 3)]), axis=0)
        dist = np.abs(rgb - bgc).max(axis=2)
        return np.clip((14 - dist) / 10, 0, 1)
    if mode == 'green':
        d = g - np.maximum(r, b)
    else:  # magenta
        d = np.minimum(r, b) - g
    # d alto = fondo. 40..110 -> transicion suave
    bg = np.clip((d - 40) / 70, 0, 1)
    return bg


def key(img, mode):
    rgb = np.asarray(img.convert('RGB')).astype(np.float32)
    bgness = chroma_alpha(rgb, mode)
    candidate = bgness > 0.05
    lab, n = ndimage.label(candidate)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    # huecos cerrados (entre brazo y cuerpo, dentro de la cola de caballo): si son color de fondo puro, tambien son fondo
    means = ndimage.mean(bgness, lab, index=np.arange(1, n + 1)) if n else []
    strong = {i + 1 for i, m in enumerate(means) if m > 0.75}
    bgmask = np.isin(lab, list(border | strong))
    alpha = np.where(bgmask, 1 - bgness, 1.0)
    # despill en el fondo y en un anillo alrededor (bordes semitransparentes sin halo de color)
    ring = ndimage.binary_dilation(bgmask, iterations=3)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    if mode == 'green':
        g = np.where(ring, np.minimum(g, np.maximum(r, b)), g)
    else:
        excess = np.clip(np.minimum(r, b) - g, 0, None)
        r = np.where(ring, r - excess, r)
        b = np.where(ring, b - excess, b)
    rgba = np.dstack([r, g, b, alpha * 255]).clip(0, 255).astype(np.uint8)
    return rgba


def split_grid(rgba, cols, rows, min_px=150):
    a = rgba[..., 3] > 128
    a = ndimage.binary_opening(a, iterations=1)
    lab, n = ndimage.label(a)
    H, W = a.shape
    cells = {}
    for i, sl in enumerate(ndimage.find_objects(lab), start=1):
        size = int((lab[sl] == i).sum())
        if size < min_px:
            continue
        cy = (sl[0].start + sl[0].stop) / 2
        cx = (sl[1].start + sl[1].stop) / 2
        c = min(cols - 1, int(cx / (W / cols))) + cols * min(rows - 1, int(cy / (H / rows)))
        cells.setdefault(c, []).append(i)
    out = []
    for c in range(cols * rows):
        ids = cells.get(c, [])
        if not ids:
            out.append(None)
            continue
        m = np.isin(lab, ids)
        m = ndimage.binary_dilation(m, iterations=3)
        ys, xs = np.where(m)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        crop = rgba[y0:y1, x0:x1].copy()
        crop[..., 3] = np.where(m[y0:y1, x0:x1], crop[..., 3], 0)
        out.append(Image.fromarray(crop))
    return out


def fit(img, max_w, max_h):
    s = min(max_w / img.width, max_h / img.height, 1.0)
    return img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)


def save(img, name):
    img.save(os.path.join(OUT, name + '.png'), optimize=True)
    print(f'{name}: {img.width}x{img.height}')


def sheet(file, mode, cols, rows, names, max_w, max_h, same_scale=False):
    rgba = key(Image.open(os.path.join(SRC, file)), mode)
    parts = split_grid(rgba, cols, rows)
    assert len(names) <= len(parts)
    if same_scale:
        # misma escala para todas las poses de un personaje (no cambia de tamaño entre poses)
        s = min(min(max_w / p.width, max_h / p.height) for p in parts if p is not None)
        s = min(s, 1.0)
        for p, nm in zip(parts, names):
            if p is None or nm is None:
                continue
            save(p.resize((round(p.width * s), round(p.height * s)), Image.LANCZOS), nm)
    else:
        for p, nm in zip(parts, names):
            if p is None:
                print('FALTA', nm)
                continue
            if nm:
                save(fit(p, max_w, max_h), nm)


# Taqueros: 3 poses (quieto, cocinando, festejando), misma escala, ~560 px de alto
for t in ['guero', 'tio', 'paisa']:
    sheet(f'{t}_hoja.png', 'green', 3, 1, [f'{t}_quieto', f'{t}_cocinando', f'{t}_festeja'], 520, 560, same_scale=True)

# Clientes: 3 columnas (personaje) x 3 filas (contento, esperando, enojado)
emo = ['contento', 'espera', 'enojado']
for hoja, quienes in [('clientes_hoja_1.png', ['cliente1', 'cliente2', 'cliente3']),
                      ('clientes_hoja_2.png', ['cliente4', 'cliente5', 'cliente6'])]:
    names = [f'{quienes[c]}_{emo[r]}' for r in range(3) for c in range(3)]
    sheet(hoja, 'green', 3, 3, names, 300, 300)
    # Los clientes se paran detras de la barra: quitar las filas de abajo transparentes o a medias
    # (se veian como una linea entre la persona y la barra)
    for n in names:
        fp = os.path.join(OUT, n + '.png')
        im = Image.open(fp); al = np.asarray(im)[..., 3]
        full = (al > 200).sum(axis=1)
        y = len(full) - 1
        while y > 0 and full[y] < 0.6 * full[max(0, y - 6)]: y -= 1
        im.crop((0, 0, im.width, y + 1)).save(fp, optimize=True)

# Ingredientes (magenta): 4x3
sheet('ingredientes_hoja.png', 'magenta', 4, 3,
      ['ing_tortilla', 'ing_pastor', 'ing_asada', 'ing_cebolla', 'ing_cilantro', 'ing_pina',
       'ing_salsa_verde', 'ing_salsa_roja', 'ing_limon', 'ing_taco', 'ing_tortillas', 'ing_plato'], 220, 220)

# Estaciones (magenta): 3x2
sheet('estaciones_hoja.png', 'magenta', 3, 2,
      ['est_plancha', 'est_comal', 'est_trompo', 'est_basura', 'est_extras', None], 420, 420)

# Carnes de la choricera (magenta): 2x2
sheet('carnes_extra_hoja.png', 'magenta', 2, 2, ['ing_chorizo', 'ing_suadero', 'ing_tripa', 'ing_longaniza'], 220, 220)

# Escena de juego: el puesto con la choricera (cazos vacios; el juego dibuja la carne encima). Sin escalar: 1536x864
# v2 (16 nov 2026): grasa dorada y mas detalles en el puesto. Posiciones medidas en KitchenScene.cs
Image.open(os.path.join(SRC, 'barra_choricera_v2.png')).convert('RGB').save(os.path.join(OUT, 'barra_choricera.jpg'), quality=90)
print('barra_choricera.jpg 1536x864')

# Mapas: puestos flotantes (zonas) y la calle de la zona 1. Posiciones en TacosGame.Maps.cs
for name in ['mapa_islas', 'mapa_zona1']:
    Image.open(os.path.join(SRC, name + '.png')).convert('RGB').save(os.path.join(OUT, name + '.jpg'), quality=88)
    print(name + '.jpg 1536x864')

# Power-ups e iconos de interfaz (magenta): 3x2
sheet('powerups_hoja.png', 'magenta', 3, 2, ['pw_cebollitas', 'pw_nopal', 'pw_salsa', 'pw_trompo', 'ui_mano', 'ui_racha'], 300, 300)

# Mapa de mundos (16 nov 2026): cielo, una isla por zona, puente entre islas e iconos de interfaz
Image.open(os.path.join(SRC, 'cielo_mapa.png')).convert('RGB').save(os.path.join(OUT, 'cielo_mapa.jpg'), quality=88)
print('cielo_mapa.jpg')
for z in ['callejero', 'chorizera', 'mercado']:
    sheet(f'isla_{z}.png', 'magenta', 1, 1, [f'isla_{z}'], 760, 760)
sheet('puente.png', 'magenta', 1, 1, ['puente'], 1400, 500)
sheet('iconos_ui_hoja.png', 'magenta', 3, 2, ['ui_moneda', 'ui_chile', 'ui_candado', 'ui_estrella', 'ui_letrero', 'ui_monedas'], 260, 260)

# Fondo: tal cual, 1920x1080
bg = Image.open(os.path.join(SRC, 'fondo_calle.png')).convert('RGB').resize((1920, 1080), Image.LANCZOS)
bg.save(os.path.join(OUT, 'fondo_calle.jpg'), quality=88)
print('fondo_calle.jpg 1920x1080')

# Logo neon sobre negro -> transparencia por luminancia (el brillo se conserva)
lg = np.asarray(Image.open(os.path.join(SRC, 'logo_taqueria_neon.png')).convert('RGB')).astype(np.float32) / 255
a = lg.max(axis=2)
a = np.clip((a - 0.04) / 0.96, 0, 1)
rgb = np.where(a[..., None] > 0.001, lg / np.maximum(a[..., None], 1e-3), 0).clip(0, 1)
logo = Image.fromarray((np.dstack([rgb, a]) * 255).astype(np.uint8))
bbox = logo.getchannel('A').point(lambda v: 255 if v > 10 else 0).getbbox()
logo = fit(logo.crop(bbox), 1200, 600)
save(logo, 'logo')

# Tienda de mejoras (puesto de jarceria) y objetos de cada mejora
Image.open(os.path.join(SRC, 'tienda_fondo.png')).convert('RGB').save(os.path.join(OUT, 'tienda_fondo.jpg'), quality=88)
sheet('mejoras_hoja.png', 'magenta', 3, 2, ['mej_radio', 'mej_domo', 'mej_platos', 'mej_cazo', 'ui_tarjeta', 'ui_sello'], 360, 420)

# Adornos que se cocinan en la grasa del comal (no se tocan: solo para que no se vea vacio)
sheet('comal_adornos_hoja.png', 'magenta', 3, 2, ['com_cebollitas', 'com_tripa', 'com_nopal', 'com_nopales', 'com_cebollitas2', 'com_tripa2'], 240, 240)
sheet('comal_adornos2_hoja.png', 'magenta', 3, 2, ['com_espiral', 'com_longaniza', 'com_chorizos', 'com_tripa3', 'com_tripa4', 'com_tripa5'], 360, 240)
# Despues: hundir_en_grasa.py hace que cada com_* se vea metido en el aceite
# Tripa de verdad (rosita y enroscada, como en la choricera): reemplaza a la tripa dorada de las hojas anteriores
sheet('comal_tripa_hoja.png', 'magenta', 3, 2, ['com_tripa3', 'com_tripa4', 'com_tripa5', 'com_tripa', 'com_tripa2', 'com_tripa_mix'], 320, 240)
# La orilla del comal bien llena, como en los puestos (chorizos encimados, carne doradita, manojo de cebollas)
sheet('comal_orilla_hoja.png', 'magenta', 3, 2, ['com_salchichas', 'com_suadero', 'com_manojo', 'com_chorizo_suelto', 'com_bistec', 'com_cabeza'], 360, 260)

# Islas del mapa: un puesto por mundo (Taqueria -> Garnachas -> Elotes -> Pozoleria -> Tamales -> La Feria)
sheet('islas_puestos1.png', 'magenta', 3, 1, ['isla_garnachas', 'isla_elotes', 'isla_pozole'], 700, 760)
sheet('islas_puestos2.png', 'magenta', 2, 1, ['isla_tamales', 'isla_feria'], 760, 760)

# ---- Mundo 2: Garnachas ----
Image.open(os.path.join(SRC, 'puestos', 'barra_garnachas.png')).convert('RGB').save(os.path.join(OUT, 'barra_garnachas.jpg'), quality=88)
sheet('garnachas_masas.png', 'magenta', 3, 3, ['base_sope', 'base_sope_lista', 'pila_sope', 'base_gordita', 'base_gordita_lista', 'pila_gordita',
      'base_quesadilla', 'base_quesadilla_lista', 'pila_quesadilla'], 220, 220)
sheet('garnachas_guisados.png', 'magenta', 4, 2, ['ing_chicharron', 'ing_tinga', 'ing_papa', 'ing_frijoles', 'ing_crema', 'ing_queso', 'ing_lechuga', 'base_quemada'], 220, 200)
# Masas mas realistas (reemplazan a las de garnachas_masas.png)
sheet('garnachas_masas2.png', 'magenta', 3, 3, ['base_sope', 'base_sope_lista', 'pila_sope', 'base_gordita', 'base_gordita_lista', 'pila_gordita',
      'base_quesadilla', 'base_quesadilla_lista', 'pila_quesadilla'], 260, 260)
# Power-ups de las garnachas: hacen lo mismo que los de la taqueria, con otro dibujo
sheet('garnachas_powerups.png', 'magenta', 4, 1, ['pw_papas', 'pw_nopalitos', 'pw_molcajete', 'pw_comal'], 260, 260)
# Adornos de la escena de garnachas (no se tocan)
sheet('garnachas_adornos.png', 'magenta', 4, 2, ['gar_papel', 'gar_prensa', 'gar_canasta', 'gar_horchata', 'gar_jamaica', 'gar_chiles', 'gar_nopales', 'gar_neon'], 380, 300)
# Clientes nuevos: aqui cada FILA es un personaje y las columnas son contento, esperando y enojado
# (la fila 2 de clientes_hoja_4, el luchador, no se usa: su mascara se parecia a la de un luchador real)
# Las camisas se tocan entre celdas, asi que se corta en tercios exactos (no por manchas)
def cells(file, mode, cols, rows, names, max_w, max_h):
    rgba = key(Image.open(os.path.join(SRC, file)), mode)
    if not isinstance(rgba, Image.Image): rgba = Image.fromarray(rgba)
    cw, ch = rgba.width // cols, rgba.height // rows
    for i, nm in enumerate(names):
        if not nm: continue
        c, r = i % cols, i // cols
        cell = rgba.crop((c * cw + 2, r * ch + 2, (c + 1) * cw - 2, (r + 1) * ch - 2))
        save(fit(cell.crop(cell.getbbox()), max_w, max_h), nm)

for hoja, filas in [('clientes_hoja_3.png', {0: 'cliente7', 1: 'cliente8', 2: 'cliente9'}),
                    ('clientes_hoja_4.png', {0: 'cliente10', 2: 'cliente11'})]:
    names = [f'{filas[r]}_{emo[c]}' if r in filas else None for r in range(3) for c in range(3)]
    cells(hoja, 'magenta', 3, 3, names, 300, 300)
    for n in names:
        if not n: continue
        fp = os.path.join(OUT, n + '.png')
        im = Image.open(fp); al = np.asarray(im)[..., 3]
        full = (al > 200).sum(axis=1)
        y = len(full) - 1
        while y > 0 and full[y] < 0.6 * full[max(0, y - 6)]: y -= 1
        im.crop((0, 0, im.width, y + 1)).save(fp, optimize=True)
# Despues: python3 hundir_en_grasa.py OUT/gar_chiles.png OUT/gar_chiles.png (igual gar_nopales): van en la orilla del comal
# Jefes finales (mision 12 de cada mundo): fila = jefe; columnas = contento, exigente, furioso
cells('jefes_hoja.png', 'magenta', 3, 2, ['jefe_taqueria_contento', 'jefe_taqueria_espera', 'jefe_taqueria_enojado',
      'jefe_garnachas_contento', 'jefe_garnachas_espera', 'jefe_garnachas_enojado'], 340, 340)
# Mejoras de la tienda del mundo 2 (garnachas)
sheet('mejoras_garnachas.png', 'magenta', 4, 1, ['mej_aguas', 'mej_comal_barro', 'mej_tabla', 'mej_cazuela'], 360, 300)
# Cazuelas completas (llenas de su guisado, vacia y tapada) y pilas de masa realistas sobre servilleta
sheet('garnachas_cazuelas.png', 'magenta', 3, 2, ['cazuela_chicharron', 'cazuela_tinga', 'cazuela_papa', 'cazuela_frijoles', 'cazuela_vacia', 'cazuela_tapada'], 300, 260)
sheet('garnachas_pilas.png', 'magenta', 3, 1, ['pila_sope', 'pila_gordita', 'pila_quesadilla'], 260, 300)
# Pantalla de resultados: cartel con foquitos, liston, botones de madera pintada y hueco de estrella
sheet('ui_resultados.png', 'magenta', 3, 2, ['ui_cartel', 'ui_liston', 'btn_verde', 'btn_madera', 'btn_rojo', 'ui_estrella_hueco'], 640, 420)
# Calle de misiones del mundo 2 (tianguis de garnachas); las posiciones de las 12 misiones estan en TacosGame.Maps.cs
Image.open(os.path.join(SRC, 'mapa_garnachas.png')).convert('RGB').save(os.path.join(OUT, 'mapa_garnachas.jpg'), quality=88)

# ---- Mundo 3: Elotes ----
Image.open(os.path.join(SRC, 'puestos', 'barra_elotes.png')).convert('RGB').save(os.path.join(OUT, 'barra_elotes.jpg'), quality=88)
Image.open(os.path.join(SRC, 'mapa_elotes.png')).convert('RGB').save(os.path.join(OUT, 'mapa_elotes.jpg'), quality=88)
cells('elotes_hoja.png', 'magenta', 4, 3, [None, None, None, None, 'ing_elote', 'ing_esquite', 'ing_tuetano', None,
      'olla_elotes', 'olla_esquite', 'olla_tuetano', None], 300, 300)
cells('elotes_extras.png', 'magenta', 4, 3, ['ing_mayonesa', 'ing_chilepolvo', 'ing_picante', 'ing_queso_seco',
      'pw_elote_oro', 'pw_raspado', 'pw_chilito', 'pw_olla', 'mej_bocina', 'mej_olla_express', 'mej_carrito', 'mej_ollota'], 300, 300)
cells('jefe_elotes.png', 'magenta', 3, 1, ['jefe_elotes_contento', 'jefe_elotes_espera', 'jefe_elotes_enojado'], 340, 340)
# Elote asado (reemplaza al esquite de tuetano). barra_elotes ya trae las ollas llenas y el anafre.
cells('elotes_asados.png', 'magenta', 3, 1, ['ing_asado', None, None], 300, 300)
# Cocineros de cada puesto (fila = cocinero; columnas = quieto, cocinando, festeja)
for hoja, ids in [('cocineros_garnachas.png', ['mago', 'chata', 'beto']), ('cocineros_elotes.png', ['cheto', 'yesi', 'chuy'])]:
    cells(hoja, 'magenta', 3, 3, [f'{i}_{p}' for i in ids for p in ['quieto', 'cocinando', 'festeja']], 380, 560)

# ---- Mundos 4, 5 y 6: Pozoleria, Tamales y La Feria ----
# Los fondos de los puestos son los de ia/puestos/cocina_*.png (ya traen las ollas llenas)
for nm in ['pozole', 'tamales', 'feria']:
    # la feria se volvio a pintar (20 nov.): la maqueta no iba con el estilo de los demas puestos
    fondo = {'feria': 'barra_feria_v2.png', 'tamales': 'cocina_tamales_v2.png'}.get(nm, 'cocina_' + nm + '.png')  # tamales: verdes y rajas repintados (repinta_vaporera.py)
    Image.open(os.path.join(SRC, 'puestos', fondo)).convert('RGB').save(os.path.join(OUT, 'barra_' + nm + '.jpg'), quality=88)
    Image.open(os.path.join(SRC, 'mapa_' + nm + '.png')).convert('RGB').save(os.path.join(OUT, 'mapa_' + nm + '.jpg'), quality=88)
sheet('pozole_hoja.png', 'gray', 4, 3, ['ing_pozole_rojo', 'ing_pozole_blanco', 'ing_pozole_verde', 'base_plato_chico',
      'base_plato_grande', 'ing_rabano', 'ing_oregano', 'pw_tostadas', 'pw_jamaica', 'pw_piquin', 'pw_olla_pozole', None], 300, 300)
cells('tamales_hoja.png', 'gray', 4, 3, ['ing_tamal_verde', 'ing_tamal_rojo', 'ing_tamal_rajas', 'ing_tamal_dulce',
      'base_hoja', 'base_bolillo', 'ing_champurrado', 'ing_atole_arroz', 'ing_atole_fresa', 'pila_hojas', 'cazuelita_crema', 'cazuelita_queso'], 300, 300)
cells('powerups_tamales_feria.png', 'magenta', 4, 3, ['pw_tamal', 'pw_cafe', 'pw_rosca', 'pw_vaporera', 'pw_algodon', 'pw_rueda', 'pw_loteria', 'pw_cohetes',
      'olla_tamales', None, None, None], 300, 300)
cells('mejoras_456.png', 'magenta', 4, 3, ['mej_rocola', 'mej_fogon', 'mej_mesa', 'mej_olla_pozolera', 'mej_atolito', 'mej_vaporera', 'mej_canasta', 'mej_tamalera',
      'mej_banda', 'mej_fogones', 'mej_puesto', 'mej_ollas_feria'], 360, 300)
cells('jefes_456.png', 'magenta', 3, 3, [f'jefe_{w}_{m}' for w in ['pozole', 'tamales', 'feria'] for m in ['contento', 'espera', 'enojado']], 340, 340)
for hoja, ids in [('cocineros_pozole.png', ['goyo', 'rosa', 'nacho']), ('cocineros_tamales.png', ['fili', 'lencha', 'tono'])]:
    cells(hoja, 'magenta', 3, 3, [f'{i}_{p}' for i in ids for p in ['quieto', 'cocinando', 'festeja']], 380, 560)
# Botones de mision de la calle (20 nov.): azulejo de talavera abierto, con papel picado si ya tiene estrellas, y de piedra si esta cerrado
cells('ui_misiones.png', 'magenta', 3, 1, ['ui_mision', 'ui_mision_hecha', 'ui_mision_cerrada'], 300, 300)
# Tamales con colores bien distintos (22 nov.): el verde y el de rajas se confundian en el globito.
# Reemplazan a los de tamales_hoja.png (van despues, con el mismo nombre)
cells('tamales_colores.png', 'magenta', 4, 1, ['ing_tamal_verde', 'ing_tamal_rojo', 'ing_tamal_rajas', 'ing_tamal_dulce'], 300, 300)
