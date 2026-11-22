# Antojitos Neón · Publicar en Google Play (paso a paso)

Identificador: `com.ollinixgames.antojitosneon` (permanente una vez publicado) · Versión 0.1.0 (versionCode 1) en `factory.json`; subir a 1.0.0 antes del lanzamiento.

## A. En la app (antes de subir)
- [x] Nombre e identificador en `factory.json` (21 de noviembre de 2026)
- [x] Ícono, imagen destacada y textos de la ficha (esta carpeta)
- [ ] **Ícono en Unity:** Edit → Project Settings → Player → pestaña Android → Icon.
      - *Adaptive*: en cada tamaño, Background = `icono_fondo_432`, Foreground = `icono_frente_432` (en `Assets/_Game/Icons`).
      - *Round* y *Legacy*: `icono_512` en cada tamaño (ya está puesto; el dibujo nuevo lo reemplazó con el mismo nombre).
- [ ] **Anuncios (AdMob):** crear la app "Antojitos Neón" en AdMob (Android, todavía no publicada) y los bloques que decidamos (bonificado para duplicar monedas, bonificado para power-up, intersticial entre misiones). Pasar a Claude los ID. Agregar el paquete de anuncios necesita tu aprobación.
- [ ] **Modo prueba apagado:** `TacosGame.TodoAbierto = false` (en `Runtime/TacosGame.cs`).
- [ ] **Versión 1.0.0** en `factory.json`.
- [ ] **Prueba final** en la tablet de principio a fin (6 mundos, tienda, anuncios).
- [ ] **Capturas** (4 a 8, horizontales): Taquería con clientes, Garnachas volteando masas, Elotes, Pozolería, el mapa de mundos y un jefe final. Tomarlas en la tablet con el APK nuevo y guardarlas en `capturas/`.
- [ ] **Política de privacidad:** publicar `privacidad.html` en Google Sites (como la de Rescate Papi) y pegar la URL en `ficha_tienda.md`.
- [ ] **AAB de lanzamiento** (la firma es la misma llave de Ollinix; nunca se sube al repo): `powershell -ExecutionPolicy Bypass -File tools\build.ps1 -Game Tacos -Mode Release` → `builds\AntojitosNen\AntojitosNen-1.0.0-release.aab`

## B. En Play Console
1. **Crear app:** nombre "Antojitos Neón", idioma Español (Latinoamérica), Juego, Gratis.
2. **Contenido de la app:** política de privacidad, acceso sin restricciones, anuncios: Sí, cuestionario IARC, público objetivo y seguridad de los datos (respuestas en `ficha_tienda.md`).
3. **Ficha de Play Store:** textos de `ficha_tienda.md`, `icono_512.png`, `destacada_1024x500.png` y capturas. Categoría Juegos → Casual.
4. **Prueba cerrada** con 12 testers o más durante 14 días seguidos (igual que Rescate Papi).
5. **Producción** al cumplir los 14 días.

## C. Después de publicar
- En AdMob, enlazar la app con Google Play.
- Cada actualización: subir `versionCode` en `factory.json` (+1), compilar Release y subir.
