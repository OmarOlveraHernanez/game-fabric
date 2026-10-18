# Chihuahua Jump

Juego vertical de trampolín con vibra prehispánica: el chihuahua rebota solo y **el jugador mueve al dueño con un trampolín** (el trampolín sigue el dedo). Dónde cae en el trampolín decide hacia dónde rebota. Serpientes, jaguares y águilas caen desde arriba (con aviso ⚠). El perro atrapa cacao (moneda) y pollitos (escudo). Pierdes si cae al fondo o lo toca un enemigo. **Diseño completo en `GDD.md`.**

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `ChihuahuaJump.Logic`, **sin referencias a UnityEngine**): toda la simulación.
  - `GameConfig` → todos los números de "sensación" (rebote, trampolín, enemigos, premios). Ajustar aquí.
  - `Entities` (Dog, Trampoline, Hazard, Pickup), `SpawnGenerator` (enemigos con aviso anticipado, premios), `Difficulty`, `BounceSimulation` (estados, rebote con zona central, colisiones, escudo, puntos, cacao).
- `Assets/_Game/Scripts/Runtime/` (asmdef `ChihuahuaJump`): `GameBootstrap` arma la escena vertical por código, `GameView` dibuja (con avisos ⚠) y lee input, `InputReader` (sigue dedo/ratón; A-D en editor).
- `Assets/_Game/Scripts/Tests/EditMode/`: física, simulación y **pruebas de diseño** con un `Bot` experto (predice el aterrizaje y elige el ángulo para esquivar). Si un cambio en `GameConfig` rompe estas pruebas, el juego dejó de ser justo.

## Convenciones
- Identidad en `factory.json` (appId **provisional hasta F6**, versión, orientación `portrait`).
- Input System (nuevo). Render: URP 2D. 60 fps fijados en `GameBootstrap`.
- En el editor: Game view vertical (p. ej. 1080x2400) o Device Simulator.
- Controles en editor: arrastrar con el ratón, o A/D / flechas; espacio para empezar.

## Estado
- F1 ✅ (2026-10-18): prototipo de trampolín v0.4 validado por el usuario ("se me hizo más fácil", le gusta la idea). Bot experto: 98% supera 45 s, mediana ~105 s.
- Siguiente: F2 · extraer al Core (Save, Audio, Input) con récord y cacao persistentes.
