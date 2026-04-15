# Phase Catalog Navigation 2.0 - Final Checklist

## Capacidades fechadas

- `AdvancePhase`
- `PreviousPhase`
- `GoToSpecificPhase`
- `RestartCatalog`
- `RestartCurrentPhase`
- `LoopCount`
- hooks canônicos mínimos de catálogo

## Invariantes principais

- `IPhaseCatalogRuntimeStateService.CurrentCommitted` é a fonte única do current ordinal.
- `PendingTarget` permanece ativo até o fim do handoff canônico.
- `Specific` pode bloquear `target == current`.
- `RestartCatalog` e `RestartCurrentPhase` podem reaplicar o mesmo target quando a intenção semântica exige.

## Cenários mínimos de validação manual

- `AdvancePhase` avança para a próxima phase e publica request, selection, composition, handoff e completion.
- `PreviousPhase` volta para a phase anterior e atualiza `LoopCount` apenas quando houver wrap real.
- `GoToSpecificPhase` aceita `phaseId` válido e bloqueia target igual ao current.
- `RestartCatalog` reaplica a primeira phase do catálogo.
- `RestartCurrentPhase` reaplica a phase atual.

## Assinaturas e logs esperados

- `PhaseCatalogNavigationRequested`
- `PhaseCatalogPendingTargetChanged`
- `PhaseCatalogCurrentCommittedChanged`
- `PhaseCatalogLoopCountChanged`
- `PhaseCatalogNavigationCompleted`
- Logs principais com rótulos consistentes de `Request`, `Selection`, `State`, `Composition`, `Handoff` e `Completion`.

## Fora de escopo

- Persistência
- Branching
- Eligibility
- Difficulty scaling
- Analytics

