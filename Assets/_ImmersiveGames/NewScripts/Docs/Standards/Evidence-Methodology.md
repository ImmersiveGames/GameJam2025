# Metodologia de Evidências (Docs/Reports)

## Objetivo

Padronizar como geramos e arquivamos evidências de que um ADR (ou um conjunto de invariants) está **implementado em produção**.

## Princípios

- Evidência é **datada** e **imutável** (snapshot).
- `Reports/Evidence/LATEST.md` aponta para a evidência canônica **mais recente** que cobre o baseline e invariants.
- Auditorias estáticas (ex.: “ADR Sync Audit”) ficam em `Reports/Audits/<YYYY-MM-DD>/`.

## Estrutura recomendada

- `Reports/Evidence/`
  - `LATEST.md` (ponteiro)
  - `Logs/` (logs de execução)
  - `<YYYY-MM-DD>/` (snapshots arquivados, se necessário)
- `Reports/Audits/`
  - `<YYYY-MM-DD>/...` (auditorias por data)

## Quando arquivar um snapshot novo?

Arquive um snapshot quando:
- Um ADR muda de status (ex.: **PARCIAL → ALINHADO**).
- Um invariant crítico muda (ex.: Strict/Release aplicado).
- Mudanças de fluxo (SceneFlow/GameLoop) podem alterar a ordem de eventos em logs.

## Checklist de um snapshot “bom”

- Contexto: build/mode, scene profile, assinatura/cause (`reason`).
- Logs com anchors canônicos (`[OBS]` + campos).
- Referência explícita aos ADRs cobertos.

## Referências
- `Standards/Observability-Contract.md`
- `Standards/Production-Policy-Strict-Release.md`
