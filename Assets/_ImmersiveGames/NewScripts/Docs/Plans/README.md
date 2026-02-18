# Plans — Registro de atividades (rastreável/auditável)

Este diretório contém **planos de execução** organizados por **atividade**.

A partir deste snapshot, o trilho canônico é:

- `Docs/Plans/Plan-Continuous.md`

Os arquivos individuais antigos permanecem apenas como **redirecionamento** (para não quebrar links em Reports/Docs).

## Fonte de verdade (não duplicar)

- **Contrato / vocabulário canônico:** `Docs/Standards/Standards.md#observability-contract`
- **Política Strict vs Release:** `Docs/Standards/Standards.md#politica-strict-vs-release`
- **Evidência vigente:** `Docs/Reports/Evidence/LATEST.md`
- **Log bruto vigente:** `Docs/Reports/lastlog.log`
- **Decisões (ADRs):** `Docs/ADRs/README.md`

> Regra: **Planos não inventam contrato**. Se algo precisa virar contrato, vira **ADR** + evidência.

## Como usar (fluxo operacional)

1) Abra `Plan-Continuous.md` e execute a atividade em sequência.
2) Para **auditoria estática**, use o CODEX **apenas em modo read-only** e arquive o output em:
   - `Docs/Reports/Audits/<YYYY-MM-DD>/...`
3) Para **evidência de runtime**, capture log e atualize:
   - `Docs/Reports/Evidence/<YYYY-MM-DD>/...`
   - `Docs/Reports/Evidence/LATEST.md` (apontar para o snapshot mais recente)

## Status (semântica)

- **PROPOSED**: ainda não começou (apenas planejamento).
- **IN_PROGRESS**: execução ativa, com checklist atualizado.
- **BLOCKED**: depende de decisão/artefato externo (listar o bloqueio no plano).
- **DONE**: aceito + evidência (ou auditoria) registrada.
- **ARCHIVED**: histórico; não usar como trilho atual.

## Status resumido de atividades

| ActivityId | Status | Atividade | Fonte de verdade | Evidência/Auditoria | Trilho canônico |
|---|---|---|---|---|---|
| **P-001** | **DONE** | Strings → Referências diretas (v1) | ADRs + Standards | Plano: `Plans/Plan-Strings-To-DirectRefs.md` + Audit final: `Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md` + Validator: `Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` | `Plan-Continuous.md#p-001` |
| **P-002** | **DONE** | Data Cleanup pós v1 (remove resíduos/compat) | ADRs + Standards | Plano: `Plans/Plan-Post-StringsToDirectRefs-v1-DataCleanup.md` + Validator: `Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` + Smoke: `Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md` | `Plan-Continuous.md#p-002` |
| **P-003** | **DONE** | Navegação: Play → `to-gameplay` (correção mínima) | ADRs + Standards | Smoke: `Reports/lastlog.log` + audit de origem `Reports/Audits/2026-02-11/Audit-NavigationRuntime-Mismatch.md` | `Plan-Continuous.md#p-003` |
| **P-004** | **DONE** | Validação (Codex) — SceneFlow/RouteResetPolicy | ADRs + Standards | Audit datada: `Reports/Audits/2026-02-18/Audit-SceneFlow-RouteResetPolicy.md` | `Plan-Continuous.md#p-004` |

## Notas

- **Plano macro (design):** o trilho arquitetural SceneFlow/Navigation/LevelFlow (v2.x) vive em `Docs/Overview/`.
- **Plano histórico:** `Plans/Archive-Plano-2.2.md` existe apenas como referência/placeholder.
