# Game Factory Core

Paquete compartido por todos los juegos. Reglas:
- **Nunca** referenciar asmdefs, tipos o assets de `games/`.
- Solo entra código que ya funciona en un juego y es genérico (regla de extracción).
- Cada módulo nuevo: carpeta en `Runtime/<Modulo>/`, interfaz pública mínima, pruebas en `Tests/EditMode/`.
- Subir `version` en `package.json` cuando cambie la API pública y anotarlo en `CHANGELOG.md`.

Asmdefs: `GameFactory.Core` (runtime), `GameFactory.Core.Editor` (solo editor), `GameFactory.Core.Tests.EditMode`.

## Módulos (0.3.0)
| Namespace | Contenido | Usado por |
|---|---|---|
| `GameFactory.Core` | `FactoryInfo` | todos |
| `GameFactory.Core.Save` | `SaveFile<T>` | Rescate Papi, Chihuahua Jump, AudioService |
| `GameFactory.Core.Audio` | `AudioService`, `ToneSynth` | Rescate Papi, Chihuahua Jump |
| `GameFactory.Core.Controls` | `TapHoldInput`, `PointerFollowInput` | Rescate Papi / Chihuahua Jump |
| `GameFactory.Core.Editor` | `FactoryBuild` (factory.json → PlayerSettings, APK/AAB), `ArtImportPostprocessor` (PNG en `_Game/Art` → Sprite 256 PPU, pivote abajo-centro) | tools/build.ps1, todos los juegos |

No usar el namespace `Input` (choca con `UnityEngine.Input`); por eso es `Controls`.
