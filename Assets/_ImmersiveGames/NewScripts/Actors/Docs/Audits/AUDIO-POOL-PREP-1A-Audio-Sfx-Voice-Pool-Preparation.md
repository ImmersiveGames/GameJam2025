# AUDIO-POOL-PREP-1A — Audio SFX voice pool preparation owner

Data: 2026-06-16

## Resumo

Owner escolhido:

- `AudioDefaultsAsset` como catálogo explícito de pools de vozes SFX.
- `AudioRuntimeComposer` como stage/composer que prepara `GlobalBoot`.

Owners rejeitados:

- `GlobalCompositionRoot` como owner único: rejeitado; a composição global não deve virar catálogo implícito de áudio.
- `AudioGlobalSfxService` como owner de preload: rejeitado; o serviço executa playback, não boot/preparation.
- `AudioSfxVoiceProfileAsset` como owner de timing: rejeitado; continua sendo authoring de referência de pool, não policy de boot.
- `ActorProjectileFireEndpoint`: rejeitado; projectile não prepara pools de áudio.
- `PoolService`: rejeitado como policy owner; continua executor técnico.

Conclusão:

- O catáogo explícito de preload fica em `AudioDefaultsAsset`.
- `PoolRegistrationMode` continua sendo a policy de timing.
- `AudioRuntimeComposer` prepara antes do primeiro playback pooled.
- `PoolDefinition_AudioProjectileFireVoices` passa a usar `registrationMode=GlobalBoot`.

## Matriz

| Arquivo/classe/método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `AudioRuntime/Authoring/Config/AudioDefaultsAsset.cs` | Defaults globais de áudio | `AudioDefaultsAsset` | Não havia catálogo explícito de preload SFX | Alta | Adicionar `GlobalSfxVoicePoolDefinitions` | Baixo | O runtime já lê `AudioDefaultsAsset` via DI |
| `AudioRuntime/Playback/Bootstrap/AudioRuntimeComposer.cs` | Composer de runtime | `AudioRuntimeComposer` | Não preparava pools globais antes do primeiro SFX | Alta | Chamar stage de preload antes de criar/registrar `IGlobalAudioService` | Baixo | Composer já é o ponto de wiring operacional do áudio |
| `AudioRuntime/Playback/Bootstrap/AudioSfxPoolPreparationStage.cs` | Preparação de pools SFX | Stage/composer de AudioRuntime | Novo owner necessário para boot de pools | Alta | Centralizar preload e logs de observabilidade | Baixo | Sem catálogo global falso |
| `AudioRuntime/Authoring/Content/AudioDefaults.asset` | Instância authoring | `AudioDefaultsAsset` | Catálogo vazio impediria preload | Alta | Vincular `PoolDefinition_AudioProjectileFireVoices` no catálogo | Baixo | Asset existe e é carregado no boot |
| `AudioRuntime/Authoring/Content/ProjectileFire/Pools/PoolDefinition_AudioProjectileFireVoices.asset` | Definição de pool | `PoolDefinitionAsset` | Timing ainda estava em `LazyOnFirstRent` | Alta | Mudar para `GlobalBoot` | Baixo | Smoke anterior mostrava primeiro SFX pagando prewarm |
| `AudioRuntime/Playback/Runtime/Core/AudioGlobalSfxService.Pooling.cs` | Playback pooled | `AudioGlobalSfxService` | Não deve decidir preload | Baixa | Manter só rent/playback | Baixo | O serviço só usa pool já pronto |

## Config final relevante

- `AudioDefaultsAsset.GlobalSfxVoicePoolDefinitions` contém `PoolDefinition_AudioProjectileFireVoices`
- `PoolDefinition_AudioProjectileFireVoices.registrationMode = GlobalBoot`
- `PoolDefinition_AudioProjectileFireVoices.prewarm = true`

## Smoke esperado

- Durante `AudioRuntimeComposer.ComposeRuntime`:
  - `AudioSfxPoolPreparationStarted`
  - `AudioSfxPoolDependencyResolved`
  - `PoolDefinition_AudioProjectileFireVoices`
  - `registrationMode='GlobalBoot'`
  - `PoolService Ensure registered asset='PoolDefinition_AudioProjectileFireVoices'`
  - `GameObjectPool Prewarm complete. asset='PoolDefinition_AudioProjectileFireVoices'`
  - `AudioSfxPoolPrepared`
  - `AudioSfxPoolPreparationCompleted`
- No primeiro SFX:
  - `PoolService Rent asset='PoolDefinition_AudioProjectileFireVoices'`
  - sem `GameObjectPool Prewarm complete. asset='PoolDefinition_AudioProjectileFireVoices'` no bloco do primeiro SFX
  - `AudioSfxPooledVoiceRented`
  - `ActorProjectileFireAudioCuePlayed`

