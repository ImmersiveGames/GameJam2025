# Documentação — NewScripts (WorldLifecycle)

Diretório oficial da documentação do **NewScripts**, com papéis claros para decisão, operação, QA e evidências.

> Regra central: **cada documento tem um owner e um papel único**.
> Quando precisar do pipeline operacional, consulte **apenas** `WorldLifecycle/WorldLifecycle.md`.

---

## Ordem Recomendada de Leitura (essenciais)

1. **DECISIONS.md** — guardrails e política de legado.
2. **ARCHITECTURE.md** — visão **as-is** e roadmap curto.
3. **ADR/ADR.md** + **ADR/ADR-ciclo-de-vida-jogo.md** — histórico e decisão do lifecycle (fases/escopos).
4. **WorldLifecycle/WorldLifecycle.md** — contrato operacional e troubleshooting.
5. **QA/WorldLifecycle-Baseline-Checklist.md** — validação prescritiva (logs/ordem).
6. **QA/GameLoop-StateFlow-QA.md** — validação do GameLoop/StateDependent (start, estados, gate).
7. **Guides/UTILS-SYSTEMS-GUIDE.md** — infraestrutura transversal (DI/EventBus/Debug/Pooling).
8. **ADR/ADR-0001-NewScripts-Migracao-Legado.md** — estratégia e guardrails de migração/bridges temporários.
9. **GameLoop/GameLoop.md** — estado global do loop (Boot/Playing/Paused) e integração com gate.
10. **CHANGELOG-docs.md** — histórico das mudanças de documentação.

---

## Papéis e Owners

| Documento | Papel | Owner |
|-----------|-------|-------|
| DECISIONS.md | Normas globais / guardrails | Arquitetura |
| ARCHITECTURE.md | Arquitetura **as-is** e roadmap | Arquitetura |
| ADR/ADR.md | Índice/histórico de ADRs | Arquitetura |
| ADR/ADR-ciclo-de-vida-jogo.md | Decisão de fases/escopos do lifecycle | Arquitetura |
| ADR/ADR-0001-NewScripts-Migracao-Legado.md | Migração/bridges temporários | Arquitetura |
| WorldLifecycle/WorldLifecycle.md | Operação do lifecycle e troubleshooting | Operação |
| QA/WorldLifecycle-Baseline-Checklist.md | QA prescritivo do lifecycle | QA |
| QA/GameLoop-StateFlow-QA.md | QA do GameLoop + StateDependent | QA |
| Guides/UTILS-SYSTEMS-GUIDE.md | Infra transversal (DI/EventBus/Debug/Pooling) | Infra |
| GameLoop/GameLoop.md | Estado global e sinais de pausa/reset | Infra/GameLoop |
| CHANGELOG-docs.md | Histórico de alterações de docs | Arquitetura |

---

## Evidências e relatórios

Conteúdos de auditoria, smoke tests e planos de normalização vivem em `Reports/` (fonte de evidência, não norma).

---

## Governança

- Pipeline e resets: `WorldLifecycle/WorldLifecycle.md` é a fonte operacional única.
- Decisões/porquês: ADRs (`ADR/`).
- Validação: checklists (`QA/`), referenciando o contrato operacional.
- Guardrails globais e legado: `DECISIONS.md`.
- Toda movimentação relevante deve ser registrada em `CHANGELOG-docs.md`.
