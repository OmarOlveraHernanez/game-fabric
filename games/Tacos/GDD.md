# Taquería Neón · GDD

**Estado:** T3a, arte, sonido, power-ups y mapas (noviembre de 2026). T2 aprobado el 15 de noviembre.

**Nombre:** Taquería Neón (`com.ollinixgames.taquerianeon`, provisional hasta publicar).

**Material de referencia:**
- Bocetos en Figma: https://www.figma.com/design/KqK4C4vZ3KEKRLtesTdnVz
- Maqueta HTML: https://claude.ai/artifact/DX7TDmyMxt1BauAYnZppFU

## Concepto
Juego de cocina en una taquería callejera de la Ciudad de México. Llegan clientes con su pedido; pones la carne en la plancha, armas el taco con sus extras y lo entregas antes de que se canse el cliente. Partidas de 90 segundos.

## Estilo (aprobado)
Anime indie: sombreado cel, sombras definidas, trazo con grosor variable, neón y fondos pintados de la ciudad de noche. Debe llamar la atención y no verse tipo Disney. La paleta del prototipo ya va en esa línea (noche, chile, maíz, limón, turquesa).

## Arte y sonido (T3a)
- Fuentes y scripts en `art-source/TaqueriaNeon/` (hojas IA de Figma + `procesar.py` + `audio/synth_tacos.py`).
- El juego carga `Resources/Sprites` y `Resources/Audio`. Si falta un archivo, usa cuadros de color y tonos del Core.
- El taquero cambia de pose: cocinando al usar comal o plancha, festejando al cobrar.
- La cara del cliente cambia con su paciencia: contento, esperando, enojado.

## Bucle de un pedido (escena de la choricera, T3a)
La pantalla es el puesto mismo: se toca la cosa, no botones. Lógica en `KitchenSimulation`, escena en `KitchenScene`.
1. Llega un cliente detrás de la barra con su pedido en un globito de dibujos y su barra de paciencia.
2. (Misión 3 en adelante) Tocas la pila de tortillas: una se calienta en el domo de la choricera. Cuando brilla, la tocas y pasa al plato. Si la dejas de más, se quema.
3. Tocas la carne: el pastor sale del trompo y las demás de su cazo en la choricera. Es al instante.
4. Cada cazo (y el trompo) tiene porciones (5; 8 con "Cazos grandes"). Cuando se vacía, lo tocas para rellenarlo y tarda 4 s (3.2 s con el Güero). La charola de carne cruda rellena antes de tiempo el cazo que más lo necesita.
5. Tocas las cazuelitas para los extras del plato elegido: cebolla, cilantro, piña (solo con pastor), una salsa y limón.
6. Tocas al cliente: se entrega el plato que coincide con su pedido. Pago = 10 + 2 por extra + propina de 0 a 5 según la paciencia que le quedaba.

Carnes por misión: pastor y asada (1 a 8), más chorizo (9 y 10), más suadero (11 y 12). Hay arte de tripa y longaniza para zonas siguientes.

## Diversión y progreso (16 de noviembre de 2026, aprobado por el usuario con maquetas)
Comentario del usuario: "es divertido pero puede ser más divertido, como Plantas contra Zombies, que te quedes pegado".

**Racha y power-ups** (lógica en `KitchenSimulation`, datos en `PowerUps.cs`):
- Cada taco bien entregado sube la racha. 4 tacos cargan un power-up (con racha de 3 o más, rinde 1.5x). Un error, un cliente que se va o una tortilla quemada rompen la racha.
- Además, cada 18 a 30 s aparece un power-up en la barra; si no lo tocas en 7 s, se va.
- Al usarlo, tu taquero entra en una franja estilo anime con el nombre del power-up (el turno se pausa 1.7 s).

| Power-up | Efecto | Desde la misión |
| --- | --- | --- |
| Cebollitas asadas | Atiende solo al cliente con menos paciencia | 2 |
| Nopal | Congela la paciencia de todos 8 s | 4 |
| Salsa de la abuela | Propina doble 10 s | 6 |
| Trompo en llamas | Cazos llenos y tortillas calientes al instante | 8 |

**Clientes especiales** (como los distintos zombies):

| Cliente | Qué hace | Desde la misión |
| --- | --- | --- |
| Crítico | Paga triple si le das bien; con un error se va | 5 |
| Borrachito | A media espera cambia su pedido (una vez) | 7 |
| Mariachi | Pide 3 tacos iguales, espera más y deja bono de 10 | 10 |

**Tutorial:** el Tío explica lo nuevo de cada misión (`LevelConfig.Intro`) la primera vez que la juegas, aunque ya tengas avance (se guarda en `introsSeen` y `guidedSeen`). En la tarjeta de misión, "¿Cómo se juega?" repite el tutorial. En las misiones 1 y 3, la primera vez, todo se oscurece menos lo que hay que tocar, una manita lo señala y la paciencia no baja (3 tacos guiados en la 1, 2 en la 3).

**Mapas:** mundos de izquierda a derecha, una isla flotante por zona unida a la siguiente con un puente colgante; se desliza para pasar de una a otra (la Chorizera pide 30 ★ y el Mercado 60; todavía no tienen misiones) y la calle de la zona con sus 12 misiones, tarjeta de misión, taquero y botón "¡A TAQUEAR!".

**Puesto vivo (16 nov):** los efectos dibujados encima (flamas, burbujas, trompo deslizándose) se veían artificiales y se quitaron. El fondo de la escena es ahora un video del mismo dibujo animado con IA (Kling en Weave, 55 créditos aprobados por el usuario): lumbre, grasa, vapor y trompo girando, al 60 % de velocidad y con la vuelta suavizada (`art-source/TaqueriaNeon/video/`). Solo quedan dos efectos en código: vapor al rellenar un cazo y sobre las tortillas del domo.

**Cocina que se ensucia (idea del usuario, 16 nov):** mientras cocinas se van quedando en la barra manchas de salsa, grasa, cebolla, cilantro, migajas, limones exprimidos y servilletas, cerca de donde pasó cada acción y solo a veces, para que se junte poco a poco. Un taco equivocado embarra salsa. Cada misión empieza con la barra limpia (`KitchenScene.Mess`, máximo 45). **Apagado el mismo día** (`MessEnabled = false`): las manchas encimadas se veían falsas. Para retomarlo hace falta pintar las manchas con la luz y el ángulo de la barra.

**Arte nuevo:** moneda propia de Taquería Neón (dorada con un taco y aro de neón), chiles y letreros dibujados, título de neón (`titulo.py`), grasa dorada (antes muy roja) y más detalles en el puesto; letra Poppins (OFL).

## Campaña · Zona 1, puesto callejero (T2)
Son 12 misiones. Cada una se abre con 1 estrella en la anterior y agrega una sola cosa nueva:

| # | Misión | Lo nuevo | Estrellas (monedas) |
| --- | --- | --- | --- |
| 1 | Primer turno | 1 cliente, 1 extra, pistas | 170 / 355 / 520 |
| 2 | Llegan más | 2 clientes | 155 / 320 / 380 |
| 3 | Al comal | Tortillas en el comal, 3 clientes | 90 / 185 / 285 |
| 4 | Hora pico | Clientes cada 1–2 s (sin comal) | 145 / 320 / 535 |
| 5 | Cero errores | Regla: un error y no hay estrellas | 95 / 200 / 295 |
| 6 | Cliente apurado | 50 % con prisa: 60 % de paciencia, propina doble | 95 / 205 / 325 |
| 7 | Cliente exigente | Pedidos de 3 a 4 extras | 85 / 185 / 290 |
| 8 | Sin quemar nada | Regla: si se quema una tortilla, no hay estrellas | 90 / 185 / 285 |
| 9 | Llega el chorizo | Tercer cazo: chorizo | 90 / 185 / 285 |
| 10 | Solo pastor | Todos piden pastor, 30 % con prisa | 90 / 195 / 295 |
| 11 | Todo junto | Llega el suadero (4 carnes), pedidos de 2 a 4 extras, prisa | 80 / 185 / 305 |
| 12 | Jefe de zona | Turno de 2 minutos con todo | 110 / 240 / 400 |

Las estrellas se calibraron con el bot de `BalanceTests`:
- tocando cada 0.5 s, la mediana saca 3 estrellas (recalibrado para los cazos el 15 de noviembre);
- tocando cada 1.5 s, no las saca;
- tocando cada 0.6 s, cualquier taquero gana cada misión.

**El bot lee el pedido al instante, así que una persona saca menos monedas que él.** Hay que ajustar las metas probando en la tablet.

## Monedas y mejoras
Las monedas de cada misión se juntan aunque no se ganen estrellas.

| Mejora | Efecto | Precio |
| --- | --- | --- |
| Clientes pacientes | +15 % de paciencia | 250 |
| Domo de fierro | Tortillas 40 % más rápido | 300 |
| Tercer plato | 3 platos en la barra | 450 |
| Cazos grandes | 3 porciones más por cazo | 600 |

Todo se guarda en la tablet (`tacos.json`, versión 2). El guardado del prototipo T1 se convierte solo.

## Taqueros
| Taquero | Se consigue | Ventaja |
| --- | --- | --- |
| El Güero | Inicial | Rellena los cazos 20 % más rápido |
| El Tío | Inicial | Los clientes tienen 20 % más paciencia |
| El Paisa | Inicial | El comal calienta 4 tortillas a la vez |
| Doña Lupe | Con estrellas | Más propina. Habilidad "Salsa de la abuela" |
| El Maestro | Con estrellas | Habilidad "Trompo en llamas" |
| La Jefa | Con estrellas | Habilidad "Doble espátula" |

## Escenarios
- **En la 1.0:** puesto callejero, chorizera y taquería de mercado.
- **Después:** trompo de pastor como jefe, tacos de canasta en bici y suadero en charola.

## Curva de dificultad y cooperativo
- Los niveles avanzados vuelven al reto completo: varios clientes, comal, pedidos largos y poca paciencia.
- **Preparar ingredientes** (picar cebolla y cilantro, partir limón, rebanar el trompo) es la mecánica del final de la campaña y la base del cooperativo, donde los jugadores se reparten las tareas.
- **Cooperativo:** de 2 a 4 jugadores en salas con código. Servidor NestJS autoritativo que aplica los mismos `Command` que la versión offline.

## Fases
- **T0, diseño:** ✅.
- **T1, núcleo:** ✅ (15 de noviembre de 2026).
- **T2, campaña de la zona 1:** ✅ 12 misiones, estrellas, monedas, mejoras y guardado.
- **T3a, arte y sonido:** ← aquí. Taqueros con 3 poses, 6 clientes con 3 caras, ingredientes, estaciones, fondo, logo, ícono, cumbia y efectos.
- **T3b:** anuncios (app nueva en AdMob), ficha de Play y lanzamiento offline.
- **T4–T5:** backend, salas y cooperativo 1.1.
