# Evidência canônica (LATEST)

Esta página aponta para o conjunto de evidências **mais recente** aceito como referência para auditoria.

## Snapshot atual

- **Data:** 2026-02-03
- **Baseline:** 2.2
- **Arquivo de evidência:** [`Baseline-2.2-Evidence-2026-02-03.md`](2026-02-03/Baseline-2.2-Evidence-2026-02-03.md)
- **Log bruto (mais recente):** [`lastlog.log`](../lastlog.log) *(atualizado em 2026-02-17)*
- **Evidências adicionais:** [ADR-0017-LevelCatalog-Evidence-2026-02-03.md](2026-02-03/ADR-0017-LevelCatalog-Evidence-2026-02-03.md)
- **SceneFlow Config Snapshot (DataCleanup v1):** [../SceneFlow-Config-Snapshot-DataCleanup-v1.md](../SceneFlow-Config-Snapshot-DataCleanup-v1.md)
- **DataCleanup v1 validation PASS (2026-02-17):** [../SceneFlow-Config-ValidationReport-DataCleanup-v1.md](../SceneFlow-Config-ValidationReport-DataCleanup-v1.md)
- **Smoke audit (2026-02-17):** [../Audits/2026-02-17/Smoke-DataCleanup-v1.md](../Audits/2026-02-17/Smoke-DataCleanup-v1.md)
- **Log bruto usado no smoke:** [../lastlog.log](../lastlog.log)

## Regras

- Quando um snapshot é promovido para “LATEST”, ele vira **fonte de verdade** até nova promoção.
- Alterações de comportamento devem atualizar o snapshot e/ou justificar divergências via ADR + evidência.


