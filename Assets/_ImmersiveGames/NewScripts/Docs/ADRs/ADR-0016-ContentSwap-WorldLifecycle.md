# ADR-0016 — ContentSwap InPlace-only (NewScripts)

## Status

- Estado: Implementado
- Data (decisão): 2026-01-28
- Última atualização: 2026-02-04
- Tipo: Implementação
- Escopo: NewScripts → Modules/ContentSwap + Infrastructure (Bootstrap/QA)

## Contexto

Em NewScripts, ContentSwap é um mecanismo simples e determinístico para trocar conteúdo **na mesma cena**, com reset/hard reset local. O escopo é intencionalmente reduzido e não considera transições entre cenas dentro do próprio ContentSwap.

## Decisão

### Objetivo de produção (sistema ideal)

Permitir troca de conteúdo/configuração em runtime (in-place) sem reiniciar a cena, integrada ao WorldLifecycle, com contrato de gating e observabilidade para QA e produção.

### Contrato de produção (mínimo)

- ContentSwap é uma operação explícita com reason canônico e assinatura observável.
- Pode executar reset in-place de subset de sistemas sem violar determinismo global.
- Operação respeita gates (scene_transition/sim.gameplay) conforme necessário.

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

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
- `GlobalCompositionRoot` registra toda a infraestrutura NewScripts necessária.
- ContentSwap é registrado **sempre** como InPlace-only.

## Fora de escopo

- Substituir o fluxo de transição de cenas (SceneFlow) para trocas que exigem load/unload.

- Qualquer expansão de escopo além do InPlace-only.
- Integração do ContentSwap com transições de cena.
- Registro/seleção dinâmica de implementação.

## Consequências

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- Evidência mostra ContentSwap in-place com reason canônico.

## Evidência

- **Última evidência (log bruto):** `Docs/Reports/lastlog.log`

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - `[QA][ContentSwap] SwapInPlace contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

## Implementação (arquivos impactados)

### Runtime / Editor (código e assets)

- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapContextService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapEvents.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapPlan.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapOptions.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gates/SimulationGateTokens.cs`

### QA / evidência

- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs`

## Referências

- ADR-TEMPLATE.md
- Standards/Standards.md#observability-contract
- Overview/Overview.md
- [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
