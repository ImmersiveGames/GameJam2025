# NewScripts — Docs

Este diretório é o **ponto de entrada** para documentação do módulo **NewScripts**.

## Como navegar (ordem sugerida)

1) **Visão geral**
- `Overview/Architecture.md`
- `Overview/WorldLifecycle.md`

2) **Contratos e políticas de produção (fonte canônica)**
- `Standards/Observability-Contract.md` — formato de logs, anchors e campos canônicos (`reason`, `signature`, `profile`, `target`).
- `Standards/Production-Policy-Strict-Release.md` — política **Strict vs Release** e definição de **DEGRADED_MODE**.
- `Standards/Evidence-Methodology.md` — como produzir e arquivar evidências datadas (baseline, auditorias, etc).
- `Standards/Reason-Registry.md` — registro prático de *reasons* canônicos (o antigo “Reason-Map” é considerado legado).

3) **Decisões de arquitetura (ADRs)**
- `ADRs/README.md` (índice + guia)
- `ADRs/ADR-TEMPLATE.md` (modelo)

4) **Relatórios (evidência e auditorias)**
- `Reports/Evidence/` — evidências canônicas, incluindo `LATEST.md`.
- `Reports/Audits/` — auditorias estáticas (ex.: sync ADR↔código, invariants, etc).

5) **Planos e WIP**
- `Plans/` — planos de execução atuais (work-in-progress).
- `Archive/` — material histórico/obsoleto preservado apenas por rastreabilidade.

## Regra operacional

- **CODEX é usado apenas para auditorias** (varredura/diagnóstico).
- Implementações e correções **devem** referenciar: ADR(s) + política Strict/Release + contrato de observabilidade + evidência datada.

## Atalhos

- **Checklist de completude ideal (ADRs 0009–0019):** `Standards/ADR-Ideal-Completeness-Checklist.md`
