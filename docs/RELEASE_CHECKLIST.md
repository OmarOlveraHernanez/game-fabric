# Checklist de validación manual

## Antes de cada merge a `main` / push
- [ ] `tools/test.ps1` pasa sin fallos.
- [ ] El APK de desarrollo se instala y abre en el emulador (desde F4: también en teléfono real).
- [ ] Jugué al menos 2 partidas completas sin errores visibles.
- [ ] Revisé el diff: no hay archivos de `Library/`, builds, keystores ni secretos.
- [ ] El Core no referencia nada de `games/`.

## Antes de subir a Google Play (adicional)
- [ ] `factory.json`: `version` y `versionCode` incrementados.
- [ ] `appId` definitivo (no se puede cambiar después de publicar).
- [ ] Probado en **teléfono real**: rendimiento, tacto, safe area, audio, pausa al salir/volver.
- [ ] Anuncios: IDs de **producción** solo en build de lanzamiento; consentimiento (UMP) aparece en región UE.
- [ ] AAB firmado con la upload key (keystore fuera del repo, con respaldo).
- [ ] Ficha de Play, clasificación de contenido, política de privacidad y Data safety actualizadas.
