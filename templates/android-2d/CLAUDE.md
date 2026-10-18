# __TITLE__

Juego creado con `tools/new-game.ps1` desde `templates/android-2d`. Describir aquí la mecánica en 2-3 líneas y mantener el diseño completo en `GDD.md`.

## Arquitectura del juego
- `Assets/_Game/Scripts/Logic/` (asmdef `__GAME__.Logic`, **sin referencias a UnityEngine**): toda la simulación, probada en EditMode.
- `Assets/_Game/Scripts/Runtime/` (asmdef `__GAME__`): conecta la lógica con Unity (escena, input, dibujo). Referencia a `GameFactory.Core`.
- `Assets/_Game/Scripts/Tests/EditMode/`: pruebas de lógica y de diseño.

## Convenciones
- Identidad en `factory.json` (appId **provisional hasta publicar**, versión, orientación `__ORIENTATION__`).
- Input System (nuevo). Render: URP 2D. 60 fps.
- Comandos: `tools\test.ps1 -Game __GAME__`, `tools\build.ps1 -Game __GAME__`, `tools\install-apk.ps1 -Game __GAME__`.

## Estado
- Recién creado desde la plantilla.
