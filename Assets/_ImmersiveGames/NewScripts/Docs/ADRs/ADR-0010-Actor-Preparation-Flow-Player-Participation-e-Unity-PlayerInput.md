# ADR-0010 - Actor Preparation Flow, Player Participation e Unity PlayerInput

## Status
- Estado: Proposed
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canÃ´nica deste contrato: este ADR, apÃ³s aceite.

## Dependência
- Depende de: ADR-0009 - SessionModeProfile e Session Mode Resolution.
- Este ADR consome o modo de sessão concreto resolvido no SessionOperationalPipeline e detalha Actor Preparation, Player Participation e integração Unity Input como execução técnica.

---

## Contexto

A Base 1.1 jÃ¡ define que o `SessionOperationalPipeline` Ã© o owner do ciclo operacional de sessÃ£o e que o `Session Transition Envelope` possui uma fase explÃ­cita de `SessionOperationalSetup` antes de `BeforeFadeOut` e antes da entrada efetiva da `SessionActivityPipeline`.

Durante a evoluÃ§Ã£o do rail de sessÃ£o, ficou claro que tratar `InputMode` como eixo arquitetural isolado Ã© insuficiente. O estado real de gameplay depende de um conjunto maior de decisÃµes envolvendo actors, players, controles, cÃ¢mera, HUD, readiness, posicionamento, preservaÃ§Ã£o e materializaÃ§Ã£o.

TambÃ©m ficou claro que a montagem principal dos actors necessÃ¡rios para iniciar uma activity nÃ£o deve ser empurrada para a `SessionActivityPipeline`. A activity deve receber um palco jÃ¡ preparado, com actors obrigatÃ³rios materializados ou preservados, posicionados, inicializados, vinculados e validados. A activity passa entÃ£o a comandar transformaÃ§Ãµes in-game, e nÃ£o a montagem inicial da sessÃ£o.

A Base 1.1 precisa formalizar esse corte sem criar um novo pipeline prematuramente e sem introduzir uma central onisciente de actors.

AlÃ©m disso, a evoluÃ§Ã£o de players exige uma restriÃ§Ã£o adicional: o set de players/actors usado para entrar em uma activity nem sempre serÃ¡ definido diretamente pela rota. Em produÃ§Ã£o, o set concreto pode vir de uma tela de seleÃ§Ã£o de players, de uma escolha de personagem, de loadout, de save/profile ou de join dinÃ¢mico via Unity Input System.

Portanto, a rota operacional pode declarar uma receita default ou um contrato esperado, mas nÃ£o deve ser tratada como dona definitiva do set concreto de players em todos os cenÃ¡rios.

---

## DecisÃ£o

Adota-se o **Actor Preparation Flow** como parte do `SessionOperationalPipeline`.

Nesta etapa da Base 1.1, `Actor Preparation` **nÃ£o Ã© um pipeline prÃ³prio**. Ele Ã© um `Pipeline Stage` operacional dentro do `SessionOperationalPipeline`, executado durante o `SessionOperationalSetup`, antes do `SessionActivityEntryHandoffPrepared`, antes de `BeforeFadeOut` e antes da abertura da cortina.

A `SessionActivityPipeline` recebe os actors preparados e passa a comandar a vida ativa dos actors durante a activity.

A decisÃ£o adicional deste ADR Ã©:

```text
OperationalRouteAsset declara contrato/default de participaÃ§Ã£o.
SessionOperationalPipeline resolve o set concreto.
ActorPreparationStage prepara o set resolvido.
PlayerInput e PlayerInputManager sÃ£o adapters/hooks Unity, nÃ£o owners de lifecycle.
```

---

## 1. Corte principal

A Base 1.1 adota o seguinte corte:

```text
SessionOperationalPipeline
-> ActorPreparationStage
-> SessionActivityEntryHandoffPrepared
-> BeforeFadeOut
-> TransitionCompleted
-> SessionActivityPipeline
-> Actor Activity
```

A leitura canÃ´nica Ã©:

```text
Operational prepara actors.
Activity transforma actors durante gameplay ativo.
Executores aplicam efeitos.
```

---

## 2. Actor Preparation Stage

`ActorPreparationStage` Ã© a fase operacional responsÃ¡vel por preparar os actors necessÃ¡rios para a activity inicial da sessÃ£o.

Essa stage pertence ao `SessionOperationalPipeline` e nÃ£o cria ownership prÃ³prio de lifecycle.

### Responsabilidades

- resolver a participaÃ§Ã£o de actors da sessÃ£o;
- resolver o actor set necessÃ¡rio para a activity inicial;
- materializar, preservar ou rematerializar actors obrigatÃ³rios;
- posicionar actors na cena;
- entregar identidade/contexto aos actors;
- executar setup interno declarado pelos prÃ³prios actors;
- aplicar vÃ­nculos externos obrigatÃ³rios;
- validar readiness dos actors;
- produzir facts, commands, snapshots e handoff data para a activity.

### NÃ£o responsabilidades

- decidir lifecycle de run;
- decidir lifecycle global de session fora do `SessionOperationalPipeline`;
- decidir engagement da activity;
- comandar comportamento in-game contÃ­nuo;
- substituir a `SessionActivityPipeline`;
- virar uma central onisciente de player, input, HUD, camera, enemy, AI e interactables;
- assumir ownership do Unity `PlayerInput` ou `PlayerInputManager`.

---

## 3. Fases conceituais do Actor Preparation Flow

O fluxo conceitual Ã© dividido em fases internas. Essas fases podem virar classes, serviÃ§os ou etapas internas no futuro, mas nÃ£o devem ser tratadas como pipelines prÃ³prios nesta etapa.

### 3.1 Actor Participation

Define quem participa da sessÃ£o.

Exemplos:

- existe player local;
- quantidade de players;
- slots de players ativos;
- player humano, bot ou remoto;
- sessÃ£o com gameplay participation;
- necessidade de controle;
- necessidade de actor jogÃ¡vel;
- players prÃ©-configurados;
- players selecionados em runtime;
- players aguardando join.

### 3.2 Actor Set Resolution

Resolve o elenco necessÃ¡rio para a activity inicial.

Exemplos:

- `PlayerActor` obrigatÃ³rio;
- inimigos iniciais obrigatÃ³rios;
- objetos interativos obrigatÃ³rios;
- companion actors;
- actor-linked camera target;
- actor-linked HUD target;
- actor set opcional.

A resoluÃ§Ã£o do set concreto deve partir de uma fonte explÃ­cita.

Fontes possÃ­veis:

```text
ActorSetDefinitionAsset default
PlayerSelectionSnapshot
Save/Profile/Loadout
Route Command payload
Runtime Join Flow
```

Regra:

```text
ActorSetDefinitionAsset Ã© uma receita default/static.
ResolvedActorSet Ã© o set concreto usado pelo ActorPreparationStage.
```

A rota nÃ£o deve mutar assets em runtime e nÃ£o deve sobrescrever o `ActorSetDefinitionAsset`.

### 3.3 Player Selection Resolution

Quando existir uma tela ou superfÃ­cie de seleÃ§Ã£o de players, ela deve produzir um resultado explÃ­cito.

Nome conceitual:

```text
PlayerSelectionSnapshot
```

ou:

```text
PlayerSelectionResult
```

Esse snapshot pode conter:

- `playerSlotId`;
- `actorDefinition`;
- `actorVariantDefinition`;
- `required`;
- personagem/modelo/skin escolhidos;
- loadout;
- prefab selecionado;
- placement desejado;
- control scheme desejado;
- device binding pretendido;
- join source;
- player index;
- split-screen index;
- flags de readiness de seleÃ§Ã£o.

Regra:

```text
Player selection produz fato/snapshot.
SessionOperationalPipeline valida e consome.
Player selection nÃ£o decide lifecycle da rota nem da activity.
```

Se uma rota exigir seleÃ§Ã£o runtime e a seleÃ§Ã£o nÃ£o existir, a falha deve ser explÃ­cita.

NÃ£o pode haver fallback silencioso para o default da rota quando a policy exigir seleÃ§Ã£o runtime.

### 3.4 Actor Materialization

Cria, preserva ou rematerializa actors necessÃ¡rios.

Exemplos:

- instanciar actor ausente;
- preservar actor compatÃ­vel jÃ¡ existente;
- rematerializar actor incompatÃ­vel;
- remover runtime stale/orphan que bloqueia slot obrigatÃ³rio;
- registrar actor materializado no runtime.

Para players, a materializaÃ§Ã£o pode ser executada de duas formas futuras:

```text
Actor materialization adapter instancia prefab e configura PlayerInput.
PlayerInputManagerAdapter executa join/materializaÃ§Ã£o conforme comando do pipeline.
```

Ambas sÃ£o formas de execuÃ§Ã£o. Nenhuma delas decide lifecycle.

### 3.5 Actor Placement

Posiciona actors na cena apÃ³s materializaÃ§Ã£o ou preservaÃ§Ã£o.

Exemplos:

- resolver spawn point;
- resolver scene marker;
- reposicionar player preservado;
- posicionar enemy inicial;
- posicionar objetos interativos obrigatÃ³rios;
- aplicar orientaÃ§Ã£o inicial.

A Base 1.1 jÃ¡ permite metadata planned-only de placement em `ActorDefinitionAsset`, mas isso ainda nÃ£o significa aplicaÃ§Ã£o real de `Transform`.

### 3.6 Actor Self Setup

Executa preparaÃ§Ã£o interna declarada pelo prÃ³prio actor.

Exemplos:

- inicializar movement;
- inicializar health;
- inicializar interaction;
- inicializar AI;
- preparar estado bloqueado/dormant;
- restaurar estado local obrigatÃ³rio;
- inicializar componentes internos que dependem de identidade/contexto operacional.

O lifecycle normal da Unity pode continuar sendo usado para inicializaÃ§Ã£o local simples do objeto, desde que nÃ£o assuma ownership de session, activity, pipeline ou lifecycle global.

Regra canÃ´nica:

```text
Unity lifecycle inicializa o objeto.
Actor Preparation integra o objeto Ã  sessÃ£o.
```

### 3.7 External Actor Bindings

Aplica vÃ­nculos externos necessÃ¡rios aos actors.

Exemplos:

- input/control binding;
- PlayerInput binding;
- PlayerInputManager join binding;
- HUD binding;
- camera binding;
- audio binding;
- save/state binding;
- target binding;
- interaction prompt binding.

Esses vÃ­nculos cruzam fronteiras de domÃ­nio e devem ser executados por adapters especializados quando necessÃ¡rio.

### 3.8 Unity PlayerInput Integration

`UnityEngine.InputSystem.PlayerInput` representa um player/usuÃ¡rio individual no Input System da Unity.

Na Base 1.1:

```text
PlayerInput Ã© executor tÃ©cnico por player.
```

Ele pode ser usado para:

- action maps por player;
- ativar/desativar input;
- trocar action map;
- receber callbacks de actions;
- reagir a device lost/regained;
- ler control scheme corrente;
- operar uma cÃ³pia privada das actions por player.

Ele nÃ£o pode decidir:

- inÃ­cio/fim da sessÃ£o;
- entrada na activity;
- materializaÃ§Ã£o do actor;
- readiness final;
- lifecycle do pipeline;
- abertura de cortina;
- fallback de player obrigatÃ³rio ausente.

Um futuro `PlayerInputAdapter` pode:

- aplicar `ActivateInput`;
- aplicar `DeactivateInput`;
- aplicar `SwitchCurrentActionMap`;
- escutar callbacks de action;
- escutar device lost/regained;
- transformar eventos Unity em `Pipeline Facts`;
- rejeitar callbacks foreign/stale por identidade.

### 3.9 Unity PlayerInputManager Integration

`UnityEngine.InputSystem.PlayerInputManager` Ã© Ãºtil para multiplayer local, join, player prefab e split-screen.

Na Base 1.1:

```text
PlayerInputManager Ã© adapter/executor de join/materializaÃ§Ã£o Unity.
```

Ele pode:

- habilitar/desabilitar joining;
- executar join conforme comando/policy;
- emitir evento/fato quando player entra/sai;
- instanciar prefab se essa for a strategy escolhida;
- servir como hook para local multiplayer;
- usar player prefab com `PlayerInput`;
- operar split-screen se a policy permitir.

Ele nÃ£o pode decidir:

- se a rota pode iniciar;
- se a activity pode comeÃ§ar;
- se a sessÃ£o estÃ¡ vÃ¡lida;
- se o pipeline deve avanÃ§ar;
- se falta player obrigatÃ³rio;
- qual fallback usar quando configuraÃ§Ã£o obrigatÃ³ria falta.

Um futuro `PlayerInputManagerAdapter` pode:

- receber `EnableJoiningCommand`;
- receber `DisableJoiningCommand`;
- receber `JoinPlayerCommand`;
- escutar `onPlayerJoined`;
- escutar `onPlayerLeft`;
- transformar join/leave em `Pipeline Facts`;
- associar facts Ã  `Pipeline Identity`, `Session Identity`, `Route Identity`, `ActorPreparationBatchId` e slot de player;
- impedir que joins foreign/stale alterem o pipeline ativo.

### 3.9.1 Esclarecimento sobre legado/teste de input

Os assets, cenas e scripts de input atualmente existentes fora do contrato Base 1.1 devem ser tratados como legado, teste ou referÃªncia transitÃ³ria.

Na Base 1.1:

- esses artefatos nÃ£o sÃ£o fonte canÃ´nica para identidade, participaÃ§Ã£o, roster ou readiness;
- `PlayerInput` e `PlayerInputManager` continuam como adapters/executors Unity, nÃ£o owners de lifecycle;
- a integraÃ§Ã£o oficial de input/player deve ocorrer por contrato novo, explÃ­cito e versionado no trilho canÃ´nico;
- nÃ£o haverÃ¡ migraÃ§Ã£o automÃ¡tica dos `InputActionAssets` atuais para o novo contrato.

Esse esclarecimento nÃ£o altera os invariantes jÃ¡ definidos para:

- `ActorSetDefinitionAsset` como receita default/static;
- `ResolvedActorSet` como set concreto de runtime;
- `PlayerSelectionSnapshot` como fonte futura de resoluÃ§Ã£o do set concreto.

### 3.10 Modelo HÃ­brido Permitido

A Base 1.1 permite uma estratÃ©gia hÃ­brida:

```text
Pipeline decide.
Unity PlayerInputManager executa join.
Unity PlayerInput executa input por player.
Adapters transformam eventos Unity em Pipeline Facts.
Pipeline valida facts com Pipeline Identity.
```

Fluxo conceitual:

```text
SessionOperationalPipeline
-> emite PlayerJoinPolicyCommand
-> PlayerInputManagerAdapter.EnableJoining()

PlayerInputManager
-> detecta join
-> instancia ou registra PlayerInput

PlayerInputManagerAdapter
-> emite PlayerJoinedFact

SessionOperationalPipeline
-> valida identity/context
-> atualiza ResolvedActorSet ou ActorPreparationSnapshot
-> decide se readiness permite handoff
```

Essa estratÃ©gia Ã© permitida desde que o `PlayerInputManager` nÃ£o vire owner de lifecycle.

### 3.11 Actor Readiness

Valida se os actors obrigatÃ³rios estÃ£o prontos antes do handoff para activity.

Exemplos de readiness obrigatÃ³ria:

- actor obrigatÃ³rio existe;
- actor obrigatÃ³rio possui identidade vÃ¡lida;
- actor obrigatÃ³rio estÃ¡ posicionado;
- actor obrigatÃ³rio executou setup interno mÃ­nimo;
- vÃ­nculos externos obrigatÃ³rios foram aplicados;
- input estÃ¡ em estado seguro;
- HUD/camera obrigatÃ³rios estÃ£o prontos quando exigidos;
- player obrigatÃ³rio possui `PlayerInput` vÃ¡lido quando policy exigir;
- player obrigatÃ³rio possui device/control scheme vÃ¡lido quando policy exigir;
- nenhum actor obrigatÃ³rio falhou silenciosamente.

Se um requisito obrigatÃ³rio falhar, o fluxo deve reportar failure explÃ­cito ou fail-fast. NÃ£o deve haver fallback silencioso.

### 3.12 Activity Handoff

Produz os dados necessÃ¡rios para a `SessionActivityPipeline` iniciar a activity sem remontar o palco.

O handoff deve carregar identidade suficiente para impedir que foreign/stale events alterem a activity ativa.

### 3.13 Actor Activity

ApÃ³s o handoff, a `SessionActivityPipeline` comanda a vida ativa dos actors durante a activity.

Exemplos:

- liberar controle real;
- trocar input map por situaÃ§Ã£o ativa;
- pausar/retomar actors;
- aplicar dano;
- mudar estado de actor;
- spawnar inimigos por gameplay;
- criar pickups por gameplay;
- destruir objetos durante gameplay;
- iniciar transformaÃ§Ã£o in-game;
- bloquear/desbloquear controle por evento de activity.

Spawn ou materializaÃ§Ã£o durante activity Ã© permitido quando for consequÃªncia do gameplay ativo. O que for obrigatÃ³rio para iniciar a activity pertence ao `ActorPreparationStage` operacional.

### 3.14 Actor Teardown / Preservation

Define o destino dos actors ao sair da activity ou trocar sessÃ£o.

Exemplos:

- preservar player entre activities;
- despawnar inimigos temporÃ¡rios;
- limpar objetos de gameplay;
- marcar actor como stale;
- preparar rematerializaÃ§Ã£o futura;
- emitir facts de saÃ­da para save/continuidade quando comandado pelo pipeline correto;
- desparear devices quando policy exigir;
- desativar joining quando rota/activity encerrar;
- desativar PlayerInput quando actor sair do ciclo ativo.

---

## 4. Actors declaram necessidades de setup

Actors nÃ£o decidem lifecycle. Actors declaram necessidades.

Um actor pode declarar:

- preciso de identidade;
- preciso de spawn point;
- preciso de player slot;
- preciso de controle;
- preciso de `PlayerInput`;
- preciso de control scheme;
- preciso de device;
- preciso de HUD;
- preciso de camera;
- preciso de target inicial;
- preciso iniciar bloqueado;
- preciso restaurar estado;
- estou pronto;
- falhei ao preparar.

Um actor nÃ£o pode decidir:

- quando a sessÃ£o comeÃ§a;
- quando a activity comeÃ§a;
- quando a cortina abre;
- quando o pipeline avanÃ§a;
- quando a rota muda;
- quando a gameplay Ã© liberada globalmente;
- quando o join global Ã© permitido;
- quando fallback para player default pode ocorrer.

Regra canÃ´nica:

```text
Actor declara necessidade.
Pipeline decide janela e ordem macro.
Fluxo interno executa preparaÃ§Ã£o.
Adapters aplicam vÃ­nculos externos.
Readiness decide se o handoff pode prosseguir.
```

---

## 5. Nomenclatura

O termo `Adapter` deve ser reservado para fronteiras com outros domÃ­nios ou runtimes externos ao fluxo de actors.

### 5.1 Nomes internos ao fluxo de actors

Preferir nomes simples:

- `ActorPreparationStage`
- `ActorPreparationPlan`
- `ActorParticipation`
- `ActorSet`
- `ResolvedActorSet`
- `ActorSetResolver`
- `PlayerSelectionSnapshot`
- `PlayerSelectionResult`
- `PlayerParticipation`
- `ActorMaterialization`
- `ActorPlacement`
- `ActorSetup`
- `ActorSetupRequirement`
- `ActorSetupResult`
- `ActorReadiness`
- `ActorPreparationSnapshot`
- `ActorPreparationReport`

Evitar, por padrÃ£o:

- `ActorPreparationSystem`
- `ActorSetupSystem`
- `PlayerSystem`
- `ActorCentralSystem`
- `ActorMaterializationAdapter`
- `ActorPlacementAdapter`
- `ActorReadinessAdapter`

### 5.2 Nomes para fronteiras externas

Usar `Adapter` quando houver integraÃ§Ã£o com outro domÃ­nio:

- `InputModeAdapter`
- `PlayerInputAdapter`
- `PlayerInputManagerAdapter`
- `HudBindingAdapter`
- `CameraBindingAdapter`
- `AudioAdapter`
- `SaveAdapter`
- `SceneCompositionAdapter`
- `PlayerControlBindingAdapter`, se a execuÃ§Ã£o cruzar fronteira de input/runtime externo

---

## 6. RelaÃ§Ã£o com InputMode, HUD, Camera e Audio

`InputMode`, HUD, camera e audio nÃ£o sÃ£o owners do setup de actors.

Eles sÃ£o vÃ­nculos externos aplicados quando o `ActorPreparationStage` ou a `SessionActivityPipeline` comandam.

Antes da activity:

- preparar binding de controle;
- preparar PlayerInput quando a policy exigir;
- preparar camera/HUD obrigatÃ³rios;
- manter input em estado seguro;
- nÃ£o liberar gameplay antes da readiness.

Durante a activity:

- liberar controle real;
- trocar mapa de controle;
- alternar pause/overlay;
- atualizar HUD por estado ativo;
- alterar camera por evento de gameplay.

A frente de `InputMode` continua vÃ¡lida para superfÃ­cie operacional de menu:

```text
FrontendMenu
-> InputMode FrontendMenu
-> EventSystem obrigatÃ³rio
```

Mas players em activity exigem contrato especÃ­fico de participaÃ§Ã£o/input:

```text
PlayerInputCapability
```

ou nome equivalente futuro.

---

## 7. Rota, ActorSet default e Player Selection runtime

`OperationalRouteAsset` pode carregar um `ActorSetDefinitionAsset` default para o sandbox e para casos simples.

PorÃ©m, a rota deve ser lida como contrato/policy/default, nÃ£o como owner definitivo do set concreto.

Modelo canÃ´nico futuro:

```text
OperationalRouteAsset
-> declara contrato de participaÃ§Ã£o

PlayerSelectionSurface
-> produz PlayerSelectionSnapshot

SessionOperationalPipeline
-> valida snapshot contra a rota
-> resolve ResolvedActorSet
-> monta ActorPreparationPlan

ActorPreparationStage
-> prepara actors com base no ResolvedActorSet
```

Invariantes:

- nÃ£o mutar `ActorSetDefinitionAsset` em runtime;
- nÃ£o sobrescrever asset autoral com seleÃ§Ã£o runtime;
- nÃ£o usar seleÃ§Ã£o runtime sem identidade;
- nÃ£o usar fallback silencioso para default se a seleÃ§Ã£o runtime for obrigatÃ³ria;
- nÃ£o deixar botÃ£o/link de UI virar owner de lifecycle;
- botÃ£o/link pode produzir command/payload;
- pipeline valida e decide.

---

## 8. Identidade e proteÃ§Ã£o contra foreign/stale events

Todo ciclo relevante de Actor Preparation deve carregar identidade suficiente para validaÃ§Ã£o.

Identidades mÃ­nimas esperadas:

- `Pipeline Identity`;
- `Session Identity`;
- `Route Identity`, quando aplicÃ¡vel;
- `Activity Identity`, quando aplicÃ¡vel;
- `Actor Identity`;
- `Player Slot Identity`, quando aplicÃ¡vel;
- `Actor Set Identity` ou `ActorPreparationBatchId`, quando necessÃ¡rio;
- `PlayerSelectionSnapshotId`, quando aplicÃ¡vel;
- `Setup Sequence` ou equivalente, quando houver mÃºltiplas preparaÃ§Ãµes dentro da mesma sessÃ£o;
- `PlayerInputUser` ou device binding identity, quando policy exigir.

Foreign/stale actor/player/input events nÃ£o podem:

- materializar actor no pipeline ativo;
- registrar player no set ativo;
- reconfigurar binding ativo;
- liberar readiness;
- trocar actor atual;
- reabrir setup concluÃ­do;
- alterar activity ativa;
- habilitar joining em rota jÃ¡ fechada;
- aplicar action map em player de outra sessÃ£o;
- associar device a slot de outro ciclo.

---

## 9. Estado atual validado no Base11Sandbox

O Base11Sandbox jÃ¡ validou um estado planned-only de `ActorPreparation`.

Componentes atuais:

- `ActorDefinitionAsset`;
- `ActorSetDefinitionAsset`;
- `ActorPreparationStage`;
- `ActorSetEntry`;
- `ActorPlannedEntry`;
- `ActorMaterializationEntry`;
- `ActorReadinessEntry`;
- metadata planned-only de prefab;
- metadata planned-only de placement.

Log validado:

```text
outcome='planned_only'
participationKind='ActorSetExpected'
plannedActors='2'
requiredActors='1'
optionalActors='1'
notMaterializedActors='2'
pendingRequiredActors='1'
pendingOptionalActors='1'
actorsWithPrefab='1'
actorsWithoutPrefab='1'
actorsWithPlacement='0'
actorsWithoutPlacement='2'
message='Actor preparation plan created. No actor materialization executed.'
```

Esse estado nÃ£o materializa actors, nÃ£o instancia prefab, nÃ£o aplica placement, nÃ£o aplica input, nÃ£o aplica HUD e nÃ£o aplica camera.

---

## 10. Invariantes

- `ActorPreparationStage` pertence ao `SessionOperationalPipeline`.
- `ActorPreparation` nÃ£o Ã© pipeline prÃ³prio nesta etapa da Base 1.1.
- `SessionOperationalPipeline` decide janela, ordem macro, readiness e handoff.
- `OperationalRouteAsset` declara contrato/default; nÃ£o Ã© owner definitivo do set concreto de players.
- `ActorSetDefinitionAsset` Ã© receita default/static, nÃ£o runtime mutable state.
- `ResolvedActorSet` Ã© o set concreto consumido pela preparaÃ§Ã£o.
- `PlayerSelectionSnapshot` pode ser fonte futura do set concreto.
- Actors declaram necessidades de setup; nÃ£o decidem lifecycle.
- Player selection nÃ£o decide lifecycle de rota/activity.
- `PlayerInput` Ã© executor tÃ©cnico por player.
- `PlayerInputManager` Ã© adapter/executor de join/materializaÃ§Ã£o Unity.
- O fluxo de actors nÃ£o pode virar uma central onisciente.
- O termo `Adapter` deve ser reservado para fronteiras externas ao domÃ­nio de actors.
- Setup obrigatÃ³rio de actors acontece antes da abertura da cortina.
- Activity nÃ£o monta o palco inicial obrigatÃ³rio.
- Activity transforma actors durante gameplay ativo.
- Spawn/materializaÃ§Ã£o durante activity Ã© permitido apenas como consequÃªncia de gameplay ativo.
- Readiness obrigatÃ³rio falho nÃ£o pode gerar fallback silencioso.
- Unity lifecycle pode inicializar objeto local, mas nÃ£o decidir session/activity lifecycle.
- Foreign/stale actor/player/input events nÃ£o podem alterar o pipeline ativo.

---

## 11. ConsequÃªncias

- `InputMode` deixa de ser tratado como eixo arquitetural isolado e passa a ser lido como vÃ­nculo/execuÃ§Ã£o tÃ©cnica dentro de um contrato maior de actors/player.
- A preparaÃ§Ã£o principal de actors passa a ocorrer no operacional, com a cortina fechada.
- A `SessionActivityPipeline` recebe um palco pronto e comanda apenas a vida ativa e transformaÃ§Ãµes in-game.
- Actors ganham autonomia declarativa sem virar owners de lifecycle.
- O setup escala melhor porque cada actor informa suas necessidades.
- HUD, camera, input, PlayerInput, PlayerInputManager, audio e save permanecem como integraÃ§Ãµes por adapters quando cruzam fronteira de domÃ­nio.
- Evita-se um `ActorSetupManager` ou `ActorSystem` centralizador.
- A tela de seleÃ§Ã£o de players pode evoluir sem forÃ§ar mutaÃ§Ã£o de rota ou asset.
- Uma futura Base 2.0 pode extrair um pipeline prÃ³prio de actor preparation se a Base 1.1 provar necessidade real.

---

## 12. RelaÃ§Ã£o com Base 1.0 e Base 2.0

- Base 1.0 espalhou materializaÃ§Ã£o, readiness, input e preparaÃ§Ã£o de actors entre serviÃ§os operacionais, readiness, input, scene-local code e lifecycle de objetos.
- Base 1.1 formaliza o corte: preparaÃ§Ã£o obrigatÃ³ria de actors pertence ao `SessionOperationalPipeline`; vida ativa pertence Ã  `SessionActivityPipeline`; efeitos externos sÃ£o executados por adapters.
- Base 1.1 mantÃ©m `ActorPreparationStage` como stage operacional, nÃ£o como pipeline prÃ³prio.
- Base 2.0 futura pode transformar `ActorPreparationStage` em sub-pipeline ou pipeline prÃ³prio se a complexidade real justificar. Isso nÃ£o deve ser feito agora.

---

## 13. Roadmap Futuro

- Auditar input legado/teste para isolamento, remoÃ§Ã£o progressiva ou referÃªncia histÃ³rica:
  - mapear onde `UnityEngine.InputSystem.PlayerInput` e `UnityEngine.InputSystem.PlayerInputManager` ainda aparecem fora do contrato Base 1.1;
  - mapear onde `InputSystemUIInputModule`, `MultiplayerEventSystem`, `InputActionAsset` e prefabs/scenes com componentes de input ainda aparecem fora do contrato Base 1.1;
  - classificar cada ocorrÃªncia como: isolamento imediato, remoÃ§Ã£o planejada ou referÃªncia histÃ³rica documentada;
  - reforÃ§ar que input atual fora do contrato Base 1.1 Ã© legado/teste e nÃ£o fonte canÃ´nica de identidade, participaÃ§Ã£o, roster ou readiness.
- Mapear owners atuais de actors, readiness, input binding, camera binding, HUD binding e materializaÃ§Ã£o.
- Criar matriz de migraÃ§Ã£o especÃ­fica de actors:

```text
owner antigo -> novo pipeline owner -> papel final -> aÃ§Ã£o necessÃ¡ria
```

- Definir contratos mÃ­nimos para:
  - `ActorSetupRequirement`;
  - `ActorPreparationPlan`;
  - `ActorReadiness`;
  - `ActorPreparationSnapshot`;
  - `ResolvedActorSet`;
  - `PlayerSelectionSnapshot`;
  - `PlayerInputCapability`;
  - `PlayerJoinPolicy`.
- Definir quais bindings externos exigem adapters prÃ³prios.
- Formalizar contrato novo de integraÃ§Ã£o `PlayerInput`/`PlayerInputManager` no trilho Base 1.1, sem migraÃ§Ã£o automÃ¡tica dos `InputActionAssets` legados/de teste.
- Definir estratÃ©gia futura para:
  - player Ãºnico;
  - multiplayer local;
  - join manual;
  - join por input action;
  - split-screen;
  - seleÃ§Ã£o de personagem/modelo/loadout.
- Implementar somente apÃ³s fechar auditoria e matriz de migraÃ§Ã£o.


