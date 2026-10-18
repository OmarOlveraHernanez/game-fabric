# Plantilla android-2d

Base para juegos nuevos de la fábrica: Unity 6.3 LTS, URP 2D, Input System, Android, referencia a Game Factory Core.

**No se edita como proyecto de Unity.** Se usa con:

```powershell
powershell -ExecutionPolicy Bypass -File tools\new-game.ps1 -Name MiJuego -Title "Mi Juego" -Orientation landscape
```

Tokens que reemplaza el script: `__GAME__` (identificador, p. ej. `MiJuego`), `__TITLE__`, `__APPID__`, `__COMPANY__`, `__ORIENTATION__`, `__PRODUCTGUID__`.
Al mejorar la configuración base de Unity (ProjectSettings, URP), actualizar también esta plantilla.
