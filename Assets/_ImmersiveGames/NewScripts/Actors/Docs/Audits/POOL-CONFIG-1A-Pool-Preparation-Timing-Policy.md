# POOL-CONFIG-1A — Pool preparation timing policy

Data: 2026-06-16

## Resumo

Owner escolhido para o timing:

- `PoolDefinitionAsset`

Owner rejeitados:

- `PoolService` como dono da política: rejeitado; ele executa registro/prewarm, mas não decide lifecycle de Activity/Route/Global.
- `ActivityEntryPoolPreparationStage` como dono da política: rejeitado; ele executa apenas a preparação da janela ActivityEntry.
- `AudioRuntimeComposer` / `GlobalCompositionRoot` como dono único do timing global: rejeitado neste corte; não existe catálogo canônico de pools para boot global sem inventar busca de assets.

Conclusão:

- `registrationMode` é authoring data + policy.
- `prewarm` continua sendo comportamento pós-registro.
- `LazyOnFirstRent` permanece como default e continua funcional.
- `GlobalBoot` fica documentado, mas sem executor real neste corte.

## Matriz

| Arquivo/classe/método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---|---|---|
| `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs` | Authoring do pool | `PoolDefinitionAsset` | Não havia policy de timing por pool | Alta | Adicionar `PoolRegistrationMode` no asset | Baixo | Dois pools reais: projectile e audio |
| `Foundation/Platform/Pooling/Runtime/PoolService.cs` | Registro, prewarm, rent, return | `PoolService` | Decidia auto-registro no rent para todo pool | Alta | Auto-registrar só `LazyOnFirstRent`; falhar nos demais modos | Médio | `Rent` chamava `GetOrCreatePool()` para qualquer definição |
| `SessionActivity/Pipeline/Stages/ActivityEntryPoolPreparationStage.cs` | Preparação antecipada da Activity | Stage de orchestration | Preparava tudo sem respeitar timing autoral | Alta | Preparar somente `ActivityEntry` | Baixo | Smoke anterior mostrava projectile preparado na Activity Entry, audio ainda pagava no primeiro SFX |
| `AudioRuntime/Playback/Bootstrap/AudioRuntimeComposer.cs` | Composer de runtime audio | Composer, não policy owner | Não há catálogo de pools para `GlobalBoot` | Média | Documentar pendência; não criar busca global | Baixo | `AudioRuntimeComposer` só compõe serviços, não lista pools |
| `Foundation/Platform/Composition/GlobalCompositionRoot.Pipeline.cs` | Pipeline de bootstrap | Pipeline/composition | Não possui owner claro de catálogo de pools | Média | Não mover policy para composição global sem catálogo | Baixo | Boot só registra serviços |
| `Actors/Projectile/Runtime/ActorProjectileFireEndpoint.cs` | Provider de dependências | Endpoint/provider | Não deve decidir timing | Baixa | Manter só a declaração das dependências | Baixo | Endpoint já expõe `RuntimePoolDefinitions` |
| `AudioRuntime/Authoring/Config/AudioSfxVoiceProfileAsset.cs` | Referência authoring ao pool de vozes | Authoring data | Continua sendo referência, não policy owner | Baixa | Manter como payload de referência | Baixo | O pool de vozes do projectile ainda depende do autor de áudio |

## Config final relevante

- `Resources/Pools/PoolDefinition_PrimaryProjectile.asset`
  - `registrationMode = ActivityEntry`
  - `prewarm = true`
- `AudioRuntime/Authoring/Content/ProjectileFire/Pools/PoolDefinition_AudioProjectileFireVoices.asset`
  - `registrationMode = LazyOnFirstRent`
  - `prewarm = true`
  - timing global permanece pendente de owner/catálogo

## Smoke esperado

- `ActivityEntryPoolPreparationStarted`
- `ActivityEntryPoolDependencyResolved`
- `ActivityEntryPoolPrepared`
- `PoolDefinition_PrimaryProjectile`
- `PoolService Ensure registered`
- `GameObjectPool Prewarm complete`
- primeiro tiro do projectile com `PoolService Ensure no-op (already registered)` para `PoolDefinition_PrimaryProjectile`
- primeiro SFX de áudio ainda pode registrar o pool se não houver executor global

