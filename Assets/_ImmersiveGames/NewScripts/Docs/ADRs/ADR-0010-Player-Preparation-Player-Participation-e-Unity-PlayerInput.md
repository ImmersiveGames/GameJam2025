# ADR-0010 - Player Preparation Flow, Player Slots e Unity PlayerInput

## Status
- Estado: CLOSED (checkpoint validado por smoke)
- Data: 2026-05-13
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Dependencia
- Depende de: ADR-0009 - Session Player Slots e Operational Input Runtime.

---

## Contexto

A Base 1.1 precisa manter separacao explicita entre:

- inicializacao operacional de sessao (slots + validacao de PlayerInputManager + input UI runtime);
- preparacao de actors reais para handoff de activity.

Tambem precisa evitar que qualquer componente vire catalogo total do jogo.

ADR-0009 congela o contrato de slots e input operacional.

ADR-0010 define o checkpoint de Player Preparation como resposta a esse contrato operacional, **sem** materializacao de player final, participacao/gameplay input final ou ActivitySetup completo neste ponto.

---

## Decisao

`PlayerPreparationStage` permanece uma `Pipeline Stage` do `SessionOperationalPipeline`, executada no `SessionOperationalSetup` antes do handoff para `SessionActivityPipeline`.

`PlayerPreparationStage` prepara apenas requisitos de players no handoff atual; actors nao-player ficam para `ActivitySetup`/SessionActivity futuro.

**`PlayerSlot` pode existir antes de `PlayerActor`.**

**`PlayerPreparation` no trilho ativo prepara apenas intencao/payload para handoff; nao materializa GameObject de player no `SessionOperationalPipeline`.**

---

## 1. Corte Canonico

```text
SessionOperationalPipeline
  Step 1: SessionPlayerSlotsValidator (ADR-0009)
  Step 2: UnityOperationalInputRuntimeAdapter (ADR-0009)
  Step 3: PlayerPreparationStage (ADR-0010)
-> SessionActivityEntryHandoffPrepared
-> SessionActivityPipeline
```

- Operacional prepara o necessario para o handoff atual.
- Activity comanda o ciclo ativo apos handoff.

---

## 2. Responsabilidades

### 2.1 SessionOperationalPipeline

- decide janela, ordem, readiness e handoff;
- valida precondicoes e requisitos obrigatorios do contexto atual;
- falha explicitamente quando requisito obrigatorio nao e atendido;
- comanda SessionPlayerSlotsValidator (ADR-0009);
- comanda UnityOperationalInputRuntimeAdapter (ADR-0009);
- comanda PlayerPreparationStage (ADR-0010).

### 2.2 PlayerPreparationStage

- resolve/prepara apenas players exigidos para o handoff atual;
- produz facts/snapshot/handoff data para a Activity;
- no trilho ativo, nao materializa `PrototypePlayer`/`PlayerActor`; prepara apenas payload minimo para handoff;
- **nao tenta materializar gameplay input neste checkpoint**;
- **nao tenta materializar participacao/selecao neste checkpoint**;
- nao tenta conhecer todos os actors possiveis do jogo;
- nao vira owner de lifecycle global;
- nao interfere com o runtime operacional de input ja inicializado por ADR-0009.

### 2.3 PlayerSlot

- eh capacidade operacional definida em ADR-0009.
- pode existir sem `PlayerActor` associado.
- validacao de existencia/unicidade/count feita em SessionPlayerSlotsValidator (ADR-0009).
- associacao real com `PlayerActor` acontece apos handoff para Activity, por policy da Activity.

### 2.4 PlayerInput e PlayerInputManager

- sao `Pipeline Adapters`/executores tecnicos Unity;
- aplicam efeitos comandados pelo pipeline;
- validacao de inicializacao feita em SessionPlayerSlotsValidator (ADR-0009);
- input UI runtime (EventSystem, InputSystemUIInputModule, UI actions binding) inicializado em UnityOperationalInputRuntimeAdapter (ADR-0009);
- gameplay input continua fora deste checkpoint;
- nao decidem lifecycle de sessao/activity;
- nao viram owner semantico de participacao.

---

## 3. Invariantes

- `PlayerSlot` eh capacidade operacional de entrada, nao ator final.
- Resolucao de actor real depende de contexto e `activity-provided requirements`.
- Pipelines decidem; adapters executam.
- Nao criar fallback silencioso para requisito obrigatorio ausente.
- `Pipeline Identity` protege contra `foreign/stale events`.
- Handoff para Activity carrega payload minimo de snapshot/fact de `PlayerPreparation`.
- **PlayerPreparation nao materializa gameplay input.**
- **PlayerPreparation nao materializa selecao de players.**
- **PlayerPreparation nao configura binding de controles para players.**

---

## 4. Nao Objetivos

Este ADR **nao** define:

- catalogo total de actors do jogo;\n- setup de enemies/NPCs/props/objetos (futuro ActivitySetup);
- regras completas de todas as activities futuras;
- ownership de features fora do contexto operacional/handoff atual;
- gameplay input ou player selection;
- player binding ou input remapping;
- split-screen ou multiplayer join mechanics.

---

## 5. Consequencias

- Evita regressao para centralizacao onisciente.
- Mantem Base 1.1 com ownership claro por pipeline stage.
- Permite evolucao de requisitos por activity sem inflar inicializacao operacional.
- Separa claramente: input UI operacional (ADR-0009) vs. gameplay input (fora deste checkpoint).
- Separa claramente: slots operacionais (ADR-0009) vs. actor materializacao (Activity policy).



## 9. Checkpoint Historico - Materializacao Minima de Prototype Player (2026-05-14)

`PlayerPreparation` permanece restrito a players e agora pode materializar `PrototypePlayer` minimo quando houver player obrigatorio com prefab valido.

Regras congeladas no checkpoint:

- required player com prefab valido: materializa.
- optional player sem prefab: skip explicito (nao fatal).
- required player sem prefab: fail-fast.
- entrada nao-player em `PlayerSetDefinition`: configuracao invalida (fail-fast de contrato).

Escopo funcional explicitamente limitado:

- `PrototypePlayer` materializado **nao** e player final de gameplay.
- nao existe gameplay input conectado ao player materializado.
- nao existe `PlayerInput` conectado ao player materializado.
- nao existe camera de player.
- nao existe Cinemachine.
- nao existe movimento/controle.
- nao existe `ActivitySetup` para actors nao-player neste checkpoint.
- nao existe `Activity Snapshot Provider` neste checkpoint.

Ownership mantido:

- `PlayerPreparation` (SessionOperational) no caminho ativo prepara apenas intencao/payload minimo do handoff atual (planned_only), sem materializacao Unity.
- `SessionActivityEntryHandoff` carrega payload minimo de `PlayerPreparation` (identidade, outcome e contagens) como fato/snapshot, sem referencias Unity runtime.
- (historico) `UnityPlayerMaterializationAdapter` executava a materializacao prototipo comandada pelo pipeline e nao decidia lifecycle.
- materializacao de actors nao-player (NPC/enemies/props/objetos) pertence ao futuro `ActivitySetup`/`SessionActivity`.

Hierarquia runtime documentada:

```text
SessionActivitySandboxScene
+-- __PrototypePlayersRuntimeRoot::<routeOperationId>
    +-- PrototypePlayer::<playerId>
```

## 10. Checkpoint CLOSED - PlayerPreparation Operacional Minimo (2026-05-17)

Checkpoint marcado como **CLOSED** com evidencia de smoke manual:

- Boot -> Menu: passou.
- Menu -> SessionActivitySandboxScene: passou.
- SessionActivitySandboxScene -> Menu: passou.

Evidencias normativas validadas:

- `PlayerPreparationStage` inclui `routeOperationId` na identidade observavel.
- `SessionActivityEntryHandoffEmitted` inclui payload minimo de `PlayerPreparation` com:
    - `playerPreparationOutcome`
    - `plannedPlayers`
    - `materializedPlayers`
    - `pendingRequiredPlayers`
- `SessionActivityEntryHandoffAccepted` confirma o mesmo payload minimo.
- Materializacao de `PrototypePlayer` no `SessionOperational` pertence ao checkpoint historico e foi removida do caminho ativo.

Limites mantidos neste fechamento:

- nao e `PlayerActor` final;
- nao e gameplay input final;
- nao e binding `input`-`player`;
- nao e `ActivitySetup` completo;
- nao abre lifecycle/deactivation completo de Activity.


---

## 11. Direcao congelada - PlayerSelectionSnapshot e nascimento do PlayerActor v0 (2026-05-18)

Esta decisao atualiza a direcao futura do trilho de player sem apagar o checkpoint historico de `PrototypePlayer` validado anteriormente.

Leitura congelada:

```text
PlayerSlot / PlayerSelectionSnapshot
-> SessionOperationalPipeline valida capacidade e prepara handoff
-> SessionActivityPipeline / ActivitySetup materializa PlayerActor v0
```

### 11.1 Slots podem ser ocupados antes da rota de gameplay

`PlayerSlot` pode ser ocupado antes da rota que leva para uma Activity de gameplay.

A ocupacao do slot representa intencao/configuracao de player, nao materializacao do player jogavel.

Campos conceituais minimos:

```text
playerSlotId
playerDefinitionId
skinId opcional
displayName opcional
```

### 11.2 PlayerSelectionSnapshot

A intencao de player deve ser representada por `PlayerSelectionSnapshot` ou contrato equivalente.

Modelo ideal futuro:

```text
CharacterSelection Activity
-> jogador ocupa slot
-> escolhe player/skin/nome
-> produz PlayerSelectionSnapshot
-> Gameplay Activity consome o snapshot
```

MVP aceito:

```text
Menu
-> botao "1 Player" ou "2 Players"
-> produz PlayerSelectionSnapshot default
-> Gameplay Activity consome o snapshot
```

`PlayerSelectionSnapshot` nao deve conter:

```text
GameObject
Transform
PlayerInput
Camera
Cinemachine
Movement component
runtime player instance
```

### 11.3 PlayerActor nao nasce no PlayerPreparation

O `SessionOperationalPipeline` continua responsavel por:

- validar slots e `PlayerInputManager`;
- validar/preparar input operacional;
- preparar payload minimo de player para handoff;
- proteger identidade contra `foreign/stale events`.

Mas a direcao canonica passa a ser:

```text
PlayerPreparation nao materializa PlayerActor jogavel final.
```

O `PrototypePlayer` deixa de ser direcao permanente. Ele permanece apenas como historico/checkpoint do sandbox anterior, nao como shape alvo.

### 11.3.1 Estado ativo consolidado (2026-05-19)

- `SessionOperationalPipeline` nao registra nem executa `UnityPlayerMaterializationAdapter`.
- `PlayerPreparation` no operacional produz `facts` e payload minimo (`playerIds`/contagens/outcome) para handoff.
- `PlayerActor v0` nasce apenas no `SessionActivityPipeline/ActivitySetup` via `PlayerActorSetupStage`.

### 11.4 PlayerActor v0 nasce no ActivitySetup

O `PlayerActor v0` nasce no `ActivitySetup` da Activity de gameplay, com a cortina fechada, a partir de `PlayerSelectionSnapshot` resolvido.

O nascimento do player real pertence ao eixo:

```text
SessionActivityPipeline
-> ActivitySetup
-> PlayerActorSetupStage
```

Nao pertence a:

```text
SessionOperationalPipeline
PlayerPreparationStage
InputModes
PlayerInputManager
Menu
```

### 11.5 PlayerActorEntryPlan e PlayerActorResetPlan

Todo `PlayerActor` materializado deve nascer a partir de `PlayerActorEntryPlan` e carregar `PlayerActorResetPlan` minimo.

Campos conceituais minimos de `PlayerActorEntryPlan`:

```text
playerActorId
playerSlotId
playerDefinitionId
activityId
spawnPointId
ownerScope = RouteOwned
releasePolicy
resetPolicy
```

Campos conceituais minimos de `PlayerActorResetPlan`:

```text
resetReason
initialSpawnPointId
initialTransformPolicy
runtimeStatePolicy
selectionRebindPolicy
```

Regra congelada:

```text
PlayerActor v0 nao nasce apenas como prefab instanciado.
Ele nasce a partir de um plano com identidade, ownership, spawn, release e reset minimos.
```

### 11.6 Fora deste corte

Continuam fora deste corte:

- gameplay input final;
- movimento/controle;
- camera por player;
- `PlayerInput` ligado ao `PlayerActor`;
- split-screen;
- save/progression de player;
- snapshot funcional real de player.

### 11.7 Transporte do PlayerSelectionSnapshot

`PlayerSelectionSnapshot` e payload de intencao, nao runtime object.

Fluxo canonico:

```text
Menu ou CharacterSelection Activity
-> PlayerSelectionSnapshot
-> Route Request para gameplay
-> SessionOperationalPipeline
-> SessionActivityEntryHandoff
-> SessionActivityPipeline
-> ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorEntryPlan
-> PlayerActor v0
```

O `SessionOperationalPipeline` pode validar capacidade e consistencia minima:

```text
playerCount <= maxPlayerSlots
slots ocupados sao validos
snapshot obrigatorio existe quando a rota/activity exige player
```

Mas ele nao materializa `PlayerActor` jogavel final.

`PlayerSelectionSnapshot` nao deve ser transportado por estado global mutavel, `static`, objeto `DontDestroyOnLoad` ou prefab ja instanciado no menu.

A intencao deve atravessar a rota/handoff como payload explicito e rastreavel por `Pipeline Identity`.

### 11.8 Validacao minima do PlayerSelectionSnapshot

`PlayerSelectionSnapshot` nao pode gerar `PlayerActorEntryPlan` sem validacao minima.

Fluxo canonico:

```text
PlayerSelectionSnapshot
-> PlayerSelectionValidation
-> PlayerActorEntryPlan
-> PlayerActorResetPlan
-> PlayerActor v0
```

Responsabilidade do `SessionOperationalPipeline`:

```text
validar que snapshot obrigatorio existe quando rota/activity exige player
validar playerCount >= 1 quando houver requisito obrigatorio de player
validar playerCount <= maxPlayerSlots
validar que slots nao estao duplicados
preservar Pipeline Identity no payload/handoff
```

Responsabilidade do `SessionActivityPipeline / ActivitySetup`:

```text
validar PlayerDefinitionId
validar SpawnPointId
validar PlayerActorEntryPlan
validar PlayerActorResetPlan
falhar explicitamente quando requisito obrigatorio de materializacao estiver invalido
```

Regras congeladas:

```text
Snapshot ausente em Activity que exige player = fail-fast.
Snapshot ausente em Activity que nao exige player = skip explicito.
PlayerDefinition obrigatoria ausente = fail-fast.
SpawnPoint obrigatorio ausente = fail-fast.
Defaults autorais, como skin/displayName, devem ser explicitos.
Nao ha fallback silencioso para primeiro prefab, primeiro slot, primeiro spawn point ou primeiro player encontrado.
```

Essa validacao nao materializa `PlayerActor`. Ela apenas protege o contrato antes do `ActivitySetup` resolver o plano real.

## 12. Checkpoint congelado - PlayerActorSetupStage no ActivitySetup (2026-05-18)

`PlayerActorSetupStage` e o sub-stage canonico do `ActivitySetup` responsavel por transformar `PlayerSelectionSnapshot` validado em `PlayerActor v0`.

Fluxo canonico minimo:

```text
ActivitySetupStarted
-> PlayerActorSetupStageStarted
-> PlayerSelectionSnapshotValidated
-> PlayerActorEntryPlanResolved
-> PlayerActorResetPlanResolved
-> PlayerActorMaterializationCommand
-> PlayerActorMaterialized
-> PlayerActorReadyFact
-> PlayerActorSetupStageCompleted
-> ActivitySetupCompleted
```

Responsabilidades do `SessionActivityPipeline / ActivitySetup`:

```text
decidir quando PlayerActorSetupStage inicia
decidir falha ou continuidade do ActivitySetup
bloquear ActivitySetupCompleted quando PlayerActorReadyFact obrigatorio nao existir
preservar Pipeline Identity
rejeitar commands/facts foreign/stale
```

Responsabilidades do `PlayerActorSetupStage`:

```text
consumir PlayerSelectionSnapshot validado
resolver PlayerActorEntryPlan
resolver PlayerActorResetPlan
emitir PlayerActorMaterializationCommand
aguardar PlayerActorReadyFact ou failure explicito
```

Responsabilidades do `PlayerActorMaterializationAdapter`:

```text
instanciar o prefab definido pelo plano
posicionar no spawn resolvido
registrar identidade runtime minima
retornar PlayerActorMaterialized/PlayerActorReadyFact ou failure
```

O adapter executa side-effects. Ele nao decide lifecycle, nao escolhe player, nao cria fallback e nao reescreve identidade.

Ficam fora deste stage neste corte:

```text
PlayerInput binding
movimento/controle
camera target binding
save/progression
vida/status/inventario
split-screen
```

Regra congelada:

```text
PlayerActorSetupStage e sub-stage do ActivitySetup.
Sem PlayerActorReadyFact obrigatorio, ActivitySetup nao pode concluir para uma Activity que exige player.
```

## 13. Checkpoint congelado - PlayerDefinition e ActivityPlayerSpawnPoint (2026-05-18)

`PlayerActor v0` nao nasce de prefab solto, nome de `GameObject`, busca de cena ou decisao do adapter.

A materializacao do player real da Activity depende de dois contratos explicitos:

```text
PlayerDefinition
+ ActivityPlayerSpawnPoint
-> PlayerActorEntryPlan
-> PlayerActorMaterializationCommand
-> PlayerActorReadyFact
```

### 13.1 PlayerDefinition

`PlayerDefinition` resolve qual player autoral deve ser materializado.

Campos conceituais minimos:

```text
PlayerDefinitionId
PlayerActorPrefab
DefaultSkinId
```

Regras congeladas:

```text
PlayerDefinitionId invalido = fail-fast.
PlayerActorPrefab obrigatorio ausente = fail-fast.
Nao ha fallback para primeiro prefab encontrado.
Nao ha prefab hardcoded no PlayerActorMaterializationAdapter.
```

### 13.2 ActivityPlayerSpawnPoint

`ActivityPlayerSpawnPoint` resolve onde o `PlayerActor v0` nasce dentro da Activity.

Campos conceituais minimos:

```text
SpawnPointId
PlayerSlotId opcional
Transform
```

Regras congeladas:

```text
Activity que exige player sem spawn point valido = fail-fast.
Slot com spawn dedicado deve resolver o spawn correspondente.
Nao ha fallback para primeiro Transform encontrado.
Nao ha spawn por nome de GameObject ou busca implicita.
```

### 13.3 Fronteira de ownership

`PlayerActorSetupStage` resolve `PlayerDefinition` e `ActivityPlayerSpawnPoint` durante o `ActivitySetup`.

`PlayerActorMaterializationAdapter` apenas executa o comando ja resolvido:

```text
instanciar prefab definido pelo PlayerActorEntryPlan
posicionar no ActivityPlayerSpawnPoint resolvido
registrar identidade runtime minima
reportar PlayerActorReadyFact ou failure
```

O adapter nao escolhe prefab, nao escolhe spawn point, nao usa `PlayerInputManager`, nao usa `Camera.main` e nao faz busca implicita na cena.

## 14. Checkpoint congelado - PlayerActorIdentity e ActivityPlayerActorRegistry (2026-05-18)

Todo `PlayerActor v0` materializado pelo `PlayerActorSetupStage` deve receber identidade explicita e ser registrado em um registry scoped a Activity atual.

Contrato minimo de identidade:

```text
PlayerActorIdentity
- PlayerActorId
- PlayerSlotId
- PlayerDefinitionId
- ActivityId
- EntrySequence
```

`PlayerActorIdentity` responde quem e o player dentro da Activity atual e permite que stages/adapters futuros encontrem o actor correto sem busca implicita.

Contrato minimo de registry:

```text
ActivityPlayerActorRegistry
- Register(PlayerActorIdentity, runtimeRoot)
- TryGetByPlayerActorId(...)
- TryGetBySlotId(...)
- Unregister(...)
```

Regras congeladas:

```text
Todo PlayerActor v0 materializado deve possuir PlayerActorIdentity valida.
Todo PlayerActor v0 materializado deve ser registrado no ActivityPlayerActorRegistry da Activity atual.
ActivityPlayerActorRegistry nao decide lifecycle.
ActivityPlayerActorRegistry nao cria player.
ActivityPlayerActorRegistry nao escolhe prefab, spawn point, slot ou player definition.
ActivityPlayerActorRegistry nao faz fallback para primeiro player, tag, nome de GameObject ou singleton global.
```

Fluxo canonico:

```text
PlayerActorMaterializationCommand
-> PlayerActorMaterializationAdapter instancia prefab
-> aplica PlayerActorIdentity
-> registra no ActivityPlayerActorRegistry
-> PlayerActorReadyFact
```

O registry existe para leitura tecnica controlada por identidade, especialmente para fases futuras como input binding, camera target binding, reset, release e snapshot/progression. Essas fases futuras ainda nao fazem parte deste checkpoint.

Ficam fora deste corte:

```text
PlayerInput binding
movimento/controle
camera target binding
save/progression
split-screen
```

## 15. Checkpoint congelado - PlayerActorReleasePlan e PlayerActorReleasedFact (2026-05-18)

Todo `PlayerActor v0` materializado pelo `PlayerActorSetupStage` deve possuir release explicito.

O release faz parte do contrato minimo do player dentro da Activity:

```text
Entry
-> Ready
-> Reset
-> Release
```

Contrato minimo de release:

```text
PlayerActorReleasePlan
- PlayerActorId
- PlayerSlotId
- ActivityId
- EntrySequence
- ReleaseReason
- ReleasePolicy
```

Policy MVP:

```text
ReleasePlayersOnRouteExit (default) ou PersistPlayersAcrossRoutes
```

Fluxo canonico:

```text
ActivityExit / Restart / ActivityTransition
-> PlayerActorReleasePlan
-> PlayerActorReleaseCommand
-> PlayerActorReleaseAdapter
-> PlayerActorReleasedFact
-> ActivityPlayerActorRegistry.Unregister
```

Regras congeladas:

```text
Todo PlayerActor RouteOwned/RouteScoped deve ser liberado ou retido por policy explicita antes de fechar o ciclo relevante da Activity.
No MVP, PlayerActor RouteOwned/RouteScoped usa ReleasePlayersOnRouteExit (default) ou PersistPlayersAcrossRoutes.
PlayerActorReleaseAdapter executa side-effects tecnicos de destroy/disable/return-to-pool quando policy futura exigir, consumindo o `IPoolService` canônico existente; ele nao decide lifecycle.
ActivityPlayerActorRegistry.Unregister ocorre apos release valido.
Sem PlayerActorReleasedFact obrigatorio, o ciclo que exige release nao deve concluir.
```

Ficam fora deste corte:

```text
retention real
integração pool-backed de PlayerActor release quando houver policy concreta
restore de checkpoint/save
release completo de HUD, input, camera, inventory ou subscriptions
```


## 16. Checkpoint congelado - PlayerActorResetPlan e PlayerActorResetCompletedFact (2026-05-18)

Todo `PlayerActor v0` materializado pelo `PlayerActorSetupStage` deve possuir reset explicito.

O reset faz parte do contrato minimo do player dentro da Activity:

```text
Entry
-> Ready
-> Reset
-> Release
```

Contrato minimo de reset:

```text
PlayerActorResetPlan
- PlayerActorId
- PlayerSlotId
- PlayerDefinitionId
- ActivityId
- EntrySequence
- ResetReason
- InitialSpawnPointId
- InitialTransformPolicy
- RuntimeStatePolicy
- SelectionRebindPolicy
```

Policy MVP:

```text
ResetToInitialActivitySpawn
```

Fluxo canonico:

```text
ActivityRestart / ActivityReset
-> PlayerActorResetPlan
-> PlayerActorResetCommand
-> PlayerActorResetAdapter
-> PlayerActorResetCompletedFact
```

Comportamento MVP:

```text
localizar PlayerActor por PlayerActorIdentity
reposicionar no ActivityPlayerSpawnPoint inicial
restaurar transform inicial conforme InitialTransformPolicy
limpar estado runtime transitorio conforme RuntimeStatePolicy
reaplicar dados do PlayerSelectionSnapshot conforme SelectionRebindPolicy
emitir PlayerActorResetCompletedFact
```

Regras congeladas:

```text
Todo PlayerActor RouteOwned/RouteScoped deve ter reset explicito por PlayerActorResetPlan.
No MVP, resetar significa voltar ao spawn inicial da Activity e limpar estado transitorio.
SessionActivityPipeline decide quando resetar.
PlayerActorResetAdapter executa side-effects tecnicos, mas nao decide lifecycle.
ActivityPlayerActorRegistry pode ser usado apenas para localizar o PlayerActor por identidade valida.
Sem PlayerActorResetCompletedFact obrigatorio, o ciclo que exige reset nao deve concluir.
Nao ha reset implicito por manager global, PlayerInputManager, InputModes, WorldReset ou fallback scene-local.
```

No restart local da Activity, `ActivityRestartCompleted` so pode ser emitido depois que os `PlayerActors` obrigatorios estiverem prontos novamente:

```text
ActivityRestartRequested
-> ActivityRestartTeardownStarted
-> PlayerActorReset/Release dos PlayerActors atuais conforme policy
-> ActivityRestartSetupStarted
-> PlayerActorSetupStage
-> PlayerActorReadyFact
-> ActivityRestartCompleted
```

Ficam fora deste corte:

```text
input rebinding completo
camera rebinding
save/progression
vida/inventario/status complexo
respawn gameplay
checkpoint restore
```

## 17. Checkpoint congelado - Componentes minimos do PlayerActor v0 (2026-05-18)

Todo `PlayerActor v0` materializado pelo `PlayerActorSetupStage` deve nascer como objeto real da Activity, mas ainda sem gameplay completo.

O prefab/root do `PlayerActor v0` deve conter apenas o minimo necessario para identidade, ownership, registro, reset e release.

Componentes conceituais minimos:

```text
PlayerActorRoot
PlayerActorIdentityComponent
PlayerActorLifecycleMarker
PlayerActorResetAnchor ou referencia equivalente de spawn inicial
```

Responsabilidades minimas:

```text
PlayerActorRoot
-> root runtime do player materializado, nomeado/identificado pelo PlayerActorId.

PlayerActorIdentityComponent
-> carrega PlayerActorIdentity aplicada pelo command/adapter.

PlayerActorLifecycleMarker
-> carrega ownership minimo do ciclo local da Activity.

PlayerActorResetAnchor / spawn reference
-> preserva dados iniciais suficientes para reset minimo.
```

Campos minimos esperados no `PlayerActorIdentityComponent`:

```text
PlayerActorId
PlayerSlotId
PlayerDefinitionId
ActivityId
EntrySequence
```

Campos minimos esperados no `PlayerActorLifecycleMarker`:

```text
ownerScope = RouteOwned
ReleasePolicy = ReleasePlayersOnRouteExit (default) ou PersistPlayersAcrossRoutes
ResetPolicy = ResetToInitialActivitySpawn
```

Dados minimos de reset/spawn preservados:

```text
InitialSpawnPointId
InitialPosition
InitialRotation
InitialScale, se aplicavel
```

Regras congeladas:

```text
PlayerActor v0 nao deve conter PlayerInput.
PlayerActor v0 nao deve conter movimento/controle final.
PlayerActor v0 nao deve conter camera follow, Cinemachine binding ou camera final.
PlayerActor v0 nao deve conter save/progression provider.
PlayerActor v0 nao deve conter health/inventory/status final.
PlayerActor v0 nao deve conter split-screen data.
PlayerActor v0 nao decide lifecycle.
```

Qualquer comportamento futuro de input, movimento, camera, save/progression ou gameplay state deve entrar por stages/adapters futuros comandados pelo pipeline dono do ciclo, preservando `PlayerActorIdentity` e rejeitando comandos `foreign/stale`.

O objetivo deste checkpoint e impedir que o `PlayerActor v0` vire objeto scene-local solto ou player final por acidente antes do contrato de cada camada existir.

## 18. Checkpoint congelado - PlayerActorReadyFact v0 (2026-05-18)

`PlayerActorReadyFact` representa a readiness minima do `PlayerActor v0` materializado no `ActivitySetup`.

No MVP, `PlayerActorReadyFact` **nao significa player jogavel completo**. Significa apenas que o `PlayerActor` foi materializado corretamente como objeto real da Activity e esta pronto para etapas futuras.

Requisitos minimos para emitir `PlayerActorReadyFact`:

```text
prefab instanciado
PlayerActorIdentity aplicada
ActivityPlayerSpawnPoint aplicado
PlayerActorEntryPlan valido
PlayerActorResetPlan valido
PlayerActorReleasePlan valido
ActivityPlayerActorRegistry registrou o actor
Pipeline Identity do command pertence ao ciclo ativo
```

Fluxo canonico minimo:

```text
PlayerActorMaterializationCommand
-> prefab instantiated
-> identity applied
-> spawn applied
-> lifecycle markers applied
-> registry registered
-> PlayerActorMaterializedFact
-> PlayerActorReadyFact
```

Campos conceituais minimos do fact:

```text
PipelineIdentity
PlayerActorId
PlayerSlotId
PlayerDefinitionId
ActivityId
EntrySequence
SpawnPointId
ReadyStage
```

No MVP:

```text
ReadyStage = MaterializedOnly
```

Regras congeladas:

```text
PlayerActorReadyFact nao implica PlayerInput conectado.
PlayerActorReadyFact nao implica movimento/controle funcionando.
PlayerActorReadyFact nao implica camera vinculada.
PlayerActorReadyFact nao implica save/progression ativo.
PlayerActorReadyFact nao implica gameplay state final.
Sem PlayerActorReadyFact obrigatorio, ActivitySetupCompleted nao pode ocorrer em Activity que exige player.
Activity sem requisito de player deve gerar skip explicito, nao fallback silencioso.
```

Qualquer camada futura de input, movimento, camera, save/progression ou gameplay state deve produzir facts proprios, sem redefinir o significado minimo de `PlayerActorReadyFact`.


## 19. Checkpoint congelado - Lifetime RouteOwned do PlayerActor v0 (2026-05-19)

Atualizacao normativa do caminho ativo:

```text
PlayerPreparation no SessionOperational = planned_only/intencao/payload.
PlayerActor v0 nasce no ActivitySetup.
Lifetime padrao do PlayerActor v0 = RouteOwned/RouteScoped.
```

Regras:

```text
Activity exit encerra participacao local, sem destruir PlayerActor por padrao.
Deactivation nao implica Release.
Route exit usa policy explicita: ReleasePlayersOnRouteExit ou PersistPlayersAcrossRoutes.
SessionOperational carrega intencao/handoff e policy de route-exit; nao destroi PlayerActor diretamente.
```

Revogacao do caminho ativo:

```text
Qualquer texto de materializacao de PrototypePlayer no SessionOperational deve ser lido apenas como historico.
Nao e contrato ativo da Base 1.1.
```

Proximo corte tecnico:

```text
PlayerActorParticipationExit v0
```


## 20. Checkpoint congelado - Participation state vs runtime inactivity (2026-05-19)

Separacao normativa para PlayerActor v0:

```text
PlayerActorParticipationExit v0 define estado de participacao no lifecycle da Activity.
Nao equivale automaticamente a destruir/desregistrar/remover lifetime do PlayerActor.
Nao equivale automaticamente a inatividade tecnica por SetActive(false).
```

No v0, o estado alvo e:

```text
participationState = ExitedActivity
retention = RetainedForRoute
```

Regras:

```text
PlayerActorParticipationExit nao destroi PlayerActor.
PlayerActorParticipationExit nao remove RouteOwned/RouteScoped.
SessionActivityPipeline decide participation exit local da Activity.
Gate/InputMode apenas executam efeitos tecnicos por command.
Route exit continua sendo o ponto de policy explicita:
ReleasePlayersOnRouteExit
PersistPlayersAcrossRoutes
```

## 21. Checkpoint CLOSED - PlayerActor v0 RouteOwned lifecycle + Participation Enter/Reenter (2026-05-19)

Status formal:

```text
PlayerActor v0 - RouteOwned lifecycle + Participation Enter/Reenter - CLOSED
```

Consolidacao normativa:

```text
PlayerPreparation no SessionOperational permanece planned_only/intencao/payload.
PlayerActor v0 nasce no SessionActivityPipeline/ActivitySetup.
Lifetime padrao do PlayerActor v0 permanece RouteOwned/RouteScoped.
ParticipationExit ocorre antes da DeactivationWindow.
ParticipationEnter/Reenter reutiliza PlayerActor retido compativel.
Catalog LoopToFirst e respeitado pela SessionActivityPipeline para permitir reentrada em Activity posterior.
```

Regras fechadas para Enter/Reenter:

```text
ActivitySetup materializa PlayerActor quando nao houver retido compativel.
ActivitySetup reutiliza PlayerActor retido quando houver compativel.
Reenter atualiza participationState=ActiveInActivity.
Reenter preserva retention=RetainedForRoute.
Reenter atualiza currentActivityId/currentEntrySequence para a Activity/entry atuais.
Reenter nao usa SetActive(true/false) como semantica.
Reenter nao destroi nem recria PlayerActor.
```

Smoke de referencia fechado:

```text
activity_01 materializa PlayerActor
activity_01 retem PlayerActor
activity_02 completa
catalogo loopa para activity_01
activity_01 reentra usando PlayerActor retido sem nova materializacao
```

Divida documentada (sem mudanca funcional agora):

```text
No caminho ativo de ActivitySetup, o contrato/fact foi alinhado para:
- PlayerActorActivityParticipationPlanResolved

Futuro reservado para route-exit release (fora do corte atual):
- PlayerActorRouteExitReleasePlanResolved
```

## 22. Checkpoint CLOSED - PlayerActorReset v0 (2026-05-19)

Status:

```text
PlayerActorReset v0 - CLOSED
```

Consolidacao normativa:

```text
Reset ocorre no ActivitySetup.
Pipeline decide groups.
Adapter executa.
Endpoints do PlayerActor aplicam campos internos por grupo.
Nao ha ResetAll cego.
Ready so e emitido apos PlayerActorResetApplied.
```

Groups v0:

```text
Placement
ActivityParticipation
MovementTransient
```

Auditabilidade de skip:

```text
PlayerActorResetApplied expõe:
- appliedGroupNames
- skippedGroupNames
- skippedGroupReasons
```

Reason codes fechados no v0:

```text
Placement:
- placement_not_required
- optional_placement_missing
- no_placement_declared
- no_endpoint_supports_group
- invalid_required_placement (fail-fast)

MovementTransient sem endpoint:
- no_endpoint_supports_group
```

Smoke validado:

```text
primeira entrada materializa + reset antes de Ready
reenter retido + reset antes de Ready
PlayerActorReleasePlanResolved nao reaparece
ResetAll/Destroy/SetActive do PlayerActor nao aparecem
```

## 23. Checkpoint congelado - Fronteira PlayerPreparation x rails deterministas da SessionActivity (2026-05-19)

Contrato complementar congelado:

```text
PlayerPreparation permanece payload/intencao para handoff.
Nao autoriza inferencia de completion de rail local da SessionActivity.
```

Regras de fronteira:

```text
SessionOperational prepara e transporta.
SessionActivity decide lifecycle local (entry/completion/restart/navigation/route-exit).
Enquanto houver pending operation de rail local, route scene unload permanece bloqueavel por contrato canonico.
```


## 24. Checkpoint complementar - RestartCurrentActivity pós PlayerInput/Movement/Camera (2026-05-22)

Status:

```text
RestartCurrentActivity com participante controlável - PASS funcional atualizado
```

O smoke grande confirmou que o `PlayerActor` materializado/retido permanece compatível com restart local depois dos cortes de input, movement e camera.

Evidência congelada:

```text
activity_01 restart:
- nova entrySequence=2
- PlayerInput permanece resolvido pelo PlayerActor materializado
- MovementBindingCompleted controlEnabled=false
- CameraBindingCompleted
- ActivationWindowReady
- MovementControlEnabled somente após CompleteActivationWindow

activity_02 restart no-content:
- nova entrySequence=4
- MovementBindingRetained
- MovementControlEnabled em ActivityRunning
```

Regra preservada:

```text
SessionActivityPipeline decide restart/lifecycle.
PlayerActor, PlayerInput, Movement e Camera endpoints executam side-effects ou expõem capacidade.
Nenhum endpoint decide ActivityRunning, restart ou continuidade.
```

Classificação:

```text
não é pendência ativa de PlayerPreparation;
era apenas recongelamento necessário após os novos checkpoints.
```
