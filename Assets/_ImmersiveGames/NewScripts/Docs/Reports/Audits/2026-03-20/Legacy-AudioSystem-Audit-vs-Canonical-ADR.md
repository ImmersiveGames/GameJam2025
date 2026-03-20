# Legacy AudioSystem Audit vs Canonical ADR

## 1. Resumo executivo

Esta auditoria analisou o legado em `Assets/_ImmersiveGames/Scripts/AudioSystem/**` e integrações reais em gameplay/presentation para comparar com a superfície canônica atual em `Assets/_ImmersiveGames/NewScripts/Docs/**`.

Conclusão principal:

- O legado de áudio tinha capacidades reais de produção (BGM global, SFX pooled, áudio por entidade, authoring por `SoundData`, integração com skin de tiro, UI de volume, tooling de preview/diagnóstico).
- A superfície canônica atual (`Docs/Canon`, `Docs/ADRs`, `Docs/Modules`, `Docs/Guides`) não possui ADR canônico explícito de áudio vigente.
- Portanto, a cobertura “ADR canônico consolidado de áudio” é, na prática, ausente/implícita hoje no NewScripts.

Implicação arquitetural:

- Há risco de regressão silenciosa e decisões conflitantes (ownership, bootstrap, limites de voz, política de pause/ducking, contratos de integração com gameplay) porque o comportamento real existe no código legado, mas sem contrato ADR formal no trilho canônico.

## 2. Capacidades reais do legado

Capacidades confirmadas no código legado:

- Bootstrap automático de áudio por `RuntimeInitializeOnLoadMethod` (`AudioSystemBootstrap.EnsureAudioSystemInitialized`).
- Registro de serviços globais em DI (`IAudioMathService`, `IAudioVolumeService`, `IAudioSfxService`, `IBgmAudioService`).
- Instanciação automática de `GlobalBgmAudioService` via prefab em `Resources/Audio/Prefabs/GlobalBgmAudioService`.
- BGM global com:
  - `PlayBGM`, `StopBGM`, `StopBGMImmediate`, `PauseBGM`, `ResumeBGM`, `CrossfadeBGM`,
  - fade-in/fade-out,
  - volume por mixer parameter (`BGM_Volume`) ou fallback em `AudioSource`.
- SFX global via `IAudioSfxService` + `AudioSfxService` com:
  - `PlayOneShot` e `PlayLoop`,
  - retorno de `IAudioHandle` para controle de playback,
  - cálculo de volume por camadas (`SoundData`, `AudioConfig`, `AudioServiceSettings`, `AudioContext`).
- Pooling/reuso de vozes via `SoundEmitter` + `ObjectPool`/`PoolManager`.
- SFX espacial e não-espacial por `AudioContext.useSpatial` + `SoundData.spatialBlend`.
- Áudio por entidade via `EntityAudioEmitter` (helper padrão para gameplay objects).
- Authoring por assets:
  - `SoundData` (clip, volume, loop, priority, random pitch, spatial settings, mixer group opcional),
  - `AudioConfig` (defaults de mixer/volume/spatial),
  - `AudioServiceSettings` (master/bgm/sfx + multipliers).
- Builder fluente `SoundBuilder` para calls de reprodução (posição, spatial, volume multiplier/override, fade-in, loop).
- Integração real com gameplay:
  - `PlayerShootController` (áudio de tiro via skin key),
  - `DamageReceiver` (hit/death/revive),
  - `ExplosionEffect`,
  - `PlanetDetectableController`,
  - `EaterBehavior`/`EaterDesireService`/`EaterEatingState`.
- Integração com SkinSystem:
  - `SkinAudioConfigData` (`SkinAudioKey -> SoundData`),
  - `SkinAudioConfigurable`/`IActorSkinAudioProvider`.
- UI/menu de áudio:
  - `AudioSettingsUI` com sliders de BGM/SFX escrevendo `AudioServiceSettings`.
- Tooling de editor/QA:
  - `SoundDataEditor` (preview no inspector),
  - `AudioRuntimeDiagnostics` (overlay runtime),
  - `AudioPreviewPanel` (runtime panel),
  - `AudioSystemScenarioTester` (cenários de teste).

## 3. Ownership e bootstrap do legado

Ownership real observado:

- Existe “global service ownership” (BGM/SFX/volume/math) no DI legado.
- Gameplay ownership é por chamada local em componentes (`EntityAudioEmitter`), mas depende do bootstrap global e container.

Bootstrap real observado:

- `AudioSystemBootstrap` inicializa automaticamente após carregamento de cena.
- Componentes também forçam init em `Awake` (`EntityAudioEmitter`, `AudioSettingsUI`, testers), criando init redundante porém defensivo.
- `AudioBootstrapper` (Mono singleton legado) ainda pode tocar BGM inicial em `Start`, em paralelo ao bootstrap estático.

Pontos críticos:

- Há gate de compilação `#if NEWSCRIPTS_MODE` que ignora bootstrap legado quando ativo.
- Se `DependencyManager`/`PoolManager`/assets de `Resources` faltarem, o sistema degrada com warnings/errors e sem hard fail central.

## 4. BGM

Capacidades reais de BGM:

- Reprodução global única por `AudioSource` dedicado.
- Fade-in/fade-out em corrotina.
- Crossfade entre faixas (`CrossfadeBGM`).
- Pause/Resume de BGM.
- Controle de volume de BGM via `AudioMixer` parameter (`BGM_Volume`) ou volume direto no source.
- `DontDestroyOnLoad` para continuidade entre cenas.

Limites/comportamentos importantes:

- BGM é essencialmente single-channel (um source principal).
- Não há contrato explícito de prioridade entre “BGM de bootstrap” vs “BGM de gameplay”; depende da ordem de chamadas.

## 5. SFX global e espacial

Capacidades reais:

- SFX disparado via `IAudioSfxService` com pooling de `SoundEmitter`.
- Suporte a:
  - one-shot,
  - loop,
  - fade-in,
  - spatial/non-spatial,
  - random pitch,
  - mixer por som (com fallback default),
  - override/multiplicador de volume por contexto.

Observações:

- Existe também fallback de one-shot em `GlobalBgmAudioService.PlaySound(...)` sem pool.
- A assinatura de `IBgmAudioService` não inclui `PlaySound`, então esse caminho depende do tipo concreto e não do contrato de interface.

## 6. Audio por entidade/objeto

Capacidade real:

- `EntityAudioEmitter` encapsula uso de `IAudioSfxService` para objetos de gameplay.
- Fornece helpers `Play(...)` e `PlayAtSelf(...)` com defaults de spatial via `AudioConfig`.

Padrão de uso real no legado:

- Sistemas de gameplay dependem de `EntityAudioEmitter` para eventos locais (tiro, hit, morte, descoberta, explosão, mordida, desejo).

Acoplamentos relevantes:

- Algumas integrações tentam resolver `EntityAudioEmitter` via `DependencyManager` por `ActorId` (ex.: `EaterBehavior`), com fallback em `GetComponent`.

## 7. Pooling / voices / playback control

Capacidades reais:

- Reuso de emitters via `ObjectPool`.
- Handle por playback (`IAudioHandle`) com `IsPlaying` e `Stop`.
- Retorno automático ao pool para one-shots ao final do clip.
- Loops podem ser encerrados via `Stop` do handle/emitter.

Lacunas/risco técnico:

- `SoundEmitterPoolData.maxSoundInstances` existe, mas não é efetivamente aplicado no pipeline de aquisição de voz do `ObjectPool`.
- Política de concorrência/voice stealing não está formalizada.
- `IAudioHandle.Stop(float fadeOutSeconds)` aceita fade-out, mas implementação de SFX ignora o parâmetro (stop imediato).
- `IsPlaying` do handle de SFX usa `activeInHierarchy`, não estado real do `AudioSource`.

## 8. Config / defaults / mixer / settings

Capacidades reais:

- `SoundData` como unidade de authoring por clip.
- `AudioConfig` para defaults de SFX (mixer default, volume default, maxDistance, spatial default).
- `AudioServiceSettings` como fonte global de master/BGM/SFX + multipliers.
- Cálculo de volume centralizado (`AudioVolumeService` + `AudioMathService`).

Roteamento/mixer:

- BGM: mixer group dedicado + parâmetro exposto em `AudioMixer`.
- SFX: mixer group por `SoundData` ou fallback de `AudioConfig`.

Persistência/config runtime:

- `AudioSettingsUI` altera `AudioServiceSettings` em memória durante runtime.
- Não há evidência no legado auditado de persistência em disco (PlayerPrefs/arquivo) do volume do usuário.

## 9. Integrações indevidas ou acoplamentos problemáticos

Acoplamentos estruturais observados:

- Legado (`Scripts/AudioSystem`) depende de namespaces do trilho novo (`_ImmersiveGames.NewScripts.Core.Composition` e `...Core.Logging`).
- Áudio legado acoplado ao container global (`DependencyManager`) e a assets fixos em `Resources`.
- Gameplay acoplado à presença de `EntityAudioEmitter` em múltiplos pontos.
- `PlayerShootController` acoplado a `SkinAudioConfigurable` para obter som.
- `GlobalBgmAudioService` mistura responsabilidade de BGM com fallback de one-shot SFX.

Ownership incorreto/ambíguo:

- Não existe owner canônico formal de áudio no índice canônico atual.
- Há dupla superfície de bootstrap (estático + componente) sem contrato arquitetural oficial.

## 10. O que o ADR canônico já cobre

Cobertura explícita encontrada na documentação canônica atual (`NewScripts/Docs`):

- Nenhum ADR vigente de áudio (BGM/SFX/mixer/pooling/handles/UI de áudio).
- Nenhum eixo explícito de áudio em `Docs/Canon/Canon-Index.md`.
- Nenhum módulo oficial dedicado a áudio em `Docs/Modules` na cadeia operacional oficial.

Cobertura indireta possível:

- ADRs de lifecycle/startup/ownership cobrem padrões gerais de responsabilidade, mas não definem contratos específicos de áudio.

## 11. O que o ADR canônico ainda não cobre explicitamente

Lacunas explícitas (sem contrato canônico formal):

- Owner canônico de áudio (quem decide política global).
- Contrato de bootstrap/init de áudio.
- Contrato de BGM (single-channel, crossfade, prioridade de troca de trilha).
- Contrato de SFX global vs SFX por entidade.
- Contrato de pooling/vozes (limite, exaustão, política quando lota).
- Contrato de `IAudioHandle` (semântica de `IsPlaying`, suporte real de fade-out).
- Contrato de anti-spam/dedupe/cooldown de eventos sonoros.
- Contrato de pause global de áudio e ducking (só BGM possui pause hoje).
- Contrato de roteamento de mixer (BGM/SFX/master parâmetros obrigatórios).
- Contrato de defaults/config e persistência de settings do usuário.
- Contrato de integração com SkinSystem (obrigatório vs opcional por domínio).
- Delimitação clara do que é tooling QA/editorial vs runtime de produção.

## 12. Recomendações de ajuste no ADR

### 12.1 Entrar no ADR consolidado

Itens que devem entrar como contrato arquitetural explícito:

- Ownership canônico de áudio (owner + boundaries).
- Bootstrap oficial único (ordem, idempotência, comportamento em fallback).
- Separação de responsabilidades:
  - BGM service (música global),
  - SFX service (efeitos),
  - emitter por entidade (call-site local).
- Contrato mínimo de BGM: play/stop/pause/resume/crossfade + política de troca.
- Contrato mínimo de SFX: one-shot/loop/spatial + contexto de volume.
- Política de vozes/pooling: limite real, exaustão, estratégia quando lota.
- Semântica formal de handle (`IsPlaying`, `Stop` com/sem fade-out).
- Política de pause/ducking (o que pausa, quando, e quem controla).
- Contrato de mixer/settings: parâmetros canônicos e fallback aceitável.
- Integração com gameplay: padrão recomendado (`EntityAudioEmitter`) e regras de acoplamento.

### 12.2 Ficar fora por decisão consciente

Itens que podem ficar fora do ADR (desde que explicitado):

- Lista concreta de `SkinAudioKey` (`Shoot`, `Hit`, etc.) como enum específica de conteúdo.
- Valores numéricos default (volumes, durações de fade, maxDistance) específicos de tuning.
- Caminhos de assets de teste/preview usados só em QA.

### 12.3 Tratar como detalhe de implementação (não ADR)

- Implementação interna de corrotinas de fade.
- Detalhes de UI debug (`OnGUI`, hotkeys de painel de teste).
- Mensagens de log específicas.
- Estrutura exata de classes utilitárias de editor (desde que capacidade de preview permaneça disponível via tooling aprovado).

## 13. Checklist final de lacunas

- [ ] ADR canônico de áudio criado e indexado em `Docs/ADRs/README.md`.
- [ ] Owner canônico de áudio adicionado em `Docs/Canon/Canon-Index.md`.
- [ ] Contrato de bootstrap de áudio definido (fonte única de init).
- [ ] Contrato de BGM formalizado (incluindo crossfade e prioridade).
- [ ] Contrato de SFX formalizado (global + por entidade).
- [ ] Política de voice limit/exaustão formalizada e implementada.
- [ ] Contrato de `IAudioHandle` alinhado com implementação real.
- [ ] Política de anti-spam/cooldown de eventos sonoros definida.
- [ ] Política de pause global + ducking definida.
- [ ] Contrato de mixer/settings (master/BGM/SFX) formalizado.
- [ ] Delimitação explícita de tooling QA/editor vs runtime de produção.
- [ ] Mapeamento de integrações de gameplay documentado (Damage, Shoot, Eater, Planet, FX).
