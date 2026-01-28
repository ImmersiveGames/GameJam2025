# ADR-0016 — ContentSwap InPlace-only (NewScripts)

## Status
- Estado: Aceito
- Data: 2026-01-28
- Escopo: NewScripts → Gameplay/ContentSwap + Infrastructure (Bootstrap/QA)

## Contexto

Em NewScripts, ContentSwap é um mecanismo simples e determinístico para trocar conteúdo **na mesma cena**, com reset/hard reset local. Não há integração com SceneFlow/WorldLifecycle dentro do ContentSwap, nem rotas alternativas para transições de cena.

## Decisão

- **ContentSwap em NewScripts é exclusivamente InPlace.**
- **Não existe WithTransition.**
- **ContentSwap não integra com SceneFlow** e **não expõe APIs de transição**.
- **Não há “capabilities”, “rejection event”, “force define”, “seleção dinâmica” ou “fallback”.**

### API
- Interface única: `IContentSwapChangeService`
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
- Implementar WithTransition.
- Integração de ContentSwap com SceneFlow.
- Registro/seleção dinâmica de implementação.
- Qualquer workaround de compatibilidade.

## Consequências
- Qualquer chamada a WithTransition é inválida e **deve falhar em compile** (API removida).
- Transições de cena são responsabilidade de SceneFlow/Navigation/LevelFlow (fora de ContentSwap).

## Referências
- ADR-TEMPLATE.md
- Reports/Observability-Contract.md
- WORLD_LIFECYCLE.md
