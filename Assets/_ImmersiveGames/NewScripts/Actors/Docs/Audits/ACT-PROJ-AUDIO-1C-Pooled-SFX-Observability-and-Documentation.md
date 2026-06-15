# ACT-PROJ-AUDIO-1C — Pooled SFX Observability and Documentation

Status: Applied / Pending compile + smoke

## Objetivo

Adicionar observabilidade suficiente para validar o caminho `FireMode -> AudioSfxCueAsset -> IGlobalAudioService -> pooled SFX voice pool` sem alterar gameplay, pools, binding, permission, reset ou lifecycle.

Este corte não cria feature nova de áudio. Ele fecha a evidência operacional da integração de áudio de tiro criada em `ACT-PROJ-AUDIO-1A/1B`.

## Fronteira normativa

```text
ActorProjectileFireProfileAsset.FireMode = define qual cue e volume do tiro.
ActorProjectileFireAudioAdapter = monta AudioPlaybackContext e chama IGlobalAudioService.
AudioGlobalSfxService = executa playback, resolve profile, aplica volume, spatial e policy.
IPoolService = executa rent/return técnico do pool de vozes.
PoolDefinition_AudioProjectileFireVoices = pool global técnico.
```

## O que mudou

- `AudioGlobalSfxService.Execution` agora loga a configuração final do `AudioSource`:
  - cue;
  - clip;
  - spatial/spatialBlend;
  - minDistance/maxDistance;
  - volumeScale;
  - baseVolume;
  - finalVolume;
  - volumes globais/categoria;
  - pitch;
  - mixer group.
- `AudioGlobalSfxService.Pooling` agora loga rent de voz pooled com:
  - cue/cueId;
  - voice profile;
  - pool definition;
  - instance alugada;
  - activeBefore/activeAfter;
  - budget;
  - allowDirectFallback;
  - releaseGraceSeconds;
  - position;
  - finalVolume;
  - spatial config.
- `AudioGlobalSfxService.Pooling` agora loga return de voz pooled com:
  - instance;
  - profile;
  - pool;
  - activeAfter;
  - delayed;
  - completion reason.

## O que não mudou

```text
Não muda volume global.
Não muda pool de projectile.
Não muda pool de áudio.
Não muda cue de tiro.
Não muda permission/gate.
Não muda reset/release.
Não muda ordem de disparo.
Não cria ProjectileAudioManager.
Não cria ProjectileAudioPool.
```

## Critério de smoke

Procurar:

```text
ActorProjectileFireAudioCuePlayed
volumeScale='2,5' ou valor configurado
[Audio][SFX] Source configured
[Audio][SFX] Pool rent event='AudioSfxPooledVoiceRented'
[Audio][SFX] Pool return event='AudioSfxPooledVoiceReturned'
ActorProjectileSpawnedFromPool
ActorProjectileSpawnTracked
ActorProjectileSpawnedRuntimeObjectsStateProfileApplied
```

Não pode aparecer:

```text
FATAL
Exception
route_transition_failed
checkpointStatus='Failed'
AudioSource.PlayClipAtPoint
ProjectileAudioManager
ProjectileAudioPool
```

## Observação sobre smoke parcial

Logs de `ActorProjectileFireAudioCuePlayed` provam que o adapter de projectile chamou o `IGlobalAudioService` e recebeu handle válido. Este corte adiciona logs no `AudioGlobalSfxService` para provar também o caminho interno: source configured, pooled voice rented e pooled voice returned.

## Status esperado pós-smoke

```text
ACT-PROJ-AUDIO-1C
CLOSED / observability smoke PASS
```
