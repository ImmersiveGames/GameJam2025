# Checklist — ContentSwap (InPlace-only)

> **Fonte de verdade:** log do Console (Editor/Dev).

## Caso único — InPlace

No objeto **[QA] ContentSwapQA** (DontDestroyOnLoad), executar o ContextMenu:
- `QA/ContentSwap/G01 - InPlace (NoVisuals)`

### Evidências esperadas
1. `ContentSwapRequested` com `mode=InPlace`.
2. `ContentSwapPendingSet` e `ContentSwapCommitted` com `reason` correspondente ao QA.
3. `ContentSwapPendingCleared` quando aplicável.

### Observações
- ContentSwap em NewScripts é **exclusivamente InPlace**.
