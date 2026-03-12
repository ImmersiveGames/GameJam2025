# Audits LATEST

Current canonical audit snapshot: `2026-03-11`.

- Summary: `Docs/Reports/Audits/2026-03-11/CANON-ONLY-AXIS-SYNC.md`
- Focus: sincronizacao documental do estado pos-H1..H7 com o codigo real.
- Decision: `canon-only no eixo principal`; `nao canon-only absoluto em todo NewScripts/** ainda`.
- Remaining exceptions:
  - `Gameplay RunRearm` com fallback legado de actor-kind/string
  - pequeno residuo editor/serializado em `GameNavigationIntentCatalogAsset`
- Historical audit snapshot: `Docs/Reports/Audits/2026-03-06/`
