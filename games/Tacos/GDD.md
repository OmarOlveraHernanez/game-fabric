# Tacos · GDD (nombre provisional)

**Estado:** T1, prototipo del núcleo en Unity (noviembre de 2026).

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

## Turnos del prototipo
Cada turno agrega una mecánica:

| Turno | Clientes | Paciencia | Extras | Comal | Pistas | Estrellas (monedas) |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 1 | 60 s | 1 | No | Sí | 60 / 140 / 200 |
| 2 | 2 | 45 s | hasta 2 | No | No | 90 / 150 / 200 |
| 3 | 3 | 40 s | hasta 3 | Sí | No | 70 / 140 / 190 |

Las estrellas se calibraron con un bot que sigue las pistas:
- con 1.5 s por toque no saca 3 estrellas;
- con 0.5 s por toque sí las saca;
- con 1 s por toque gana el turno 1.

Hay que ajustarlas probando con personas.

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
- **T1, núcleo:** ← aquí. Criterio de aceptación: varias personas juegan 3 minutos y piden otra.
- **T2:** campaña de la zona 1 con 12 misiones, estrellas, monedas, mejoras y guardado.
- **T3:** arte final, sonido, menús, anuncios y lanzamiento offline.
- **T4–T5:** backend, salas y cooperativo 1.1.
