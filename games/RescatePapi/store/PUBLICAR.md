# Rescate Papi · Publicar en Google Play (paso a paso)

Identificador: `com.ollinixgames.rescatepapi` (permanente) · Versión 1.0.0 (versionCode 1) · Target API 36 ✅

## A. En la app (antes de subir)
- [x] Identificador, empresa (Ollinix Games) y versión 1.0.0 en `factory.json`
- [ ] **AdMob (cuenta nueva Ollinix):** crear app "Rescate Papi" (Android, todavía no publicada) y 3 bloques: Bonificado `continuar`, Bonificado `monedas_x2`, Intersticial `entre_partidas`. Pasar a Claude el ID de app (`~`) y los 3 ID (`/`).
- [ ] **Ícono en Unity:** Edit → Project Settings → Player → pestaña Android → Icon.
      - *Adaptive*: en cada tamaño, Background = `icono_fondo_432`, Foreground = `icono_frente_432` (están en `Assets/_Game/Icons`).
      - *Round* y *Legacy*: `icono_512` en cada tamaño.
- [ ] **Commit** de la rama `feature/f4-anuncios`, push solo, PR y merge.
- [ ] **Llave de firma** (`D:\llaves\ollinix-upload.keystore`, alias `rescatepapi`) con respaldo.
- [ ] **Variables de firma** en Windows (una vez): `GF_KEYSTORE_PATH`, `GF_KEYSTORE_PASS`, `GF_KEY_ALIAS`, `GF_KEY_PASS`.
- [ ] **AAB de lanzamiento:** `powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Mode Release` → `builds\RescatePapi\RescatePapi-1.0.0-release.aab`
- [ ] **Capturas** (4 a 8, horizontales) desde un APK de prueba nuevo; con 3 dedos se esconde la línea de diagnóstico.
- [ ] **Política de privacidad** publicada en Google Sites (copiar `privacidad.html`).

## B. En Play Console
1. **Crear app:** nombre "Rescate Papi", idioma Español (Latinoamérica), Juego, Gratis, aceptar declaraciones.
2. **Contenido de la app** (Panel → "Configura tu app"):
   - Política de privacidad: URL de Google Sites
   - Acceso a la app: todo disponible sin restricciones
   - Anuncios: **Sí, contiene anuncios**
   - Clasificación de contenido: cuestionario IARC (juego, violencia caricaturesca leve, sin sangre, sin interacción entre usuarios, sin compras)
   - Público objetivo: **13-15, 16-17 y 18+** (no niños); no atrae principalmente a niños
   - Seguridad de los datos: ver `ficha_tienda.md` (SDK de AdMob: ID de publicidad, info del dispositivo, interacciones; cifrado en tránsito; no se pueden borrar porque no hay cuenta)
   - Apps gubernamentales / financieras / salud: No
3. **Ficha de Play Store:** textos de `ficha_tienda.md`, `icono_512.png`, `destacada_1024x500.png` y capturas. Categoría Juegos → Arcade. Correo: ollinixgames24@gmail.com.
4. **Prueba cerrada** (Pruebas → Prueba cerrada → Alpha):
   - Países: México (y Latinoamérica si quieres)
   - Testers: lista de correos (12 o más) → crear versión → subir el `.aab` → notas "Primera versión de prueba" → revisar y lanzar
   - Mandar a los testers el **enlace de participación**: deben aceptarlo, instalar desde Play y no salirse **14 días seguidos**
5. **Solicitar acceso a producción** (Panel, al cumplir 14 días): cuestionario sobre la prueba
6. **Producción:** crear versión con el mismo `.aab` (o uno nuevo con versionCode mayor) → países → lanzar. Revisión de Google: de horas a unos días.

## C. Después de publicar
- AdMob → Apps → Rescate Papi → **Agregar tienda (enlazar con Google Play)**; la revisión tarda unos días y antes de eso los anuncios salen limitados.
- `app-ads.txt` (recomendado para más ingresos): necesita un sitio web propio; lo vemos después.
- Cada actualización: subir `versionCode` en `factory.json` (+1), compilar Release y subir.
