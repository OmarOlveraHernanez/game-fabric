# Game Factory · Instrucciones para Claude Code

Fábrica de videojuegos 2D **solo Android** (Unity + C#). Arquitectura completa en `docs/ARCHITECTURE.md`.
Idioma: documentación y mensajes en **español**; identificadores de código en **inglés**.

## Estructura
- `packages/com.gamefactory.core/` → **Game Factory Core** (paquete UPM local, compartido).
- `games/<Juego>/` → un proyecto Unity por juego; referencia el Core con `file:../../../packages/com.gamefactory.core`.
- `games/<Juego>/factory.json` → identidad del juego (appId, nombre, versión). El build lo aplica a PlayerSettings.
- `tools/` → scripts PowerShell: `build.ps1`, `install-apk.ps1`, `test.ps1`, `check-env.ps1`, `setup-emulator.ps1`.
- `docs/` → arquitectura, setup, checklist de lanzamiento, decisiones (`docs/decisions/`).

## Reglas obligatorias
1. **El Core nunca depende de un juego.** Nada en `packages/` puede referenciar asmdefs o tipos de `games/`.
2. **El código entra al Core solo si ya funciona en un juego y es genérico.** No diseñar sistemas "por si acaso".
3. **Nunca hacer `git push`, publicar, ni tocar keystores.** Eso lo hace solo el usuario tras validar manualmente.
4. Trabajar en ramas `feature/<tema>`; se permiten commits locales en esas ramas, **nunca en `main`**.
5. **No agregar paquetes, SDKs ni servicios cloud** sin aprobación explícita del usuario.
6. Lógica de juego en **C# puro** (sin MonoBehaviour) siempre que sea posible, con pruebas EditMode. Los MonoBehaviour solo conectan esa lógica con Unity.
7. Versión de Unity fijada: **6000.3.26f1 (6.3 LTS)**. No actualizar sin aprobación.
8. No editar archivos `.meta` a mano. Si se crean archivos fuera de Unity, Unity genera sus `.meta` al abrir; esos `.meta` sí se versionan.
9. Antes de declarar algo terminado: ejecutar `tools/test.ps1` y, si cambió el runtime, `tools/build.ps1` + `tools/install-apk.ps1` en el emulador.

## Comandos (PowerShell, desde la raíz)
```powershell
powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Game ChihuahuaJump          # pruebas EditMode
powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Game ChihuahuaJump -Mode Dev # APK de desarrollo
powershell -ExecutionPolicy Bypass -File tools\install-apk.ps1 -Game ChihuahuaJump    # instala y abre en emulador/teléfono
powershell -ExecutionPolicy Bypass -File tools\setup-emulator.ps1 -Start              # abre el emulador
```
Los scripts en modo batch **fallan si el proyecto está abierto en el editor de Unity**: pedir al usuario que lo cierre.

## Fase actual
F0 completada (2026-10-17): APK hola mundo corre en el emulador.
Siguiente: **F1 · Prototipo jugable** de Chihuahua Jump (ver roadmap en `docs/ARCHITECTURE.md`).
