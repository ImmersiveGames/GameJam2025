# Codex — Audit Only

## Objetivo

Usar o CODEX **somente** para auditorias de sincronização (Docs ⇄ Código) e inventário de implementação.

## Regras

1. **Proibido:** solicitar ao CODEX qualquer ação que altere o repositório (criar/editar/remover arquivos, refatorar, “corrigir”, etc.).
2. **Permitido:** leitura e análise (listar arquivos, localizar símbolos, mapear fluxos e comparar com ADRs/contratos).
3. O output do CODEX deve ser **sempre** um artefato em `Docs/Reports/Audits/<YYYY-MM-DD>/`.
4. Qualquer decisão de implementação entra como plano humano (fora do CODEX) e, só depois, mudanças reais são feitas no repositório.

## Prompt canônico

Use `Docs/Reports/Audits/ADR-Sync-Audit-Prompt.md`.
