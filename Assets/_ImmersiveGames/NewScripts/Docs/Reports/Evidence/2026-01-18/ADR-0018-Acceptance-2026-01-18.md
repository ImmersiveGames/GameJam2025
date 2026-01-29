# Evidência — ADR-0018 Aceito (2026-01-18)

- **ADR:** ADR-0018 — Mudança de semântica: ContentSwap + LevelManager
- **Estado:** Aceito
- **Data (snapshot):** 2026-01-18

## O que este snapshot comprova

Neste ponto do repositório (snapshot datado), a decisão do ADR-0018 está aplicada como **semântica e boundary**:

- **ContentSwap** permanece como executor técnico de troca de conteúdo (InPlace-only), mantendo contratos públicos existentes.
- **LevelManager** é o orquestrador de progressão de nível/fase, reutilizando ContentSwap.

## Referências canônicas

- Snapshot de evidência principal (Baseline 2.1): `Baseline-2.1-Evidence-2026-01-18.md`
- Ponte canônica (regressão contínua): `../LATEST.md`
- ADR-0016 (ContentSwap InPlace-only): `../../ADRs/ADR-0016-ContentSwap-WorldLifecycle.md`
- ADR-0019 (promoção Baseline 2.2): `../../ADRs/ADR-0019-Promocao-Baseline2.2.md`
