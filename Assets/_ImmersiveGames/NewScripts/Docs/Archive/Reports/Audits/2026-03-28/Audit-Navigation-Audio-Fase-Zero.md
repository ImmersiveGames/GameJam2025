# Audit - Navigation / Audio, fase zero

Data: 2026-03-28  
Status: Draft

## Base lida

- `Modules/Navigation/GameNavigationService.cs`
- `Modules/Navigation/GameNavigationCatalogAsset.cs`
- `Modules/Audio/Bootstrap/AudioRuntimeComposer.cs`
- `Modules/Audio/Interop/NavigationLevelRouteBgmBridge.cs`
- `Modules/Audio/Runtime/AudioEntitySemanticService.cs`
- `Modules/Audio/Config/EntityAudioSemanticMapAsset.cs`
- `Modules/LevelFlow/Config/LevelDefinitionAsset.cs`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`
- `Infrastructure/Config/BootstrapConfigAsset.cs`

## Resumo executivo

- `Navigation` hoje e, de fato, um núcleo de resolucao de intent e dispatch para `SceneFlow`.
- O núcleo real de `Navigation` nao inclui audio, UI, loading, fade ou lifecycle de level.
- O acoplamento com `Audio` acontece por metadados de BGM espalhados em tres lados: `LevelFlow`, `SceneFlow` e `Navigation`.
- O runtime de audio mantem o core standalone, mas a bridge de BGM depende de um catalogo concreto de `Navigation`.
- `EntityAudioSemanticMapAsset` e util para `EntityAudio`, mas hoje mistura semantica, routing de cue e defaults de emissao/execucao/voz.

## O que pertence ao core de `Navigation`

| Area | Estado atual | Leitura arquitetural |
|---|---|---|
| Intents canonicos | `GameNavigationIntents` | Fonte canonica de ids e aliases oficiais. |
| Resolução de intent | `GameNavigationCatalogAsset` + `GameNavigationService` | Resolve slot/rota/style e falha cedo quando falta configuracao. |
| Dispatch | `GameNavigationService -> ISceneTransitionService` | `Navigation` despacha; nao executa pipeline macro. |
| Guardas | dedupe e validacoes de rota | Protegem contra reentrada e config invalida. |

## O que esta espalhado indevidamente

- BGM de contexto hoje aparece em `LevelDefinitionAsset.BgmCue`, `SceneRouteDefinitionAsset.BgmCue` e `GameNavigationCatalogAsset.CoreIntentSlot/RouteEntry.bgmCueRef`.
- Isso cria tres fontes de verdade para o mesmo problema de contexto musical.
- `Navigation` carrega audio no catalogo, mas `SceneFlow` e `LevelFlow` tambem carregam audio de contexto nas suas definicoes.
- O resultado e uma fronteira de ownership difusa: o audio contextual nao esta claramente em um unico dominio.

## Como `Audio` esta integrado hoje

- `AudioRuntimeComposer` instala o runtime de audio e registra `NavigationLevelRouteBgmBridge`.
- A bridge escuta `SceneTransitionStartedEvent`, `SceneTransitionBeforeFadeOutEvent` e `LevelSwapLocalAppliedEvent`.
- A precedencia atual de resolucao e `level > navigation-intent > route`.
- `Audio` continua standalone no core, mas a integraçao de BGM macro ainda consulta `GameNavigationCatalogAsset` concreto.

## Contratos frágeis

- `AudioRuntimeComposer` faz cast concreto de `bootstrapConfig.NavigationCatalog` para `GameNavigationCatalogAsset`.
- `GameNavigationCatalogAsset` acumula funcao de catalogo de navegacao e fonte de BGM contextual.
- `NavigationLevelRouteBgmBridge` depende de eventos de `SceneFlow` e de dados de `LevelFlow`, entao e um seam valido, mas nao um contrato neutro.
- `EntityAudioSemanticService.SetSemanticMap(...)` torna o mapa mutavel em runtime, o que exige disciplina forte de ownership.
- `EntityAudioSemanticMapAsset.TryResolve(...)` mistura ausencia de mapa, ausencia de entrada e ausencia de cue no mesmo resultado operacional.

## Avaliacao de `EntityAudioSemanticMapAsset`

### Papel atual

- Mapa de `purpose -> cue` para audio semantico de entidade.
- Permite overrides de emissao, execucao, voz, volume e follow target.

### Problemas de modelagem e ownership

- Mistura semantica de negocio com policy tecnica de playback.
- Mistura conveniencia de emitter com decisao de resolucao.
- O asset e usado como mapa semantico, mas os dados de exemplo incluem `semantic_global_*`, o que enfraquece a leitura de "entidade".

### O que ele nao cobre bem

- Nao e um bom owner para BGM de menu.
- Nao e um bom owner para BGM de rota.
- Nao e um bom owner para BGM de level.
- Nao deve virar catalogo generico de audio contextual do jogo.

### Leitura pratica

- Adequado para audio semantico de entidade e SFX contextual local.
- Inadequado como solucao unica para menu, rota, level ou semantica global de navegacao.

## Riscos atuais

- Redundancia cruzada entre `LevelFlow`, `SceneFlow` e `Navigation` para BGM.
- Regressao silenciosa se uma origem de BGM ganhar precedencia sem estar documentada.
- Dupla autoria de contexto musical entre catalogo de navegacao e assets de rota/level.
- Crescimento de bridges ad hoc para compensar ownership pouco claro.
- Leitura errada de `EntityAudioSemanticMapAsset` como catalogo universal de audio.

