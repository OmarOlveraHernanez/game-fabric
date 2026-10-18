# Chihuahua Jump · Documento de diseño (GDD)

**Versión:** 0.4 · 2026-10-18 · **Estado:** mecánica de trampolín aprobada, prototipo en validación

## 1. Concepto en una frase
Tu chihuahua rebota sin parar; tú, su dueño, lo sostienes con un trampolín mientras serpientes, jaguares y águilas caen desde el templo. Atrapa cacao en el aire y no lo dejes caer.

**Raíz cultural:** el chihuahua desciende del *techichi*, perro de toltecas y mexicas; Xólotl, dios con forma de perro, guiaba a las almas por el Mictlán. Águila, serpiente y jaguar son símbolos mexicas. Tratamiento **respetuoso**: inspiración en códices, sin caricaturizar.

## 2. Pilares
1. **Un dedo, control fino:** el trampolín sigue tu dedo; *dónde* cae el perro en el trampolín decide hacia dónde rebota.
2. **Justo siempre:** todo enemigo avisa con tiempo (⚠ arriba); toda muerte es culpa del jugador. Las pruebas de diseño lo verifican con un bot.
3. **Identidad visual única:** estilo códice.

## 3. Controles
| Acción | Gesto |
|---|---|
| Mover trampolín | Deslizar el dedo: el trampolín va hacia donde apuntas (velocidad máx. 10 u/s) |
| Empezar / reintentar | Tocar |
| (Editor) | Arrastrar con el ratón, o A/D / flechas; espacio para empezar |

**Dirección del rebote:** el 30% central del trampolín rebota recto (perdona imprecisión); hacia las orillas desvía cada vez más. El dueño puede salir un poco por las orillas para devolver al perro hacia el centro.

## 4. Enemigos (caen desde arriba; aviso ⚠ en el borde superior)
| Enemigo | Comportamiento | Aparece desde |
|---|---|---|
| Serpiente | Cae recta, angosta | Inicio |
| Jaguar | Cae más rápido y es más ancho | Nivel 0.1 (~9 s) |
| Águila | Cae en picada en diagonal y rebota en las paredes | Nivel 0.2 (~18 s) |

*Nivel* sube de 0 a 1 en 90 s: más enemigos (máx. 2 → 3 a la vez) y caen más rápido.
Pierdes si el perro **cae al fondo** o si lo **toca un enemigo** (el pollito te salva una vez).

## 5. Premios (flotan quietos ~6 s a alturas alcanzables; parpadean antes de desaparecer; los atrapa el perro con área generosa)
| Premio | Efecto | MVP |
|---|---|---|
| Grano de cacao | Moneda (la real de los mexicas); en filas de 1 a 3 | ✅ |
| Pollito | Escudo: aguanta un golpe + 1 s de invulnerabilidad | ✅ |
| Pez | Imán de cacao por unos segundos | Después |
| Power | Invencibilidad corta | Después |

## 6. Jefe (de vez en cuando)
Cada cierto número de rebotes aparece **el Águila Real**: sobrevuela arriba y lanza patrones (plumas/serpientes) ~20 s. Si sobrevives, premio grande de cacao. *Después de validar que la base es divertida.*

## 7. Progresión y monetización
- Puntuación = rebotes. Cacao acumulado → **tienda de trajes** (para el dueño y el chihuahua). *Después del MVP.*
- Anuncios: **rewarded** (continuar tras perder / duplicar cacao) + **interstitial** cada N partidas con tiempo mínimo. Sin banner en partida.
- Público **13+**.

## 8. Estilo visual y audio
- **Estilo códice** (ref. Códice Borgia): colores planos, contorno negro grueso.
- Paleta: turquesa, jade, ocre, rojo grana, negro, crema (papel amate).
- Pantalla **vertical**: muros de templo a los lados; escenarios por progreso (selva → pirámide → Mictlán).
- Audio: percusión (huéhuetl, teponaztli), flautas y sonajas; "boing" del trampolín con timbre de tambor.

## 9. Balance verificado (pruebas de diseño)
- El perro sube a ~3/4 de la pantalla; vuelo ≈ 1.7 s; el trampolín cruza el campo en < medio vuelo.
- Todo enemigo tarda > 1 vuelo completo en llegar desde que aparece su aviso.
- Primer enemigo a los 4 s. Bot experto en 100 partidas: ninguna termina antes de 39 s, 98% supera 45 s, mediana ~105 s. La dificultad sube hasta que pierdes.

## 10. Feedback de pruebas
- v0.3 (usuario): control por mitades se sentía muy rápido; el perro solo llegaba a media pantalla; costaba agarrar cacao → v0.4: seguir el dedo, rebote más alto, cacao flotante con área generosa.

## 11. Pendientes de decidir
- **Nombre definitivo** (antes de F6; el appId depende de esto).
- Diseño del dueño y del chihuahua.
