# Rescate Papi · JUEGO PRINCIPAL

Runner horizontal con vibra prehispánica: un águila se llevó a **Papi** (el chihuahua) y **el dueño** corre para rescatarlo. Esquiva serpientes (salto corto), jaguares (salto alto; pueden embestir) y águilas (no saltar). Recoge cacao (moneda) y pollitos (escudo). **Diseño completo en `GDD.md`.**

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `RescatePapi.Logic`, **sin referencias a UnityEngine**): toda la simulación.
  - `RunnerConfig` → todos los números de "sensación". Ajustar aquí.
  - `JumpMotor` (salto variable + buffer), `Difficulty`, `Entities` (Hazard/Pickup), `SpawnGenerator`, `RunnerSimulation` (estados, colisiones AABB, escudo, puntos, cacao).
- `Assets/_Game/Scripts/Runtime/` (asmdef `RescatePapi`): `GameBootstrap` arma la escena por código, `RunnerView` dibuja y lee input, `InputReader` (tocar/mantener).
- `Assets/_Game/Scripts/Tests/EditMode/`: lógica + **pruebas de diseño** (incluye bot que sobrevive 2 min). Si un cambio en `RunnerConfig` las rompe, el juego dejó de ser justo.

## Convenciones
- Identidad en `factory.json` (appId **provisional hasta publicar**, orientación `landscape`).
- Input System (nuevo). Render: URP 2D. 60 fps.
- Editor: Game view horizontal (1920x1080). Controles: clic o espacio.
- Comandos: `tools\test.ps1 -Game RescatePapi`, `tools\build.ps1 -Game RescatePapi`, `tools\install-apk.ps1 -Game RescatePapi`.

## Estado
- Creado con `tools/new-game.ps1` (primera prueba real de la plantilla).
- Prototipo con formas: lógica portada del runner (22 pruebas OK). Pendiente validación del usuario.
