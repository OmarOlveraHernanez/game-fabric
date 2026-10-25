# Rescate Papi · JUEGO PRINCIPAL

Runner horizontal con vibra prehispánica: el dueño está atrapado en la pirámide y **Papi (el chihuahua) corre** para salvarlo. El jugador controla a Papi. Esquiva serpientes (salto corto), jaguares (salto alto; pueden embestir) y águilas (no saltar). Recoge cacao (moneda) y pollitos (escudo). **Diseño completo en `GDD.md`. Guía de arte en `ARTE.md`.**

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `RescatePapi.Logic`, **sin referencias a UnityEngine**): toda la simulación.
  - `RunnerConfig` → todos los números de "sensación". Ajustar aquí.
  - `JumpMotor` (salto variable + buffer), `Difficulty`, `Entities` (Hazard/Pickup), `SpawnGenerator`, `RunnerSimulation` (estados, colisiones AABB, escudo, puntos, cacao).
- `Assets/_Game/Scripts/Runtime/` (asmdef `RescatePapi`): `GameBootstrap` arma la escena (fondos con parallax, suelo), `RunnerView` dibuja (sprites con respaldo a formas de color), `SpriteSet` (carga `Resources/Sprites` por nombre), `ScrollingStrip`, `GameData` (guardado), `GameSounds`.
- Arte: `Assets/_Game/Art/Resources/Sprites/*.png` — el nombre del archivo decide dónde aparece (ver `ARTE.md`). Si falta, se dibuja la forma de color.
- `Assets/_Game/Scripts/Tests/EditMode/`: lógica + **pruebas de diseño** (incluye bot que sobrevive 2 min). Si un cambio en `RunnerConfig` las rompe, el juego dejó de ser justo.

## Convenciones
- Identidad en `factory.json` (appId **provisional hasta publicar**, orientación `landscape`).
- Input System (nuevo). Render: URP 2D. 60 fps.
- Editor: Game view horizontal (1920x1080). Controles: clic o espacio.
- Comandos: `tools\test.ps1 -Game RescatePapi`, `tools\build.ps1 -Game RescatePapi`, `tools\install-apk.ps1 -Game RescatePapi`.

## Estado
- Creado con `tools/new-game.ps1` (primera prueba real de la plantilla).
- F2 ✅: récord/cacao guardados, sonidos provisionales.
- F3 en curso: guía de arte, importador automático, set provisional estilo códice; el usuario dibujará los sprites finales.
