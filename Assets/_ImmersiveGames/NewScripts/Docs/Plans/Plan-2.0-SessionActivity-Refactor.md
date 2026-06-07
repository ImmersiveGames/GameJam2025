# Plano de RefatoraÃ§Ã£o â€” SessionActivity Base 2.0

> Historical planning document. The active normative boundary is frozen by `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md` and `ENTRY-BOUNDARY-DOC-0` inside it. Use this plan only as refactor context; do not read inventory-backed setup/binding as an allowed final shape.

> Closure note: `ENTRY-BOUNDARY Closure â€” PASS funcional + PASS arquitetural parcial` is now recorded in the ADR. Treat the current shape as frozen for entry boundary work: `ActivityCapabilityInventory` remains transversal only; Presentation/Attribute/Camera moved to contributions; gate binding is owned explicitly; ObjectEmission stays out of inventory.

> ACT-EMIT-2 closure note: `ACT-EMIT-2 â€” ObjectEmission Pool/Rent/Return MVP â€” PASS` is now recorded in `ADR-2.0-0002`. The MVP shape is frozen: `FirePrimary` is routed through `PlayerActorCommandInputHub`, `ActorObjectEmitterEndpoint` builds the resolved payload, `ObjectEmissionRuntimeComposer`/`ObjectEmissionPoolRuntimeBridge` wire `IPoolService`, `ObjectEmissionPoolAdapter` owns `Rent`, `ObjectEmissionPoolReturnSink` owns `Return`, and the runtime stays out of scanner/inventory/setup/binding/gate reopening. Next possible cuts start at `ACT-EMIT-3A`, `ACT-EMIT-4A`, `ACT-EMIT-5A` and `ACT-EMIT-6A`.

## Objetivo

Decompor `SessionActivityPipeline` sem criar trilho paralelo, fallback silencioso ou novo owner ambÃ­guo.

O alvo Ã© separar:

```text
SessionActivityPipeline = lifecycle macro / ordem / handoff
ActivityEntryPipeline = entry lifecycle por Activity entry
Stages = passos determinÃ­sticos
Policies = classificaÃ§Ã£o
Commands = payload runtime resolvido
Facts = registro
Adapters = side-effects
Endpoints = reaÃ§Ã£o/capacidade local
```

## Premissas

- Base 1.1 atual Ã© baseline funcional seguro.
- NÃ£o preservar compatibilidade de shape ruim.
- NÃ£o usar `partial` para esconder pipeline monolÃ­tico.
- NÃ£o criar manager/coordinator genÃ©rico.
- NÃ£o pedir ao Codex build, compile, tests, smoke, playmode ou batchmode.
- Toda implementaÃ§Ã£o precisa voltar com smoke/log manual antes de PASS.


## Status consolidado pÃ³s-SA-CONTENT-REL-2

Este plano registra a direÃ§Ã£o inicial. A ordem normativa e os checkpoints detalhados vivem no `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`.

Cortes fechados relevantes apÃ³s a decomposiÃ§Ã£o principal:

```text
SA-6C   Movement binding stage â€” CLOSED / PASS; shim morto removido.
SA-6D   Camera binding stage â€” CLOSED / PASS.
SA-8B   ObjectRelease/SnapshotCapture cleanup â€” CLOSED / PASS; helper morto removido.
SA-8C   Actor release/participation exit cleanup â€” CLOSED / PASS.
SA-11A  ActivityEntry state/context extraction â€” CLOSED / PASS arquitetural do ownership de entry.
SA-12   Command/contract hygiene â€” CLOSED apÃ³s SA-12F5 e SA-12F-MOV-H1.
SA-CONTENT-REL-1/2 ActivityContent loaded set ownership normalization â€” CLOSED / PASS.
```

Fechamento documental adicional:

```text
SA-16B confirmou que o release de ActivityContent continua async apenas por side-effect Unity.
CompleteActivityContentSceneUnloadOperation foi reduzido ao callback tecnico de completion.
ContinueAfterActivityContentUnloadCompletionAsync(...) permaneceu no SessionActivityPipeline como continuation macro explicita.
Nao houve criacao de ActivityContentReleasePipeline ou ActivityExitPipeline.
SA-16C confirmou que PendingOperation permanece state tecnico e que CompletePendingOperation foi limpo nos unloads de ActivationWindow e DeactivationWindow.
SessionActivityPendingOperationKind ficou restrito a operacoes async reais pendentes.
SA-16D confirmou que PlayerInput canonical actions explicit composition resolve o asset canonico na composition root, injeta ate o ActivityEntryPipeline e remove o lookup global do PlayerInputBindingAdapter.
```

SA-16B  ActivityContent async release completion boundary â€” CLOSED.
SA-16B1 ActivityContent unload callback boundary cleanup â€” PASS funcional + PASS arquitetural do corte.
SA-16C  PendingOperation callback contract â€” CLOSED.
SA-16C1 PendingOperation window unload callback boundary cleanup â€” PASS funcional + PASS arquitetural do corte.
SA-16C2 PendingOperation kind contract cleanup â€” PASS funcional + PASS arquitetural do corte.
SA-16D  PlayerInput canonical actions explicit composition â€” CLOSED / PASS funcional + PASS arquitetural do corte.

Ownership consolidado:

```text
SessionActivityPipeline = lifecycle macro / ordem / handoffs / continuation.
ActivityEntryPipeline = owner do lifecycle e state canÃ´nico de entry.
ActivityContentRuntimeState = owner canÃ´nico do loaded set vivo de ActivityContent.
ActivityContentReleaseRuntimeState = state operacional da release assÃ­ncrona, sem loaded set duplicado.
SessionActivityRuntimeState = state macro legítimo, sem mirrors de entry sem consumer comprovado.
```

Próximo trabalho deve começar por auditoria dos débitos restantes reais; não reabrir cortes fechados sem regressão explícita.

## Corte SA-0 â€” Congelamento documental

### Objetivo

Adicionar ADR e plano ao projeto sem alterar runtime.

### AÃ§Ã£o

- Criar `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`.
- Atualizar Ã­ndice/README de ADRs se existir.
- Registrar que a auditoria atual Ã© fonte de decisÃ£o.

### Aceite

- Sem alteraÃ§Ã£o de cÃ³digo runtime.
- Documento reflete owner de `SessionActivityPipeline`, `ActivityEntryPipeline`, Host, stages, policies, commands, facts, adapters e endpoints.

### Risco

Baixo.

## Corte SA-1 â€” RouteExit teardown owner unification

### Objetivo

Remover owner duplicado entre `SessionActivityHost` e `SessionActivityPipeline` para `RouteExit teardown`.

### AÃ§Ã£o

- Auditar chamadas externas de `ISessionActivityRouteExitTeardownBoundary`.
- Fazer `SessionActivityPipeline` ser owner Ãºnico de decisÃ£o/estado de teardown.
- Fazer `SessionActivityHost` virar delegador/endpoint externo.
- Mover classificaÃ§Ã£o duplicada de stages para policy Ãºnica ou mÃ©todo Ãºnico canÃ´nico.
- Remover hardcode QA de `activity_01` se estiver acoplado ao lifecycle real.

### NÃ£o fazer

- NÃ£o criar `ActivityEntryPipeline` ainda.
- NÃ£o mexer em input/movement/camera/adapters.
- NÃ£o mexer em service locator/global registry ainda, salvo se for necessÃ¡rio para remover decisÃ£o de lifecycle.

### Aceite arquitetural

- Host nÃ£o decide lifecycle de teardown.
- Pipeline decide route-exit teardown.
- Sem duas listas divergentes de route-exit/deactivation stages.
- Sem fallback para caminho antigo.

### Smoke obrigatÃ³rio

- Boot -> Menu -> Sandbox.
- CompleteActivationWindow.
- BackToMenu / RouteExit.
- Confirmar `RouteExitBackToMenu PASS`.
- Confirmar ausÃªncia de `FATAL`, `Exception`, `route_transition_failed`, foreign/stale indevido.

### Risco

MÃ©dio. Pode quebrar contrato externo de `SessionOperational` se o Host deixar de responder corretamente.

## Corte SA-2 â€” ActivityEntryPipeline shell canÃ´nico

### Objetivo

Criar `ActivityEntryPipeline` concreto como owner real de entry, sem ainda migrar todos os stages.

### AÃ§Ã£o

- Criar `ActivityEntryPipeline` com `ExecuteAsync(ActivityEntryCommand)`.
- Criar `ActivityEntryCommand`, `ActivityEntryResult`, `ActivityEntryResultKind` se os contratos atuais forem insuficientes.
- Compor o pipeline por DI explÃ­cita, nÃ£o por service locator.
- Fazer `SessionActivityPipeline` chamar `ActivityEntryPipeline` no ponto de entry.
- O primeiro corte pode migrar apenas um bloco pequeno e coeso de entry, removendo o caminho antigo equivalente.

### NÃ£o fazer

- NÃ£o criar fallback para o fluxo antigo.
- NÃ£o criar boundary que chama sub-stages como mini-pipeline.
- NÃ£o passar delegates/Func/stages dentro de command.
- NÃ£o mover ActivationWindow completion para `ActivityEntryPipeline`.

### Aceite arquitetural

- Existe classe concreta `ActivityEntryPipeline`.
- O path migrado tem owner Ãºnico.
- O caminho antigo equivalente foi removido ou deixou de ser chamado.
- Logs mostram inÃ­cio/fim de `ActivityEntryPipeline`.

### Smoke obrigatÃ³rio

- Boot -> Menu -> Sandbox.
- Activity entry inicial.
- CompleteActivationWindow.
- Confirmar chegada em `ActivityRunning`.
- Confirmar ausÃªncia de `FATAL`, `Exception`, foreign/stale indevido.

### Risco

Alto. O corte define a costura principal da decomposiÃ§Ã£o.

## Corte SA-3 â€” ActivityContent + Inventory para ActivityEntryPipeline

### Objetivo

Mover prepare/load/readiness de `ActivityContent` e preview/resolution de `ActivityCapabilityInventory` para `ActivityEntryPipeline`.

### AÃ§Ã£o

- Migrar stages de content load/prepare para entry pipeline.
- Migrar inventory preview/resolution para entry pipeline.
- Garantir que content obrigatÃ³rio ausente falhe explicitamente.
- Garantir skip explÃ­cito para content opcional/no-content.
- Rejeitar completions stale/foreign por `SessionActivityIdentity + ActivityId + EntrySequence`.

### NÃ£o fazer

- NÃ£o mover Actor setup inteiro no mesmo corte.
- NÃ£o alterar adapters jÃ¡ fail-fast.
- NÃ£o criar inventÃ¡rio universal em `ActivityContentProfileAsset`.

### Aceite arquitetural

- `ActivityEntryPipeline` Ã© owner de content/inventory para a entry.
- `SessionActivityPipeline` nÃ£o executa diretamente esses passos.
- Facts preservam nomes/checkpoints relevantes ou mapeiam novo owner claramente.

### Smoke obrigatÃ³rio

- Entry com `activity_01` content.
- TransiÃ§Ã£o para `activity_02` no-content/skip.
- `Activity01ToActivity02 PASS`.

### Risco

Alto. Content/inventory alimentam quase todos os stages posteriores.

## Corte SA-4 â€” Actor/Object setup para ActivityEntryPipeline

### Objetivo

Mover setup/readiness de actors, objects e capabilities para `ActivityEntryPipeline`.

### AÃ§Ã£o

Migrar em subcortes, nÃ£o em bloco Ãºnico, mas sem transformar especializaÃ§Ãµes concretas em trilhos arquiteturais:

1. ActorDiscovery / ActorInventoryFeed audit.
2. ActorReadiness genÃ©rico por `ActorScanTarget` / `ActorCapabilitySurface`.
3. ActorPresentation setup.
4. ActorAttributes setup.
5. ActorParticipation enter readiness.
6. ActivityObject reset/restore.
7. Input binding quando for capacidade de actor/participante, nÃ£o rail de player.
8. Movement binding quando for capacidade/endpoints do actor, nÃ£o branch global de player.
9. Camera binding por capability target, nÃ£o por tipo concreto de actor.
10. Permission target preparation com identidades separadas.

### NÃ£o fazer

- NÃ£o recriar rails `PlayerActor` vs `NonPlayerActor` como lifecycle global.
- NÃ£o criar subcorte/stage canÃ´nico nomeado por especializaÃ§Ã£o concreta (`PlayerActorReadiness`, `NonPlayerActorDiscovery`) quando o domÃ­nio correto Ã© `Actor`.
- NÃ£o alterar reaction local de permission.
- NÃ£o mover Deactivation/Release neste corte.
- NÃ£o misturar `ActorId`, `PlayerActorId`, `PlayerSlotId`, `ActorInstanceRuntimeId` e receiver tÃ©cnico.

### Aceite arquitetural

- Cada subcorte tem stage owner explÃ­cito.
- Capabilities usam `ActorCapabilitySurface`/inventory quando jÃ¡ migradas.
- AusÃªncia obrigatÃ³ria Ã© fail-fast.
- AusÃªncia opcional Ã© skip explÃ­cito.
- Sem fallback silencioso.

### Smoke obrigatÃ³rio

- RestartCurrentActivity PASS.
- Activity01ToActivity02 PASS.
- RouteExitBackToMenu PASS.
- Checkpoints de Presentation/Attribute/Participation/Input/Movement/Camera preservados.

### Risco

Muito alto. Deve ser dividido em subcortes pequenos.

## Corte SA-5 â€” Exit/Release decomposition audit

### Objetivo

Auditar e decidir se saÃ­da/release/snapshot continuam como stages chamados pelo `SessionActivityPipeline` ou se exigem um `ActivityExitPipeline` prÃ³prio.

### AÃ§Ã£o

- Auditar release de ActorPresentation, ActorParticipation exit, ActivityObject snapshot/release, ActivityContent unload, DeactivationWindow ordering.
- Separar o que Ã© macro lifecycle do que Ã© stage determinÃ­stico.
- SÃ³ criar `ActivityExitPipeline` se houver owner final claro.

### NÃ£o fazer

- NÃ£o criar `ActivityExitPipeline` apenas por simetria com entry.
- NÃ£o mover release antes de confirmar ordering de DeactivationWindow.
- NÃ£o quebrar RouteExit ordering congelado.

### Aceite arquitetural

- DecisÃ£o explÃ­cita: stages de exit sob `SessionActivityPipeline` ou pipeline prÃ³prio.
- Sem owner duplicado.
- Sem release antes da janela correta.

### Smoke obrigatÃ³rio

- CompleteCurrentActivity.
- Activity01ToActivity02.
- RestartCurrentActivity.
- BackToMenu / RouteExit.

### Risco

Alto. Exit mistura deactivation, snapshot, release e route-exit.

## Corte SA-6 â€” Permission identity cleanup

### Objetivo

Separar domÃ­nio de identidade em permission target/receiver.

### AÃ§Ã£o

- Auditar `PlayerMovementPermissionReceiver.TargetsCurrentActor`.
- Separar `PlayerActorId`, `PlayerSlotId`, `ReceiverId` e futuro `PermissionTargetId`.
- Remover comparaÃ§Ã£o cruzada como fallback.
- Manter receiver player-specific enquanto movement for player-specific.

### NÃ£o fazer

- NÃ£o generalizar movement para todos os actors neste corte.
- NÃ£o criar fallback por `ActorId`.
- NÃ£o alterar local reaction.

### Aceite arquitetural

- Nenhum domÃ­nio de identidade Ã© comparado como equivalente.
- Missing identity Ã© observÃ¡vel.
- Permission runtime continua command/fact, sem side-effect direto.

### Smoke obrigatÃ³rio

- Movement control bloqueado antes de ActivityRunning.
- Movement control liberado em ActivityRunning.
- Movement control bloqueado em completion/route-exit.
- Sem `PermissionTargetIdentityUnresolved` em cenÃ¡rio vÃ¡lido.

### Risco

MÃ©dio. Pode quebrar receivers existentes se a separaÃ§Ã£o for agressiva.

## Corte SA-7 â€” Host/composition boundary cleanup

### Objetivo

Remover `DependencyManager.Provider` e registros globais do Host quando a fronteira de lifecycle jÃ¡ estiver estabilizada.

### AÃ§Ã£o

- Auditar `RegisterGlobal*` / `UnregisterGlobal*`.
- Mover composiÃ§Ã£o/global registry para composition root/installer correto.
- Host fica boundary/endpoint externo.

### NÃ£o fazer

- NÃ£o iniciar antes de SA-1 e SA-2.
- NÃ£o alterar bootstrap global sem auditoria de dependentes.

### Aceite arquitetural

- Host nÃ£o usa service locator para lifecycle/composition ativa.
- Composition root registra dependÃªncias.
- Sem regressÃ£o de acesso externo ao boundary.

### Smoke obrigatÃ³rio

- Full smoke de rota/activity.

### Risco

MÃ©dio/alto. Pode quebrar bootstrap.

## Corte SA-8 â€” State/fact hygiene

### Objetivo

Reduzir state mutÃ¡vel e fact emission dentro do pipeline macro apÃ³s os owners estarem corretos.

### AÃ§Ã£o

- Auditar runtime state usado por SessionActivity.
- Separar fact recording de decisÃ£o.
- Garantir que recorder nÃ£o decide lifecycle/policy.

### NÃ£o fazer

- NÃ£o repetir erro do recorder crescendo alÃ©m de fact/trace.
- NÃ£o mover result building para recorder.

### Aceite arquitetural

- State Ã© tÃ©cnico e explÃ­cito.
- Facts nÃ£o executam side-effects.
- Recorder nÃ£o decide stage order.

### Smoke obrigatÃ³rio

- Full smoke.

### Risco

MÃ©dio. SÃ³ deve ocorrer depois da decomposiÃ§Ã£o principal.

## Ordem recomendada

```text
SA-0 -> SA-1 -> SA-2 -> SA-3 -> SA-4 subcortes -> SA-5 audit -> SA-6 -> SA-7 -> SA-8
```

## Primeiro prompt recomendado para Codex

```text
Audite e implemente somente o Corte SA-1 de SessionActivity Base 2.0.

Objetivo: unificar o owner de RouteExit teardown.

Regras:
- NÃ£o rode build, compile, tests, smoke, playmode ou batchmode.
- Antes de alterar, audite chamadas de ISessionActivityRouteExitTeardownBoundary, SessionActivityHost.RequestRouteExitTeardown/AwaitRouteExitTeardownAsync e os mÃ©todos equivalentes no SessionActivityPipeline.
- SessionActivityPipeline deve ser o Ãºnico owner de decisÃ£o/estado de RouteExit teardown.
- SessionActivityHost deve virar delegador/endpoint externo, sem classificar lifecycle/stages como owner final.
- Remova ou consolide listas duplicadas de stage policy se estiverem no mesmo domÃ­nio de decisÃ£o.
- NÃ£o crie ActivityEntryPipeline ainda.
- NÃ£o altere input/movement/camera/adapters.
- NÃ£o crie fallback para caminho antigo.

Entregue:
1. arquivos alterados;
2. resumo do novo ownership;
3. confirmaÃ§Ã£o de que nÃ£o hÃ¡ dois owners de RouteExit teardown;
4. como gerar smoke manual para Boot -> Menu -> Sandbox -> CompleteActivationWindow -> BackToMenu.
```

## EvidÃªncia exigida apÃ³s cada implementaÃ§Ã£o

NÃ£o aceitar PASS sem log/smoke contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RestartCurrentActivity PASS quando aplicÃ¡vel
Activity01ToActivity02 PASS quando aplicÃ¡vel
RouteExitBackToMenu PASS
owner correto visÃ­vel nos logs
sem fallback silencioso
sem trilho paralelo novo
```
## SA-16A - Movement / GameplayControl / Reset / Save boundary

### Status

SA-16A: CLOSED.
SA-16A1: PASS funcional + PASS arquitetural do corte.
SA-16A2: PASS funcional + PASS arquitetural do corte.

### Registro

- `MovementBindingAdapter` deixou de publicar gate state e ficou como preparation/binding tecnico.
- `ActivityEntryMovementBindingStage` continua como owner da publicacao inicial de `Blocked` antes de `ActivityRunning`.
- `ActivityCapabilityPermissionRuntime` continua aplicando command/fact/snapshot e notificando receivers.
- `PlayerMovementPermissionReceiver` continua sendo reaction local.
- `PlayerActorDefaultResetEndpoint` passou a suportar `MovementTransient` via `PlayerMovementController.ClearMovementState()`.
- `Movement` continua fora de Save/Snapshot e fora de lifecycle macro.
- `ActivityEntryParticipantBindingStage` continua dono do mapping `RuntimeTransient -> MovementTransient`.
- `ActorResetAdapter` continua sendo o executor canonico de reset.

### Invariantes registradas

- Movement e uma capability local de Actor.
- MovementBinding e stage de ActivityEntryPipeline.
- Gate/control nao pertence ao MovementController.
- Movement pode expor endpoint bloqueavel/reagivel e reset transitorio.
- Movement nao e save contributor por padrao.
- Save so consome snapshot provider explicito.
- Reset nao e Save.
- Reset nao decide lifecycle.
- Receiver local reage; nao decide policy.
- Pipeline decide macro lifecycle; nao manipula componente de Movement diretamente.

