# Rescate Papi · Documento de diseño (GDD)

**Versión:** 0.2 · 2026-10-20 · **Estado:** juego principal; F3 (arte) en curso

## 1. Concepto en una frase
Su dueño quedó atrapado en lo alto de la pirámide, y **Papi, un chihuahua valiente, corre sin parar** por la selva y las pirámides esquivando serpientes, jaguares y águilas para salvarlo.

**Raíz cultural:** el chihuahua desciende del *techichi*, perro de toltecas y mexicas; Xólotl, dios con forma de perro, guiaba a las almas por el Mictlán. Águila, serpiente y jaguar son símbolos mexicas. Tratamiento **respetuoso**: inspiración en códices, sin caricaturizar.

## 2. Pilares
1. **Un dedo, decisiones distintas:** tocar (salto corto), mantener (salto alto) o *no saltar*. Cada enemigo pide una respuesta diferente.
2. **Justo siempre:** toda muerte es culpa del jugador; las pruebas de diseño lo garantizan (bot experto sobrevive 300/300 partidas de 2 min).
3. **Identidad visual única:** estilo códice, nada de "dinosaurio de Google".

## 3. Controles (pantalla horizontal; controlas a Papi)
| Acción | Gesto |
|---|---|
| Salto corto | Tocar |
| Salto alto | Mantener presionado |
| (Editor) | Clic o barra espaciadora |

## 4. Enemigos
| Enemigo | Comportamiento | Respuesta correcta | Aparece desde |
|---|---|---|---|
| Serpiente | Baja, en el suelo | Salto corto (o alto) | Inicio |
| Jaguar | Alto; desde nivel 0.3 puede **embestir** (acelera al acercarse, se pone rojo) | Salto alto con buen timing | Inicio |
| Águila | Vuela a media altura; pasa sobre ti si no saltas | **No saltar** | Nivel 0.15 (~10 s) |

*Nivel* = 0 a velocidad base, 1 a velocidad máxima (~67 s).

## 5. Premios
| Premio | Efecto | MVP |
|---|---|---|
| Grano de cacao | Moneda (la real de los mexicas). Filas en el suelo y arcos sobre jaguares | ✅ |
| Pollito | Escudo: aguanta un golpe + 1 s de invulnerabilidad | ✅ |
| Pez | Imán de cacao por unos segundos | Después |
| Power | Invencibilidad corta | Después |

## 6. Jefe (de vez en cuando)
Cada ~1000 m Papi llega al pie de una pirámide donde **el Águila Real** custodia al dueño: lanza patrones (plumas/serpientes) ~20 s. Si sobrevives, Papi ladra, el águila huye a la siguiente pirámide con el dueño, recibes un premio grande de cacao y la carrera sigue más rápida. *Después de validar la base.*

## 7. Progresión y monetización
- **Cacao** acumulado → **tienda de trajes para Papi** (guerrero águila, guerrero jaguar, collares…). *Después del MVP.*
- Anuncios: **rewarded** (continuar tras morir / duplicar cacao) + **interstitial** cada N muertes con tiempo mínimo. Sin banner en partida.
- Público **13+**.

## 8. Estilo visual y audio
- **Estilo códice** (ref. Códice Borgia): colores planos, contorno negro grueso, perfiles laterales.
- Paleta: turquesa, jade, ocre, rojo grana, negro, crema (papel amate).
- Escenarios por distancia: selva → pirámide → Mictlán.
- Audio: percusión (huéhuetl, teponaztli), flautas y sonajas.

## 9. Balance verificado
- Hueco mínimo entre enemigos > 1.25 × tiempo en el aire del salto alto.
- El tap libra la serpiente más larga incluso a velocidad base; el jaguar más alto es librable aun embistiendo.
- De pie pasas bajo el águila; cualquier salto la choca.
- Bot con buena técnica: 300/300 partidas de 2 min.

## 10. Historial
- 2026-10-20: cambio de historia — ahora **Papi corre** para salvar a su dueño atrapado en la pirámide (antes el dueño rescataba a Papi).

## 11. Pendientes
- Sprites finales de Papi y del dueño atrapado (los dibuja el usuario; ver `ARTE.md`).
- Validación de sensación por el usuario (salto, velocidad, lectura de enemigos).
