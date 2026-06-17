# ACT-PROJ-AUDIO-1B — Fire Mode Audio Volume Scale

Status: Applied / Pending compile + smoke

## Objetivo

Permitir que cada modo de disparo ajuste o volume do seu SFX sem alterar o volume global do AudioRuntime, o pool de vozes ou outros cues.

## Fronteira

```text
ActorProjectileFireProfileAsset / FireMode
= authoring do tiro e dono do multiplicador de volume daquele tiro.

ActorProjectileFireAudioAdapter
= recebe o multiplicador resolvido e monta AudioPlaybackContext.

AudioGlobalSfxService
= executa o playback e aplica volume final no AudioSource.
```

## Decisões

- O volume extra é definido por `FireMode`, não pelo pool de áudio.
- O pool de áudio continua global e técnico.
- `fireAudioVolumeScale` pode ser maior que `1` para compensar percepção baixa em SFX espacial.
- `fireAudioVolumeScale = 0` permite silenciar o cue daquele modo sem remover a referência do asset.
- O cálculo final do `AudioSource.volume` não usa mais `Clamp01`, para que `AudioPlaybackContext.volumeScale > 1` não seja descartado pelo runtime de áudio.
- O cue continua podendo ser compartilhado entre múltiplos fire modes; cada fire mode pode aplicar um volume diferente.

## Arquivos alterados

```text
Actors/Projectile/Authoring/ActorProjectileFireProfileAsset.cs
Actors/Projectile/Contracts/ActorProjectileContracts.cs
Actors/Projectile/Contracts/ActorProjectileEndpointContracts.cs
Actors/Projectile/Runtime/ActorProjectileFireEndpoint.cs
Actors/Projectile/Audio/ActorProjectileFireAudioAdapter.cs
AudioRuntime/Playback/Runtime/Core/AudioGlobalSfxService.Execution.cs
Resources/Actors/ActorProjectileFireProfile_PrimaryShot.asset
Docs/ADRs/ADR-2.0-0005-ActorCommandHub-ActorProjectileCapability-PoolingAudio.md
```

## Smoke esperado

Procurar:

```text
ActorProjectileFireAudioAdapterConfigured
ActorProjectileFireAudioCuePlayed
volumeScale='2.5'
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
```
