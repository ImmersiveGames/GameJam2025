# ActorAttributes — Endpoint Local

Base: Base 1.2 — Actors Convergence / Convergência de Atores

Este pacote contém os contratos passivos iniciais de `ActorAttributes` e a Fase B com `ActorAttributeEndpoint` local.

## Inclui

- `ActorAttributeDefinitionAsset`
- `ActorAttributeProfileAsset`
- `ActorAttributeSemanticKind`
- `ActorAttributeId`
- `ActorAttributeState`
- `ActorAttributeOperation`
- `ActorAttributeCommand`
- `ActorAttributeChangedFact`
- `ActorAttributeEndpoint`
- `ActorAttributeSetupResult`
- `ActorAttributeApplyResult`
- `ActorAttributeReleaseResult`

## Responsabilidade do endpoint

- Inicializar estado runtime a partir de `ActorAttributeProfileAsset`.
- Aplicar comandos locais: `Set`, `Add`, `Subtract`, `ResetToInitial`, `RestoreToMax`.
- Aplicar clamp apenas em mutation runtime.
- Retornar `ActorAttributeChangedFact` após mudança.
- Rejeitar actor identity ou pipeline/activity identity incompatível quando a identity foi fornecida na inicialização.
- Liberar estado local quando comandado.

## Não inclui

- `ActorAttributeSetupStage`
- `ActorAttributeReleaseStage` integrado ao pipeline
- integração com `ActivityEntryPipeline`
- `DebugUtility`
- DI
- events canônicos
- UI/HUD
- combat/damage
- auto regen/drain
- save/snapshot
- singleton/global manager
- bridge legado

## Regras preservadas

- Authoring referencia `ActorAttributeDefinitionAsset`, não string manual.
- `attributeId` interno da definition existe para logs/save/snapshot futuro.
- Profile valida `definition`, duplicidade, `minValue > maxValue` e `initialValue` fora do range.
- Clamp runtime pertence ao estado/comando; não mascara config inválida.
- Endpoint não decide lifecycle global.
- Pipeline futuro decidirá quando chamar setup/release/reset.
