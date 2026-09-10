# ARModule (opcional, Fase 3)

Módulo de RA (ARQUITECTURA §6). **Aislado**: `autoReferenced: false` y **nadie lo referencia**
(§3); se descubre/carga por Addressables solo si el dispositivo soporta RA, con fallback a los
mockups de pared (`Social` SOC-05).

Hoy contiene **solo abstracciones + un stub** (`StubArPlacementSession`, `IsSupported=false`) para
que compile sin los paquetes de AR/Addressables. `ArModule.CreateSession()` devuelve el stub.

## Cómo añadir la implementación real (en el editor, con dispositivo)

1. Package Manager: instala **AR Foundation** + **ARKit XR Plugin** + **ARCore XR Plugin**, y
   **Addressables**.
2. Crea un asmdef aparte, p. ej. `PieceBook.ARModule.ARFoundation`, que referencie
   `PieceBook.ARModule`, `Unity.XR.ARFoundation` y `Unity.XR.CoreUtils`, con un
   `defineConstraint` (p. ej. `PIECEBOOK_ARFOUNDATION`) para que solo compile cuando el paquete está.
3. Implementa `IArPlacementSession` con AR Foundation:
   - `StartScan`/`StopScan` → `ARSession` + `ARPlaneManager` (solo planos verticales, AR-01).
   - `PlaceArtwork` → crea un `ARAnchor` en el plano y renderiza la textura (AR-02); light estimation
     para integrar la pieza (AR-03).
   - `CaptureFrame` → captura la escena RA con la pieza anclada → PNG (AR-04), que se pasa a
     `Social` (`ShareService`/`IArtworkUploader`).
4. Cambia el loader (AR-00): marca la etiqueta Addressable del módulo y, si `ARSession.state`
   indica soporte, resuelve la implementación real; si no, deja el stub.

Criterios de aceptación: ver `docs/planes/PLAN-FASE-3.md` (AR-00…AR-04).
