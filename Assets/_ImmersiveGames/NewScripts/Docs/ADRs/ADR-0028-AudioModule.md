# ADR-0028 â€” Arquitetura CanÃ´nica de Audio em NewScripts (`GlobalAudio` vs `EntityAudio`)

## Status

- Estado: **Aceito**
- Data (decisÃ£o): 2026-03-20
- Supersedes:
    - versao anterior de ADR-0028 (revisao pre-consolidacao)
    - `ADR-0032-Audio-Arquitetura-GlobalAudio-vs-EntityAudio.md`
- Relacionados:
    - ADR-0030 â€” ActorSystems como Contrato-Base
    - ADR-0031 â€” PresentationSystems como Orquestrador de Apresentacao

## Contexto

O sistema de Ã¡udio legado possuÃ­a capacidades reais de produÃ§Ã£o, incluindo:

- bootstrap automÃ¡tico,
- BGM global,
- SFX global e espacial,
- pooling de voices,
- Ã¡udio por entidade,
- controle de playback por handle,
- settings de volume,
- integraÃ§Ãµes com gameplay,
- tooling/editor/QA.

PorÃ©m, a estrutura do legado nÃ£o era compatÃ­vel com o canon de `NewScripts`, por depender de:

- bootstrap prÃ³prio do mÃ³dulo,
- `Resources.Load`,
- singleton estrutural como owner,
- fallback frouxo de DI,
- mistura estrutural entre `Scripts/**` e `NewScripts/**`.

A primeira canonizaÃ§Ã£o em `NewScripts` recuperou parte importante do comportamento Ãºtil, mas a documentaÃ§Ã£o ficou dividida:

- um ADR mais operacional, com contratos Ãºteis de runtime e conteÃºdo;
- um ADR posterior mais correto em ownership e fronteiras, porÃ©m menos completo nos detalhes operacionais.

AlÃ©m disso, a primeira tentativa de reconstruÃ§Ã£o arrastou um problema de direÃ§Ã£o arquitetural: o mÃ³dulo de Ã¡udio passou a ser pensado jÃ¡ acoplado a consumidores como `Navigation`, `Gameplay`, `Presentation`, `Skin` e outros domÃ­nios especÃ­ficos.

Este ADR unifica os dois contratos anteriores e explicita uma correÃ§Ã£o adicional:

**o mÃ³dulo `Audio` deve nascer standalone, com portas prÃ³prias e sem dependÃªncia de mÃ³dulos consumidores.**

## DecisÃ£o

SerÃ¡ mantido um Ãºnico mÃ³dulo canÃ´nico em:

`Assets/_ImmersiveGames/NewScripts/Modules/Audio/**`

A arquitetura canÃ´nica do mÃ³dulo passa a ser explicitamente dividida em **duas trilhas independentes**:

1. `GlobalAudio`
2. `EntityAudio`

PrincÃ­pio de evoluÃ§Ã£o:

**reaproveitar comportamento e conceito Ãºtil do legado, reimplementar o runtime no canon atual.**

## Regras canÃ´nicas

### 1. Ownership e bootstrap

A inicializaÃ§Ã£o do sistema de Ã¡udio em `NewScripts` Ã© responsabilidade do `GlobalCompositionRoot`.

NÃ£o entram no canon:

- bootstrap prÃ³prio de Ã¡udio,
- `RuntimeInitializeOnLoadMethod`,
- `EnsureAudioSystemInitialized`,
- `Resources.Load`,
- singleton estrutural como owner do sistema,
- compat layer permanente para preservar shape legado.

### 2. O mÃ³dulo `Audio` Ã© standalone

O mÃ³dulo `Audio` deve ser autocontido no seu domÃ­nio.

Ele pode conhecer apenas:

- seus contratos pÃºblicos,
- seus assets base,
- seu runtime,
- sua infra interna,
- a infraestrutura compartilhada canÃ´nica permitida pelo projeto.

O mÃ³dulo `Audio` nÃ£o deve depender estruturalmente de:

- `Modules/Navigation/**`
- `Modules/Gameplay/**`
- `Modules/Presentation/**`
- `Modules/LevelFlow/**`
- `Modules/Skin/**`
- qualquer outro mÃ³dulo consumidor especÃ­fico.

ConsequÃªncia:

- coleÃ§Ãµes, catÃ¡logos, profiles e regras especÃ­ficas de domÃ­nio ficam fora do core do mÃ³dulo `Audio`;
- integraÃ§Ãµes com outros mÃ³dulos acontecem depois, por `helpers`, `facades`, `bridges` ou `adapters`, no lado consumidor ou em `Interop/**`, sem transferir ownership para o mÃ³dulo de Ã¡udio.

### 3. `Presentation` nÃ£o Ã© runtime owner de Ã¡udio

`PresentationSystems` pode:

- orquestrar apresentaÃ§Ã£o,
- traduzir intenÃ§Ã£o visual/apresentacional para intenÃ§Ã£o de Ã¡udio,
- acionar bridges para o mÃ³dulo de Ã¡udio.

`PresentationSystems` nÃ£o pode:

- possuir runtime concreto de Ã¡udio,
- executar polÃ­tica interna de Ã¡udio,
- virar owner de estado interno de Ã¡udio,
- resolver estruturalmente contratos de entidade que pertencem a `EntityAudio`.

`Presentation -> Audio` permanece permitido como bridge de traduÃ§Ã£o/orquestraÃ§Ã£o, nunca como ownership de runtime.

### 4. `GlobalAudio` e `EntityAudio` sÃ£o trilhas distintas

#### `GlobalAudio`

ResponsÃ¡vel por:

- BGM global,
- Ã¡udio de menu/UI,
- stingers globais,
- SFX de fluxo macro do jogo,
- ducking global por contexto macro.

SuperfÃ­cies canÃ´nicas alvo:

- `IAudioBgmService`
- `IGlobalAudioService`
- `IAudioSettingsService`

#### `EntityAudio`

ResponsÃ¡vel por:

- Ã¡udio local de ator/objeto,
- playback por semÃ¢ntica de `purpose`,
- integraÃ§Ã£o opcional com contexto espacial do ator,
- resoluÃ§Ã£o contextual de `purpose -> cue` quando aplicÃ¡vel.

SuperfÃ­cie canÃ´nica alvo:

- `IEntityAudioService`

### 5. `cue` define comportamento; `purpose` define intenÃ§Ã£o semÃ¢ntica

- `cue` define comportamento tÃ©cnico de playback:
    - conteÃºdo,
    - modo,
    - policy,
    - concorrÃªncia,
    - execuÃ§Ã£o direta vs pooled,
    - spatialidade,
    - routing,
    - overrides.
- `purpose` define intenÃ§Ã£o semÃ¢ntica para consumidores de entidade:
    - `Movement`,
    - `Impact`,
    - `Attack`,
    - `Death`,
    - etc.

A resoluÃ§Ã£o `purpose -> cue` pertence ao domÃ­nio `EntityAudio` ou a contrato explicitamente delegado por ele.

Ela nÃ£o pertence a `Presentation`.

### 6. BGM e SFX continuam separados

#### BGM

BGM Ã© domÃ­nio global do jogo.

Uso esperado:

- startup,
- frontend,
- gameplay,
- transiÃ§Ãµes macro,
- pause via ducking.

O contrato canÃ´nico de BGM deve suportar, no mÃ­nimo:

- play,
- stop com fade,
- stop imediato,
- pause ducking,
- troca com fade/crossfade quando necessÃ¡rio.

#### SFX

SFX Ã© domÃ­nio contextual, com duas naturezas:

- **global/contextual macro** quando pertence ao fluxo do jogo/UI,
- **local/entity** quando pertence a ator/objeto.

Nem todo SFX Ã© de entidade.

### 7. Pooling Ã© infraestrutura compartilhada

O mÃ³dulo de Ã¡udio nÃ£o cria subsistema prÃ³prio de pooling.

O Ã¡udio consome a infraestrutura canÃ´nica em `Infrastructure/Pooling/**`.

ConsequÃªncias:

- nÃ£o hÃ¡ `PoolManager` prÃ³prio do mÃ³dulo,
- nÃ£o hÃ¡ pool resolvido por `Resources.Load`,
- o ownership do pooling permanece no `GlobalCompositionRoot`,
- a polÃ­tica pooled de SFX usa `PoolDefinitionAsset` via `AudioSfxVoiceProfileAsset`.

### 8. Nem todo SFX Ã© pooled

O mÃ³dulo suporta canonicamente:

- `DirectOneShot`
- `PooledOneShot`

Uso tÃ­pico:

- click/hover de UI: geralmente `DirectOneShot`
- tiros, impactos repetitivos, explosÃµes frequentes: geralmente `PooledOneShot`

A escolha do trilho direto vs pooled Ã© parte da policy do `cue`, com override contextual quando o contrato permitir.

### 9. `EntityAudioEmitter` mÃ­nimo ou inexistente

`EntityAudioEmitter` pode existir apenas como binding estrutural mÃ­nimo de contexto, por exemplo:

- `spatialAnchor`,
- `defaultVolumeScale`,
- `defaultVoiceProfile`,
- ponto de emissÃ£o local,
- `defaultCue` apenas quando fizer sentido como conveniÃªncia local.

Ele nÃ£o deve:

- ser fonte canÃ´nica de authoring de comportamento,
- carregar fallback estrutural,
- virar owner de resoluÃ§Ã£o semÃ¢ntica,
- mascarar lacuna arquitetural do domÃ­nio `EntityAudio`.

Se nÃ£o agregar valor real ao contrato final, pode ser removido.

### 10. Sem compat layer e sem fallback estrutural como estratÃ©gia final

NÃ£o Ã© permitido:

- manter API/shape legado por medo de corte,
- transformar camada de compatibilidade temporÃ¡ria em soluÃ§Ã£o permanente,
- mascarar erro estrutural com fallback silencioso.

Erros estruturais devem ser corrigidos na origem.

### 11. Fronteiras com ActorSystems e PresentationSystems

#### Com ADR-0030 (ActorSystems)

- Ã¡udio nÃ£o pertence ao core de `ActorSystems`,
- no mÃ¡ximo, capabilities pequenas e opcionais de integraÃ§Ã£o estrutural.

#### Com ADR-0031 (PresentationSystems)

- `PresentationSystems` Ã© orquestrador de apresentaÃ§Ã£o,
- bridge `Presentation -> Audio` Ã© permitida,
- ownership de runtime de Ã¡udio permanece no mÃ³dulo `Audio`.

## Estrutura canÃ´nica do mÃ³dulo

A estrutura canÃ´nica permanece organizada em:

- `Modules/Audio/Bindings/**`
- `Modules/Audio/Config/**`
- `Modules/Audio/Runtime/**`
- `Modules/Audio/Interop/**`
- `Modules/Audio/Editor/**`
- `Modules/Audio/QA/**`

## SuperfÃ­cie pÃºblica canÃ´nica

A superfÃ­cie pÃºblica consolidada do mÃ³dulo contempla:

### Global
- `IAudioBgmService`
- `IGlobalAudioService`
- `IAudioSettingsService`

### Entity / playback
- `IEntityAudioService`
- `IAudioPlaybackHandle`
- `AudioPlaybackContext`

### ConteÃºdo / config
- `AudioCueAsset` (base abstrata)
- `AudioBgmCueAsset`
- `AudioSfxCueAsset`
- `AudioDefaultsAsset`
- `AudioSfxVoiceProfileAsset`

### Binding estrutural opcional
- `EntityAudioEmitter`

## Modelo de conteÃºdo

### `AudioCueAsset`

Base abstrata compartilhada entre BGM e SFX.

Concentra apenas campos comuns:

- lista de clips,
- mixer group opcional,
- volume base,
- loop quando aplicÃ¡vel,
- pitch mÃ­nimo/mÃ¡ximo,
- randomizaÃ§Ã£o simples de volume,
- validaÃ§Ã£o runtime comum.

A base nÃ£o deve ser criada diretamente como asset final de conteÃºdo.

### `AudioBgmCueAsset`

Representa mÃºsica global.

CaracterÃ­sticas:

- sempre global,
- nÃ£o expÃµe policy de execuÃ§Ã£o de SFX,
- nÃ£o expÃµe spatializaÃ§Ã£o.

### `AudioSfxCueAsset`

Representa efeito sonoro.

Campos canÃ´nicos:

- `PlaybackMode` (`Global` / `Spatial`)
- `SpatialBlend`
- `MinDistance`
- `MaxDistance`
- `AudioSfxExecutionMode` (`DirectOneShot` / `PooledOneShot`)
- `VoiceProfileOverride`
- `MaxSimultaneousInstances`
- `SfxRetriggerCooldownSeconds` quando houver policy de anti-spam por cue

## Defaults do projeto vs estado do jogador

### `AudioDefaultsAsset`

Define defaults do projeto:

- volumes padrÃ£o,
- multiplicadores por categoria,
- fade padrÃ£o de BGM,
- mixer routing base,
- parÃ¢metros tÃ©cnicos iniciais do mÃ³dulo.

Limites explÃ­citos de responsabilidade:

- `AudioDefaultsAsset` nÃ£o seleciona conteÃºdo de Ã¡udio.
- `AudioDefaultsAsset` nÃ£o escolhe `AudioBgmCueAsset`.
- `AudioDefaultsAsset` nÃ£o escolhe `AudioSfxCueAsset`.
- `AudioDefaultsAsset` nÃ£o escolhe pool/perfil de voice.
- conteÃºdo de BGM/SFX deve vir de caller/bridge/catÃ¡logo/camada acima.

### `IAudioSettingsService`

Representa estado runtime do jogador/sessÃ£o.

ConsequÃªncias:

- UI nÃ£o grava diretamente no asset de defaults,
- defaults nÃ£o representam o estado atual da sessÃ£o,
- persistÃªncia de settings do jogador Ã© preocupaÃ§Ã£o do serviÃ§o/runtime, nÃ£o do asset de defaults,
- defaults existem como seed/fallback canÃ´nico quando ainda nÃ£o hÃ¡ customizaÃ§Ã£o de jogador.

## Playback context

`AudioPlaybackContext` Ã© o contexto canÃ´nico de playback.

Ele transporta, no mÃ­nimo:

- intenÃ§Ã£o `global` / `spatial`,
- posiÃ§Ã£o ou target de follow,
- `volumeScale`,
- `reason`,
- profile contextual opcional.

Objetivo:

- evitar explosÃ£o de overloads frÃ¡geis,
- permitir override explÃ­cito sem multiplicar assinaturas pÃºblicas.

## Voice profile

`AudioSfxVoiceProfileAsset` representa a famÃ­lia/configuraÃ§Ã£o da voice de SFX.

Ele liga o domÃ­nio de Ã¡udio ao pooling canÃ´nico, definindo:

- `PooledVoicePoolDefinition` (`PoolDefinitionAsset`),
- polÃ­tica de fallback direto quando aplicÃ¡vel,
- tuning por famÃ­lia de uso.

Estado canonico de authoring (F5):

- pools de audio sao definidos por `PoolDefinitionAsset` e referenciados no voice profile;
- existem dois trilhos canonicos de pool:
  - global/2D (`AudioSfxPoolDefinition_Global2D.asset` -> `AudioSfxVoiceGlobal.prefab`);
  - spatial/3D (`AudioSfxPoolDefinition_Spatial3D.asset` -> `AudioSfxVoiceSpatial.prefab`);
- o prefab do pool representa a voice runtime (carrier), nao o conteudo final do som;
- o conteudo continua no `AudioSfxCueAsset` (clip/volume/pitch/routing/playback policy).

## Runtime canÃ´nico

### `IAudioBgmService`

ResponsÃ¡vel por mÃºsica global.

Contrato mÃ­nimo esperado:

- `Play(...)`
- `Stop(...)`
- `StopImmediate(...)`
- `SetPauseDucking(...)`
- troca com fade/crossfade quando aplicÃ¡vel pelo contrato final do serviÃ§o

### `IGlobalAudioService`

ResponsÃ¡vel por Ã¡udio global nÃ£o-BGM:

- UI,
- stingers,
- SFX macro do jogo,
- playback global de cues fora de entidade.

Ele deve resolver:

- execuÃ§Ã£o direta vs pooled,
- playback global vs spatial macro quando aplicÃ¡vel,
- profile efetivo,
- anti-spam simples,
- concorrÃªncia por cue,
- handle real ou no-op conforme o modo de execuÃ§Ã£o.

Estado atual (F5):

- `IGlobalAudioService` jÃ¡ opera com trilha direta e trilha pooled.
- Em cues `PooledOneShot`, o runtime consome `IPoolService` + `PoolDefinitionAsset` via `AudioSfxVoiceProfileAsset`.
- Sem profile/pool vÃ¡lido, aplica fallback para trilha direta apenas quando `allowDirectFallback` estiver habilitado.
- O authoring canonico de pooled audio usa prefabs dedicados de audio (`AudioSfxVoiceGlobal.prefab` e `AudioSfxVoiceSpatial.prefab`), sem depender de prefab generico de teste.

Saneamento arquitetural pre-F6/F7 (A1 + B1):

- policy interna de SFX extraida do fluxo monolitico para componentes dedicados:
  - `AudioSfxDirectPolicyEngine` (retrigger/cooldown/limit no trilho direto);
  - `AudioSfxPooledPolicyEngine` (budget/fallback/bloqueio no trilho pooled).
- `AudioGlobalSfxService` permanece como orquestrador, com menor concentracao de decisao no mesmo metodo.
- sem redesign completo de authoring/asset contract nesta etapa (Opção C permanece fora de escopo).

### `IEntityAudioService`

ResponsÃ¡vel por Ã¡udio local de ator/objeto e por resoluÃ§Ã£o semÃ¢ntica de `purpose` para playback efetivo.

Ele deve ser capaz de:

- receber intenÃ§Ã£o semÃ¢ntica ou cue jÃ¡ resolvida, conforme contrato adotado,
- resolver playback local/global quando aplicÃ¡vel,
- integrar contexto espacial do ator,
- aplicar policy de cue/profile sem depender de `Presentation`.

### `IAudioPlaybackHandle`

Representa instÃ¢ncia runtime reproduzida pelo sistema.

Contrato esperado:

- `IsValid`
- `IsPlaying`
- `Stop(float fadeOutSeconds = 0f)`

Regras canÃ´nicas:

- `IsPlaying` deve representar playback real de forma consistente, e nÃ£o apenas â€œobjeto ativo em hierarquiaâ€.
- se `Stop(fadeOutSeconds)` existir no contrato, a implementaÃ§Ã£o deve respeitar a semÃ¢ntica prometida ou documentar explicitamente o limite do handle retornado.

### `AudioSfxVoice`

Voice dedicada e reutilizÃ¡vel de SFX.

Responsabilidades:

- ownar `AudioSource`,
- aplicar playback resolvido,
- acompanhar `followTarget` quando aplicÃ¡vel,
- devolver-se ao pool ou destruir-se ao fim do playback,
- responder a `Stop()` com ou sem fade, conforme o contrato do handle/voice.

## Ordem canÃ´nica de resoluÃ§Ã£o de `VoiceProfile`

Para playback pooled, a resoluÃ§Ã£o efetiva Ã©:

1. `AudioSfxCueAsset.VoiceProfileOverride`
2. profile contextual do playback (`AudioPlaybackContext.VoiceProfile` ou binding estrutural mÃ­nimo equivalente)
3. profile explÃ­cito da camada chamadora/bridge/catÃ¡logo quando o fluxo exigir

Se nenhum profile for resolvido explicitamente, o runtime deve degradar para trilho nÃ£o-pooled (quando permitido) em vez de depender de defaults de conteÃºdo.

## Anti-spam e concorrÃªncia

O mÃ³dulo inclui proteÃ§Ã£o mÃ­nima via:

- cooldown curto por cue,
- limite simples de instÃ¢ncias simultÃ¢neas por cue,
- drop simples quando o limite Ã© atingido.

### Politica de retrigger para SFX Global / 2D

- SFX `Global/2D` representa feedback imediato e nao deve ser suprimido quando repetido.
- Quando o mesmo cue `Global/2D` for solicitado novamente:
  - a instancia ativa anterior deve ser interrompida;
  - o novo playback deve iniciar imediatamente (`restart_existing`);
  - cooldown e limite de simultaneos nao devem bloquear esse retrigger.

Esta semantica vale no F4 (direct one-shot) e deve ser preservada no F5 (pooled).

No F5, o mesmo comportamento `restart_existing` tambem vale quando a instancia anterior estiver em voice pooled (com retorno correto ao pool).

### Politica para SFX Spatial / 3D

- SFX `Spatial/3D` pode seguir politica distinta de concorrencia.
- Cooldown e limite de simultaneos permanecem validos conforme runtime/fase.

Importante:

esses limites devem ser **enforcement real** do runtime, e nÃ£o apenas campos declarativos sem efeito prÃ¡tico.

Voice stealing sofisticado fica fora do escopo atual.

## PolÃ­tica de BGM por contexto

### Regra canÃ´nica
BGM Ã© reproduzido por cue explÃ­cita recebida por chamada de serviÃ§o.

O mÃ³dulo `Audio` nÃ£o escolhe mÃºsica por conta prÃ³pria a partir de defaults.

Mapeamento de contexto (`startup`, `frontend`, `gameplay`, `route`, `level`) pertence Ã  camada chamadora/bridge/catÃ¡logo acima do core de `Modules/Audio/**`.

### Pause
Aplica ducking de BGM em vez de pausar completamente.

## Mixer e routing

O uso do `AudioMixer` da Unity Ã© compatÃ­vel com este ADR e faz parte do desenho do mÃ³dulo, mas o contrato detalhado de mixer ainda nÃ£o estÃ¡ completamente fechado neste momento.

Este ADR jÃ¡ assume:

- `mixer group` opcional em cue,
- routing base em `AudioDefaultsAsset`,
- possibilidade de ducking global por pause,
- integraÃ§Ã£o futura entre `IAudioSettingsService` e parÃ¢metros expostos do mixer.

Ainda assim, o detalhamento completo de:

- mixer asset canÃ´nico,
- grupos obrigatÃ³rios,
- parÃ¢metros expostos obrigatÃ³rios,
- regra final de routing,
- polÃ­tica final de aplicaÃ§Ã£o de volume do usuÃ¡rio,
- implementaÃ§Ã£o final de ducking,

fica registrado como refinamento complementar do contrato deste ADR.

## IntegraÃ§Ãµes opcionais

### Gameplay
IntegraÃ§Ãµes com gameplay sÃ£o permitidas e esperadas, mas devem consumir contratos canÃ´nicos do mÃ³dulo.

### Navigation / LevelFlow / rotas
Uso de BGM por rota, cue por contexto de navegaÃ§Ã£o ou catÃ¡logos por route/level Ã© permitido como integraÃ§Ã£o futura.

Esses catÃ¡logos e bridges pertencem ao mÃ³dulo consumidor ou Ã  camada de integraÃ§Ã£o, nÃ£o ao core de `Modules/Audio/**`.

### Skin audio
IntegraÃ§Ã£o com skin/config temÃ¡tica Ã© opcional.

Ela pode existir como camada de resoluÃ§Ã£o de conteÃºdo, mas nÃ£o deve definir ownership do mÃ³dulo nem contaminar o contrato central de `GlobalAudio` / `EntityAudio`.

## Editor UX e QA

O mÃ³dulo pode manter:

- custom editors,
- preview/harness de QA,
- cues QA dedicadas,
- tooling de inspeÃ§Ã£o e diagnÃ³stico.

Estado atual de QA de SFX (pre-F6/F7):

- `AudioSfxDirectQaSceneHarness` cobre somente trilho direto (2D/3D + cooldown/limit/stop basico).
- `AudioSfxPooledQaSceneHarness` cobre somente trilho pooled (restart_existing, budget, fallback e sequence reuse).
- probes forcados de diagnostico ficam restritos ao harness pooled.
- `AudioSfxQaSceneHarness` permanece apenas como shim legado de migracao, sem concentrar todos os testes.

Esses artefatos sÃ£o suporte operacional do mÃ³dulo e nÃ£o alteram o contrato arquitetural central.

## Fora de escopo

Ficam fora do escopo imediato:

- adaptive music,
- playlists complexas,
- stingers/camadas avanÃ§adas alÃ©m do contrato bÃ¡sico,
- priority / voice stealing sofisticado,
- preview/tooling legado como contrato obrigatÃ³rio,
- sistema completo de skin audio como eixo central do mÃ³dulo,
- catÃ¡logos especÃ­ficos de `Navigation`, `Gameplay`, `LevelFlow` ou `Skin` dentro do core do mÃ³dulo.

## ConsequÃªncias

### Positivas

- ownership de domÃ­nio fica explÃ­cito e auditÃ¡vel,
- reduz acoplamento com slices de `Presentation`,
- evita heranÃ§a indevida de premissas de Player/Eater,
- preserva contratos operacionais Ãºteis do mÃ³dulo anterior,
- mantÃ©m pooling, playback context, defaults e settings em trilho canÃ´nico,
- o mÃ³dulo pode nascer standalone e receber integraÃ§Ãµes depois,
- melhora rollout incremental por trilho (`GlobalAudio` e `EntityAudio`).

### Trade-offs

- exige corte de artefatos transicionais,
- exige migraÃ§Ã£o de contratos/documentaÃ§Ã£o,
- exige revalidaÃ§Ã£o por trilho,
- exige hardening real de pontos antes implÃ­citos no legado,
- exige disciplina para nÃ£o recolocar semÃ¢ntica de entidade dentro de `Presentation` ou `Emitter`.

## CritÃ©rio de aderÃªncia

Uma implementaÃ§Ã£o Ã© aderente a este ADR se:

- separa claramente `GlobalAudio` e `EntityAudio`,
- mantÃ©m bootstrap e ownership no `GlobalCompositionRoot`,
- garante que `Presentation` nÃ£o Ã© runtime owner de Ã¡udio,
- mantÃ©m o mÃ³dulo `Audio` standalone,
- nÃ£o cria dependÃªncia estrutural de mÃ³dulos consumidores,
- mantÃ©m `cue` como contrato de comportamento,
- mantÃ©m `purpose` como intenÃ§Ã£o semÃ¢ntica de entidade,
- mantÃ©m `EntityAudioEmitter` mÃ­nimo ou o remove quando nÃ£o agrega valor,
- consome pooling canÃ´nico do projeto,
- suporta `DirectOneShot` e `PooledOneShot`,
- preserva defaults do projeto separados do estado runtime do jogador,
- explicita contrato de BGM, handle e concorrÃªncia,
- evita compat layer/fallback estrutural como soluÃ§Ã£o final,
- nÃ£o depende de bootstrap legado, `Resources.Load` ou singleton paralelo.

## Estado do legado apÃ³s esta decisÃ£o

O sistema legado permanece apenas como referÃªncia funcional temporÃ¡ria para auditoria comparativa.

Ele nÃ£o Ã© fonte de verdade do runtime atual e nÃ£o deve ser promovido como base estrutural do mÃ³dulo canÃ´nico.

## Estado atual do rollout (2026-03-20)

Estado real consolidado antes de integracoes com Navigation/rotas:

- F3 (BGM runtime) esta concluido e validado funcionalmente.
- IAudioBgmService possui implementacao concreta registrada no DI global.
- O runtime de BGM opera em single-channel logico com duas AudioSource internas usadas apenas para crossfade.
- Play, Stop (com fade), StopImmediate, SetPauseDucking, fade-in/fade-out e crossfade estao funcionais.
- Existe harness manual de QA (Modules/Audio/QA/AudioBgmQaSceneHarness.cs) com validacao de setup e cenario basico para F3.

Contrato atual de listener no modulo de Audio:

- O modulo garante host runtime proprio de AudioListener no boot via GlobalCompositionRoot.Audio.
- O listener canonico e global, persistente entre cenas (DontDestroyOnLoad) e independe de camera.
- Essa decisao atende F3/BGM e nao fecha a politica final de listener para spatial/SFX.

Separacao canonica consolidada (source of truth):

- AudioDefaultsAsset = configuracao tecnica inicial/fallback (nao seleciona conteudo).
- IAudioSettingsService = estado runtime mutavel da sessao/jogador.
- Conteudo de BGM/SFX = fornecido por caller/bridge/catalogo/camada acima, nunca por defaults.

Ownership de BGM por contexto (opcional):

- level: LevelDefinitionAsset.BgmCue (owner preferencial).
- navigation/intent: GameNavigationCatalogAsset (slots core e extras).
- route: SceneRouteDefinitionAsset.BgmCue (default estrutural de rota).

Precedencia de resolucao no bridge de integracao:

- level > navigation/intent > route.
- se nada estiver configurado, nao ha troca automatica.

Timing canonico de aplicacao no fluxo de transicao:

- em transicoes macro, `SceneTransitionStartedEvent` e o gatilho principal para resolver/aplicar BGM com a melhor informacao disponivel (ordem: level snapshot -> navigation -> route).
- `SceneTransitionBeforeFadeOutEvent` e o ponto de confirmacao/correcao final com contexto consolidado pos-`LevelPrepare` (ainda antes de `FadeOutStarted`).
- em trocas locais de level sem loading visual, `LevelSwapLocalAppliedEvent` e o gatilho principal e deve manter transicao sonora perceptivel.
- objetivo: quando a imagem voltar, a proxima musica ja deve estar tocando ou em crossfade.
- para evitar ruido de reaplicacao, o bridge deduplica por assinatura no gatilho `before_fade_out`; quando a cue final diverge da inicial, a correcao final e aplicada explicitamente.

Semantica final de aplicacao de BGM:

- mesma cue efetiva da cue atual: continue tocando (no-op, sem replay/restart).
- cue efetiva diferente: aplicar via IAudioBgmService.Play(...) para fade/crossfade.
- cue efetiva nula/ausente: nao forcar mudanca (mantem atual; se nada toca, segue silencio).
- parada de musica: somente por acao explicita (Stop/StopImmediate), nunca por ausencia de cue.

Status de progresso:

- F0/F1/F2/F3: DONE.
- F4 (Global SFX direto): proximo passo natural.
- F5+ permanece fora de escopo desta etapa.
