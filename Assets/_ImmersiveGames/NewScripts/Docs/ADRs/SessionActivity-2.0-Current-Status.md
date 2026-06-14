# SessionActivity 2.0 — Current Status

## Status atual

Baseline funcional congelado após os cortes:

```text
RESET-ARCH-7
RESET-OBS-1
RESET-OBS-1-FIX1
RESET-OBS-2
RESET-OBS-3/FIX1/FIX2
SA-12F5A
```

## Smoke aceito

```text
sem error CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS
```

## Reset — decisões fechadas

```text
ActivityResetBoundaryEligibility.All removido do runtime ativo.
RuntimeAll é o agregado explícito quando o endpoint aceita todos os intents runtime aplicáveis.
Reset continua dirigido por ActivityResetIntent + ActivityResetStateProfileKind.
ResetGroup/ActivityStateResetGroup não voltam como policy de execução.
TargetGroups permanecem reservados para corte futuro explícito.
```

Observabilidade de reset fechada por agora:

```text
ActorResetInventoryReferencesResolved fica no stage agregador.
ActorResetEndpointAppliedFromInventory fica compacto por endpoint.
ActorResetQaAppliedFromInventory não usa sourceReferenceCount no sucesso.
ActorProjectileSpawnedRuntimeObjectsStateProfileApplied é o summary canônico de retorno ao pool.
```

## SessionActivity — decisões fechadas

```text
SA-12F5A removeu SessionActivityDefinition dos caminhos residuais conhecidos em commands/stages runtime.
SessionActivityDefinition pode continuar em boundary/catálogo/stage quando authoring data real ainda é necessário.
Commands runtime não devem usar SessionActivityDefinition como carrier genérico.
```

## Pendências reais

```text
SA-12F5B — auditoria final de SessionActivityDefinition residual no próximo pacote completo.
SA-12F-MOV-H1 — mover retained PlayerActor target projection para ActivityEntryPipeline / ActivityEntryActorInventoryStage.
PERMISSION-OBS-1 — futuro, após auditoria; não pertence à frente RESET-OBS.
ACT-OBS-* — futuro, para projectile/command logs; não pertence à frente RESET-OBS.
```

## Próxima ação

```text
Aguardar envio do pacote completo atualizado.
Executar auditoria SA-12F5B.
Não iniciar novo corte de implementação antes da auditoria.
```
