# Relatório de Normalização da Documentação

## Papel final de cada documento
- **docs/DECISIONS.md** — Normas/guardrails globais e política de legado; não contém pipeline operacional.
- **docs/ARCHITECTURE.md** — Descrição As-Is com resumo do fluxo de vida; detalhes operacionais ficam em `docs/world-lifecycle/WorldLifecycle.md`.
- **docs/adr/ADR-ciclo-de-vida-jogo.md** — Decisão arquitetural (fases, escopos, porquês e consequências); aponta para o contrato operacional.
- **docs/world-lifecycle/WorldLifecycle.md** — Fonte operacional única para pipeline, fases, escopos, hooks, troubleshooting e validação.
- **Docs/QA/WorldLifecycle-Baseline-Checklist.md** — Checklist prescritivo de QA, referenciando o contrato operacional para pipeline/ordenção.
- **docs/UTILS-SYSTEMS-GUIDE.md** — Guia técnico de infraestrutura; menciona relação com reset apenas pelo prisma de infra.
- **Docs/ADR/ADR-0001-NewScripts-Migracao-Legado.md** — Decisão/guardrails de migração legado → NewScripts; referências ao contrato operacional para detalhes de execução.
- **docs/adr/ADR.md** — Compilado histórico de ADRs; inclui apontamento para a fonte operacional do WorldLifecycle.
- **README.md** — Índice de navegação destacando owners (operacional vs decisão vs QA).

## Duplicações removidas (com owner declarado)
- Pipeline, passes de spawn, fases de readiness e escopos de reset: agora apontam para `docs/world-lifecycle/WorldLifecycle.md` (owner operacional). Removidos resumos duplicados em `docs/adr/ADR-ciclo-de-vida-jogo.md`, `Docs/QA/WorldLifecycle-Baseline-Checklist.md`, `docs/ARCHITECTURE.md`, `Docs/ADR/ADR-0001-NewScripts-Migracao-Legado.md`.
- Ordering/troubleshooting de hooks e reset: consolidado em `docs/world-lifecycle/WorldLifecycle.md`; demais documentos apenas referenciam.
- Checklist de QA mantém passos/logs esperados, mas remete ao contrato para pipeline em vez de duplicar.

## Validações aplicadas
- Nenhum conteúdo operacional foi apagado: toda descrição removida está presente em `docs/world-lifecycle/WorldLifecycle.md` ou referenciada por link/âncora.
- ADRs mantêm foco em decisão/guardrails, com referências explícitas ao owner operacional.
- Checklist de QA preserva critérios de aprovação/reprovação e fluxos A/B, usando links para o pipeline oficial.
- README e DECISIONS reforçam a separação de responsabilidades e a localização do contrato operacional.
