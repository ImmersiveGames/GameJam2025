# NewScripts — Docs

Este diretório é o **ponto de entrada** para documentação do módulo **NewScripts**.

## Como navegar (ordem sugerida)

1) **Visão geral**
- `Overview/Overview.md`

2) **Contratos e políticas de produção (fonte canônica)**
- `Standards/Standards.md#observability-contract` — formato de logs, anchors e campos canônicos (`reason`, `signature`, `profile`, `target`).
- `Standards/Standards.md#politica-strict-vs-release` — política **Strict vs Release** e definição de **DEGRADED_MODE**.
- `Reports/Evidence/README.md` — como produzir e arquivar evidências datadas (baseline, auditorias, etc).
- `Standards/Standards.md#reason-map-legado` — redirect legado para o contrato (não manter lista paralela).

3) **Decisões de arquitetura (ADRs)**
- `ADRs/README.md` (índice + guia)
- `ADRs/ADR-TEMPLATE.md` (template — implementação)
- `ADRs/ADR-TEMPLATE-COMPLETENESS.md` (template — completude/governança)

4) **Relatórios (evidência e auditorias)**
- `Reports/Evidence/` — evidências canônicas, incluindo `LATEST.md`.
- `Reports/lastlog.log` — log bruto mais recente (evidência rápida).
- `Reports/Audits/` — auditorias estáticas (ex.: sync ADR↔código, invariants, etc).

5) **Guias operacionais**
- `Guides.md` — HowTo + Checklists consolidados.

6) **Planos e WIP**
- `Plans/` — planos de execução atuais (work-in-progress).
- `Plans/Plan-Continuous.md` — trilho canônico + status das atividades.

## Regra operacional

- **CODEX é usado apenas para auditorias** (varredura/diagnóstico). Veja: `Standards/Standards.md#politica-de-uso-do-codex`.
- Implementações e correções **devem** referenciar: ADR(s) + política Strict/Release + contrato de observabilidade + evidência datada.

## Atalhos

- **Checklist de completude ideal (ADRs 0009–0019):** `Standards/Standards.md#checklist-adrs`
