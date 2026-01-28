# ADR-0016 — ContentSwap InPlace-only (NewScripts)

## Status
- Estado: Aceito
- Data: 2026-01-28
- Escopo: NewScripts → Gameplay/ContentSwap + Infrastructure (Bootstrap/QA)

## Contexto

Em NewScripts, ContentSwap é um mecanismo simples e determinístico para trocar conteúdo **na mesma cena**, com reset/hard reset local. O escopo é intencionalmente reduzido e não considera transições entre cenas dentro do próprio ContentSwap.

## Decisão

- **ContentSwap em NewScripts é exclusivamente InPlace.**
- **Não há múltiplos tipos de ContentSwap.**
- **Não existem mecanismos de seleção dinâmica de implementação.**

### API
- Interface única: `IContentSwapChangeService`.
- **Apenas** métodos `RequestContentSwapInPlaceAsync(...)` permanecem disponíveis.

### Observabilidade mínima
Para cada request InPlace, o sistema deve produzir logs/eventos contendo:
- `mode=InPlace`
- `contentId`
- `reason`

Eventos/logs mínimos:
- `ContentSwapRequested`
- `ContentSwapPendingSet`
- `ContentSwapCommitted`
- `ContentSwapPendingCleared`

### Bootstrap
- `GlobalBootstrap` registra toda a infraestrutura NewScripts necessária.
- ContentSwap é registrado **sempre** como InPlace-only.

## Non-goals
- Qualquer expansão de escopo além do InPlace-only.
- Integração do ContentSwap com transições de cena.
- Registro/seleção dinâmica de implementação.

## Consequências
- O sistema deve permanecer simples e determinístico ao trocar conteúdo na mesma cena.
- Transições de cena são responsabilidade de SceneFlow/Navigation/LevelFlow (fora de ContentSwap).

## Referências
- ADR-TEMPLATE.md
- Reports/Observability-Contract.md
- WORLD_LIFECYCLE.md
