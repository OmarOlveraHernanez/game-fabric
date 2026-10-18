# Game Factory · Arquitectura (aprobada 2026-10-17)

Fábrica de videojuegos 2D **solo Android** con Unity + C#, operada por una persona con apoyo de Claude Code. Primer juego: **Chihuahua Jump**.

## 1. Principio rector
**El Core se extrae, no se diseña por adelantado.** Primero se construye el juego; el código pasa al Core solo cuando ya funciona y es claramente genérico. La fábrica se valida con el **juego 2**: debe quedar jugable en menos de 1 día sin tocar el Core.

## 2. Repositorio
Monorepo único. El Core es un paquete UPM local y cada juego lo referencia desde `Packages/manifest.json`:
`"com.gamefactory.core": "file:../../../packages/com.gamefactory.core"`.
Solo se separaría en otro repo (con etiquetas Git) si un juego publicado necesita un Core congelado.

## 3. Estructura
```
game-fabric/
├─ CLAUDE.md                 reglas globales para Claude Code
├─ .claude/settings.json     git push y publicación DENEGADOS
├─ .claude/commands/         /test, /build, /new-game (F7)
├─ packages/com.gamefactory.core/
│  ├─ Runtime/  Bootstrap, Save, Audio, Input, UI, Ads, Pooling, Util
│  ├─ Editor/   BuildPipeline (APK/AAB por línea de comandos)
│  └─ Tests/    EditMode, PlayMode
├─ games/ChihuahuaJump/      proyecto Unity (Assets/_Game/...)
├─ templates/                vacío hasta F7
├─ tools/                    check-env.ps1, test.ps1, build.ps1
├─ art-source/               PSD/Aseprite + prompts de IA
├─ builds/                   (ignorado por Git)
└─ docs/                     ARCHITECTURE, SETUP, RELEASE_CHECKLIST, decisions/
```
La separación se impone con Assembly Definitions: `GameFactory.Core` no puede referenciar a ningún juego.

## 4. Módulos del Core (MVP)
| Módulo | Decisión |
|---|---|
| Bootstrap | Service locator mínimo, sin frameworks de inyección de dependencias |
| Save | JSON en `persistentDataPath`, escritura atómica, campo `version` |
| Audio | Pool de AudioSources + AudioMixer, volúmenes guardados |
| Input | Nuevo Input System (tap, mantener, deslizar) detrás de una interfaz |
| UI | ScreenManager con uGUI (Menú/HUD/Pausa/GameOver) + safe area |
| Ads | `IAdsService` + AdMob + Fake; consentimiento UMP; límite de frecuencia |
| Pooling | Envoltura fina de `UnityEngine.Pool` |
| Build | Script de editor en modo batch: APK de desarrollo / AAB de lanzamiento |

Fuera del MVP: localización, analítica, compras integradas, configuración remota, nube, backend.
Regla: la lógica se escribe en C# puro (probable en EditMode) y los MonoBehaviour solo la conectan con Unity.

## 5. Chihuahua Jump
PlayerController (salto variable), ObstacleSpawner, CoinSpawner, DifficultyCurve (ScriptableObject), ScoreSystem, Parallax, GameStateMachine.
Monetización: rewarded (continuar / duplicar monedas) + interstitial cada N muertes con tiempo mínimo. Sin banner en partida. Público 13+.

## 6. Dispositivos de prueba
- **Device Simulator de Unity** (Window → General → Device Simulator): uso diario, tamaños de pantalla y safe area.
- **Emulador Android** (`tools/setup-emulator.ps1`, AVD "GameFactory", Android 15 x86_64, en D:): instalar y probar APKs en F0–F3. Los builds son solo ARM64 (Unity 6.3 ya no compila x86_64 para Android); el emulador los traduce. Si alguna vez no corre ahí, se prueba en el teléfono.
- **Teléfono real**: obligatorio desde F4 y antes de cualquier publicación (rendimiento, tacto, anuncios).

## 6b. Flujo con Claude Code (validación manual obligatoria)
1. Claude trabaja en `feature/...`, implementa y corre `/test`.
2. Claude genera un APK de desarrollo.
3. **Tú** pruebas en el editor y en el emulador (teléfono real desde F4) con `RELEASE_CHECKLIST.md`.
4. **Tú** haces merge y push (bloqueado para Claude; un hook pre-push corre las pruebas y pide confirmación).

## 7. No hacer todavía (sobreingeniería)
Frameworks de inyección de dependencias, ECS/DOTS, Addressables, bus de eventos genérico, varios repos, CI en la nube, backend, analítica, mediación de anuncios, herramientas de editor sin un problema repetido, generador de plantillas antes de terminar el juego 1.

## 8. Riesgos
Prueba cerrada de Play (≈12 testers × 14 días, cuentas personales nuevas) · política de familias · consentimiento UMP · API objetivo, AAB y páginas de 16 KB (fijar Unity 6 LTS) · keystore fuera del repo y con respaldo + Play App Signing · Unity en modo batch falla con el editor abierto · licencia comercial del arte IA · ingresos modestos sin adquisición de usuarios.

## 9. Roadmap
| Fase | Contenido | Listo cuando |
|---|---|---|
| **F-1 Entorno** ✅ | Instalar Git, Claude Code, Unity Hub + Unity 6 LTS con Android, VS Code, emulador Android (tools/setup-emulator.ps1); abrir cuentas GitHub y Play Console (ver `SETUP.md`) | `tools/check-env.ps1` sin `[FALTA]` |
| F0 Fundaciones ✅ | Repo Git, CLAUDE.md, permisos, proyecto Unity vacío | APK "hola mundo" corre en el emulador |
| F1 Prototipo | Correr, saltar, obstáculos, morir, reiniciar (con cubos) | Divertido 2 minutos |
| F2 Extracción | Save/Audio/Input/UI/Pooling al Core con pruebas | El juego funciona igual; el Core no depende del juego |
| F3 Contenido | Monedas, dificultad, sprites, sonido, menús | Partida completa |
| F4 Anuncios | AdMob en IDs de prueba, consentimiento, frecuencia. **Desde aquí el teléfono real es obligatorio** | Rewarded e interstitial funcionan en teléfono real |
| F5 Automatización | test.ps1, build.ps1, comandos de Claude, hook pre-push, checklist | Claude prueba y genera el AAB solo |
| F6 Publicación | Prueba interna → cerrada (14 días) → producción | En producción |
| F7 Validar fábrica | Plantilla + `/new-game`, prototipo del juego 2 | Juego 2 jugable en menos de 1 día |
