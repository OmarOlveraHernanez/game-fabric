# Tacos (nombre provisional)

Juego de cocina en una taquería callejera: llegan clientes con su pedido, pones la carne en la plancha, armas el taco con sus extras y lo entregas antes de que se canse el cliente. Estaciones fijas (se tocan, no se camina). Primero campaña offline y después cooperativo en línea. Diseño completo en `GDD.md`.

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `Tacos.Logic`, **sin referencias a UnityEngine**): toda la simulación, probada en EditMode.
  - `KitchenSimulation`: un turno completo. La vista llama `Tick(dt)` y `Apply(Command)`.
  - **Todo lo que hace el jugador es un `Command`.** El cooperativo usará los mismos comandos: el teléfono los envía y el servidor los aplica. No meter reglas en la vista.
  - `Hints`: pistas paso a paso (turno 1) y bot de las pruebas de equilibrio.
  - `Config`: tiempos (`KitchenRules`), turnos (`LevelConfig.Turnos`) y taqueros (`TaqueroDef`).
- `Assets/_Game/Scripts/Runtime/` (asmdef `Tacos`): interfaz uGUI armada por código (`TacosGame`), sin prefabs. `UI/UiKit` es una copia adaptada de la de Rescate Papi; se unifican en el Core cuando ambas estén estables.
- `Assets/_Game/Scripts/Tests/EditMode/`: reglas (`KitchenSimulationTests`) y equilibrio con bot (`BalanceTests`).

## Reglas de diseño (de la prueba de la maqueta, nov. 2026)
- Cada turno agrega **una sola** mecánica nueva. El turno 1 trae pistas.
- Las estrellas se calibran con `BalanceTests`. Un jugador lento (1 s por toque) saca al menos 1 estrella en el turno 1. Las 3 estrellas piden jugar rápido.
- Al cambiar tiempos o turnos, correr las pruebas antes de probar en la tablet.

## Convenciones
- Identidad en `factory.json`. El appId `com.ollinixgames.tacos` y el nombre son **provisionales hasta publicar**.
- Input System (nuevo). Render: URP 2D. 60 fps. Horizontal.
- Comandos: `tools\test.ps1 -Game Tacos`, `tools\build.ps1 -Game Tacos`, `tools\install-apk.ps1 -Game Tacos`.

## Estado
- **T1, prototipo del núcleo:**
  - 3 turnos jugables con cuadros de colores.
  - 3 taqueros (El Güero, El Tío, El Paisa).
  - Guardado de estrellas.
  - Sin arte, sonido ni anuncios todavía.
