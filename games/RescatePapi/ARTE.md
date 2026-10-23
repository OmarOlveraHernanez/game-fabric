# Rescate Papi · Guía de arte

Estilo **códice plano**: colores planos, contorno negro grueso, figuras de perfil, ojos grandes y expresivos. Referencia: Códice Borgia, simplificado y amigable. **Respetuoso**: inspiración, no copia de glifos sagrados.

Material de apoyo en `art-source/RescatePapi/`: `paleta.png`, `vista_previa.png` y `plantillas/` (un PNG por sprite con la caja de choque en rojo y el pivote en azul).

## 1. Reglas técnicas (para que el dibujo encaje solo)
| Regla | Valor |
|---|---|
| Escala | **256 px = 1 unidad del juego** (Papi, con orejas, ≈ 1 unidad) |
| Formato | **PNG con fondo transparente**, sin bordes de color alrededor |
| Lienzo | **Exactamente** el tamaño de la tabla (sección 3). Dibuja sobre la plantilla |
| Pivote | Abajo al centro: los pies/la base tocan el **borde inferior** del lienzo |
| Contorno | Negro `#1A1410`, **6 px** de grosor, igual en todos los sprites |
| Color | Solo de la paleta. Máximo **1 tono de sombra plana** por color. Sin degradados |
| Dirección | Papi mira a la **derecha**; los enemigos miran a la **izquierda** (vienen hacia ti) |
| Margen | El cuerpo debe llenar la caja roja de la plantilla (ni más chico ni más grande) |

Unity configura todo solo al importar (Core: `ArtImportPostprocessor`). No toques ajustes de importación.

## 2. Paleta
| Nombre | Hex | Uso |
|---|---|---|
| INK | `#1A1410` | contornos, pelo, ojos |
| AMATE | `#F2E6CC` | cielo/fondo claro |
| AMATE_D | `#D9C29A` | pirámides lejanas |
| TURQ / TURQ_L | `#1F8A8A` / `#4FB7B0` | ropa del dueño |
| JADE / JADE_L | `#3E8E4F` / `#7DBE6A` | serpiente, selva, pasto |
| OCRE / OCRE_L | `#D9962B` / `#F2C14E` | jaguar, grecas, pollito |
| GRANA / GRANA_L | `#B0302A` / `#D9574A` | paliacate, detalles |
| MAYA | `#3A6EA5` | acentos (poco) |
| CAFE / CAFE_D | `#7A4A2A` / `#4A2C1A` | águila, tierra, cacao |
| HUESO | `#FAF5EA` | blanco de ojos |
| PIEL / PIEL_S | `#C68A5E` / `#A06A44` | piel y su sombra |

## 3. Lista de sprites (MVP)
Carpeta: `games/RescatePapi/Assets/_Game/Art/Resources/Sprites/`. **El nombre del archivo decide dónde aparece.** Los cuadros de animación terminan en `_0`, `_1`, …

| Archivo | Lienzo (px) | Cuadros | Notas |
|---|---|---|---|
| `papi_correr_0..3` | 256×256 | 4 | **Papi (protagonista)** corriendo; patas alternas, orejas al viento |
| `papi_salto` | 256×256 | 1 | patas recogidas, orejas hacia atrás |
| `papi_golpe` | 256×256 | 1 | al chocar; ojos en X |
| `dueno_atrapado_0..1` | 256×256 | 2 | **el dueño** pidiendo ayuda (brazos arriba); se ve en la pantalla de inicio y con el jefe |
| `serpiente_0..1` | 320×160 | 2 | ondulación; penacho de plumas |
| `jaguar_0..1` | 384×384 | 2 | patas alternas (se tiñe de rojo al embestir) |
| `aguila_0..1` | 384×192 | 2 | alas arriba / abajo; **águila real** (toda café, nuca dorada; NO águila calva) |
| `cacao` | 128×128 | 1 | grano de cacao con brillo |
| `pollito_0..1` | 160×160 | 2 | aleteo |
| `suelo_tierra` | 512×256 | 1 | **repetible**: el borde izquierdo debe empatar con el derecho; pasto arriba (franja de ~26 px) |
| `fondo_lejano` | 2048×2048 | 1 | pirámides lejanas, sol; mayormente transparente arriba; repetible |
| `fondo_medio` | 2048×1024 | 1 | selva; transparente arriba; repetible |

Después del MVP: `aguila_jefe_0..1` (jefe), `piramide_meta`, íconos de la app, logo, trajes de Papi.

## 4. Flujo de trabajo
1. Abre la plantilla de `art-source/RescatePapi/plantillas/` en tu app de dibujo como capa de guía.
2. Boceta encima → limpia la línea (a mano o con IA, ver sección 6) → colorea con la paleta.
3. Oculta la plantilla y **exporta PNG transparente** del tamaño exacto, con el nombre de la tabla.
4. Guarda el archivo editable (PSD/Krita/etc.) en `art-source/RescatePapi/` y el PNG en la carpeta de Sprites (reemplaza el provisional).
5. En Unity pulsa ▶ Play: aparece solo. Si se ve mal, revisa tamaño del lienzo y que la base toque el borde inferior.

## 5. Diseño de personajes
- **Papi (protagonista):** chihuahua color **canela** con panza clara, **orejas enormes** con interior rosa, ojos grandes y brillantes, cola enroscada, **collar de jade**. Valiente y tierno. Debe leerse bien de perfil a tamaño pequeño.
- **El dueño (la meta):** hombre joven mexicano, piel morena, pelo negro corto, ojos grandes. Playera **turquesa** con greca ocre, **paliacate rojo**, pantalón café, huaraches. Aparece atrapado pidiendo ayuda. **Evitar estereotipos** (nada de sombrero gigante, bigote caricaturesco ni sarape de cliché).
- **Serpiente:** jade, cuerpo ondulado, penacho de plumas turquesa/ocre/grana (guiño a la serpiente emplumada, sin copiar a Quetzalcóatl).
- **Jaguar:** ocre con manchas café oscuro en anillo, perfil de códice, colmillos visibles pero no aterrador.
- **Águila real:** café con nuca dorada, pico amarillo, alas grandes de plumas escalonadas.

## 6. IA de apoyo (limpiar y colorear tus bocetos)
Úsala solo para **limpiar la línea y aplicar color plano**; la forma y la pose son tuyas. Revisa siempre la licencia de uso comercial de la herramienta y guarda el prompt usado en `art-source/prompts/`.

**Limpiar boceto → línea:**
> Clean line art from this sketch, keep exact pose and proportions. Flat 2D game sprite, side view, thick uniform black outline (6 px at 256 px height), no shading, no texture, transparent background, Mesoamerican codex style (Codex Borgia inspired), friendly and simple.

**Colorear con la paleta:**
> Color this line art with flat colors only, no gradients, at most one flat shadow tone per color. Use only this palette: #1A1410, #F2E6CC, #1F8A8A, #4FB7B0, #3E8E4F, #7DBE6A, #D9962B, #F2C14E, #B0302A, #C68A5E, #A06A44, #7A4A2A, #4A2C1A, #FAF5EA. Keep the black outline. Transparent background.

**Consistencia entre cuadros (animación):** genera cada cuadro con el **mismo prompt** y la imagen del cuadro anterior como referencia; corrige a mano las diferencias (la IA cambia detalles entre cuadros).

## 7. Arte provisional
Los sprites actuales en `Resources/Sprites/` son **provisionales** (generados por `art-source/RescatePapi/generar_provisional.py` y `generar_papi.py`) para probar el flujo y servir de referencia de estilo. Cada dibujo tuyo con el mismo nombre los reemplaza.
