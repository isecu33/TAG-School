# Checklist de requisitos de tienda (MVP) — §10

**Referenciado por:** [`planes/PLAN-FASE-2.md`](planes/PLAN-FASE-2.md) (MVP-01) ·
**Fuente:** `ARQUITECTURA §10` (Seguridad, privacidad y tiendas)

Estado de los requisitos de `§10` para publicar el MVP en App Store / Play. Marcado según lo
que cubre el código de Fase 2; lo demás queda como acción explícita antes del envío.

| Requisito (§10) | Estado | Nota |
|---|---|---|
| Cuenta **opcional**, anónima por defecto | ⏳ Fase 3 | Auth anónima llega en `DATA-01`; el MVP funciona sin cuenta (offline-first). |
| **Nada de datos de menores**: age gate | ☐ Pendiente | Age gate a implementar en el onboarding antes del envío. |
| **Analítica reducida bajo 16** | ⏳ Fase 3 | Se aplica con `CI-02` (eventos custom) respetando el age gate. |
| Contenido compartido **solo hacia fuera** (sin galería interna en MVP) | ✅ | `ShareService` solo comparte por la hoja nativa; no hay galería (§10). Galería + moderación es Fase 4 (`SOC-06`). |
| **Marca de agua** en exports | ✅ | `Watermark` (SOC-04); premium la omite (flag → Remote Config en Fase 3). |
| Posicionamiento **educativo/creativo** | ✅ (contenido) | Lecciones + glosario; los muros del juego son espacios legales (importante para el review de Apple). |
| Muros del juego = **espacios legales** | ✅ (diseño) | Mockups de persianas/trenes en depósito legal/halls (§6); sin incitación a pintar ilegal. |
| Privacidad de export (sin PII incrustada) | ✅ | El PNG/MP4 se genera desde el canvas; no incrusta datos de usuario. |

## Antes del envío (acciones)

1. Implementar el **age gate** en el onboarding y cablearlo a la analítica (bloquea eventos
   sensibles < 16).
2. Redactar **política de privacidad** y los textos de la ficha de tienda (educativo/creativo).
3. Preparar **capturas y vídeo** de la ficha usando el replay (`SOC-01`) y los mockups (`SOC-05`).
4. Configurar **build de release** (Fase 3 `CI-01`: Fastlane → TestFlight / Play Internal).

> El smoke test `MvpSmokeEditTests` cubre el bucle jugable (3 capítulos → export → share →
> blackbook → persistencia). Las casillas ⏳ dependen de servicios de Fase 3 y las ☐ son trabajo
> de producto pendiente; ninguna es código de motor bloqueado por este entorno.
