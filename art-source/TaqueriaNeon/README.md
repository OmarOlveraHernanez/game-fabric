# Taquería Neón · fuentes de arte y sonido

- `ia/`: hojas originales generadas con IA en Figma (archivo KqK4C4vZ3KEKRLtesTdnVz). Fondo verde #00FF00 o magenta #FF00FF.
- `procesar.py`: quita el fondo, separa cada figura y escribe los sprites PNG.
  `python3 procesar.py ia ../../games/Tacos/Assets/_Game/Art/Resources/Sprites`
- `audio/synth_tacos.py`: compone la música (cumbia, 100 BPM) y los efectos por código. 100 % propios, sin licencias.
  `python3 audio/synth_tacos.py ../../games/Tacos/Assets/_Game/Audio/Resources/Audio`
- El juego carga todo con `Resources.Load`; si falta un archivo usa cuadros de color y tonos del Core.
- Ícono: `games/Tacos/Assets/_Game/Icons/icono_1024.png` (512 para Play Console).
