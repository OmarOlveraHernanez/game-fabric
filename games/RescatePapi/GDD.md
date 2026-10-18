# Rescate Papi · Documento de diseño (GDD)

**Versión:** 0.1 · 2026-10-18 · **Estado:** juego principal de la fábrica; prototipo con formas listo

## 1. Concepto en una frase
Un águila se lleva a **Papi**, tu chihuahua; como su dueño, corres sin parar por la selva, pirámides y el Mictlán esquivando serpientes, jaguares y águilas para rescatarlo.

**Raíz cultural:** el chihuahua desciende del *techichi*, perro de toltecas y mexicas; Xólotl, dios con forma de perro, guiaba a las almas por el Mictlán. Águila, serpiente y jaguar son símbolos mexicas. Tratamiento **respetuoso**: inspiración en códices, sin caricaturizar.

## 2. Pilares
1. **Un dedo, decisiones distintas:** tocar (salto corto), mantener (salto alto) o *no saltar*. Cada enemigo pide una respuesta diferente.
2. **Justo siempre:** toda muerte es culpa del jugador; las pruebas de diseño lo garantizan (bot experto sobrevive 300/300 partidas de 2 min).
3. **Identidad visual única:** estilo códice, nada de "dinosaurio de Google".

## 3. Controles (pantalla horizontal)
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
Cada ~1000 m aparece **el Águila Real con Papi**: vuela adelante y lanza patrones (plumas/serpientes) ~20 s. Si sobrevives, casi lo alcanzas: Papi ladra, el águila escapa, recibes un premio grande de cacao y la carrera sigue más rápida. *Después de validar la base.*

## 7. Progresión y monetización
- **Cacao** acumulado → **tienda de trajes** del dueño (guerrero águila, guerrero jaguar…). *Después del MVP.*
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

## 10. Pendientes
- Diseño visual del dueño y de Papi.
- Validación de sensación por el usuario (salto, velocidad, lectura de enemigos).
