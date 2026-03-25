> [!WARNING]
> **Obsoleto por supersedência.**
>
> Este ADR foi movido para histórico da baseline de SceneFlow/LevelFlow.
> Use os ADRs canônicos `ADR-0030` a `ADR-0033` para leitura operacional atual.
>
> Motivo: consolidação pós-baseline 0027 para reduzir leitura cruzada e ambiguidade de ownership.

# ADR-0026 — Troca de Level Intra-Macro via Swap Local (sem Transição Macro)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): **2026-02-19**
- Última atualização: **2026-03-25**

## Contexto

Depois de separar rota macro de conteúdo local, o sistema precisava permitir troca de level dentro da mesma gameplay sem forçar uma nova transição macro.

## Decisão canônica atual

### 1) Swap local não usa o trilho macro do SceneFlow

A troca local continua pertencendo ao domínio de `LevelFlow` e `SceneComposition` local.

### 2) A macro route atual delimita o universo válido

O swap local usa a `LevelCollection` da macro route atual como fonte dos levels permitidos.

### 3) Restart local de mesmo level continua sendo local

Mesmo quando o nível final e o atual são o mesmo asset, o reload local continua podendo executar unload/load local sem reentrar no SceneFlow macro.

## Consequências

### Positivas
- evita churn de transição macro para operação local;
- preserva a assinatura macro;
- mantém a troca local coerente com `ADR-0020` e `ADR-0022`.

### Trade-offs
- exige manter a distinção entre restart macro e restart local muito clara na API e nos docs.

## Relação com outros ADRs

- `ADR-0020`: localidade não pertence à route macro.
- `ADR-0024`: levels válidos vêm da `LevelCollection` da route.
- `ADR-0027`: intro/post não reclassificam o swap local como transição macro.
