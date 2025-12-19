# Documentação — WorldLifecycle & NewScripts

Este diretório contém a **documentação oficial e normalizada** da arquitetura NewScripts, com foco em:
- ciclo de vida determinístico do mundo
- reset por escopos
- governança clara entre decisão, operação e validação
- separação explícita entre infraestrutura, arquitetura e gameplay

> Regra central: **cada documento tem um papel único**.
> Evite duplicar explicações entre arquivos.

---

## Ordem Recomendada de Leitura

Para entender o sistema corretamente, siga esta ordem:

1. **DECISIONS.md**
   Limites, guardrails e política de uso do legado.
   → *Documento normativo.*

2. **ARCHITECTURE.md**
   Visão geral da arquitetura **as-is** e roadmap.
   → *Descritivo, sem regras duras.*

3. **ADR – Ciclo de Vida do Jogo** (`docs/adr/ADR-ciclo-de-vida-jogo.md`)
   Justificativa e decisões arquiteturais sobre fases, resets e readiness.
   → *Por que o lifecycle é assim.*

4. **WorldLifecycle.md** (`docs/world-lifecycle/WorldLifecycle.md`)
   Contrato operacional completo do ciclo de vida e reset determinístico.
   → *Fonte única de verdade operacional.*

5. **WorldLifecycle-Baseline-Checklist.md** (`Docs/QA/WorldLifecycle-Baseline-Checklist.md`)
   Checklist prescritivo de QA para validar ordem, logs e comportamento.
   → *Como verificar se está correto.*

6. **UTILS-SYSTEMS-GUIDE.md**
   Guia técnico de sistemas transversais (DI, EventBus, Debug, Pooling, etc.).
   → *Infraestrutura, não gameplay.*

7. **ADR-0001 — Migração do Legado** (`Docs/ADR/ADR-0001-NewScripts-Migracao-Legado.md`)
   Estratégia oficial de migração incremental do legado para o NewScripts.
   → *Como atravessar fronteiras sem quebrar o determinismo.*

---

## Papéis dos Documentos (Resumo Rápido)

| Documento | Papel |
|---------|------|
| DECISIONS.md | Normas e guardrails |
| ARCHITECTURE.md | Arquitetura *as-is* |
| ADR-ciclo-de-vida-jogo.md | Decisão arquitetural |
| WorldLifecycle.md | Contrato operacional |
| WorldLifecycle-Baseline-Checklist.md | Validação QA |
| UTILS-SYSTEMS-GUIDE.md | Infraestrutura |
| ADR-0001 | Migração do legado |
| docs/adr/ADR.md | Histórico consolidado de ADRs |

---

## Regras de Governança (Importante)

- **Não duplicar conteúdo operacional** fora de `WorldLifecycle.md`.
- ADRs **não explicam pipeline**, apenas decisões e consequências.
- Checklists **não explicam arquitetura**, apenas validam.
- Infraestrutura não define gameplay.
- Qualquer exceção deve ser documentada explicitamente.

---

## Sobre Mudanças na Documentação

- Alterações devem respeitar o papel de cada arquivo.
- Movimentações relevantes devem ser registradas em `CHANGELOG-docs.md`.
- Dúvidas sobre onde documentar algo:
    - decisão → ADR
    - funcionamento → WorldLifecycle
    - validação → Checklist
    - regras → DECISIONS

---

**Status:**
📌 *Documentação normalizada e validada — Baseline v1.0*
