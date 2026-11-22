# Antojitos Neón (antes Taquería Neón; carpeta `games/Tacos`)

Juego de cocina en una taquería callejera: llegan clientes con su pedido, pones la carne en la plancha, armas el taco con sus extras y lo entregas antes de que se canse el cliente. Estaciones fijas (se tocan, no se camina). Primero campaña offline y después cooperativo en línea. Diseño completo en `GDD.md`.

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `Tacos.Logic`, **sin referencias a UnityEngine**): toda la simulación, probada en EditMode.
  - `KitchenSimulation`: un turno completo. La vista llama `Tick(dt)` y `Apply(Command)`.
  - **Todo lo que hace el jugador es un `Command`.** El cooperativo usará los mismos comandos: el teléfono los envía y el servidor los aplica. No meter reglas en la vista.
  - `Hints`: pistas paso a paso (turno 1) y bot de las pruebas de equilibrio.
  - `Config`: tiempos (`KitchenRules`), turnos (`LevelConfig.Turnos`) y taqueros (`TaqueroDef`).
- `Assets/_Game/Scripts/Runtime/` (asmdef `Tacos`): interfaz uGUI armada por código (`TacosGame`), sin prefabs. `UI/UiKit` es una copia adaptada de la de Rescate Papi; se unifican en el Core cuando ambas estén estables.
- `Assets/_Game/Scripts/Tests/EditMode/`: reglas (`KitchenSimulationTests`), campaña y mejoras (`CampaignTests`) y equilibrio con bot (`BalanceTests`).

## Reglas de diseño (de la prueba de la maqueta, nov. 2026)
- Cada turno agrega **una sola** mecánica nueva. El turno 1 trae pistas.
- Las estrellas se calibran con `BalanceTests`. Un jugador lento (1 s por toque) saca al menos 1 estrella en el turno 1. Las 3 estrellas piden jugar rápido.
- Al cambiar tiempos o turnos, correr las pruebas antes de probar en la tablet.

## Convenciones
- Identidad en `factory.json`: **Antojitos Neón**, appId `com.ollinixgames.antojitosneon` (nombre elegido por el usuario el 21 de noviembre de 2026, antes de publicar). La carpeta, el código (`Tacos`) y `art-source/TaqueriaNeon` conservan el nombre viejo.
- Input System (nuevo). Render: URP 2D. 60 fps. Horizontal.
- Comandos: `tools\test.ps1 -Game Tacos`, `tools\build.ps1 -Game Tacos`, `tools\install-apk.ps1 -Game Tacos`.

## Estado
- **T1, núcleo:** aprobado en la tablet (15 de noviembre de 2026).
- **T2, campaña de la zona 1:**
  - 12 misiones (`LevelConfig.Zona1`) con reglas especiales (`MissionRule`).
  - Clientes con prisa.
  - Monedas acumuladas y 4 mejoras (`Campaign.cs`).
  - Mapa de misiones y tienda.
- **T3a, arte y sonido:** `TacosArt` carga `Resources/Sprites` y `TacosSounds` carga `Resources/Audio`; todo tiene respaldo si falta el archivo. Fuentes en `art-source/TaqueriaNeon/`. Para cambiar arte: edita la hoja en `ia/`, corre `procesar.py` y vuelve a copiar.
- **T3a+, diversión (16 nov):** racha y power-ups (`PowerUps.cs`, `PowerUpTests`), clientes especiales, tutorial del Tío (`TutorialOverlay`), mapas de zonas y misiones (`TacosGame.Maps`), HUD y franja anime (`TacosGame.Hud`). `UI/Stage` coloca cosas en pixeles de una imagen pintada. Todo comando del jugador sigue siendo un `Command` (también usar y agarrar power-ups).
- **Pendiente (T3b):** anuncios (app nueva en AdMob; agregar el paquete GMA requiere aprobación) y ficha de Play.

## Antes de publicar (no olvidar)
- `TacosGame.TodoAbierto` (Runtime/TacosGame.cs) en **false**. Está en true para probar con todos los mundos y misiones abiertos; el mapa muestra "MODO PRUEBA · todo abierto" mientras siga prendido.
