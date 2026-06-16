# ACT-PROJ — RuntimeSpawned Projectile, SpawnPoint e Motion Plan

Data: 2026-06-16  
Status: Plano canônico atualizado / Sem implementação nova neste documento  
Base: Base 2.0 — Session Architecture Convergence  
Escopo: projectile runtime-spawned já existente; endurecimento de spawn lógico, spawn point dedicado, origin/direction e movimento próprio.

---

## 1. Objetivo

Registrar o plano antes de implementar novos cortes de projectile para evitar criação de trilhos paralelos, falsa generalização ou mistura de responsabilidades.

O projectile já existe e já dispara pelo trilho ativo:

```text
PlayerActorCommandInputHub
-> ActorProjectileFireEndpoint
-> PooledActorProjectileSpawnAdapter
-> PoolDefinition_PrimaryProjectile
-> RuntimeSpawnedActor
-> ActorProjectileSpawnRuntimeState
-> AudioRuntime pooled SFX
```

Este plano não recria fire, pool, `RuntimeSpawnedActor`, áudio nem spawner.

O objetivo é corrigir e completar o objeto projectile runtime-spawned:

1. manter `ACT-PROJ-SPAWN-1A` fechado: spawn lógico não exige renderer/material;
2. criar contrato/endpoint próprio de spawn point, fora de `ActorPresentationSlotKind`;
3. resolver `origin`/`direction` do disparo a partir de spawn point dedicado no ator que dispara;
4. adicionar movimento próprio de projectile depois que spawn point estiver resolvido;
5. preservar a diferença entre actor materializado pelo pipeline e actor runtime-spawned.

---

## 2. Estado atual confirmado

### 2.1 `ACT-PROJ-SPAWN-1A` fechado

`ACT-PROJ-SPAWN-1A — Remove visual hard-gate from pooled projectile spawn` está fechado com smoke PASS.

Resultado aceito:

```text
PooledActorProjectileSpawnAdapter
= adapter técnico de spawn lógico/runtime-spawned.

Renderer/material
= observação visual diagnóstica, não hard-gate de spawn.
```

Evidência esperada e já observada no smoke:

```text
ActorProjectileSpawnPoolRentRequested
RuntimeSpawnedActorMetadataBound
ActorProjectileSpawnVisualObserved
visualContract='optional_for_runtime_spawn'
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
```

Motivos de falha visual removidos do contrato de sucesso do spawn:

```text
projectile_spawn_visual_renderer_missing
projectile_spawn_visual_renderer_disabled
projectile_spawn_visual_material_missing
```

### 2.2 Teoria de Presentation confirmada no essencial

Auditoria posterior confirmou:

```text
Presentation = capability/endpoint/authoring local do Actor.
ActivityEntryActorPresentationStage = stage de orquestração de setup para actors que entram pela Activity.
SessionActivityPipeline / exit stages = owner de ordem de release/retention.
RuntimeSpawned projectile = trilho próprio de spawn/runtime; não passa pela ActivityEntryActorPresentationStage por tiro.
```

### 2.3 Teoria corrigida: `ActorPresentationContainer` não é spawn point

A auditoria negou a extensão anterior do plano:

```text
ActorPresentationContainer não é SpawnPoint no contrato atual.
ActorPresentationSlotKind.SpawnPoint não é compatível como extensão limpa sem criar novo owner/contrato.
```

Portanto, o corte antigo abaixo está cancelado:

```text
CANCELADO:
ACT-PROJ-SPAWNPOINT-1A — Add ActorPresentationSlotKind.SpawnPoint
```

Substituição canônica:

```text
ACT-PROJ-SPAWNPOINT-1A — Add dedicated Actor Spawn Point endpoint
```

---

## 3. Decisões canônicas

### 3.1 RuntimeSpawnedActor já é o trilho correto

Não criar novo spawner, manager ou rail paralelo.

O caminho ativo continua sendo:

```text
ActorProjectileFireProfileAsset
-> ActorProjectileSpawnProfileAsset
-> PoolDefinitionAsset
-> PooledActorProjectileSpawnAdapter
-> RuntimeSpawnedActor
```

`ActorMaterializationKind.RuntimeSpawned` continua sendo o separador entre actor de entrada/pipeline e actor spawnado em runtime.

### 3.2 Spawn/pool não exige renderer

`PooledActorProjectileSpawnAdapter` valida spawn lógico, não forma visual.

O spawn lógico pode falhar por:

```text
- PoolDefinition ausente ou inválido;
- pool retornou instância nula;
- RuntimeSpawnedActor ausente;
- metadata runtime não pôde ser bindada;
- posição/rotação inicial não pôde ser aplicada;
- instância não ficou ativa quando deveria;
- endpoint obrigatório de motion ausente, somente quando motion passar a ser requisito explícito.
```

O spawn lógico não deve falhar por:

```text
- Renderer ausente;
- Renderer desabilitado;
- material ausente;
- presentation ainda não materializada;
- projectile lógico collision-only/invisível.
```

### 3.3 Spawn point não pertence ao contrato de Presentation

Não usar:

```text
ActorPresentationSlotKind.SpawnPoint
ActorPresentationContainer como origem de spawn
FxRoot como muzzle/spawn point
```

Motivo:

```text
ActorPresentationContainer é container visual de presentation.
Spawn point é origem/direção de materialização runtime e precisa de owner próprio.
```

### 3.4 Criar endpoint/contrato dedicado de spawn point

Criar um owner próprio:

```text
ActorSpawnPointEndpoint
```

Ou, se a auditoria local indicar escopo estreito demais para genérico:

```text
ActorProjectileSpawnPointEndpoint
```

Decisão preferida atual:

```text
ActorSpawnPointEndpoint
```

Justificativa:

```text
Spawn point não é exclusivo de projectile.
Pode ser usado por projectile.primary, projectile.secondary, grenade.throw, spell.cast, item.drop, fx.emit etc.
```

Mas o endpoint não deve virar manager genérico. Ele deve ser uma capability local simples do Actor.

### 3.5 Projectile motion é capability própria de projectile

Não usar:

```text
PlayerMovementController
IActorMovementEndpoint
MovementBindingStage
ActivityGameplayControl receiver de movimento do player
```

Criar movimento próprio do projectile somente depois do spawn lógico e spawn point dedicado estarem corrigidos.

---

## 4. Não objetivos

Não fazer nesta frente:

```text
- recriar fire;
- recriar pool;
- recriar RuntimeSpawnedActor;
- criar ProjectileManager;
- criar ProjectilePresentationManager;
- criar ProjectileMovementManager;
- criar novo spawner paralelo;
- chamar ActivityEntryActorPresentationStage por tiro;
- usar renderer como contrato obrigatório de spawn;
- mover lifecycle de projectile para pipeline;
- implementar collision/damage/impact no mesmo corte de motion;
- implementar homing, spread, multi-shot ou radial arc antes do spawn point + motion linear básico;
- adicionar SpawnPoint em ActorPresentationSlotKind;
- reutilizar ActorPresentationContainer como semântica de spawn.
```

---

## 5. Ownership canônico

| Responsabilidade | Owner correto |
|---|---|
| Emitir input `FirePrimary` | `PlayerActorCommandInputHub` |
| Executar fire | `ActorProjectileFireEndpoint` |
| Resolver fire mode/profile | `ActorProjectileFireEndpoint` |
| Declarar pontos de spawn locais do Actor | `ActorSpawnPointEndpoint` |
| Resolver origem/direção para o tiro | `ActorProjectileFireEndpoint` ou helper local `ActorProjectileSpawnOriginResolver` lendo `ActorSpawnPointEndpoint` |
| Renter/spawn técnico | `PooledActorProjectileSpawnAdapter` |
| Identidade do actor spawnado | `RuntimeSpawnedActor` |
| Tracking/return por reset/release | `ActorProjectileSpawnRuntimeState` owned pelo `ActorProjectileFireEndpoint` |
| Forma visual do actor | `ActorPresentationEndpoint` / `ActorPresentationProfileAsset` / `UnityActorPresentationMaterializationAdapter` |
| Setup de presentation de actors de Activity | `ActivityEntryActorPresentationStage` |
| Release/retention de presentation | `SessionActivityPipeline` + exit stages/runtime state |
| Movimento do projectile | `ActorProjectileLinearMotionEndpoint` ou equivalente próprio de projectile |
| Pool técnico | `IPoolService` + `PoolDefinitionAsset` |
| Áudio do tiro | `ActorProjectileFireAudioAdapter` + `AudioGlobalSfxService` |

---

## 6. Respostas obrigatórias

### Qual pipeline é dono desta decisão?

Nenhum pipeline deve executar movement por tiro ou decidir renderer do projectile.

`ActivityEntryPipeline` pode preparar binding e presentation dos actors de entrada.  
`PooledActorProjectileSpawnAdapter` executa spawn técnico do runtime-spawned projectile.  
`ActorProjectileFireEndpoint` executa fire e resolve origin/direction.  
`ActorSpawnPointEndpoint` declara pontos de spawn locais do Actor.  
`ActorProjectileLinearMotionEndpoint` executa movement do projectile.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

| Item | Tipo |
|---|---|
| Spawn point local do Actor | endpoint/capability + authoring local |
| Origin/direction resolvidos | command payload runtime |
| Spawn pooled | adapter |
| Visual renderer/material | presentation/observability |
| Motion de projectile | endpoint/capability |
| Tracking/return | runtime state + reset/release endpoint |

### Isso é comportamento final ou bridge transitória?

- `ActorSpawnPointEndpoint` é comportamento final.
- Remover renderer hard-gate é correção final de fronteira.
- Logs de renderer/material no spawn adapter são observação diagnóstica.
- `ActorPresentationSlotKind.SpawnPoint` foi rejeitado; não implementar como bridge.

### Essa compatibilidade ainda é necessária?

Não preservar renderer obrigatório como compat. Não há produção dependente.

Não preservar `ActorPresentationContainer` como spawn point implícito. Isso criaria compat/bridge escondido.

### O erro está no sintoma ou na fronteira arquitetural errada?

Fronteira arquitetural errada: adapter técnico de spawn estava validando forma visual.

Outra fronteira corrigida: presentation visual não deve ser usada como contrato de spawn point.

### Existe owner duplicado para o mesmo lifecycle?

Risco existe se pipeline, spawn adapter, presentation e spawn point tentarem decidir forma/motion/lifecycle do projectile. Este plano evita duplicação:

```text
spawn adapter = spawn lógico
presentation = forma
spawn point endpoint = origem/direção local do Actor
motion endpoint = deslocamento
spawn runtime state = tracking/return
```

---

## 7. Cortes congelados

### 7.1 ACT-PROJ-SPAWN-1A — Remove visual hard-gate from pooled projectile spawn

**Status**  
CLOSED / smoke PASS.

**Objetivo**  
Remover renderer/material como requisito de sucesso do spawn lógico.

**Arquivos alterados no corte aplicado**

```text
Actors/Projectile/Runtime/PooledActorProjectileSpawnAdapter.cs
Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md
Actors/Docs/Audits/ACT-PROJ-SPAWN-1A-Remove-Visual-Hard-Gate.md
Actors/Projectile/README.md
```

**Mudança aplicada**

Antes:

```text
renderer ausente => Failed
renderer desabilitado => Failed
material inválido => Failed
```

Depois:

```text
RuntimeSpawnedActor ausente => Failed
pool null => Failed
metadata inválida => Failed
posição/rotação não aplicada => Failed
renderer ausente => Observed
renderer desabilitado => Observed
material inválido => Observed
```

**Logs de aceite**

```text
ActorProjectileSpawnVisualObserved
visualContract='optional_for_runtime_spawn'
ActorProjectileSpawnInstancePrepared
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
```

---

### 7.2 ACT-PROJ-SPAWNPOINT-1A — Add dedicated Actor Spawn Point endpoint

**Objetivo**  
Criar contrato/endpoint dedicado para pontos de spawn locais do Actor, sem reutilizar `ActorPresentationContainer` nem ampliar `ActorPresentationSlotKind`.

**Nome preferido**

```text
ActorSpawnPointEndpoint
```

**Escopo provável**

```text
Actors/SpawnPoints/Contracts/ActorSpawnPointContracts.cs
Actors/SpawnPoints/Runtime/ActorSpawnPointEndpoint.cs
Actors/SpawnPoints/Authoring/ActorSpawnPointId.cs ou equivalente, se o padrão do projeto pedir tipo forte
Actors/Runtime/ActorCapabilitySurface.cs
Resources/Actors/PlayerActor_v0.prefab
Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md
Actors/Docs/Audits/ACT-PROJ-SPAWNPOINT-1A-Dedicated-Actor-Spawn-Point-Endpoint.md
```

O caminho exato de pasta pode ser ajustado conforme auditoria local, mas não criar manager ou pipeline.

**Contrato mínimo esperado**

```text
ActorSpawnPointEndpoint
- declara um ou mais spawn points locais do Actor;
- expõe id lógico do ponto;
- expõe Transform/origin;
- expõe forward/direction;
- valida duplicidade/ausência conforme config;
- não executa spawn;
- não chama pool;
- não conhece projectile profile;
- não conhece presentation materialization;
- não decide lifecycle.
```

**Config mínima esperada**

```text
spawnPointId='projectile.primary'
required/default flag se necessário
Transform source explícito ou o próprio transform do item
```

**Não fazer neste corte**

```text
- não resolver tiro ainda;
- não alterar spawn adapter;
- não adicionar motion;
- não chamar ActivityEntryActorPresentationStage por tiro;
- não adicionar ActorPresentationSlotKind.SpawnPoint;
- não usar ActorPresentationContainer como origem de spawn.
```

**Critério de aceite**

```text
- endpoint compila como capability local do Actor;
- sem owner de lifecycle novo;
- sem manager;
- sem fallback silencioso;
- docs registram que spawn point é contrato próprio, não presentation slot.
```

---

### 7.3 ACT-PROJ-MUZZLE-1A — Resolve projectile fire origin from Actor Spawn Point endpoint

**Objetivo**  
`ActorProjectileFireEndpoint` deixa de depender do próprio transform quando o fire mode exigir spawn point.

**Arquivos prováveis**

```text
Actors/Projectile/Authoring/ActorProjectileFireProfileAsset.cs
Actors/Projectile/Contracts/ActorProjectileContracts.cs
Actors/Projectile/Runtime/ActorProjectileFireEndpoint.cs
Actors/Projectile/Binding/ActorProjectileFireCommandBindingExecutor.cs
Actors/Projectile/Runtime/ActorProjectileSpawnOriginResolver.cs   // se helper for criado
Actors/SpawnPoints/Contracts/ActorSpawnPointContracts.cs
Resources/Actors/PlayerActor_v0.prefab
Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md
Actors/Docs/Audits/ACT-PROJ-MUZZLE-1A-SpawnPoint-Origin-Resolution.md
```

**Mudança esperada**

FireMode passa a carregar configuração de origem:

```text
muzzlePolicy = ActorForward | NamedSpawnPoint
spawnPointId = projectile.primary
```

Regra:

```text
ActorForward:
  mantém comportamento atual.

NamedSpawnPoint:
  exige ActorSpawnPointEndpoint configurado e ponto encontrado.
  ausente => FireRejected MissingSpawnPoint.
```

**Logs esperados**

```text
ActorProjectileSpawnPointResolved
spawnPointId='projectile.primary'
origin='...'
direction='...'

ActorProjectileFireCommandBuilt
origin='<spawn point position>'
direction='<spawn point forward>'
```

**Critério de aceite**

```text
- FirePrimary continua funcionando;
- origin/direction vêm do ActorSpawnPointEndpoint quando policy pede;
- policy ActorForward preserva comportamento atual;
- spawn point ausente não faz fallback silencioso;
- erro de configuração ausente é bloqueio observável.
```

---

### 7.4 ACT-PROJ-MOTION-1A — Runtime spawned projectile linear motion endpoint

**Objetivo**  
Adicionar movimento próprio do projectile runtime-spawned.

**Arquivos prováveis**

```text
Actors/Projectile/Runtime/ActorProjectileLinearMotionEndpoint.cs
Actors/Projectile/Contracts/ActorProjectileMotionContracts.cs
Actors/Projectile/Authoring/ActorProjectileMotionProfileAsset.cs
Actors/Projectile/Authoring/ActorProjectileSpawnProfileAsset.cs
Actors/Projectile/Runtime/PooledActorProjectileSpawnAdapter.cs
Resources/Actors/ProjectileActor_Primary.prefab
Resources/Actors/ActorProjectileSpawnProfile_PrimaryProjectile.asset
Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md
Actors/Docs/Audits/ACT-PROJ-MOTION-1A-Projectile-Linear-Motion.md
```

**Mudança esperada**

```text
ActorProjectileSpawnProfileAsset
-> referencia motion profile

PooledActorProjectileSpawnAdapter
-> depois de BindRuntimeMetadata
-> resolve IActorProjectileMotionEndpoint no spawned instance
-> inicializa motion com origin/direction/speed
```

**Não usar**

```text
PlayerMovementController
IActorMovementEndpoint
MovementBindingStage
ActivityGameplayControl movement receiver
```

**Logs esperados**

```text
ActorProjectileMotionInitialized
ActorProjectileMotionStarted
ActorProjectileMotionStopped ou ActorProjectileMotionReset
```

**Critério de aceite**

```text
- projectile se move após spawn;
- movimento usa direction do command resolvido;
- sem input dependency;
- sem permission receiver de player movement;
- reset/release ainda retorna tracked projectiles ao pool;
- sem checkpointStatus='Failed'.
```

---

### 7.5 ACT-PROJ-LIFE-1A — Projectile lifetime/despawn

**Status**  
Planejado, fora dos cortes imediatos.

**Motivo**  
Lifetime/despawn/impact adiciona outra decisão de lifecycle. Não misturar com motion inicial.

Escopo futuro:

```text
- lifetime seconds;
- return to origin pool por lifetime;
- collision/impact depois;
- sem coroutine escondida no pool service;
- sem lifecycle duplicado entre projectile endpoint, pool e fire endpoint.
```

---

## 8. Ordem obrigatória

Não inverter sem nova auditoria.

```text
1. ACT-PROJ-SPAWN-1A
   Fechado. Corrigiu spawn lógico: renderer/material deixaram de ser gate.

2. ACT-PROJ-SPAWNPOINT-1A
   Criar endpoint/contrato dedicado de spawn point.

3. ACT-PROJ-MUZZLE-1A
   Resolver origin/direction do tiro por ActorSpawnPointEndpoint.

4. ACT-PROJ-MOTION-1A
   Adicionar movimento linear próprio do projectile.

5. ACT-PROJ-LIFE-1A
   Lifetime/despawn/impact depois.
```

---

## 9. Evidência da auditoria

Auditoria de spawn lógico confirmou:

```text
- RuntimeSpawnedActor já tem trilho próprio.
- Não há outro spawner ativo além de PooledActorProjectileSpawnAdapter.
- Renderer/material hard-gate estava no spawn adapter e foi removido em ACT-PROJ-SPAWN-1A.
```

Auditoria de Presentation confirmou:

```text
- Presentation está modelada como capability/endpoint/authoring local do Actor, exposta por ActorCapabilitySurface.
- ActivityEntryActorPresentationStage orquestra resolução, materialização, retenção e release de actors que entram pela Activity.
- ActivityEntryActorPresentationStage não é owner indevido do visual.
- RuntimeSpawned projectile usa trilho próprio e não deve passar por ActivityEntryActorPresentationStage por tiro.
- RuntimeSpawnedActor pode aparecer em scanners genéricos se possuir surface compatível, mas isso não cria trilho de presentation por tiro.
- ActorPresentationContainer é container visual, não spawn point.
- ActorPresentationSlotKind.SpawnPoint não é extensão limpa do modelo atual.
```

---

## 10. Prompts base para Codex nos próximos cortes

Usar prompts curtos e específicos por corte.

### Para ACT-PROJ-SPAWNPOINT-1A

```text
Implemente ACT-PROJ-SPAWNPOINT-1A.

Objetivo: criar contrato/endpoint dedicado de spawn point local do Actor. Não usar ActorPresentationSlotKind. Não usar ActorPresentationContainer como spawn point.

Nome preferido: ActorSpawnPointEndpoint.

O endpoint deve declarar pontos nomeados do Actor e expor origin/forward para consumidores, sem executar spawn, sem chamar pool, sem conhecer projectile profile, sem decidir lifecycle.

Audite antes de editar se existe padrão atual para capability local exposta por ActorCapabilitySurface. Use esse padrão.

Não alterar fire ainda.
Não alterar spawn adapter.
Não adicionar motion.
Não chamar ActivityEntryActorPresentationStage por tiro.
Não criar manager.
Não criar fallback silencioso.
Não rodar build, compile, tests, smoke, playmode ou batchmode.

Atualize ADR 0005 e crie audit doc do corte.
Entregue lista de arquivos alterados e smoke esperado.
```

### Para ACT-PROJ-MUZZLE-1A

```text
Implemente ACT-PROJ-MUZZLE-1A.

Objetivo: ActorProjectileFireEndpoint deve resolver origin/direction por ActorSpawnPointEndpoint quando o FireMode pedir NamedSpawnPoint. Policy ActorForward mantém comportamento atual. Spawn point ausente deve rejeitar o fire com erro observável, sem fallback silencioso.

Não usar ActorPresentationContainer como spawn point.
Não adicionar ActorPresentationSlotKind.SpawnPoint.
Não alterar motion.
Não alterar pool/audio.
Não chamar ActivityEntryActorPresentationStage por tiro.
Não rodar build, compile, tests, smoke, playmode ou batchmode.

Atualize ADR 0005 e crie audit doc do corte.
Entregue lista de arquivos alterados e smoke esperado.
```

### Para ACT-PROJ-MOTION-1A

```text
Implemente ACT-PROJ-MOTION-1A.

Objetivo: adicionar motion linear próprio para projectile runtime-spawned. O motion deve ser inicializado pelo PooledActorProjectileSpawnAdapter após RuntimeSpawnedActor.BindRuntimeMetadata, usando origin/direction já resolvidos no fire command.

Não usar PlayerMovementController.
Não usar IActorMovementEndpoint.
Não usar MovementBindingStage.
Não criar permission receiver de player movement.
Não implementar collision/damage/impact/lifetime neste corte.
Não rodar build, compile, tests, smoke, playmode ou batchmode.

Atualize ADR 0005 e crie audit doc do corte.
Entregue lista de arquivos alterados e smoke esperado.
```

---

## 11. Critério global de PASS

Nenhum corte é PASS sem smoke/log do usuário.

PASS exige:

```text
- sem FATAL;
- sem Exception;
- sem route_transition_failed;
- sem checkpointStatus='Failed';
- sem fallback silencioso;
- sem trilho paralelo novo;
- side-effects no adapter/stage correto;
- pipeline sem responsabilidade indevida;
- identities separadas;
- logs mostram owner correto;
- reset/release preserva return ao pool;
- projectiles runtime-spawned continuam pelo trilho PooledActorProjectileSpawnAdapter + RuntimeSpawnedActor.
```
