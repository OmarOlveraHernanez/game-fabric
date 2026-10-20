# Changelog

## 0.2.0 - 2026-10-18
- **Save**: `SaveFile<T>` (JSON en persistentDataPath, escritura segura con .tmp, recuperación ante corrupción).
- **Audio**: `AudioService` (pool de efectos, música, volumen/silencio guardados) y `ToneSynth` (sonidos provisionales generados por código).
- **Controls**: `TapHoldInput` (tocar/mantener) y `PointerFollowInput` (seguir el dedo), extraídos de Rescate Papi y Chihuahua Jump.
- Dependencia declarada: `com.unity.inputsystem`.

## 0.1.1 - 2026-10-17
- `factory.json` acepta `orientation` (`landscape` por defecto o `portrait`).
- Build solo ARM64 (Unity 6.3 ya no admite x86_64 en Android).

## 0.1.0 - 2026-10-17
- Esqueleto del paquete: asmdefs runtime/editor/tests.
- `FactoryInfo` (versión del Core).
- `FactoryBuild`: aplica `factory.json` y genera APK de desarrollo / AAB de lanzamiento por línea de comandos.
