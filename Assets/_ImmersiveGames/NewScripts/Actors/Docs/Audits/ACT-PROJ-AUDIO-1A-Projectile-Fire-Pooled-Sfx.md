# ACT-PROJ-AUDIO-1A — Projectile Fire Pooled SFX

Status: Applied / Pending compile + smoke

## Objetivo

Adicionar áudio de disparo por `FireMode` sem criar pool, manager ou lifecycle paralelo no domínio de projectile.

## Fronteira correta

```text
ActorProjectileFireProfileAsset / FireMode
= authoring data de gameplay. Define qual cue de SFX pertence ao modo de disparo.

ActorProjectileFireEndpoint
= endpoint local da capability. Toca o cue após spawn aceito.

ActorProjectileFireAudioAdapter
= adapter fino. Recebe `IGlobalAudioService`, monta `AudioPlaybackContext.Spatial` e solicita playback.

AudioGlobalSfxService
= owner do playback SFX e da execução pooled.

IPoolService / PoolDefinition_AudioProjectileFireVoices
= infraestrutura técnica global de vozes de áudio.
```

## Decisões

- O pool de áudio de tiro é global e técnico.
- O tiro/fire mode pode ser activity-scoped, actor-scoped ou profile-scoped.
- Vários fire modes podem usar cues diferentes e compartilhar o mesmo pool global de vozes.
- `ActorProjectileFireAudioAdapter` não conhece `IPoolService`.
- `ActorProjectileFireEndpoint` não cria `AudioSource` e não chama `AudioSource.PlayClipAtPoint`.
- O áudio toca após o spawn adapter retornar accepted/spawned no corte inicial.
- `fireAudioCue == null` significa tiro sem áudio configurado, não fallback técnico.
- O volume do SFX de disparo é controlado por `fireAudioVolumeScale` no próprio fire mode.

## Assets criados

```text
AudioRuntime/Authoring/Content/ProjectileFire/Pools/PoolDefinition_AudioProjectileFireVoices.asset
AudioRuntime/Authoring/Content/ProjectileFire/Voices/AudioSfxVoiceProfile_ProjectileFire.asset
AudioRuntime/Authoring/Content/ProjectileFire/Execution/AudioSfxExecution_ProjectileFire_PooledSpatial.asset
AudioRuntime/Authoring/Content/ProjectileFire/Emission/AudioSfxEmission_ProjectileFire_Spatial3D.asset
AudioRuntime/Authoring/Content/ProjectileFire/Cue/AudioSfxCue_ProjectileFire_Primary.asset
```

## Configuração inicial

```text
PoolDefinition_AudioProjectileFireVoices
- lifetimeScope: Global
- initialSize: 16
- canExpand: true
- maxSize: 64
- prewarm: true

AudioSfxVoiceProfile_ProjectileFire
- allowDirectFallback: false
- defaultVoiceBudget: 32
- releaseGraceSeconds: 0.05

AudioSfxCue_ProjectileFire_Primary
- maxSimultaneousInstances: 32
- sfxRetriggerCooldownSeconds: 0

ActorProjectileFireProfile_PrimaryShot / fire.primary.single
- fireAudioVolumeScale: 2.5
```

## Correção em AudioRuntime

`AudioGlobalSfxService.Pooling` agora posiciona a instância pooled antes de configurar/tocar o `AudioSource`:

```text
rentedInstance.transform.position = context.followTarget != null
    ? context.followTarget.position
    : context.worldPosition;
```

## Critério de smoke

Procurar:

```text
ActorProjectileFireAudioAdapterConfigured
ActorProjectileFireAudioCuePlayed
[Audio][SFX] Pool rent cue='AudioSfxCue_ProjectileFire_Primary'
[Audio][SFX] Pool return
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
pool_service_unavailable no projectile audio adapter
```
