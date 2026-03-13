# Event Hooks Reference

Esta referencia lista os hooks reais do runtime atual e como usa-los em producao.

## Regra simples

- hooks operacionais: primeira escolha para UI, gameplay e systems
- hooks tecnicos: existem no runtime, mas nao sao a primeira escolha de integracao
- `Exit` continua resultado formal do `PostGame` global, mas nao tem evento operacional promoted dedicado
- `Restart` nao passa por post hook

## Como assinar um hook

Exemplo base:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameRunEndedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameRunEndedEvent>(OnRunEnded);
    EventBus<GameRunEndedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameRunEndedEvent>.Unregister(_binding);
}

private void OnRunEnded(GameRunEndedEvent evt)
{
    // usar evt.Outcome e evt.Reason
}
```

## Mapa rapido

| Se voce quer... | Use este hook | Publisher atual | Use para |
|---|---|---|---|
| saber que a troca de rota terminou | `SceneTransitionCompletedEvent` | `SceneTransitionService` | UI e systems que dependem da rota ja aplicada |
| saber que o reset completo terminou | `WorldLifecycleResetCompletedEvent` | `WorldResetOrchestrator` | systems que precisam do mundo pronto |
| saber que a run comecou | `GameRunStartedEvent` | `GameLoopService` | ligar comportamento de gameplay ativo |
| saber que a run terminou | `GameRunEndedEvent` | `GameRunOutcomeService` | reagir a `Victory` ou `Defeat` |
| saber que um level entrou no fluxo | `LevelSelectedEvent` | `LevelMacroPrepareService` e `LevelSwapLocalService` | UI e systems ligados ao level atual |
| saber que a troca local terminou | `LevelSwapLocalAppliedEvent` | `LevelSwapLocalService` | atualizar HUD e cameras apos swap |
| observar pedido de fim de run | `GameRunEndRequestedEvent` | `GameRunEndRequestService` | auditoria, telemetria e bridges |
| observar restart macro | `GameResetRequestedEvent` | `GameCommands` | ouvir intencao de restart |
| observar saida para menu | `GameExitToMenuRequestedEvent` | `GameCommands` | ouvir intencao de exit |

## Hooks operacionais recomendados

### `SceneTransitionCompletedEvent`

Quem publica:
- `SceneTransitionService`

Quando dispara:
- no fim da transicao macro, com a rota ja aplicada

Para que serve no mundo real:
- liberar UI da rota nova
- atualizar systems que dependem de `RouteKind`
- saber se o jogo ja esta em frontend ou gameplay

Campos uteis:
- `evt.context.RouteId`
- `evt.context.RouteKind`
- `evt.context.TargetActiveScene`
- `evt.context.Reason`
- `evt.context.ContextSignature`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Transition.Runtime;

private EventBinding<SceneTransitionCompletedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
    EventBus<SceneTransitionCompletedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<SceneTransitionCompletedEvent>.Unregister(_binding);
}

private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
{
    if (evt.context.RouteKind != SceneRouteKind.Gameplay)
    {
        return;
    }

    string reason = evt.context.Reason;
    string signature = evt.context.ContextSignature;
}
```

Quando usar:
- quando o seu system precisa reagir ao estado final da troca de rota

Quando nao usar:
- quando voce precisa do inicio tecnico do pipeline; nesse caso o hook e outro

### `WorldLifecycleResetCompletedEvent`

Quem publica:
- `WorldResetOrchestrator`

Quando dispara:
- quando o reset deterministico do mundo concluiu

Para que serve no mundo real:
- soltar systems que esperam o mundo ja resetado
- rearmar logica que depende do ambiente pronto

Campos uteis:
- `evt.ContextSignature`
- `evt.Reason`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.Runtime;

private EventBinding<WorldLifecycleResetCompletedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<WorldLifecycleResetCompletedEvent>(OnResetCompleted);
    EventBus<WorldLifecycleResetCompletedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<WorldLifecycleResetCompletedEvent>.Unregister(_binding);
}

private void OnResetCompleted(WorldLifecycleResetCompletedEvent evt)
{
    string reason = evt.Reason;
    string signature = evt.ContextSignature;
}
```

Quando usar:
- quando o seu system realmente depende do reset completo terminado

Quando nao usar:
- quando basta saber que a rota terminou; nesse caso prefira `SceneTransitionCompletedEvent`

### `GameRunStartedEvent`

Quem publica:
- `GameLoopService`

Quando dispara:
- quando o GameLoop entra em `Playing`

Para que serve no mundo real:
- habilitar HUD de gameplay
- iniciar systems que so fazem sentido com run ativa

Campos uteis:
- `evt.StateId`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameRunStartedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameRunStartedEvent>(OnRunStarted);
    EventBus<GameRunStartedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameRunStartedEvent>.Unregister(_binding);
}

private void OnRunStarted(GameRunStartedEvent evt)
{
    GameLoopStateId stateId = evt.StateId;
}
```

Quando usar:
- quando voce precisa marcar o inicio real da run

Quando nao usar:
- quando voce quer observar a intencao de iniciar gameplay; esse hook nao e um request

### `GameRunEndedEvent`

Quem publica:
- `GameRunOutcomeService`

Quando dispara:
- quando o fim de run terminal foi aceito em `Playing`

Para que serve no mundo real:
- abrir UI de `Victory` ou `Defeat`
- gravar telemetria de fim de run
- desligar systems que so existem durante gameplay ativo

Campos uteis:
- `evt.Outcome`
- `evt.Reason`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameRunEndedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameRunEndedEvent>(OnRunEnded);
    EventBus<GameRunEndedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameRunEndedEvent>.Unregister(_binding);
}

private void OnRunEnded(GameRunEndedEvent evt)
{
    if (evt.Outcome == GameRunOutcome.Victory)
    {
        // mostrar tela de vitoria
    }
}
```

Quando usar:
- quando voce quer reagir ao resultado final da run

Quando nao usar:
- quando voce quer observar apenas o pedido de fim de run; nesse caso use `GameRunEndRequestedEvent`

Observacao:
- este e o hook principal para `Victory` e `Defeat`
- `Exit` continua dentro do `PostGame` global, sem evento operacional dedicated promoted

### `LevelSelectedEvent`

Quem publica:
- `LevelMacroPrepareService`
- `LevelSwapLocalService`

Quando dispara:
- quando um `LevelDefinitionAsset` e selecionado para o fluxo atual

Para que serve no mundo real:
- atualizar HUD com o level atual
- preparar cameras e managers especificos do level
- registrar assinatura atual do level

Campos uteis:
- `evt.LevelRef`
- `evt.MacroRouteId`
- `evt.SelectionVersion`
- `evt.Reason`
- `evt.LevelSignature`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

private EventBinding<LevelSelectedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<LevelSelectedEvent>(OnLevelSelected);
    EventBus<LevelSelectedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<LevelSelectedEvent>.Unregister(_binding);
}

private void OnLevelSelected(LevelSelectedEvent evt)
{
    string levelName = evt.LevelRef != null ? evt.LevelRef.name : "<none>";
    string signature = evt.LevelSignature;
}
```

Quando usar:
- quando o seu system depende do level que entrou no fluxo

Quando nao usar:
- quando voce precisa saber que a troca local terminou de fato; nesse caso prefira `LevelSwapLocalAppliedEvent`

### `LevelSwapLocalAppliedEvent`

Quem publica:
- `LevelSwapLocalService`

Quando dispara:
- no fim de um swap local valido

Para que serve no mundo real:
- atualizar HUD, cameras e binds depois da troca local
- reagir ao level novo sem depender da macro route

Campos uteis:
- `evt.LevelRef`
- `evt.MacroRouteId`
- `evt.SelectionVersion`
- `evt.Reason`
- `evt.LevelSignature`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;

private EventBinding<LevelSwapLocalAppliedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<LevelSwapLocalAppliedEvent>(OnLevelSwapApplied);
    EventBus<LevelSwapLocalAppliedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<LevelSwapLocalAppliedEvent>.Unregister(_binding);
}

private void OnLevelSwapApplied(LevelSwapLocalAppliedEvent evt)
{
    string reason = evt.Reason;
    int selectionVersion = evt.SelectionVersion;
}
```

Quando usar:
- quando o seu system precisa do fim do swap local, nao apenas da selecao do level

Quando nao usar:
- quando o seu objetivo e saber qual level foi escolhido durante prepare; nesse caso `LevelSelectedEvent` basta

### `GameRunEndRequestedEvent`

Quem publica:
- `GameRunEndRequestService`

Quando dispara:
- quando alguem pede `Victory` ou `Defeat`

Para que serve no mundo real:
- observar requests antes do resultado final
- instrumentar bridges e telemetria

Campos uteis:
- `evt.Outcome`
- `evt.Reason`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameRunEndRequestedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameRunEndRequestedEvent>(OnRunEndRequested);
    EventBus<GameRunEndRequestedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameRunEndRequestedEvent>.Unregister(_binding);
}

private void OnRunEndRequested(GameRunEndRequestedEvent evt)
{
    GameRunOutcome outcome = evt.Outcome;
    string reason = evt.Reason;
}
```

Quando usar:
- quando voce precisa observar a intencao de termino

Quando nao usar:
- quando voce precisa do resultado final consolidado; nesse caso use `GameRunEndedEvent`

### `GameResetRequestedEvent`

Quem publica:
- `GameCommands`

Quando dispara:
- quando alguem pede restart macro

Para que serve no mundo real:
- observar intencao de restart
- integrar analytics ou UI que precisa fechar antes do restart

Campo util:
- `evt.Reason`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameResetRequestedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameResetRequestedEvent>(OnResetRequested);
    EventBus<GameResetRequestedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameResetRequestedEvent>.Unregister(_binding);
}

private void OnResetRequested(GameResetRequestedEvent evt)
{
    string reason = evt.Reason;
}
```

Quando usar:
- quando voce so precisa ouvir o request de restart

Quando nao usar:
- quando voce quer tratar pos-run; `Restart` nao passa por post hook

### `GameExitToMenuRequestedEvent`

Quem publica:
- `GameCommands`

Quando dispara:
- quando alguem pede sair para menu

Para que serve no mundo real:
- fechar overlays locais
- telemetria de saida voluntaria

Campo util:
- `evt.Reason`

Mini exemplo real:

```csharp
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;

private EventBinding<GameExitToMenuRequestedEvent> _binding;

private void OnEnable()
{
    _binding = new EventBinding<GameExitToMenuRequestedEvent>(OnExitToMenuRequested);
    EventBus<GameExitToMenuRequestedEvent>.Register(_binding);
}

private void OnDisable()
{
    EventBus<GameExitToMenuRequestedEvent>.Unregister(_binding);
}

private void OnExitToMenuRequested(GameExitToMenuRequestedEvent evt)
{
    string reason = evt.Reason;
}
```

Quando usar:
- quando voce quer ouvir a intencao de sair para menu

Quando nao usar:
- quando voce precisa saber que a rota de menu ja entrou; nesse caso prefira `SceneTransitionCompletedEvent`

## Hooks tecnicos do pipeline

- `SceneTransitionStartedEvent`
- `SceneTransitionFadeInCompletedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `WorldLifecycleResetStartedEvent`
- `InputModeRequestEvent`

Use esses hooks apenas quando o caso realmente depender do ponto tecnico do pipeline.

## O que nao existe como hook operacional principal

- nao existe hook publico promoted para o hook opcional de post por level; ele e resolvido pelo contrato interno do level atual
- nao existe post stage generico por level
- `Restart` nao passa por post hook

## O que ficou de fora

| Item | Classificacao | Motivo |
|---|---|---|
| eventos `Dev` e `DevQA` | `DEV_QA_ONLY` | nao sao integracao de producao |
| eventos de editor | `EDITOR_ONLY` | nao sao runtime de producao |
| eventos V2 de reset | `OBSERVABILITY_ONLY` | telemetria, nao hook operacional principal |
| eventos internos sem uso de integracao publica | `INTERNAL_PIPELINE` | nao ajudam integracao operacional |