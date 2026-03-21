# ADR-0028 Audio Rollout Tracker

Data: 2026-03-20
Status geral: IN PROGRESS
Estratégia: 1 fase por PR (sequencial)
Pacote A: FECHADO (F0/F1/F2 concluídos, sem playback real ainda)

## Baseline obrigatório

- ADR: `Docs/ADRs/ADR-0028-AudioModule.md`
- Auditoria legado vs canônico: `Docs/Reports/Audits/2026-03-20/Legacy-AudioSystem-Audit-vs-Canonical-ADR.md`

## Guardrails ativos

- Não abrir Unity.
- Não executar build de player.
- Sem migração big-bang.
- Sem acoplamento estrutural de `Modules/Audio/**` com módulos consumidores.
- Sem remover comportamento antigo antes de substituto observável.

## Nota de escopo standalone

- F0 ate F7 pertencem ao core standalone de `Modules/Audio/**`.
- Integracoes com modulos consumidores entram apenas em F8+.
- Catalogos/collections/profiles especificos de `Navigation`, `Gameplay`, `LevelFlow`, `Skin` e similares nao pertencem ao core de `Modules/Audio/**`.

## Papel do IAudioRoutingResolver

- `IAudioRoutingResolver` pertence ao core standalone de audio como concern interno de roteamento base (mixer/routing).
- Nao e porta de integracao intermodular e nao deve ser promovido para dependencias de consumidores.
- Qualquer traducao de dominio consumidor para audio deve ocorrer em bridges/adapters (F8+), sem transferir ownership do routing para modulos consumidores.

## Fases (status)

| Fase | Status | Observações |
|---|---|---|
| F0 - Baseline e rastreio | DONE | Tracker criado, baseline congelado e matriz de paridade fixada para execução incremental. |
| F1 - Contratos e estrutura | DONE | Estrutura `Modules/Audio/**` criada com contratos e assets base do ADR-0028. |
| F2 - Bootstrap + defaults/settings/routing base | DONE | Estágio `Audio` adicionado ao pipeline global antes de GameLoop, com registro DI e logs de boot. |
| F3 - GlobalAudio BGM | DONE | `IAudioBgmService` runtime canônico implementado e registrado no DI global (single-channel lógico com fade/crossfade e pause/resume via ducking). |
| F4 - GlobalAudio SFX direto | DONE | `IGlobalAudioService` runtime canônico implementado (direct one-shot 2D/3D, anti-spam real e handle funcional). |
| F5 - GlobalAudio pooled voices | DONE | `IGlobalAudioService` passou a consumir `IPoolService` + `PoolDefinitionAsset` para cues `PooledOneShot`, mantendo trilha direta como fallback canônico. |
| F6 - EntityAudio semântico | NOT STARTED | Dependente de F5. |
| F7 - EntityAudioEmitter mínimo | NOT STARTED | Dependente de F6. |
| F8 - Integrações opcionais | NOT STARTED | Dependente de F7. |
| F9 - Tooling/QA/hardening | NOT STARTED | Dependente de F8. |
| F10 - Cleanup final | NOT STARTED | Dependente de F9. |

## Matriz de paridade (freeze executável)

A matriz de paridade oficial segue a definida no plano aprovado e deve ser usada como checklist de stop/go por fase.

## Stop/Go por fase

Só avança quando:

- Entregáveis completos.
- Teste manual objetivo definido para a fase.
- Risco residual registrado.
- Regressões da própria fase tratadas.

## Regressões abertas

- Nenhuma registrada até o fechamento do Pacote A.

## Riscos residuais conhecidos após Pacote A

- Playback real de BGM e Global SFX (direto + pooled) fechados em F3/F4/F5; permanece sem runtime real `IEntityAudioService` (F6+).
- `AudioDefaultsAsset` ausente no bootstrap entra em modo degradado com fallback runtime (já logado/trackeado).
- Contrato detalhado de mixer/routing segue base inicial (refino previsto para F3+).

## Atualização F3 (2026-03-20)

- Status: DONE.
- Implementação concreta de `IAudioBgmService` adicionada em `Modules/Audio/Runtime/AudioBgmService.cs`.
- Registro DI global realizado em `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs`.
- Escopo respeitado: sem implementação de `IGlobalAudioService`, sem `IEntityAudioService` runtime e sem consumo de `Infrastructure/Pooling/**` nesta fase.
- Próximo passo natural do rollout: F4 (Global SFX direto), seguido de F5 (pooled voices).

## Atualização F4 (2026-03-21) — SFX-1

- Status: DONE (escopo F4 direto, sem pooling).
- Implementação concreta de `IGlobalAudioService` adicionada em `Modules/Audio/Runtime/AudioGlobalSfxService.cs`.
- Handle real de playback direto adicionado em `Modules/Audio/Runtime/AudioSfxPlaybackHandle.cs`.
- Registro DI global realizado em `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs`.
- Escopo respeitado:
  - sem consumo de `IPoolService`;
  - sem uso de `PoolDefinitionAsset` no runtime de execução;
  - sem mudanças em bridges de domínio consumidor.
- Ajuste semântico aplicado (F4 hardening):
  - retrigger do mesmo cue em `Global/2D` usa `restart_existing` (interrompe a instância anterior e toca novamente de imediato);
  - nesse retrigger específico, cooldown e limite de simultâneos não bloqueiam o novo feedback;
  - trilha `Spatial/3D` mantém a política de cooldown/limite desta fase.
- Próximo passo natural do rollout: F5 (Global SFX pooled voices).

## Atualização F5 (2026-03-21) — SFX-2 (Pooled Voices)

- Status: DONE (escopo F5 pooled, sem iniciar EntityAudio).
- `AudioGlobalSfxService` agora decide runtime path por cue/contexto:
  - `path='direct'`
  - `path='pooled'`
  - `path='fallback_direct'`
- Consumo canônico de pooling implementado:
  - resolve `AudioSfxVoiceProfileAsset` (cue override > context);
  - usa `PoolDefinitionAsset` do profile;
  - `IPoolService.EnsureRegistered(...)`;
  - `Prewarm(...)` quando `PoolDefinitionAsset.Prewarm` estiver ativo;
  - `Rent(...)` para playback pooled;
  - `Return(...)` no término natural ou `Stop()`, respeitando `releaseGraceSeconds`.
- Semântica preservada:
  - `Global/2D` mantém `retrigger='restart_existing'` também no pooled;
  - `Spatial/3D` mantém política de cooldown/limite;
  - fallback para trilha direta só quando `allowDirectFallback=true`.
- Enforcement mínimo adicional:
  - `defaultVoiceBudget` do profile passa a bloquear trilha pooled por budget (`policy='block_budget'`) com fallback direto opcional.
- Observabilidade de F5:
  - `path='direct|pooled|fallback_direct'`;
  - `retrigger='restart_existing'`;
  - `policy='block_cooldown|block_limit|block_budget'`;
  - rent/return de pool com profile/pool/reason.
- QA F5 fortalecido com probes determinísticos:
  - `Probe Pooled Budget Forced` para evidenciar `policy='block_budget'` sem colisão com `block_limit`;
  - `Probe Pooled Fallback Forced` para evidenciar `path='fallback_direct'` de forma controlada;
  - `Probe Pooled Sequence Reuse` para observar ciclo saudável de `rent -> complete -> return` em sequência com espaçamento.
- Próximo passo natural do rollout: F6 (EntityAudio semântico).

## Atualização de saneamento arquitetural SFX (2026-03-21) — A1 + B1

- Status: DONE (pré-F6/F7, sem redesign completo de authoring).
- Objetivo cumprido:
  - reduzir acoplamento interno do runtime de SFX;
  - separar QA por trilho/tipo para eliminar harness único "deus".
- Extração de policy interna no runtime:
  - `AudioSfxDirectPolicyEngine` centraliza decisão de `restart_existing`, `block_cooldown` e `block_limit`;
  - `AudioSfxPooledPolicyEngine` centraliza decisão de `pooled proceed`, `fallback_direct` e `block_budget`;
  - `AudioGlobalSfxService` permanece como orquestrador (resolução + execução), sem concentrar toda a policy no mesmo bloco monolítico.
- Semântica preservada:
  - `Global/2D` mantém `restart_existing`;
  - `Spatial/3D` mantém policy de concorrência da fase;
  - trilha pooled segue consumindo `IPoolService` + `PoolDefinitionAsset`.
- Escopo respeitado:
  - sem mudanças em BGM/F3;
  - sem início de F6/F7;
  - sem redesign estrutural completo de `AudioSfxCueAsset`.

## Atualização F5 (2026-03-21) — Hardening de retorno pooled e sequence reuse

- Corrigido risco de dupla origem de retorno (`autoReturn` de pool vs retorno manual por completion/stop do handle):
  - pools canônicos de áudio (`Global2D` e `Spatial3D`) ajustados para `autoReturnSeconds=0` (retorno ownership do runtime de áudio);
  - `AudioGlobalSfxService` trata `already_returned` como skip idempotente (log observável, sem warning ruidoso).
- `ProbePooledSequenceReuse` fortalecido para validar ciclo real de reuso:
  - cada passo aguarda término natural do handle (com timeout explícito);
  - reduz falso negativo por `block_limit` em burst não controlado;
  - evidencia múltiplos ciclos `Pool rent -> Playback complete -> Pool return -> Pool rent`.

## Atualização F5 (2026-03-21) — Fechamento de authoring/config de pooled audio

- Status: DONE (shape canônico de authoring fechado para F5).
- Prefabs canônicos de voice pooled criados:
  - `Modules/Audio/Content/Pooled/Prefabs/AudioSfxVoiceGlobal.prefab`
  - `Modules/Audio/Content/Pooled/Prefabs/AudioSfxVoiceSpatial.prefab`
- Pool definitions canônicos de audio criados:
  - `Modules/Audio/Content/Pooled/Pools/AudioSfxPoolDefinition_Global2D.asset`
  - `Modules/Audio/Content/Pooled/Pools/AudioSfxPoolDefinition_Spatial3D.asset`
- Voice profiles canônicos alinhados:
  - `AudioSfxVoiceGlobalProfile.asset` -> `AudioSfxPoolDefinition_Global2D.asset`
  - `AudioSfxVoiceSpatialProfile.asset` -> `AudioSfxPoolDefinition_Spatial3D.asset`
- Cues pooled alinhados para trilha real de pooling:
  - `AudioSfxCue_GlobalPooled.asset` com `executionMode=PooledOneShot`
  - `AudioSfxCue_SpetialPooled.asset` com `executionMode=PooledOneShot`
- Diretriz consolidada:
  - conteudo permanece no `AudioSfxCueAsset`;
  - `PoolDefinitionAsset` define apenas a voice runtime pooled;
  - Audio pooled nao usa mais `Infrastructure/Testing/TestPools.prefab` como shape final.

## Evidência funcional de fechamento F4 (validação manual)

- `IGlobalAudioService` resolve via DI global em runtime.
- `Play` direto 2D funcional (`AudioSfxCueAsset` + `AudioPlaybackContext.Global`).
- `Play` direto 3D funcional por posição/follow target (`AudioPlaybackContext.Spatial`).
- Enforcement real de anti-spam:
  - `Global/2D` (mesmo cue, retrigger): `restart_existing` em vez de supressão;
  - `Spatial/3D`: bloqueio por cooldown (`SfxRetriggerCooldownSeconds`);
  - `Spatial/3D`: bloqueio por limite de simultâneos (`MaxSimultaneousInstances`).
- `IAudioPlaybackHandle` funcional:
  - `IsValid` e `IsPlaying` refletem estado real;
  - `Stop()` encerra playback (com suporte a fade no handle direto).
- Observabilidade adicionada:
  - `play start`;
  - `retrigger='restart_existing'` para `Global/2D`;
  - `blocked by cooldown`;
  - `blocked by simultaneous limit`;
  - `stop/completion`;
  - modo `2D/3D`;
  - cue/cueId;
  - reason/routing.

## Harnesses QA de cena para SFX (2026-03-21)

- `Modules/Audio/QA/AudioSfxDirectQaSceneHarness.cs` (trilho direto F4):
  - `QA/Audio/SFX/Direct/Validate Setup`
  - `QA/Audio/SFX/Direct/Play 2D`
  - `QA/Audio/SFX/Direct/Play 3D Position`
  - `QA/Audio/SFX/Direct/Play 3D Follow`
  - `QA/Audio/SFX/Direct/Burst Simultaneous`
  - `QA/Audio/SFX/Direct/Stop Last Handle`
  - `QA/Audio/SFX/Direct/Log Harness State`
- `Modules/Audio/QA/AudioSfxPooledQaSceneHarness.cs` (trilho pooled F5):
  - `QA/Audio/SFX/Pooled/Validate Setup`
  - `QA/Audio/SFX/Pooled/Play 2D`
  - `QA/Audio/SFX/Pooled/Play 3D`
  - `QA/Audio/SFX/Pooled/Probe Restart Existing`
  - `QA/Audio/SFX/Pooled/Probe Budget (Forced Diagnostic)`
  - `QA/Audio/SFX/Pooled/Probe Fallback (Forced Diagnostic)`
  - `QA/Audio/SFX/Pooled/Probe Sequence Reuse`
  - `QA/Audio/SFX/Pooled/Log State`
  - `QA/Audio/SFX/Pooled/Stop Last Handle`
- `Modules/Audio/QA/AudioSfxQaSceneHarness.cs` foi reduzido para shim legado de migração, sem concentrar testes.

## Evidência funcional de fechamento F3 (validação manual)

- `IAudioBgmService` resolve via DI global em runtime (`AudioBgmQaSceneHarness`).
- `FadeIn` funcional em `Play(primary)`.
- `Crossfade` funcional entre `primary` e `alternate`.
- `Stop` funcional com fade-out (semântica separada de `StopImmediate`).
- `StopImmediate` funcional sem fade.
- `SetPauseDucking(true/false)` funcional.
- Listener global canônico presente no boot (`AudioListenerRuntimeHost`) e persistente entre cenas.

## Harness QA de cena para F3 (2026-03-20)

- Arquivo: `Modules/Audio/QA/AudioBgmQaSceneHarness.cs`.
- Objetivo: validar F3 em Play Mode com componente solto de cena, sem integração com rotas/navigation.
- Campos mínimos:
  - `primaryCue`
  - `alternateCue`
  - `playPrimaryOnStart`
  - `scenarioStepDelaySeconds`
  - `verboseLogs`
- ContextMenu disponível:
  - `QA/Audio/BGM/Validate Setup`
  - `QA/Audio/BGM/Play Primary`
  - `QA/Audio/BGM/Play Alternate`
  - `QA/Audio/BGM/Crossfade To Primary`
  - `QA/Audio/BGM/Crossfade To Alternate`
  - `QA/Audio/BGM/Stop`
  - `QA/Audio/BGM/Stop Immediate`
  - `QA/Audio/BGM/Pause Ducking On`
  - `QA/Audio/BGM/Pause Ducking Off`
  - `QA/Audio/BGM/Run Basic Scenario`
  - `QA/Audio/BGM/Log Harness State`
- Sequência automática básica (`Run Basic Scenario`): validate -> play primary -> crossfade alternate -> ducking on -> ducking off -> stop.

## Hardening mínimo de listener global para F3 (2026-03-20)

- Arquivo: `Modules/Audio/Runtime/AudioPlaybackContext.cs` (classe `AudioListenerRuntimeHost`).
- Ownership: criado/garantido no boot por `Infrastructure/Composition/GlobalCompositionRoot.Audio.cs`.
- Comportamento:
  - garante host canônico `DontDestroyOnLoad` para `AudioListener`;
  - reusa host existente quando já presente;
  - aplica política mínima de duplicidade: mantém listener canônico ativo e desativa listeners extras habilitados de cena/câmera com log claro.
- Escopo explícito:
  - atende F3/BGM;
  - não representa ainda a política final de listener para áudio espacial;
  - sem integração com câmera/player/pose provider nesta etapa.
- Nota operacional:
  - o listener global mínimo não substitui a necessidade de configuração; AudioDefaults.asset deve estar referenciado no BootstrapConfig para o módulo operar sem modo degradado.

## Atualização semântica de defaults (2026-03-20)

- `AudioDefaultsAsset` consolidado como configuração técnica inicial/fallback do módulo.
- Removidos do defaults campos de conteúdo (`AudioBgmCueAsset`) e seleção de profile/pool de voice.
- Separação explícita reforçada:
  - defaults = valores técnicos iniciais
  - settings service = estado runtime mutável
  - conteúdo (BGM/SFX/profile) = fornecido por caller/bridge/catálogo/camada acima

## Atualização de cleanup (2026-03-20)

- Status: DONE (eixo de contrato `AudioSfxVoiceProfileAsset` x Pooling).
- `Modules/Audio/Config/AudioSfxVoiceProfileAsset.cs` removido de `PoolData` legado e alinhado para `PoolDefinitionAsset` canônico.
- Assets em `Modules/Audio/Content/VoicesProfile/**` mantidos sem pool configurado (null), já no novo shape serializado.
- Escopo respeitado: sem runtime de BGM, sem runtime de SFX pooled e sem consumo real de `IPoolService` nesta etapa.

## Atualização de integração BGM por contexto (2026-03-20)

- Ownership de conteúdo BGM opcional adicionado em três níveis:
  - `LevelDefinitionAsset` (owner preferencial por level).
  - `GameNavigationCatalogAsset` (owner por intent/navigation).
  - `SceneRouteDefinitionAsset` (owner por route como default estrutural).
- Bridge de integração registrado fora do core de Audio:
  - `Modules/Navigation/Runtime/NavigationLevelRouteBgmBridge.cs`
  - inscrição em `SceneTransitionStartedEvent`, `SceneTransitionBeforeFadeOutEvent` e `LevelSwapLocalAppliedEvent`.
- Timing de aplicação:
  - `SceneTransitionStartedEvent` = gatilho principal para macro transition (initial apply).
  - resolve no started com melhor contexto disponivel: `level_snapshot > navigation > route`.
  - `SceneTransitionBeforeFadeOutEvent` = confirmacao/correcao final (final confirm) com contexto pos-`LevelPrepare`.
  - `LevelSwapLocalAppliedEvent` = gatilho principal para troca intra-macro (local swap) com transicao sonora perceptivel.
  - dedupe por signature permanece no gatilho `before_fade_out`; divergencia entre cue inicial e final gera correction log explicito.
- Precedência efetiva aplicada pelo bridge:
  - `level > navigation/intent > route > sem troca automática`.
- Semântica final de aplicação registrada:
  - mesma cue efetiva da atual => no-op (continua tocando, sem replay);
  - cue efetiva diferente => `IAudioBgmService.Play(...)` (fade/crossfade pelo runtime);
  - cue efetiva nula/ausente => sem mudança forçada;
  - parada de música somente por ação explícita (`Stop`/`StopImmediate`).
- Escopo preservado:
  - sem alterações em `Modules/Audio/**` além da execução de `IAudioBgmService`.
  - sem F4/F5, sem SFX e sem pooling nesta entrega.

