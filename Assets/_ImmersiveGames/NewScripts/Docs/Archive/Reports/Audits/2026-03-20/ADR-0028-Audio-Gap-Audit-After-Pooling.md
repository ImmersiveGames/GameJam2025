# ADR-0028 Audio Gap Audit After Pooling

> Status note (2026-03-20): este documento nasceu como fotografia pre-F3. Os addendums no fim deste arquivo atualizam o estado real apos cleanup + F3. O estado atual correto e: F3 DONE e proximo passo natural F4.

## 1. Resumo executivo

Este relatorio passou a ter dois contextos:

- Fotografia original pre-F3 (historico da auditoria inicial).
- Estado atual pos-addendums (source of truth para retomada do rollout).

Estado atual valido em 2026-03-20:

- F0/F1/F2: DONE.
- Cleanup de AudioSfxVoiceProfileAsset para PoolDefinitionAsset: DONE.
- F3 (IAudioBgmService runtime): DONE e validado funcionalmente.
- Listener global canonico no boot, independente de camera: DONE.
- F4/F5/F6+: ainda nao iniciados.

Conclusao operacional atual: nao existe mais bloqueio de F3; o proximo passo natural do rollout e F4 (Global SFX direto), mantendo F5 (pooled voices) para a sequencia.

## 2. Matriz de cobertura do ADR

| Requisito/feature ADR-0028 | Arquivo(s) | Status | Observacao curta |
|---|---|---|---|
| ownership/boot do modulo | `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`, `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs` | DONE | Estagio `Audio` no pipeline global; sem bootstrap paralelo em `Modules/Audio/**`. |
| modulo Audio standalone (sem consumer-specific deps) | `Modules/Audio/**` | DONE | Nao ha dependencia estrutural de `Navigation/Gameplay/Presentation/Skin`. |
| contracts publicos base | `Modules/Audio/Runtime/*.cs` (interfaces) | DONE | Interfaces canonicamente definidas para BGM/Global/Entity/Settings/Handle/Context/Routing. |
| assets base | `Modules/Audio/Config/AudioCueAsset.cs`, `AudioBgmCueAsset.cs`, `AudioSfxCueAsset.cs`, `AudioDefaultsAsset.cs`, `AudioSfxVoiceProfileAsset.cs` | DONE | Todos os tipos base existem. |
| `IAudioBgmService` | `Modules/Audio/Runtime/IAudioBgmService.cs`, `Modules/Audio/Runtime/AudioBgmService.cs`, `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs` | DONE | Contrato + runtime concreto + registro DI global (F3). |
| `IGlobalAudioService` | `Modules/Audio/Runtime/IGlobalAudioService.cs` | PARTIAL | So contrato; sem runtime real de SFX global. |
| `IEntityAudioService` | `Modules/Audio/Runtime/IEntityAudioService.cs` | PARTIAL | So contrato; sem runtime semantico `purpose -> cue`. |
| `IAudioSettingsService` | `Modules/Audio/Runtime/IAudioSettingsService.cs`, `AudioSettingsService.cs`, `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs` | DONE | Contrato + implementacao + registro DI. |
| `IAudioPlaybackHandle` | `Modules/Audio/Runtime/IAudioPlaybackHandle.cs`, `NullAudioPlaybackHandle.cs` | PARTIAL | Existe handle no-op; sem handle real com semantica de playback. |
| `AudioCueAsset` | `Modules/Audio/Config/AudioCueAsset.cs` | DONE | Base abstrata com clips, volume/pitch/jitter, validacao runtime. |
| `AudioBgmCueAsset` | `Modules/Audio/Config/AudioBgmCueAsset.cs` | DONE | Asset derivado presente. |
| `AudioSfxCueAsset` | `Modules/Audio/Config/AudioSfxCueAsset.cs` | DONE | Inclui playback mode, execution mode, cooldown e limite declarativo. |
| `AudioDefaultsAsset` | `Modules/Audio/Config/AudioDefaultsAsset.cs`, `Modules/Audio/Content/AudioDefaults.asset` | DONE | Defaults e campos de routing/profile presentes. |
| `AudioSfxVoiceProfileAsset` | `Modules/Audio/Config/AudioSfxVoiceProfileAsset.cs`, `Modules/Audio/Content/VoicesProfile/*.asset` | PARTIAL | Tipo existe, mas integra por `PoolData` legado e assets estao sem referencia de pool. |
| runtime real de BGM | `Modules/Audio/Runtime/AudioBgmService.cs` | DONE | Runtime global single-channel logico com fade/crossfade/pause ducking. |
| runtime real de SFX | (nao encontrado) | MISSING | Nao ha classe implementando `IGlobalAudioService`. |
| pooled voices / voice allocation | (nao encontrado em `Modules/Audio/**`) | MISSING | Sem `AudioSfxVoice`, sem rent/return via `IPoolService`. |
| handle semantics (`IsPlaying`/`Stop`) | `IAudioPlaybackHandle.cs`, `NullAudioPlaybackHandle.cs` | PARTIAL | Contrato existe; sem enforcement semantico real em playback concreto. |
| mixer/routing | `IAudioRoutingResolver.cs`, `AudioRoutingResolver.cs`, `AudioDefaultsAsset.cs` | PARTIAL | Resolver base implementado; sem aplicacao efetiva em runtime de BGM/SFX. |
| pause/resume (ducking) | `Modules/Audio/Runtime/AudioBgmService.cs` | DONE | `SetPauseDucking` funcional no runtime de BGM. |
| fade/crossfade | `Modules/Audio/Runtime/AudioBgmService.cs`, `Modules/Audio/Config/AudioDefaultsAsset.cs` | DONE | Fade-in/fade-out/crossfade implementados e validados em F3. |
| anti-spam / cooldown / dedupe | `AudioSfxCueAsset.cs` | PARTIAL | Campos declarados (`SfxRetriggerCooldownSeconds`, `MaxSimultaneousInstances`), sem enforcement runtime. |
| separacao runtime canonico vs tooling/QA | `Modules/Audio/{Runtime,Editor,QA,Interop,Bindings}` | DIFFERENT-BUT-EQUIVALENT | Separacao estrutural de pastas existe; `Editor/QA/Interop/Bindings` ainda vazios. |
| integracao planejada com Pooling (ADR-0029) | `AudioSfxVoiceProfileAsset.cs`, `Infrastructure/Pooling/**` | PARTIAL | Pooling canonico pronto, mas Audio ainda nao consome `IPoolService` nem `PoolDefinitionAsset`. |

## 3. Status real por fase do plano

Fonte do plano vigente: `Docs/Reports/Plan-Audio-ADR-0028-Implementation-v2.md`.

| Fase | Status real | Evidencia concreta |
|---|---|---|
| F0 | DONE | Existe tracker de rollout em `Docs/Reports/Audits/2026-03-20/ADR-0028-Audio-Rollout-Tracker.md` com baseline congelado. |
| F1 | DONE | Contratos e assets base implementados em `Modules/Audio/Runtime/*.cs` e `Modules/Audio/Config/*.cs`. |
| F2 | DONE | Bootstrap no `GlobalCompositionRoot` (`GlobalCompositionRoot.Pipeline.cs` + `GlobalCompositionRoot.Audio.cs`), com registro de `AudioDefaultsAsset`, `IAudioSettingsService` e `IAudioRoutingResolver`. |
| F3 | DONE | Implementacao concreta em `Modules/Audio/Runtime/AudioBgmService.cs` e registro em `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs`. |
| F4 | MISSING | Nao existe implementacao de `IGlobalAudioService` para trilha `DirectOneShot`. |
| F5 | MISSING | Nao existe runtime pooled de SFX; sem consumo de `IPoolService` em Audio. |
| F6 | MISSING | Nao existe implementacao funcional de `IEntityAudioService` semantico. |
| F7 | MISSING | Nao existe `EntityAudioEmitter` canonico em `Modules/Audio/**` (nem forma minima finalizada). |
| F8 | MISSING | Nao ha bridges de integracao de consumidores no eixo audio canonico atual. |
| F9 | MISSING | Pastas `Editor/QA` do modulo Audio existem, mas sem tooling/harness implementado. |
| F10 | MISSING | Nao ha evidencias de cleanup final de rollout. |

## 4. Onde o rollout realmente parou

- Ultimo ponto consolidado no codigo: **F3 (BGM runtime funcional + listener global no boot)**.
- Primeiro ponto claramente incompleto: **F4 (runtime Global SFX direto)**.
- Proximo passo real mais correto: **iniciar F4 (`IGlobalAudioService` direto), mantendo Audio standalone e sem acoplamento a modulos consumidores**.

Observacao importante de encadeamento: embora F5 seja a fase formal de pooled voices, o desalinhamento atual de `AudioSfxVoiceProfileAsset` com ADR-0029 deve ser limpo antes de avancar para pooled runtime, para evitar retrabalho.

## 5. Auditoria especifica de voice profile / poolData

### Estado atual

`AudioSfxVoiceProfileAsset` existe em `Modules/Audio/Config/AudioSfxVoiceProfileAsset.cs` com campos:

- `pooledVoicePool` (tipo `PoolData` do legado)
- `allowDirectFallback`
- `defaultVoiceBudget`
- `releaseGraceSeconds`

Os assets de perfil (`Modules/Audio/Content/VoicesProfile/AudioSfxVoiceGlobalProfile.asset` e `AudioSfxVoiceSpatialProfile.asset`) estao com `pooledVoicePool: {fileID: 0}` (sem referencia ativa).

### Classificacao da referencia de pool

- Referencia atual (`PoolData`) esta **declarada e nao usada** no runtime canonico de Audio.
- Tambem esta **arquiteturalmente incorreta** para ADR-0028 + ADR-0029, porque o contrato atualizado exige `PoolDefinitionAsset`/`IPoolService` em `Infrastructure/Pooling/**`.

### Resposta objetiva

- Este ponto ja esta preparado para consumir Pooling? **Nao.**
- Esta parcial? **Sim, parcial/residual.**
- Esta errado? **Sim, errado arquiteturalmente no shape atual (`PoolData` legado).**
- Precisa redesign ou so continuidade? **Precisa cleanup de contrato (troca para infraestrutura canonica) + continuidade.**

## 6. Como o Audio deve consumir Pooling

Com base no estado atual do codigo:

1. `Infrastructure/Pooling/**` deve continuar owner de pooling (`IPoolService` + `PoolDefinitionAsset`), sem subsistema de pool dentro de Audio.
2. Audio deve consumir pooling apenas na trilha de SFX pooled (F5), especialmente no runtime que implementar `IGlobalAudioService`/voices.
3. Pontos de integracao corretos no Audio:
   - `Modules/Audio/Config/AudioSfxVoiceProfileAsset.cs`: referenciar `PoolDefinitionAsset` canonico (nao `PoolData`).
   - runtime de SFX pooled em `Modules/Audio/Runtime/**`: resolver profile efetivo e chamar `IPoolService.EnsureRegistered/Prewarm/Rent/Return`.
   - composicao em `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs`: registrar runtime(s) de audio que consumirao `IPoolService` ja disponivel no estagio `Pooling`.
4. Ja existe helper/base consumer para pooling: **sim**, `PoolConsumerBehaviourBase` em `Infrastructure/Pooling/QA/PoolingQaContextMenuDriver.cs` (namespace `...Pooling.Interop`) demonstra padrao de consumo e prewarm explicito.
5. Como modulo canonico, Audio pode reutilizar esse padrao (ou extrair equivalente em `Interop`) para declarar dependencias de pool de forma explicita, sem quebrar ownership da infraestrutura.

## 7. Contradicoes entre docs e codigo

1. **Plano em caminho divergente**
- Pedido/referencia cita `Docs/Plans/Plan-Audio-ADR-0028-Implementation-v2.md`, mas o plano vigente existe em `Docs/Reports/Plan-Audio-ADR-0028-Implementation-v2.md`.

2. **Tracker marca F2 como fechado; codigo confirma, mas com ressalva de degradado**
- `ADR-0028-Audio-Rollout-Tracker.md` indica pacote A fechado.
- Codigo confirma bootstrap/base, porem `GlobalCompositionRoot.Audio.cs` ainda tem caminho degradado quando `AudioDefaults` vem nulo no bootstrap.

3. **ADR-0028 pede integracao pooled por `PoolDefinitionAsset` via voice profile; codigo usa `PoolData` legado**
- Divergencia direta entre contrato ADR e shape real de `AudioSfxVoiceProfileAsset`.

4. **Plano descreve F3+ como proximas fases; codigo permanece sem runtime dessas fases**
- Coerente como status de parada, mas importante registrar que cooldown/limite/fade/ducking seguem apenas declarativos sem enforcement.

## 8. Lacunas obrigatorias

- Implementacao concreta de `IAudioBgmService` (F3).
- Implementacao concreta de `IGlobalAudioService` (F4/F5).
- Implementacao concreta de `IEntityAudioService` (F6).
- Enforcements runtime de cooldown/concorrencia (`SfxRetriggerCooldownSeconds` e `MaxSimultaneousInstances`).
- Handle real de playback (semantica `IsPlaying`/`Stop(fade)`), nao apenas no-op.
- Alinhamento de `AudioSfxVoiceProfileAsset` com infraestrutura canonica (`PoolDefinitionAsset` + consumo de `IPoolService`).

## 9. Proximo passo recomendado

Pacote recomendado (ordem pratica):

1. **Pacote B1 - F3 puro (BGM runtime):** implementar `IAudioBgmService` com play/stop/stopImmediate/ducking/fade e registrar no `GlobalCompositionRoot`.
2. **Pacote B2 - Cleanup de contrato para Pooling (pre-F5):** alinhar `AudioSfxVoiceProfileAsset` ao shape canonico de pooling para destravar pooled voices sem retrabalho.
3. **Pacote B3 - F4/F5:** implementar `IGlobalAudioService` em duas trilhas (`DirectOneShot` depois `PooledOneShot`) consumindo `IPoolService`.

## 10. Veredito

**READY TO RESUME AT F4**

Correcoes obrigatorias antes de avancar:

- Nenhuma correcao obrigatoria remanescente para F3 (cleanup de contrato e runtime BGM ja aplicados).

Observacoes opcionais:

- Consolidar docs para apontar somente um caminho oficial do plano (evitar `Docs/Plans` inexistente).
- Adicionar tracker complementar especifico para "Audio x Pooling integration readiness" ao iniciar F5.

Proximo pacote de implementacao recomendado:

- **Pacote C (F4, Global SFX direto)**, mantendo F5 (pooled voices) para a etapa seguinte.

---

## Addendum - Contract Cleanup Applied (2026-03-20)

Este gap especifico foi executado apos a auditoria:

- `AudioSfxVoiceProfileAsset` deixou de depender de `PoolData` legado.
- O contrato agora referencia `PoolDefinitionAsset` (infraestrutura canônica de `Infrastructure/Pooling/**`).
- Os assets de `VoicesProfile` foram ajustados para o novo campo serializado e seguem com pool nulo (estado válido para esta etapa).

Importante:

- Este addendum fecha apenas o alinhamento de contrato para preparar F5.
- Continua sem implementação de runtime BGM/SFX/Entity e sem consumo runtime de `IPoolService`.

## Addendum - F3 Implemented (2026-03-20)

Evolução aplicada após esta auditoria:

- F3 (BGM runtime) foi implementado no core standalone de Audio.
- `IAudioBgmService` agora possui runtime concreto registrado no DI global.
- O runtime de BGM fecha trilha global single-channel lógica com:
  - `Play`, `Stop`, `StopImmediate`
  - fade-in/fade-out
  - crossfade
  - pause/resume via `SetPauseDucking(bool paused, ...)`
  - consumo de `AudioBgmCueAsset`, `AudioDefaultsAsset`, `IAudioSettingsService` e `IAudioRoutingResolver`

Fora de escopo mantido:

- sem F4 (`IGlobalAudioService` direto)
- sem F5 (pooled voices / integração de pooling no runtime de áudio)
- sem `IEntityAudioService` runtime

## Addendum - Defaults Scope Corrected (2026-03-20)

Correção arquitetural aplicada após os addendums anteriores:

- `AudioDefaultsAsset` foi reduzido para configuração técnica inicial/fallback.
- Foram removidas do defaults referências de conteúdo (`AudioBgmCueAsset`) e de seleção de profile/pool de voice.
- `AudioBgmService` permanece dependente de cue explícita por chamada, sem resolver conteúdo via defaults.

Separação consolidada:

- defaults = valores técnicos iniciais
- settings service = estado runtime mutável
- conteúdo (BGM/SFX/profile) = camada chamadora/bridge/catálogo acima do core


