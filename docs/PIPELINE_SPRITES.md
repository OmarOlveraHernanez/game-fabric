# Pipeline de sprites 2D con IA · Propuesta

**Estado:** propuesta (2026-10-22). **Nada instalado ni contratado** hasta autorización del usuario.
Precios consultados el 2026-10-22 en fuentes públicas; verificar en la página del proveedor antes de pagar.

## 1. Qué tenemos hoy (sin gastar)
| Recurso | Estado | Uso posible |
|---|---|---|
| **Figma (conector MCP)** | ✅ conectado, plan **Starter** (500 créditos IA/mes, máx. 150/día) | `generate_image` con Gemini Flash Image / GPT Image dentro de un archivo de Figma. Sirve para **explorar diseños** sin pagar extra (consume créditos del plan; el costo por imagen no está publicado) |
| **Figma Weave** (catálogo de modelos) | ⚠️ no vinculado | Requiere vincular la cuenta en app.weavy.ai; gasta créditos de Weave |
| **Unity 2D Animation + PSD Importer** | ✅ ya instalados en el proyecto | Animación por **huesos** (gratis): se dibuja el personaje una vez por partes y se anima |
| **Core: ArtImportPostprocessor** | ✅ | PNG en `_Game/Art` → Sprite listo (256 PPU, pivote abajo-centro) |
| Python + Pillow (en la nube de Claude) | ✅ | Post-proceso: recorte, escala, paleta, validación, spritesheets |
| Conectores MCP de generación de imágenes en el registro | ❌ no hay | Se usaría la API directa con una llave (o un MCP propio pequeño, después) |

## 2. Herramientas evaluadas
| Herramienta | Para qué | Costo aprox. | API/MCP | Encaje con estilo códice |
|---|---|---|---|---|
| **Google Gemini Image** ("Nano Banana" 2.1 / 2 / Pro) | Generar y **editar con imagen de referencia** (misma criatura, otra pose) | ~$0.034 (2.1, 1K) · ~$0.067 (NB2, 1K) · ~$0.13 (Pro) por imagen | API ✅ | ⭐⭐⭐ muy buena consistencia por referencia |
| **OpenAI GPT Image** (1 / 1 Mini / 2) | Generar; GPT-Image-2 puede sacar **PNG transparente** directo (`background=transparent`, en preview) | GPT Image 1: $0.011 / $0.042 / $0.167 (low/med/high) · Mini desde $0.005 | API ✅ | ⭐⭐⭐ buena línea limpia |
| **FLUX (BFL)** Kontext/FLUX.2 | Edición consistente | ~$0.048/imagen (BFL) | API ✅ | ⭐⭐ |
| **Scenario** | Entrenar un **modelo con tu estilo** | Pro $45/mes (entrenamiento incluido); Free no permite uso comercial | API (planes pagados) | ⭐⭐⭐ cuando haya varios juegos |
| **PixelLab** | Personajes y animación por esqueleto | $12–50/mes | API ✅ | ⭐ es pixel art, no nuestro estilo |
| **Ludo.ai** | Sprites y animación | Pro $35/mes (anual) | API + MCP en Pro | ⭐⭐ estilo por presets |
| **Quitar fondo – local** rembg + ISNet/u2net/BiRefNet | PNG transparente | **Gratis** (MIT/Apache) | local | ✅ (evitar el modelo BRIA: licencia no comercial) |
| **Quitar fondo – fondo verde** | Pedir a la IA fondo #00FF00 y recortarlo por color | **Gratis** y exacto en arte plano | local | ✅ el mejor para colores planos |
| **Spritesheets** | Empaquetar cuadros | Script propio (gratis) o Sprite Atlas de Unity (gratis) | local | ✅ |

## 3. Decisión clave: cómo conseguir CONSISTENCIA barata
La IA cambia detalles entre imágenes (orejas, collar, proporciones). Dos caminos:

**A) Cuadro por cuadro con referencia** (para enemigos y objetos, 1–2 cuadros)
Generar el cuadro 0, y los siguientes como *edición* de ese ("mismo personaje, pata trasera adelante"). Barato y rápido; requiere revisar cada cuadro.

**B) Personaje por partes + huesos en Unity** (para **Papi**, recomendado)
La IA genera **una sola hoja de partes** (cabeza, orejas, cuerpo, 4 patas, cola) en la misma imagen → se separan y se arman en Unity 2D Animation con huesos. Ventajas:
- **Consistencia perfecta** (es el mismo dibujo en todas las poses).
- Correr, saltar, golpe, celebrar… salen **gratis** animando huesos; más fluido que 4 cuadros.
- **Trajes de la tienda** = cambiar una parte (sombrero, collar, armadura) sin redibujar al personaje → monetización barata.

## 4. Arquitectura propuesta
```
 Idea (texto del usuario)
   │  Claude: ficha de personaje (proporciones, paleta, lienzo, caja de choque) + prompts
   ▼
 [1] Exploración ── Figma generate_image (créditos del plan, $0 extra) ──► 3-4 opciones → usuario elige
   ▼
 [2] Producción ── tools/art/gen.py → API (Gemini Image; alternativa GPT Image)
   │      · referencia = diseño elegido · fondo verde liso · registro de prompt/modelo/fecha
   ▼
 [3] Post-proceso local (gratis) ── tools/art/process.py
   │      · quitar fondo (verde → alfa; rembg como respaldo) · ajustar a la paleta oficial
   │      · recortar/escalar al lienzo (256 px = 1 u) · pivote abajo-centro
   │      · validar: llena la caja de choque, contorno, sin semitransparencias raras
   ▼
 [4] Revisión humana ── hoja de contacto en el chat → apruebo / pido cambios
   ▼
 [5] Entrega a Unity ── PNG en Assets/_Game/Art/Resources/Sprites (cuadros sueltos)
   │      o PSB por partes para huesos (Papi) · el Core los importa solo
   ▼
 [6] Prueba en juego ── ▶ Play / emulador
```
- Las llaves de API van en **variables de entorno** del PC del usuario (nunca en el repo) y con **tope de gasto** en la consola del proveedor.
- Scripts en `tools/art/` (los corre Claude Code en el PC del usuario). Un MCP propio solo si después hace falta.
- Todo prompt usado queda en `art-source/<Juego>/prompts/` (trazabilidad y licencias).

## 5. Costos estimados (MVP de Rescate Papi)
| Concepto | Cantidad | Costo |
|---|---|---|
| Exploración de diseño en Figma | ~20–40 imágenes | **$0 extra** (créditos del plan Starter) |
| Producción con Gemini 2.1 (~$0.034) | ~11 sprites, ~20 cuadros, ~5 intentos c/u ≈ 150 imágenes | **≈ $5** |
| Margen para Pro/alta calidad en piezas clave (Papi, logo, fondos) | ~30 imágenes × $0.13 | **≈ $4** |
| Quitar fondo, paleta, spritesheets, huesos en Unity | — | **$0** |
| **Total MVP** | | **≈ $10 USD** (tope sugerido: $15) |
| Opcional después: Scenario Pro (estilo entrenado, para varios juegos) | por mes, solo cuando se use | $45/mes |

## 6. Fases
| Fase | Qué | Costo | Requiere autorización |
|---|---|---|---|
| **S0** Ficha de Papi | Claude escribe la ficha y los prompts a partir de tus ideas (sin boceto) | $0 | no |
| **S1** Exploración | 3–4 diseños de Papi con Figma `generate_image`; eliges uno | $0 extra (créditos Figma) | **sí** (usa créditos del plan) |
| **S2** Pipeline local | `tools/art/process.py`: fondo verde→alfa, paleta, lienzo, validación, hoja de contacto | $0 | sí (instalar paquetes Python en tu PC) |
| **S3** Producción | Llave de API Gemini con tope $15; `gen.py` genera hoja de partes de Papi + enemigos/objetos | ≈ $10 | **sí** (gasto) |
| **S4** Huesos | Papi por partes → rig en Unity 2D Animation (correr, saltar, golpe) | $0 | no |
| **S5** Fábrica | Mover el pipeline a la plantilla para todos los juegos; evaluar Scenario si hay 3+ juegos | $0 / $45 mes | sí |

## 7. Riesgos
- **Consistencia**: mitigada con referencia + partes/huesos + revisión humana.
- **Estilo genérico de IA**: mitigado con ficha estricta, paleta forzada en post-proceso y retoque.
- **Licencias**: usar solo planes que permitan uso comercial (no el Free de Scenario/Leonardo; no el modelo BRIA RMBG). Guardar prompts y fechas.
- **Precios cambiantes**: verificar antes de cargar saldo; topes de gasto en la consola.
- **Privacidad**: no usar niveles gratuitos de API que entrenan con tus imágenes para arte del juego.
