# ADR-XXXX - Título

## Status

* **Status:** Aberto | Em andamento | Concluído | Parcial | Cancelado
* **Data:** YYYY-MM-DD
* **Decisores:** (nomes/roles)
* **Tags:** (ex.: SceneFlow, WorldLifecycle, Observability)

## Contexto

Descreva o problema, restrições (produção/QA), e por que isso é necessário.

## Decisão

### Objetivo de produção (sistema ideal)

Descreva o estado ideal “production complete” — o que precisa existir para o fluxo funcionar de ponta a ponta.

### Contrato de produção (mínimo)

Regras objetivas e verificáveis (ordem de eventos, invariantes, ownership, dependências).

### Não-objetivos (resumo)

O que **não** está sendo resolvido aqui (detalhe completo em “Fora de escopo”).

## Fora de escopo

Lista explícita do que fica fora (para reduzir escopo e evitar promessas implícitas).

## Consequências

### Benefícios

### Custos / Riscos

### Política de falhas e fallback (fail-fast)

Em Unity, preferir falhar cedo para sinalizar bugs de pipeline/config, evitando “auto-criação em voo” que mascara problemas.

### Critérios de pronto (DoD)

Checklist objetiva do que precisa estar verdadeiro para considerar “feito”.

## Notas de implementação (se necessário)

Detalhes de arquitetura, pontos de integração, exemplos de API, etc.

## Evidência

* **Fonte canônica atual:** `Docs/Reports/Evidence/LATEST.md`
* **Âncoras/assinaturas relevantes:** (strings ou padrões de log)
* **Contrato de observabilidade:** `Docs/Standards/Standards.md#observability-contract`

## Evidências

Links para logs e relatórios datados (snapshots). Evitar “evidência viva” sem data.

## Referências

Links para ADRs relacionados, documentos e contratos (Observability, Evidence, etc).
