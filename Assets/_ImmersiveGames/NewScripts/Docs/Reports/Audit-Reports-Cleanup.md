# Audit — Reports Cleanup

Data: 2026-01-16

## Resumo
Classificação e limpeza de `Docs/Reports`, com arquivamento de históricos em `Reports/Archive/2025` e remoção de arquivos obsoletos não referenciados.

## Tabela de classificação

| Arquivo | Categoria | Ação | Justificativa | Links ajustados |
| --- | --- | --- | --- | --- |
| `Baseline-2.0-Checklist.md` | CANÔNICO | Keep | Checklist operacional do Baseline 2.0. | n/a |
| `Baseline-2.0-ChecklistVerification-LastRun.md` | CANÔNICO | Keep | Evidência do último smoke verificado. | n/a |
| `Baseline-2.0-Smoke-LastRun.md` | SUPORTE | Keep | Snapshot do smoke com evidências. | n/a |
| `Baseline-2.0-Spec.md` | CANÔNICO (histórico) | Keep | Spec congelada usada como referência histórica. | n/a |
| `Baseline-Audit-2026-01-03.md` | SUPORTE | Keep | Matriz de evidências do baseline. | Links para Archive/2025 atualizados |
| `GameLoop.md` | SUPORTE | Keep | Documenta fluxo do GameLoop. | n/a |
| `QA-GameplayReset-RequestMatrix.md` | SUPORTE | Keep | Evidência de QA para matriz de reset. | n/a |
| `QA-GameplayResetKind.md` | SUPORTE | Keep | Evidência de QA para reset por kind. | n/a |
| `ResetWorld-Audit-2026-01-05.md` | SUPORTE | Keep | Auditoria do reset fora de transição. | n/a |
| `SceneFlow-Assets-Checklist.md` | CANÔNICO | Keep | Checklist de assets do SceneFlow. | n/a |
| `SceneFlow-Production-EndToEnd-Validation.md` | CANÔNICO | Keep | Report master end-to-end. | Link para blockers atualizado |
| `WORLDLIFECYCLE_RESET_ANALYSIS.md` | SUPORTE | Keep | Análise de reset. | n/a |
| `WORLDLIFECYCLE_SPAWN_ANALYSIS.md` | SUPORTE | Keep | Análise de spawn. | n/a |
| `SceneFlow-Smoke-Result.md` | HISTÓRICO | Move | Evidência de smoke 2025. | Movido para `Archive/2025/` |
| `SceneFlow-Gameplay-To-Menu-Report.md` | HISTÓRICO | Move | Checklist histórico de retorno ao menu. | Movido para `Archive/2025/` |
| `SceneFlow-Production-Evidence-2025-12-31.md` | HISTÓRICO | Move | Evidência de produção com data. | Movido para `Archive/2025/` |
| `Report-SceneFlow-Production-Log-2025-12-31.md` | HISTÓRICO | Move | Log de produção com data. | Movido para `Archive/2025/` |
| `SceneFlow-Gameplay-Blockers-Report.md` | HISTÓRICO | Move | Report de blockers 2025. | Movido para `Archive/2025/` |
| `SceneFlow-Profile-Audit.md` | HISTÓRICO | Move | Auditoria de profiles. | Movido para `Archive/2025/` |
| `Legacy-Cleanup-Report.md` | HISTÓRICO | Move | Limpeza de legado (2025). | Movido para `Archive/2025/` |
| `Marco0-Phase-Observability-Checklist.md` | HISTÓRICO | Move | Marco inicial de observabilidade. | Movido para `Archive/2025/` |
| `QA-Audit-2025-12-27.md` | HISTÓRICO | Move | Inventário histórico de QA. | Movido para `Archive/2025/` |
| `NEW_SCRIPTS_HEALTH_REPORT.md` | OBSOLETO | Delete | Arquivo sem referência atual e sem data explícita. | Removido |
| `NEW_SCRIPTS_INCOMPLETE_REPORT.md` | OBSOLETO | Delete | Marcado como incompleto e sem referência. | Removido |

## Links ajustados

- ADRs e reports foram atualizados para apontar `Reports/Archive/2025/...` quando aplicável.
- README passou a referenciar o diretório `Archive/2025` como coleção histórica, sem listar cada arquivo.
