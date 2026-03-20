# ADR-0016 â€” ContentSwap InPlace-only (NewScripts)

## Status

- Estado: Implementado
- Data (decisÃ£o): 2026-01-28
- Ãšltima atualizaÃ§Ã£o: 2026-02-04
- Tipo: ImplementaÃ§Ã£o
- Escopo: NewScripts â†’ Modules/ContentSwap + Infrastructure (Bootstrap/QA)

## Contexto

Em NewScripts, ContentSwap Ã© um mecanismo simples e determinÃ­stico para trocar conteÃºdo **na mesma cena**, com reset/hard reset local. O escopo Ã© intencionalmente reduzido e nÃ£o considera transiÃ§Ãµes entre cenas dentro do prÃ³prio ContentSwap.

## DecisÃ£o

### Objetivo de produÃ§Ã£o (sistema ideal)

Permitir troca de conteÃºdo/configuraÃ§Ã£o em runtime (in-place) sem reiniciar a cena, integrada ao WorldLifecycle, com contrato de gating e observabilidade para QA e produÃ§Ã£o.

### Contrato de produÃ§Ã£o (mÃ­nimo)

- ContentSwap Ã© uma operaÃ§Ã£o explÃ­cita com reason canÃ´nico e assinatura observÃ¡vel.
- Pode executar reset in-place de subset de sistemas sem violar determinismo global.
- OperaÃ§Ã£o respeita gates (scene_transition/sim.gameplay) conforme necessÃ¡rio.

### NÃ£o-objetivos (resumo)

Ver seÃ§Ã£o **Fora de escopo**.

### API
- Interface Ãºnica: `IContentSwapChangeService`.
- **Apenas** mÃ©todos `RequestContentSwapInPlaceAsync(...)` permanecem disponÃ­veis.

### Observabilidade mÃ­nima
Para cada request InPlace, o sistema deve produzir logs/eventos contendo:
- `mode=InPlace`
- `contentId`
- `reason`

Eventos/logs mÃ­nimos:
- `ContentSwapRequested`
- `ContentSwapPendingSet`
- `ContentSwapCommitted`
- `ContentSwapPendingCleared`

### Bootstrap
- `GlobalCompositionRoot` registra toda a infraestrutura NewScripts necessÃ¡ria.
- ContentSwap Ã© registrado **sempre** como InPlace-only.

## Fora de escopo

- Substituir o fluxo de transiÃ§Ã£o de cenas (SceneFlow) para trocas que exigem load/unload.

- Qualquer expansÃ£o de escopo alÃ©m do InPlace-only.
- IntegraÃ§Ã£o do ContentSwap com transiÃ§Ãµes de cena.
- Registro/seleÃ§Ã£o dinÃ¢mica de implementaÃ§Ã£o.

## ConsequÃªncias

### PolÃ­tica de falhas e fallback (fail-fast)

- Em Unity, ausÃªncia de referÃªncias/configs crÃ­ticas deve **falhar cedo** (erro claro) para evitar estados invÃ¡lidos.
- Evitar "auto-criaÃ§Ã£o em voo" (instanciar prefabs/serviÃ§os silenciosamente) em produÃ§Ã£o.
- ExceÃ§Ãµes: apenas quando houver **config explÃ­cita** de modo degradado (ex.: HUD desabilitado) e com log Ã¢ncora indicando modo degradado.


### CritÃ©rios de pronto (DoD)

- EvidÃªncia mostra ContentSwap in-place com reason canÃ´nico.

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`

- **Fonte canÃ´nica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Ã‚ncoras/assinaturas relevantes:**
  - `[QA][ContentSwap] SwapInPlace contentId='content.2' reason='QA/ContentSwap/InPlace/NoVisuals'`
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)

## ImplementaÃ§Ã£o (arquivos impactados)

### Runtime / Editor (cÃ³digo e assets)

- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/InPlaceContentSwapService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapContextService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapEvents.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapPlan.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Runtime/ContentSwapOptions.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gates/SimulationGateTokens.cs`

### QA / evidÃªncia

- `Assets/_ImmersiveGames/NewScripts/Modules/ContentSwap/Dev/Bindings/ContentSwapDevContextMenu.cs`

## ReferÃªncias

- ADR-TEMPLATE.md
- Standards/Standards.md#observability-contract
- Overview/Overview.md
- [`Observability-Contract.md`](../Standards/Standards.md#observability-contract)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)

