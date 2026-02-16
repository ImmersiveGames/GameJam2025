# DataCleanup v1 — Step 04 (ProfileCatalog validation-only)

## Objetivo
Tornar o `SceneTransitionProfileCatalogAsset` estritamente de validação/tooling e remover qualquer resolução de profile em runtime via `profileId`.

## O que mudou
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`
  - Runtime agora exige `profileRef` direto por style.
  - `profileId` permanece como metadado obrigatório de configuração/validação.
  - Campo `transitionProfileCatalog` foi mantido apenas como legado/editor (compatibilidade YAML), sem uso para lookup em runtime.
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`
  - Validação expandida para **todos** os styles do catálogo.
  - Inclusão de checagens de duplicidade de `styleId`, validade de `profileId`, nulidade de `profileRef`, resolução opcional contra catálogo canônico e aviso de divergência de referência.
  - Relatório em Markdown ganhou seção `## Transition styles` com tabela de status por style.

## Impacto
- O runtime não resolve mais `SceneTransitionProfile` via `SceneTransitionProfileCatalogAsset` nem via `profileId`.
- `TransitionStyleCatalogAsset.TryGet(...)` só retorna definições válidas quando `profileRef` direto está presente.
- Configuração inválida continua em fail-fast com `[FATAL][Config]` + exception.

## Como validar
1. Confirmar ausência de lookup runtime via catálogo no `TransitionStyleCatalogAsset`:
   - `rg -n "CollectTransitionProfileCatalog|transitionProfileCatalog|_profileCatalog|TryGetProfile\(" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`
2. Confirmar presença das validações e seção de styles no validator:
   - `rg -n "Transition styles|profileRef|profileId" Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`
3. Unity (manual):
   - Executar menu: `ImmersiveGames/NewScripts/Config/Validate SceneFlow Config (DataCleanup v1)`.
   - Verificar geração do report e ocorrência de FATAL quando existir style sem `profileRef`.
