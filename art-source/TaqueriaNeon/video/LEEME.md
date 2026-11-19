# Puesto vivo (video de fondo de la escena)

- Origen: Weave, modelo "Kling First & Last Frame" (55 créditos, 16 nov 2026), a partir de `ia/barra_choricera_v2.png` (Figma nodo 35:5).
  El video original (5 s, 24 fps, 1920x1080) lo bajó el usuario a mano; Figma bloquea la descarga desde la nube.
- Proceso (ffmpeg), igual que `hacer_loop.sh`:
  1. Más lento al 60 % con cuadros intermedios: `setpts=PTS/0.6,minterpolate=fps=30:mi_mode=mci:mc_mode=aobmc:vsbmc=1`.
  2. El final no coincidía con el inicio (los quemadores pasan de azul a naranja): se une la vuelta con un fundido de 1.5 s.
  3. H.264 baseline (sin cuadros B, lo pide Unity), crf 23, 30 fps → `games/Tacos/Assets/_Game/Video/Resources/Video/puesto_vivo.mp4` (6.8 s, ~5.5 MB).
- El puesto no se mueve de lugar en el video: las zonas que se tocan siguen alineadas con `KitchenScene`.

## Versión 2 (16 nov 2026, noche)
El usuario vio el trompo muy rápido y el corte al reiniciar. No alcanzaron los créditos de Weave para un video nuevo de 10 s (109),
así que se eligió la opción 3: el trompo y los quemadores quedan quietos (se toman del primer cuadro con una máscara suave, `maskedmerge`)
y solo se mueven la grasa, el vapor y las luces de la calle. Fundido de la vuelta: 2 s. Resultado: 6.3 s, ~4.6 MB, H.264 baseline, BT.709.
Máscara: rectángulo del trompo (x 0..331, y 0..781 en 1920x1080) y franja de quemadores (x 187..1187, y 818..937), difuminada 14 px.

## Versión 3: el trompo girando aparte (16 nov 2026, noche)
El usuario extrañó el trompo girando. Ahora es un segundo video encima del puesto (`puesto_trompo.mp4`, 360x800, rincón superior izquierdo):
el trompo del video original al 30 % de velocidad, de ida y vuelta (33 s) para que se repita sin brinco, y con las orillas iguales al dibujo.
En el juego va en `KitchenScene.AddVideo` sobre el rectángulo x 0..288, y 0..640 del dibujo (1536x864).

## Versión 4: lumbre azul aparte (16 nov 2026, noche)
Al usuario le gustaban las flamas del primer video. Ahora son un tercer video (`puesto_lumbre.mp4`, 1060x166): solo los primeros 1.3 s
del original (después se ponen naranjas), a la mitad de velocidad y de ida y vuelta (4.9 s). En el juego, rectángulo x 128..976, y 635..768.

## Versión 5: la lumbre del primer video (16 nov 2026, noche)
La lumbre de ida y vuelta no le gustó al usuario: prefirió la del primer video. `puesto_lumbre.mp4` es ahora la franja de quemadores
(1060x166) recortada de la versión 1 (60 % de velocidad, fundido de 1.5 s, 6.8 s), con las orillas iguales al dibujo.

## Garnachas (18 de noviembre de 2026)
- `garnachas_original.mp4`: Kling First & Last Frame en Weave (55 créditos, aprobados por el usuario), a partir de `barra_garnachas`.
- `garnachas_vivo.mp4` (en `Video/Resources/Video`): escalado a 1536x864, al 60 % de velocidad, loop con crossfade de 1.5 s, H.264 baseline `-bf 0` y etiquetas BT.709. Mismo proceso que `hacer_loop.sh`.
- Se usa como fondo completo del puesto (`StallLayout.Garnachas.Video`); las cazuelas, masas y adornos van dibujados encima.
