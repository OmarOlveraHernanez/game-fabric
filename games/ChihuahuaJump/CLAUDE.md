# Chihuahua Jump

Endless runner 2D: un chihuahua corre solo, salta obstáculos (tap / mantener para salto más alto), recoge monedas y suma puntos. Dificultad progresiva. Monetización con anuncios (rewarded + interstitial).

## Convenciones
- Todo lo propio del juego vive en `Assets/_Game/` (asmdef `ChihuahuaJump`, referencia a `GameFactory.Core`).
- Identidad (appId, nombre, versión) en `factory.json`. **El appId es provisional hasta F6** y no puede cambiar después de publicar.
- Escena principal: `Assets/Scenes/SampleScene.unity` (se renombrará en F1).
- Input: Input System (nuevo). Render: URP 2D.
- Orientación: horizontal (landscape) — a definir en F1.

## Estado
- F0: `HelloF0.cs` muestra una pantalla de verificación. **Borrarlo al iniciar F1.**
