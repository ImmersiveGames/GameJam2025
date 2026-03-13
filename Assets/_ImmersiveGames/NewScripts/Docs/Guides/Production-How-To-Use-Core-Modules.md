# Production How-To Use Core Modules

## O que este guia cobre

Este guia mostra o uso operacional atual da superficie publica real do runtime.

Ele cobre:
- servicos publicos que voce realmente chama
- assets canonicos que voce realmente configura
- receitas do zero para rota, style, level e rearm
- loading de producao do macro flow
- contratos atuais de intro, post game, level e rearm
- erros comuns de configuracao e de chamada

Ele nao promove como API principal:
- classes de composition root
- tooling `Dev`, `DevQA` ou `Editor`
- detalhes internos de pipeline
- contratos historicos fora da superficie publica atual

## Comece por aqui

| Quero fazer isto | Use isto | Tipo esperado | Exemplo curto real |
|---|---|---|---|
| Abrir o gameplay | `ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)` | `Task` | `await levelFlow.StartGameplayDefaultAsync("Menu/PlayButton", cancellationToken);` |
| Reiniciar a run | `IGameCommands.RequestRestart(reason)` ou `IPostLevelActionsService.RestartLevelAsync(reason, ct)` | `void` ou `Task` | `gameCommands.RequestRestart("Pause/RestartButton");` |
| Ir para o menu | `IGameNavigationService.ExitToMenuAsync(reason)` ou `IPostLevelActionsService.ExitToMenuAsync(reason, ct)` | `Task` | `await navigation.ExitToMenuAsync("Pause/ExitToMenu");` |
| Trocar para o proximo level | `IPostLevelActionsService.NextLevelAsync(reason, ct)` | `Task` | `await postLevelActions.NextLevelAsync("PostGame/NextLevel", cancellationToken);` |
| Trocar para um level especifico | `ILevelFlowRuntimeService.SwapLevelLocalAsync(levelRef, reason, ct)` | `Task` | `await levelFlow.SwapLevelLocalAsync(levelRef, "UI/SelectLevel", cancellationToken);` |
| Rearm local de atores | `IActorGroupRearmOrchestrator.RequestResetAsync(request)` | `Task<bool>` | `await actorGroupRearm.RequestResetAsync(request);` |
| Fechar ou pular intro atual | `IIntroStageControlService.CompleteIntroStage(reason)` | `void` | `introStageControl.CompleteIntroStage("Intro/ContinueButton");` |
| Atualizar a HUD de loading | `ILoadingPresentationService.SetProgress(signature, snapshot)` | `void` | `loadingPresentation.SetProgress(signature, snapshot);` |

## Como pensar o fluxo atual

- `startup` pertence ao bootstrap.
- `frontend` e `gameplay` pertencem a `SceneRouteKind`.
- Navigation resolve `routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolve `profileRef + useFade`.
- `SceneTransitionProfile` e asset leaf visual.
- `LoadingHudScene` e a HUD canonica de loading do macro flow.
- `ILoadingPresentationService` cuida apenas da apresentacao de loading.
- `IntroStage` e level-owned e opcional.
- `PostGame` e global.
- O level atual pode apenas complementar o `PostGame` global com um hook opcional.
- `Restart` nao passa por esse hook.
- `ActorGroupRearm` e o trilho canonico de rearm local.

## Servicos publicos que voce realmente chama

### `ILevelFlowRuntimeService`

Assinatura real:

```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;

public interface ILevelFlowRuntimeService
{
    Task StartGameplayDefaultAsync(string reason = null, CancellationToken ct = default);
    Task RestartLastGameplayAsync(string reason = null, CancellationToken ct = default);
    Task SwapLevelLocalAsync(LevelDefinitionAsset levelRef, string reason = null, CancellationToken ct = default);
}
```

Use quando:
- o usuario apertou Play
- voce quer retomar gameplay pela entrada padrao
- voce quer trocar de level sem transicao macro

### `IPostLevelActionsService`

Assinatura real:

```csharp
using System.Threading;
using System.Threading.Tasks;

public interface IPostLevelActionsService
{
    Task RestartLevelAsync(string reason = null, CancellationToken ct = default);
    Task NextLevelAsync(string reason = null, CancellationToken ct = default);
    Task ExitToMenuAsync(string reason = null, CancellationToken ct = default);
}
```

Use quando:
- um botao do contexto atual precisa pedir restart
- o usuario quer ir para o proximo level da colecao atual
- o usuario quer sair para menu a partir do contexto atual

Observacao:
- este servico continua sendo API de acao do contexto atual
- ele nao transforma `PostGame` em stage owned por level

### `IGameNavigationService`

Assinatura real:

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

public interface IGameNavigationService
{
    Task GoToMenuAsync(string reason = null);
    Task RestartAsync(string reason = null);
    Task ExitToMenuAsync(string reason = null);
    SceneRouteId ResolveGameplayRouteIdOrFail();
    Task StartGameplayRouteAsync(SceneRouteId routeId, SceneTransitionPayload payload = null, string reason = null);
    Task NavigateAsync(GameNavigationIntentKind intent, string reason = null);
}
```

Use quando:
- o fluxo e macro e voce quer navegar explicitamente para menu
- voce quer sair do gameplay para menu
- voce quer um caminho avancado de gameplay por `SceneRouteId`

### `IGameCommands`

Assinatura real:

```csharp
public interface IGameCommands
{
    void RequestPause(string reason = null);
    void RequestResume(string reason = null);
    void RequestVictory(string reason);
    void RequestDefeat(string reason);
    void RequestRestart(string reason);
    void RequestExitToMenu(string reason);
}
```

Use quando:
- a UI precisa pedir pause ou resume
- um system precisa pedir `Victory` ou `Defeat`
- um botao de pause quer restart ou exit to menu

### `IActorGroupRearmOrchestrator`

Assinatura real:

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core;

public interface IActorGroupRearmOrchestrator
{
    bool IsResetInProgress { get; }
    Task<bool> RequestResetAsync(ActorGroupRearmRequest request);
}
```

Use quando:
- voce precisa rearm local de gameplay sem navegar de macro route
- voce quer resetar grupos por `ActorKind`
- voce quer um reset tecnico por `ActorIdSet`

### `IIntroStageControlService`

Use quando uma UI ou system precisa concluir ou pular a intro atual.

```csharp
using _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Runtime;

introStageControl.CompleteIntroStage("Intro/ContinueButton");
```

### `ILoadingPresentationService`

Use quando um owner de pipeline precisa apenas apresentar loading no macro flow atual.

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Runtime;

public interface ILoadingPresentationService
{
    Task EnsureReadyAsync(string signature);
    void Show(string signature, string phase, string message = null);
    void Hide(string signature, string phase);
    void SetMessage(string signature, string message, string phase = null);
    void SetProgress(string signature, LoadingProgressSnapshot snapshot);
}
```

Use quando:
- um owner de fluxo precisa garantir que a `LoadingHudScene` esteja carregada
- o pipeline precisa mostrar ou esconder a HUD sem delegar ownership
- voce quer empurrar `LoadingProgressSnapshot` para barra, porcentagem, etapa e spinner

Nao use para:
- decidir se a transicao macro deve acontecer
- substituir `SceneTransitionService`, `WorldLifecycle` ou `LevelFlow`
- inventar progresso por tempo

## Assets canonicos atuais

Paths canonicos confirmados em `Assets/Resources/**`:
- `Assets/Resources/NewScriptsBootstrapConfig.asset`
- `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Startup.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Frontend.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_FrontendNoFade.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_Gameplay.asset`
- `Assets/Resources/SceneFlow/Styles/TransitionStyle_GameplayNoFade.asset`
- `Assets/Resources/SceneFlow/LevelCollectionAsset.asset`

Outros assets canonicos usados por esses arquivos:
- `SceneRouteDefinitionAsset`
- `LevelDefinitionAsset`
- `SceneTransitionProfile`

Asset visual canonico do loading:
- `Assets/_ImmersiveGames/Scenes/LoadingHudScene.unity`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Assets/LoadingSpinner.png`

## Contratos publicos de configuracao

### `NewScriptsBootstrapConfigAsset`

Campos principais:
- `navigationCatalog`
- `sceneRouteCatalog`
- `startupTransitionStyleRef`
- `fadeSceneKey`

O que esperar:
- startup sempre vem do bootstrap
- `startupTransitionStyleRef` e obrigatorio e precisa apontar para um `TransitionStyleAsset` com `profileRef` valido

### `GameNavigationCatalogAsset`

Cada slot core ou entry extra guarda:
- `routeRef`
- `transitionStyleRef`

Slots core observados hoje:
- `menu`
- `gameplay`
- `gameOver`
- `victory`
- `restart`
- `exitToMenu`

### `SceneRouteDefinitionAsset`

Campos principais:
- `routeId`
- `scenesToLoadKeys`
- `scenesToUnloadKeys`
- `targetActiveSceneKey`
- `routeKind`
- `requiresWorldReset`
- `levelCollection`

Regras atuais:
- rota `Gameplay` exige `requiresWorldReset=true`
- rota `Gameplay` exige `levelCollection`
- rota `Frontend` exige `requiresWorldReset=false`
- rota `Frontend` nao pode ter `levelCollection`

### `TransitionStyleAsset`

Campos principais:
- `profileRef`
- `useFade`

O que esperar:
- `profileRef` e obrigatorio
- o runtime resolve styles por referencia direta ao asset
- `styleLabel` e `profileLabel` servem para observabilidade, nao para semantica de fluxo

### `LevelCollectionAsset`

Campos principais:
- `levels`
- `enforceIndex0AsDefault`

O que esperar:
- `levels[0]` e o level default
- nao pode ter entradas nulas
- nao pode repetir o mesmo `LevelDefinitionAsset`

### `LevelDefinitionAsset`

Campos relevantes para o fluxo atual:
- `additiveScenes`
- `hasIntroStage`
- `hasPostGameReactionHook`
- `allowLocalCurtainIn`
- `allowLocalCurtainOut`

O que esperar:
- `additiveScenes` e obrigatorio e nao pode ficar vazio
- a intro e apenas opcional
- o post hook e apenas complementar ao `PostGame` global

## Loading de producao no macro flow

### O que e a `LoadingHudScene`

`LoadingHudScene` e a HUD canonica de loading do macro flow atual. Ela entra como cena aditiva de apresentacao para cobrir:
- `startup`
- `menu -> gameplay`
- `gameplay -> menu`
- `restart macro`

Ela mostra:
- barra de progresso
- porcentagem numerica
- etapa atual
- spinner visual

### O que ela faz

- apresenta visualmente o estado do loading
- recebe progresso hibrido pelo `ILoadingPresentationService`
- fica pronta sob demanda e depois permanece carregada para `Show/Hide`

### O que ela NAO faz

- nao decide a navegacao
- nao executa reset de mundo
- nao prepara level
- nao substitui fade, gates ou transition
- nao usa progresso fake baseado so em tempo

### Como o progresso funciona hoje

O progresso e hibrido:
- parte real: operacoes assincronas de load e unload de cena no `SceneTransitionService` via `SceneFlowRouteLoadingProgressEvent`
- parte por marcos: `LoadingProgressOrchestrator` fecha o restante por etapas ponderadas de reset, prepare e finalizacao

Pesos observados hoje:
- gameplay:
  - operacoes de rota: `0.55`
  - prepare de level: `0.15`
  - reset de mundo: `0.20`
  - finalizacao: `0.05`
  - fechamento real em `1.00`
- frontend e startup:
  - operacoes de rota: `0.80`
  - finalizacao: `0.15`
  - fechamento real em `1.00`

### Binding obrigatorio da HUD

O `LoadingHudController` atual exige:
- `Canvas`
- `CanvasGroup`
- `TMP_Text loadingText`
- `TMP_Text progressPercentText`
- `Image progressFillImage`
- `GameObject spinnerVisual`
- `RectTransform spinnerTransform`

O que esperar:
- se qualquer referencia obrigatoria faltar, o binding falha cedo
- se a `LoadingHudScene` nao estiver no build ou o root nao existir, o servico falha explicitamente

### Passo a passo

1. `SceneTransitionService` continua owner da timeline macro.
2. `LoadingHudOrchestrator` observa o inicio da transicao.
3. O `ILoadingPresentationService` garante a `LoadingHudScene` pronta e mostra a HUD.
4. `LoadingProgressOrchestrator` empurra snapshots de progresso.
5. A HUD atualiza barra, porcentagem, etapa e spinner.
6. Ao final real da transicao, a HUD vai para `100%`, mostra `Ready` e some.

### Erro comum

Tratar o loading como dono do fluxo ou esperar progresso puramente temporal. Hoje a HUD so apresenta; a decisao continua em `SceneFlow + WorldLifecycle + LevelFlow`.

## Receita: criar uma rota nova do zero

### O que precisa existir
- um `SceneRouteCatalogAsset`
- `SceneKeyAsset` para as cenas que a rota vai carregar ou descarregar
- se a rota for gameplay, uma `LevelCollectionAsset`

### O que configurar
1. Crie um `SceneRouteDefinitionAsset`.
2. Preencha `routeId` com um id novo e estavel.
3. Preencha `scenesToLoadKeys`, `scenesToUnloadKeys` e `targetActiveSceneKey`.
4. Escolha `routeKind`.
5. Se `routeKind == Gameplay`, marque `requiresWorldReset=true` e aponte `levelCollection`.
6. Adicione esse route asset em `SceneRouteCatalogAsset.routeDefinitions`.

### O que chamar
Voce nao chama a rota diretamente no asset. Em runtime, use `IGameNavigationService` ou `ILevelFlowRuntimeService`.

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Navigation;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;

public async Task OpenKnownGameplayRouteAsync(IGameNavigationService navigation)
{
    SceneRouteId routeId = navigation.ResolveGameplayRouteIdOrFail();
    await navigation.StartGameplayRouteAsync(routeId, payload: null, reason: "Menu/PlayButton");
}
```

### O que esperar
- a rota fica resolvivel pelo catalogo
- `SceneRouteCatalogAsset` valida policy de gameplay/frontend
- rotas invalidas falham cedo

### Erro comum
Tratar `Frontend` como se pudesse ter `levelCollection`. Hoje isso falha na validacao do route asset.

## Receita: criar um `TransitionStyleAsset` novo do zero

### O que precisa existir
- um `SceneTransitionProfile`
- um path de assets coerente, normalmente em `Assets/Resources/SceneFlow/Styles/`

### O que configurar
1. Crie um `TransitionStyleAsset`.
2. Preencha `profileRef` com um `SceneTransitionProfile` valido.
3. Escolha `useFade`.
4. Dê um nome claro ao asset, por exemplo `TransitionStyle_BossIntro`.

### O que chamar
Nada em runtime para registrar o style. O uso do style acontece quando ele e referenciado por `GameNavigationCatalogAsset` ou `startupTransitionStyleRef`.

### O que esperar
- o runtime resolve `profileRef + useFade` a partir da referencia direta
- nomes de style e profile ajudam em logs, nao definem a semantica do fluxo

### Erro comum
Criar o asset e esquecer `profileRef`. Hoje isso falha em `ToDefinitionOrFail(...)`.

## Receita: ligar esse style numa rota

### O que precisa existir
- uma `SceneRouteDefinitionAsset`
- um `TransitionStyleAsset`
- o `GameNavigationCatalogAsset`

### O que configurar
1. Abra `GameNavigationCatalogAsset`.
2. Se a navegacao for core, preencha o slot correspondente com:
   - `routeRef`
   - `transitionStyleRef`
3. Se for uma intent extra, adicione uma nova entry em `routes`.

### O que chamar
A navegacao em runtime continua vindo do servico:

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Navigation;

public async Task GoToMenuAsync(IGameNavigationService navigation)
{
    await navigation.GoToMenuAsync("Frontend/CloseModal");
}
```

### O que esperar
- a route e o style saem do catalogo canonico
- o runtime aplica a transicao com o style referenciado

### Erro comum
Configurar `routeRef` e deixar `transitionStyleRef` nulo. O catalogo falha cedo e nao trata isso como opcional.

## Receita: configurar `startupTransitionStyleRef`

### O que precisa existir
- `Assets/Resources/NewScriptsBootstrapConfig.asset`
- um `TransitionStyleAsset` valido

### O que configurar
1. Abra `NewScriptsBootstrapConfig.asset`.
2. Preencha `startupTransitionStyleRef` com o style de startup.
3. Confirme que o style aponta para um `SceneTransitionProfile` valido.

### O que chamar
Nada. O bootstrap usa esse campo automaticamente.

### O que esperar
- a transicao inicial sai do bootstrap
- nao existe semantica de startup em `RouteKind`

### Erro comum
Tentar configurar startup pelo catalogo de navigation. Hoje `startup` nao pertence a `RouteKind`; pertence ao bootstrap.

## Receita: criar um level novo do zero

### O que precisa existir
- uma ou mais `SceneBuildIndexRef` validas
- um `LevelDefinitionAsset`

### O que configurar
1. Crie um `LevelDefinitionAsset`.
2. Preencha `additiveScenes` com as cenas do level.
3. Ligue ou desligue `hasIntroStage`.
4. Ligue ou desligue `hasPostGameReactionHook`.
5. Ajuste `allowLocalCurtainIn` e `allowLocalCurtainOut` se o level exigir.

### O que chamar
Nada no asset. O level entra no fluxo quando estiver dentro de uma `LevelCollectionAsset` usada por uma rota `Gameplay`.

### O que esperar
- `TryValidateRuntime(...)` exige `additiveScenes` nao vazio e sem duplicatas
- o level vira selecionavel pelo trilho de gameplay

### Erro comum
Criar o level sem `additiveScenes`. Hoje isso invalida o asset imediatamente.

## Receita: colocar esse level numa `LevelCollection`

### O que precisa existir
- uma `LevelCollectionAsset`
- um ou mais `LevelDefinitionAsset`

### O que configurar
1. Abra `LevelCollectionAsset`.
2. Adicione os levels em `levels`.
3. Coloque o level default em `levels[0]`.
4. Nao repita referencias.

### O que chamar
Nada. A colecao e consumida pelo prepare de gameplay e pelo swap local.

### O que esperar
- `levels[0]` vira o default usado por `LevelMacroPrepareService`
- a rota de gameplay passa a ter fonte unica de levels

### Erro comum
Colocar o mesmo `LevelDefinitionAsset` duas vezes. Hoje a colecao falha na validacao runtime.

## Receita: adicionar uma `IntroStage` ao level

### O que precisa existir
- um `LevelDefinitionAsset`
- o fluxo atual de gameplay passando por `ILevelFlowRuntimeService.StartGameplayDefaultAsync(...)`

### O que configurar
1. No `LevelDefinitionAsset`, ligue `hasIntroStage`.
2. Garanta que o level esteja dentro da `LevelCollectionAsset` da rota de gameplay.
3. Se voce estiver em build de desenvolvimento, pode usar o mock atual de intro para validar o fluxo.

### O que chamar
```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task StartGameplayAsync(ILevelFlowRuntimeService levelFlow, CancellationToken cancellationToken)
{
    await levelFlow.StartGameplayDefaultAsync("Menu/PlayButton", cancellationToken);
}
```

### O que esperar
- `LevelMacroPrepareService` seleciona o level
- `LevelStageOrchestrator` consulta o contrato atual do level
- se `hasIntroStage=true`, a intro roda antes do gameplay
- se `hasIntroStage=false`, o fluxo segue sem erro

### Erro comum
Esperar que a intro seja global ou por rota. Hoje ela e level-owned e opcional por `LevelDefinitionAsset`.

## Receita: adicionar hook opcional de post ao level

### O que precisa existir
- um `LevelDefinitionAsset`
- o fluxo global de `PostGame` ativo

### O que configurar
1. No `LevelDefinitionAsset`, ligue `hasPostGameReactionHook`.
2. Nao tente trocar o owner do `PostGame`.
3. Trate essa flag como permissao para a reacao opcional do level atual.

### O que chamar
Nada diretamente para o hook. O `PostGame` global continua sendo dono da entrada e da saida do pos-run.

### O que esperar
- `Victory`, `Defeat` e `Exit` continuam globais
- o level atual pode complementar esse fluxo com reacao visual
- `Restart` segue direto para reset/restart e nao passa por esse hook

### Erro comum
Tratar esse hook como um `PostStage` por level. Hoje isso nao existe no contrato atual.

## Receita: fazer um ator participar do `ActorGroupRearm`

### O que precisa existir
- um ator na cena de gameplay
- o ator implementando `IActorGroupRearmable` ou `IActorGroupRearmableSync`
- quando necessario, um `IActorKindProvider`

### O que configurar
1. No componente do ator, implemente o contrato de rearm.
2. Se o ator participa de `ByActorKind`, garanta classificacao por `ActorKind`.
3. Mantenha as tres etapas coerentes: `Cleanup`, `Restore`, `Rebind`.

### O que chamar
Exemplo do participante:

```csharp
using System.Threading.Tasks;
using UnityEngine;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core;

public sealed class SampleEnemyRearm : MonoBehaviour, IActorGroupRearmable
{
    public Task ResetCleanupAsync(ActorGroupRearmContext ctx)
    {
        return Task.CompletedTask;
    }

    public Task ResetRestoreAsync(ActorGroupRearmContext ctx)
    {
        return Task.CompletedTask;
    }

    public Task ResetRebindAsync(ActorGroupRearmContext ctx)
    {
        return Task.CompletedTask;
    }
}
```

Exemplo do request:

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.ActorGroupRearm.Core;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.Actors.Core;

public async Task SoftResetPlayersAsync(IActorGroupRearmOrchestrator actorGroupRearm)
{
    ActorGroupRearmRequest request = ActorGroupRearmRequest.ByActorKind(ActorKind.Player, "Gameplay/SoftReset");
    bool applied = await actorGroupRearm.RequestResetAsync(request);
}
```

### O que esperar
- `ByActorKind` e o trilho principal
- `ActorIdSet` fica para casos tecnicos especificos
- `RequestResetAsync(...)` devolve `bool` indicando se o request foi aplicado

### Erro comum
Usar `ActorIdSet` por padrao quando `ByActorKind` ja resolveria o caso.

## Receita: disparar gameplay

### O que precisa existir
- bootstrap valido
- `GameNavigationCatalogAsset`
- `SceneRouteCatalogAsset`
- rota de gameplay com `levelCollection`

### O que configurar
- confirme `menuSlot` e `gameplaySlot` no `GameNavigationCatalogAsset`
- confirme `startupTransitionStyleRef` no bootstrap

### O que chamar
```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task OnPlayPressedAsync(ILevelFlowRuntimeService levelFlow, CancellationToken cancellationToken)
{
    await levelFlow.StartGameplayDefaultAsync("Menu/PlayButton", cancellationToken);
}
```

### O que esperar
- SceneFlow entra na rota de gameplay
- o loading de producao entra como HUD de apresentacao do macro flow
- level default e preparado
- intro opcional roda se o level atual expuser intro
- gameplay entra em `Playing`

## Receita: disparar restart

### O que precisa existir
- contexto atual de gameplay ou pos-run

### O que configurar
- use `reason` clara para rastreio

### O que chamar
Opcao direta por comando global:

```csharp
using _ImmersiveGames.NewScripts.Modules.GameLoop.Commands;

public void OnRestartPressed(IGameCommands gameCommands)
{
    gameCommands.RequestRestart("Pause/RestartButton");
}
```

Opcao pelo contexto atual:

```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task OnRestartPressedAsync(IPostLevelActionsService postLevelActions, CancellationToken cancellationToken)
{
    await postLevelActions.RestartLevelAsync("PostGame/RestartButton", cancellationToken);
}
```

### O que esperar
- o fluxo publica `GameResetRequestedEvent`
- o loading de producao cobre o restart macro quando esse trilho entra em macro transition
- `Restart` nao passa pelo post hook do level
- a run reinicia pelo trilho de reset/restart

## Receita: disparar exit to menu

### O que precisa existir
- contexto atual capaz de sair para menu

### O que configurar
- garanta que `exitToMenu` no `GameNavigationCatalogAsset` aponte para menu

### O que chamar
Opcao de navigation:

```csharp
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.Navigation;

public async Task OnExitPressedAsync(IGameNavigationService navigation)
{
    await navigation.ExitToMenuAsync("Pause/ExitToMenu");
}
```

Opcao pelo contexto atual:

```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task OnExitPressedAsync(IPostLevelActionsService postLevelActions, CancellationToken cancellationToken)
{
    await postLevelActions.ExitToMenuAsync("PostGame/ExitToMenu", cancellationToken);
}
```

### O que esperar
- o fluxo publica `GameExitToMenuRequestedEvent`
- o loading de producao cobre a saida macro para menu
- o `PostGame` global formaliza `Exit` quando aplicavel
- se o level atual expuser hook opcional, ele apenas complementa a saida global

## Receita: disparar next level

### O que precisa existir
- contexto atual de gameplay dentro de uma `LevelCollectionAsset`

### O que configurar
- garanta que a colecao atual tenha o proximo level configurado

### O que chamar
```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task OnNextLevelPressedAsync(IPostLevelActionsService postLevelActions, CancellationToken cancellationToken)
{
    await postLevelActions.NextLevelAsync("PostGame/NextLevel", cancellationToken);
}
```

### O que esperar
- o servico resolve o proximo `LevelDefinitionAsset` na colecao atual
- o fluxo aplica a troca local sem promover nova macro transition

## Receita: disparar swap local

### O que precisa existir
- um `LevelDefinitionAsset levelRef` que faca parte da `LevelCollectionAsset` atual
- snapshot atual de gameplay valido

### O que configurar
- o level alvo precisa estar dentro da colecao da rota atual

### O que chamar
```csharp
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Config;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

public async Task SelectLevelAsync(
    ILevelFlowRuntimeService levelFlow,
    LevelDefinitionAsset levelRef,
    CancellationToken cancellationToken)
{
    await levelFlow.SwapLevelLocalAsync(levelRef, "UI/SelectLevel", cancellationToken);
}
```

### O que esperar
- `LevelSelectedEvent` e publicado
- `ResetLevelAsync(...)` roda para o level alvo
- `LevelSwapLocalAppliedEvent` fecha a troca local

## Erros comuns por assunto

### Navigation
Erro comum:
- tentar navegar mudando asset diretamente em runtime em vez de usar `IGameNavigationService` ou `ILevelFlowRuntimeService`

O que fazer:
- configure assets no editor
- em runtime, chame servicos publicos com `reason` clara

### Styles
Erro comum:
- tratar nome de style ou nome de profile como se definissem a semantica do fluxo

O que fazer:
- trate `TransitionStyleAsset` como owner estrutural
- use nomes apenas para leitura humana e logs

### Loading
Erro comum:
- esperar que a HUD de loading substitua o pipeline ou que a porcentagem venha so de tempo

O que fazer:
- trate `LoadingHudScene` como apresentacao do macro flow
- deixe `SceneTransitionService`, `WorldLifecycle` e `LevelFlow` decidirem o pipeline
- use o progresso hibrido atual

### Level
Erro comum:
- criar `LevelDefinitionAsset` sem `additiveScenes` ou esquecer de colocá-lo na `LevelCollectionAsset`

O que fazer:
- valide `additiveScenes`
- confirme que o level esta na colecao usada pela rota de gameplay

### Intro
Erro comum:
- esperar que a intro seja global ou que toda rota de gameplay sempre tenha intro

O que fazer:
- habilite `hasIntroStage` apenas no `LevelDefinitionAsset` que precisa disso
- deixe o restante seguir direto para gameplay

### Post hook
Erro comum:
- tentar substituir `PostGame` global por comportamento do level

O que fazer:
- use a flag `hasPostGameReactionHook` apenas para complementar `Victory`, `Defeat` ou `Exit`
- mantenha `Restart` fora desse fluxo

### Rearm
Erro comum:
- usar `ActorIdSet` como primeira escolha

O que fazer:
- comece por `ActorGroupRearmRequest.ByActorKind(...)`
- use `ActorIdSet` so quando o alvo tecnico exigir ids explicitos

## Checklist de producao

- bootstrap com `navigationCatalog`, `sceneRouteCatalog`, `startupTransitionStyleRef` e `fadeSceneKey`
- `GameNavigationCatalogAsset` com slots core validos
- rota `Gameplay` com `levelCollection`
- `TransitionStyleAsset` com `profileRef`
- `LoadingHudScene` presente no build e binding da HUD completo
- `LevelCollectionAsset` com levels ordenados e sem duplicatas
- `LevelDefinitionAsset` com `additiveScenes` validas
- flags corretas de `hasIntroStage` e `hasPostGameReactionHook`
- atores por grupo com `IActorKindProvider` quando necessario
- componentes de rearm com `IActorGroupRearmable` ou `IActorGroupRearmableSync`
- `reason` clara e estavel nas chamadas publicas

## Resumo final

Se voce quer usar os modulos principais em producao hoje:
- configure bootstrap, routes, styles e levels nos assets canonicos
- use `ILevelFlowRuntimeService` para abrir gameplay e trocar level local
- use `IGameNavigationService` para menu e navegacao macro
- use `IGameCommands` e `IPostLevelActionsService` para comandos de run e acoes do contexto atual
- trate `LoadingHudScene` como HUD canonica do macro flow, sempre em apresentacao apenas
- use `IActorGroupRearmOrchestrator` para rearm local de atores
- use os hooks operacionais para integrar UI e systems
- mantenha `IntroStage` level-owned e `PostGame` global
