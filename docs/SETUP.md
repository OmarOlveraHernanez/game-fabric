# Fase F-1 · Instalación del entorno (Windows)

Objetivo: dejar tu PC listo para que Unity compile para Android y Claude Code pueda ejecutar pruebas y builds.
Al terminar, ejecuta `tools\check-env.ps1` y pega el resultado a Claude.

Espacio necesario: ~25 GB libres (Unity + módulos Android ~15 GB, proyectos y caché el resto). Se puede instalar todo en D:.

## Orden de instalación

| # | Herramienta | Para qué | Cómo |
|---|---|---|---|
| 1 | **Git for Windows** (incluye Git LFS y Git Bash) | Control de versiones; Claude Code usa Git Bash como shell | https://git-scm.com/downloads/win · opciones por defecto. Después, en una terminal: `git lfs install` y `git config --global user.name "Tu Nombre"` / `git config --global user.email "tu@correo"` |
| 2 | **Claude Code** | Desarrollo y automatización | En PowerShell (sin administrador): `irm https://claude.ai/install.ps1 \| iex` · abrir terminal nueva · `claude --version` · `claude` para iniciar sesión (requiere plan Pro/Max) |
| 3 | **Unity Hub** | Gestiona versiones de Unity y la licencia | https://unity.com/download · crear/entrar con cuenta Unity · activar licencia **Personal** (gratis hasta 200 mil USD de ingresos anuales) |
| 4 | **Unity 6 LTS** (la LTS más reciente que ofrezca el Hub) | Motor | Hub → Installs → Install Editor → pestaña *Official releases* → versión marcada **LTS**. En *Preferences → Installs* puedes cambiar la ruta a `D:\Unity` |
| 4a | Módulos de esa versión (obligatorios) | Compilar para Android | Marcar: **Android Build Support** con sus submódulos **OpenJDK** y **Android SDK & NDK Tools**. No marques iOS, WebGL, etc. |
| 5 | **Visual Studio Code** + extensión **Unity** (Microsoft) | Editar C# con autocompletado | https://code.visualstudio.com · en VS Code instalar la extensión "Unity" (instala C# Dev Kit). En Unity: *Edit → Preferences → External Tools → Visual Studio Code* |
| 6 | **Emulador Android**: `powershell -ExecutionPolicy Bypass -File tools\setup-emulator.ps1` (abrir con `-Start`) | Probar APKs sin teléfono (F0–F3) | Usa el SDK de Unity; queda en D:. Requiere "Plataforma de hipervisor de Windows" activada |
| 6b | **Teléfono Android** en modo desarrollador (desde F4) | Probar en dispositivo real (obligatorio antes de publicar) | Ajustes → Acerca del teléfono → tocar 7 veces "Número de compilación" → Opciones de desarrollador → **Depuración USB** activada. Conectar por USB y aceptar la huella del PC. Algunos fabricantes (Samsung, Xiaomi) requieren su driver USB |

Opcionales (cuando lleguemos al arte): **Aseprite** (de pago, ideal para pixel art) o **Krita** / **LibreSprite** (gratis). Sonidos: packs CC0 como Kenney.nl.

## Cuentas (en paralelo, tardan días en verificarse)

| Cuenta | Costo | Cuándo | Nota |
|---|---|---|---|
| **GitHub** | Gratis (repos privados) | Ahora | Respaldo remoto del repo |
| **Google Play Console** | 25 USD pago único | **Ahora** | La verificación de identidad tarda días. Las cuentas personales nuevas deben hacer prueba cerrada (≈12 testers × 14 días) antes de producción: conviene ir juntando testers |
| **AdMob** | Gratis | Fase F4 | Se vincula a la app ya creada en Play |

## Verificación

```powershell
cd D:\game-fabric
powershell -ExecutionPolicy Bypass -File tools\check-env.ps1
```

Todo debe salir en `[OK]`. Los `[FALTA]` indican qué paso repetir.
