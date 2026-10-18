# 0001 · Monorepo con Core como paquete UPM local

**Fecha:** 2026-10-17 · **Estado:** aceptada

**Contexto:** una sola persona con Claude Code, varios juegos 2D Android que comparten sistemas.

**Decisión:** un repositorio. El Core vive en `packages/com.gamefactory.core` y cada juego lo referencia con `file:` en su `manifest.json`. La separación se impone con Assembly Definitions.

**Consecuencias:** cambios a Core + juego en un solo commit; Claude ve todo el contexto. Si un juego publicado necesita un Core congelado, se pasará a referenciar una etiqueta Git.
