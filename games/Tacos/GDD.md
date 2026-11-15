# Tacos · GDD (nombre provisional)

**Estado:** T2, campaña de la zona 1 (noviembre de 2026). T1 aprobado en la tablet el 15 de noviembre.

**Material de referencia:**
- Bocetos en Figma: https://www.figma.com/design/KqK4C4vZ3KEKRLtesTdnVz
- Maqueta HTML: https://claude.ai/artifact/DX7TDmyMxt1BauAYnZppFU

## Concepto
Juego de cocina en una taquería callejera de la Ciudad de México. Llegan clientes con su pedido; pones la carne en la plancha, armas el taco con sus extras y lo entregas antes de que se canse el cliente. Partidas de 90 segundos.

## Estilo (aprobado)
Anime indie: sombreado cel, sombras definidas, trazo con grosor variable, neón y fondos pintados de la ciudad de noche. Debe llamar la atención y no verse tipo Disney. La paleta del prototipo ya va en esa línea (noche, chile, maíz, limón, turquesa).

## Bucle de un pedido
1. Llega un cliente con su pedido y su barra de paciencia.
2. (Turno 3 en adelante) Pones una tortilla en el comal y la pasas al plato cuando está caliente.
3. Pones la carne (pastor o asada) en la plancha. Cuando está lista, pasa sola al plato libre. Si la dejas de más, se quema.
4. Agregas los extras que pide al plato elegido: cebolla, cilantro, piña (solo con pastor), una salsa y limón.
5. Tocas al cliente: se entrega el plato que coincide con su pedido. Pago = 10 + 2 por extra + propina de 0 a 5 según la paciencia que le quedaba.

## Campaña · Zona 1, puesto callejero (T2)
Son 12 misiones. Cada una se abre con 1 estrella en la anterior y agrega una sola cosa nueva:

| # | Misión | Lo nuevo | Estrellas (monedas) |
| --- | --- | --- | --- |
| 1 | Primer turno | 1 cliente, 1 extra, pistas | 60 / 140 / 200 |
| 2 | Llegan más | 2 clientes | 90 / 150 / 200 |
| 3 | Al comal | Tortillas en el comal, 3 clientes | 70 / 140 / 190 |
| 4 | Hora pico | Clientes cada 1–2 s (sin comal) | 120 / 180 / 220 |
| 5 | Cero errores | Regla: un error y no hay estrellas | 110 / 180 / 225 |
| 6 | Cliente apurado | 50 % con prisa: 60 % de paciencia, propina doble | 110 / 190 / 245 |
| 7 | Cliente exigente | Pedidos de 3 a 4 extras | 100 / 180 / 240 |
| 8 | Sin quemar nada | Regla: si algo se quema, no hay estrellas | 100 / 170 / 225 |
| 9 | Meta alta | Metas de monedas más altas | 120 / 180 / 230 |
| 10 | Solo pastor | Todos piden pastor, 30 % con prisa | 110 / 180 / 235 |
| 11 | Todo junto | Comal, pedidos de 2 a 4 extras, prisa | 100 / 170 / 240 |
| 12 | Jefe de zona | Turno de 2 minutos con todo | 130 / 230 / 310 |

Las estrellas se calibraron con el bot de `BalanceTests`:
- tocando cada 0.5 s, la mediana saca 3 estrellas;
- tocando cada 1.5 s, no las saca;
- tocando cada 0.6 s, cualquier taquero gana cada misión.

**El bot lee el pedido al instante, así que una persona saca menos monedas que él.** Hay que ajustar las metas probando en la tablet.

## Monedas y mejoras
Las monedas de cada misión se juntan aunque no se ganen estrellas.

| Mejora | Efecto | Precio |
| --- | --- | --- |
| Clientes pacientes | +15 % de paciencia | 250 |
| Comal de fierro | Tortillas 40 % más rápido | 300 |
| Tercer plato | 3 platos en la barra | 450 |
| Plancha grande | 3 carnes a la vez | 600 |

Todo se guarda en la tablet (`tacos.json`, versión 2). El guardado del prototipo T1 se convierte solo.

## Taqueros
| Taquero | Se consigue | Ventaja |
| --- | --- | --- |
| El Güero | Inicial | La carne se cocina 20 % más rápido |
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
- **T2, campaña de la zona 1:** ← aquí. 12 misiones, estrellas, monedas, mejoras y guardado.
- **T3:** arte final, sonido, menús, anuncios y lanzamiento offline.
- **T4–T5:** backend, salas y cooperativo 1.1.
