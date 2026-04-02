# ADR-0028 – Arquitetura Canônica de Audio em NewScripts (GlobalAudio vs EntityAudio)

## Status atual do runtime

- O contrato canônico de Audio continua válido.
- O runtime atual já mostra integrações contextuais com Navigation e LevelFlow para BGM e routing, então a separação é de ownership, não de ausência de sinais cruzados.

## Status

- Estado: **Aceito**
- Data (decisão): 2026-03-20
- Supersedes:
    - versao anterior de ADR-0028 (revisao pre-consolidacao)
    - `ADR-0032-Audio-Arquitetura-GlobalAudio-vs-EntityAudio.md`
- Relacionados:
    - ADR-0030 – ActorSystems como Contrato-Base
    - ADR-0031 – PresentationSystems como Orquestrador de Apresentacao

## Contexto

O sistema de áudio legado possuía capacidades reais de produção, incluindo:

- bootstrap automático,
- BGM global,
- SFX global e espacial,
- pooling de voices,
- áudio por entidade,
- controle de playback por handle,
- settings de volume,
- integrações com gameplay,
- tooling/editor/QA.

Porém, a estrutura do legado não era compatível com o canon de `NewScripts`, por depender de:

- bootstrap próprio do módulo,
- `Resources.Load`,
- singleton estrutural como owner,
- fallback frouxo de DI,
- mistura estrutural entre `Scripts/**` e `NewScripts/**`.

A primeira canonização em `NewScripts` recuperou parte importante do comportamento útil, mas a documentação ficou dividida:

- um ADR mais operacional, com contratos úteis de runtime e conteúdo;
- um ADR posterior mais correto em ownership e fronteiras, porém menos completo nos detalhes operacionais.

Além disso, a primeira tentativa de reconstrução arrastou um problema de direção arquitetural: o módulo de áudio passou a ser pensado já acoplado a consumidores como `Navigation`, `Gameplay`, `Presentation`, `Skin` e outros domínios específicos.

Este ADR unifica os dois contratos anteriores e explicita uma correção adicional:

**o módulo `Audio` deve nascer standalone, com portas próprias e sem dependência estrutural de módulos consumidores.**

## Decisão

Será mantido um único módulo canônico em:

`Assets/_ImmersiveGames/NewScripts/Modules/Audio/**`

A arquitetura canônica do módulo passa a ser explicitamente dividida em **duas trilhas independentes**:

1. `GlobalAudio`
2. `EntityAudio`

Princípio de evolução:

**reaproveitar comportamento e conceito útil do legado, reimplementar o runtime no canon atual.**

### Estado consolidado da rodada
- `Audio` saiu do `GlobalCompositionRoot` antigo e passou a ser instalado/composto por `AudioCompositionDescriptor`, `AudioInstaller` e `AudioRuntimeComposer`.
- `AudioInstaller` registra contratos e configs do módulo.
- `AudioRuntimeComposer` fecha o runtime operacional do módulo.
- `AudioSfxCueAsset` segue usando perfis canônicos de emissão/execução.
- `maxSimultaneousInstances` e `sfxRetriggerCooldownSeconds` continuam ativos no trilho de SFX.
- o runtime/QA legado não é mais a rota documental do fluxo canônico.

## Regras canônicas

### 1. Ownership e bootstrap

A inicialização do sistema de áudio em `NewScripts` é responsabilidade do pipeline modular canônico.

Não entram no canon:

- bootstrap próprio fora do pipeline modular,
- `RuntimeInitializeOnLoadMethod`,
- `Resources.Load`,
- singleton estrutural como owner do sistema,
- compat layer permanente para preservar shape legado.

### 2. O módulo `Audio` é standalone

O módulo `Audio` deve ser autocontido no seu domínio.

Ele pode conhecer apenas:

- seus contratos públicos,
- seus assets base,
- seu runtime,
- sua infra interna,
- a infraestrutura compartilhada canônica permitida pelo projeto.

O módulo `Audio` não deve depender estruturalmente de:

- `Modules/Navigation/**`
- `Modules/Gameplay/**`
- `Modules/Presentation/**`
- `Modules/LevelFlow/**`
- `Modules/Skin/**`
- qualquer outro módulo consumidor específico.

Consequência:

- coleções, catálogos, profiles e regras específicas de domínio ficam fora do core do módulo `Audio`;
- integrações com outros módulos acontecem depois, por helpers, facades, bridges ou adapters, no lado consumidor ou em Interop/**, sem transferir ownership para o módulo de áudio.
- o consumo de contexto vindo de Navigation, LevelFlow ou Gameplay segue permitido quando a origem já está definida e o áudio apenas reage.

### 3. `Presentation` não é runtime owner de áudio

`PresentationSystems` pode:

- orquestrar apresentação,
- traduzir intenção visual/apresentacional para intenção de áudio,
- acionar bridges para o módulo de áudio.

`PresentationSystems` não pode:

- possuir runtime concreto de áudio,
- executar política interna de áudio,
- virar owner de estado interno de áudio,
- resolver estruturalmente contratos de entidade que pertencem a `EntityAudio`.

`Presentation -> Audio` permanece permitido como bridge de tradução/orquestração, nunca como ownership de runtime.

### 4. `GlobalAudio` e `EntityAudio` são trilhas distintas

#### `GlobalAudio`

Responsável por:

- BGM global,
- áudio de menu/UI,
- stingers globais,
- SFX de fluxo macro do jogo,
- ducking global por contexto macro.

Superfícies canônicas alvo:

- `IAudioBgmService`
- `IGlobalAudioService`
- `IAudioSettingsService`

#### `EntityAudio`

Responsável por:

- áudio local de ator/objeto,
- playback por semântica de `purpose`,
- integração opcional com contexto espacial do ator,
- resolução contextual de `purpose -> cue` quando aplicável.

Superfície canônica alvo:

- `IEntityAudioService`

### 5. `cue` define comportamento; `purpose` define intenção semântica

- `cue` define comportamento técnico de playback:
    - conteúdo,
    - modo,
    - policy,
    - concorrência,
    - execução direta vs pooled,
    - spatialidade,
    - routing,
    - overrides.
- `purpose` define intenção semântica para consumidores de entidade:
    - `Movement`,
    - `Impact`,
    - `Attack`,
    - `Death`,
    - etc.

A resolução `purpose -> cue` pertence ao domínio `EntityAudio` ou a contrato explicitamente delegado por ele.

Ela não pertence a `Presentation`.

### 6. BGM e SFX continuam separados

#### BGM

BGM é domínio global do jogo.

Uso esperado:

- startup,
- frontend,
- gameplay,
- transições macro,
- pause via ducking.

O contrato canônico de BGM deve suportar, no mínimo:

- play,
- stop com fade,
- stop imediato,
- pause ducking,
- troca com fade/crossfade quando necessário.

#### SFX

SFX é domínio contextual, com duas naturezas:

- **global/contextual macro** quando pertence ao fluxo do jogo/UI,
- **local/entity** quando pertence a ator/objeto.

Nem todo SFX é de entidade.

### 7. Pooling é infraestrutura compartilhada

O módulo de áudio não cria subsistema próprio de pooling.

O áudio consome a infraestrutura canônica em `Infrastructure/Pooling/**`.

Consequências:

- não há `PoolManager` próprio do módulo,
- não há pool resolvido por `Resources.Load`,
- o ownership do pooling permanece no `GlobalCompositionRoot`,
- a política pooled de SFX usa `PoolDefinitionAsset` via `AudioSfxVoiceProfileAsset`.

### 8. Nem todo SFX é pooled

O módulo suporta canonicamente:

- `DirectOneShot`
- `PooledOneShot`

Uso típico:

- click/hover de UI: geralmente `DirectOneShot`
- tiros, impactos repetitivos, explosões frequentes: geralmente `PooledOneShot`

A escolha do trilho direto vs pooled é parte da policy do `cue`, com override contextual quando o contrato permitir.

### 9. `EntityAudioEmitter` mínimo ou inexistente

`EntityAudioEmitter` pode existir apenas como binding estrutural mínimo de contexto, por exemplo:

- `spatialAnchor`,
- `defaultVolumeScale`,
- `defaultVoiceProfile`,
- ponto de emissão local,
- `defaultCue` apenas quando fizer sentido como conveniência local.

Ele não deve:

- ser fonte canônica de authoring de comportamento,
- carregar fallback estrutural,
- virar owner de resolução semântica,
- mascarar lacuna arquitetural do domínio `EntityAudio`.

Se não agregar valor real ao contrato final, pode ser removido.

Observação pós-F7: a precedência canônica de `EmissionProfile` / `ExecutionProfile` e da intenção espacial deve permanecer concentrada em helper puro de runtime, compartilhado entre `AudioEntitySemanticService` e `AudioGlobalSfxService`, sem mover o ownership do playback para fora de `IGlobalAudioService`.

Estado atual do rollout: o emitter mínimo foi entregue e validado em F7 por harness dedicado (`AudioEntityEmitterQaSceneHarness`), com telemetria explícita de owner efetivo e evidência de auto-stop do handle ainda ativo.

### 10. Sem compat layer e sem fallback estrutural como estratégia final

Não é permitido:

- manter API/shape legado por medo de corte,
- transformar camada de compatibilidade temporária em solução permanente,
- mascarar erro estrutural com fallback silencioso.

Erros estruturais devem ser corrigidos na origem.

### 11. Fronteiras com ActorSystems e PresentationSystems

#### Com ADR-0030 (ActorSystems)

- áudio não pertence ao core de `ActorSystems`,
- no máximo, capabilities pequenas e opcionais de integração estrutural.

#### Com ADR-0031 (PresentationSystems)

- `PresentationSystems` é orquestrador de apresentação,
- bridge `Presentation -> Audio` é permitida,
- ownership de runtime de áudio permanece no módulo `Audio`.

## Estrutura canônica do módulo

A estrutura canônica permanece organizada em:

- `Modules/Audio/Bindings/**`
- `Modules/Audio/Config/**`
- `Modules/Audio/Runtime/**`
- `Modules/Audio/Interop/**`
- `Modules/Audio/Editor/**`
- `Modules/Audio/QA/**`

## Superfície pública canônica

A superfície pública consolidada do módulo contempla:

### Global
- `IAudioBgmService`
- `IGlobalAudioService`
- `IAudioSettingsService`

### Entity / playback
- `IEntityAudioService`
- `IAudioPlaybackHandle`
- `AudioPlaybackContext`

### Conteúdo / config
- `AudioCueAsset` (base abstrata)
- `AudioBgmCueAsset`
- `AudioSfxCueAsset`
- `AudioDefaultsAsset`
- `AudioSfxVoiceProfileAsset`

### Binding estrutural opcional
- `EntityAudioEmitter`

## Modelo de conteúdo

### `AudioCueAsset`

Base abstrata compartilhada entre BGM e SFX.

Concentra apenas campos comuns:

- lista de clips,
- mixer group opcional,
- volume base,
- loop quando aplicável,
- pitch mínimo/máximo,
- randomização simples de volume,
- validação runtime comum.

A base não deve ser criada diretamente como asset final de conteúdo.

### `AudioBgmCueAsset`

Representa música global.

Características:

- sempre global,
- não expõe policy de execução de SFX,
- não expõe espacialização.

### `AudioSfxCueAsset`

Representa efeito sonoro.

Campos canônicos:

- `PlaybackMode` (`Global` / `Spatial`)
- `SpatialBlend`
- `MinDistance`
- `MaxDistance`
- `AudioSfxExecutionMode` (`DirectOneShot` / `PooledOneShot`)
- `VoiceProfileOverride`
- `MaxSimultaneousInstances`
- `SfxRetriggerCooldownSeconds` quando houver policy de anti-spam por cue

Evolucao incremental (Etapa 1 + Etapa 2):

- `AudioSfxCueAsset` passa a suportar referencias opcionais para:
  - `AudioSfxEmissionProfileAsset` (eixo de emissao);
  - `AudioSfxExecutionProfileAsset` (eixo de execucao).
- Campos legados do cue permanecem temporariamente para compatibilidade de migracao (sem big bang).
- Runtime de SFX opera em dual-read:
  - tenta resolver pelo shape novo (`context/profile`);
  - sem dados novos, cai para resolucao legada do cue.
- Semantica funcional de F4/F5 permanece inalterada nesta etapa.

Evolucao incremental (Etapa 3):

- conjunto canonico minimo de cues foi migrado para usar `EmissionProfile` + `ExecutionProfile`:
  - `AudioSfxCue_Global.asset` (direct/global 2D);
  - `AudioSfxCue_Spetial.asset` (direct/spatial 3D);
  - `AudioSfxCue_GlobalPooled.asset` (pooled/global 2D);
  - `AudioSfxCue_SpetialPooled.asset` (pooled/spatial 3D).
- assets seed base de execucao permanecem validos e, quando necessario para manter coerencia de profile pooled, podem existir derivados minimos por contexto (ex.: pooled spatial).
- campos legados permanecem ativos como trilho de compatibilidade e rollback.
- runtime continua em dual-read, com observabilidade explicita de origem:
  - emissao: `context | emission_profile | legacy_cue`;
  - execucao: `context | execution_profile | legacy_cue`;
  - voice profile pooled: `context | execution_profile | legacy_cue`.

## Defaults do projeto vs estado do jogador

### `AudioDefaultsAsset`

Define defaults do projeto:

- volumes padrão,
- multiplicadores por categoria,
- fade padrão de BGM,
- mixer routing base,
- parâmetros técnicos iniciais do módulo.

Limites explícitos de responsabilidade:

- `AudioDefaultsAsset` não seleciona conteúdo de áudio.
- `AudioDefaultsAsset` não escolhe `AudioBgmCueAsset`.
- `AudioDefaultsAsset` não escolhe `AudioSfxCueAsset`.
- `AudioDefaultsAsset` não escolhe pool/perfil de voice.
- conteúdo de BGM/SFX deve vir de caller/bridge/catálogo/camada acima.

### `IAudioSettingsService`

Representa estado runtime do jogador/sessão.

Consequências:

- UI não grava diretamente no asset de defaults,
- defaults não representam o estado atual da sessão,
- persistência de settings do jogador é preocupação do serviço/runtime, não do asset de defaults,
- defaults existem como seed/fallback canônico quando ainda não há customização de jogador.

## Playback context

`AudioPlaybackContext` é o contexto canônico de playback.

Ele transporta, no mínimo:

- intenção `global` / `spatial`,
- posição ou target de follow,
- `volumeScale`,
- `reason`,
- profile contextual opcional.

Objetivo:

- evitar explosão de overloads frágeis,
- permitir override explícito sem multiplicar assinaturas públicas.

## Voice profile

`AudioSfxVoiceProfileAsset` representa a família/configuração da voice de SFX.

Ele liga o domínio de áudio ao pooling canônico, definindo:

- `PooledVoicePoolDefinition` (`PoolDefinitionAsset`),
- política de fallback direto quando aplicável,
- tuning por família de uso.

Estado canonico de authoring (F5):

- pools de audio sao definidos por `PoolDefinitionAsset` e referenciados no voice profile;
- existem dois trilhos canonicos de pool:
  - global/2D (`AudioSfxPoolDefinition_Global2D.asset` -> `AudioSfxVoiceGlobal.prefab`);
  - spatial/3D (`AudioSfxPoolDefinition_Spatial3D.asset` -> `AudioSfxVoiceSpatial.prefab`);
- o prefab do pool representa a voice runtime (carrier), nao o conteudo final do som;
- o conteudo continua no `AudioSfxCueAsset` (clip/volume/pitch/routing/playback policy).

## Runtime canônico

### `IAudioBgmService`

Responsável por música global.

Contrato mínimo esperado:

- `Play(...)`
- `Stop(...)`
- `StopImmediate(...)`
- `SetPauseDucking(...)`
- troca com fade/crossfade quando aplicável pelo contrato final do serviço

### `IGlobalAudioService`

Responsável por áudio global não-BGM:

- UI,
- stingers,
- SFX macro do jogo,
- playback global de cues fora de entidade.

Ele deve resolver:

- execução direta vs pooled,
- playback global vs spatial macro quando aplicável,
- profile efetivo,
- anti-spam simples,
- concorrência por cue,
- handle real ou no-op conforme o modo de execução.

Estado atual (F5):

- `IGlobalAudioService` já opera com trilha direta e trilha pooled.
- Em cues `PooledOneShot`, o runtime consome `IPoolService` + `PoolDefinitionAsset` via `AudioSfxVoiceProfileAsset`.
- Sem profile/pool válido, aplica fallback para trilha direta apenas quando `allowDirectFallback` estiver habilitado.
- O authoring canonico de pooled audio usa prefabs dedicados de audio (`AudioSfxVoiceGlobal.prefab` e `AudioSfxVoiceSpatial.prefab`), sem depender de prefab generico de teste.

Saneamento arquitetural pre-F6/F7 (A1 + B1):

- policy interna de SFX extraida do fluxo monolitico para componentes dedicados:
  - `AudioSfxDirectPolicyEngine` (retrigger/cooldown/limit no trilho direto);
  - `AudioSfxPooledPolicyEngine` (budget/fallback/bloqueio no trilho pooled).
- `AudioGlobalSfxService` permanece como orquestrador, com menor concentracao de decisao no mesmo metodo.
- sem redesign completo de authoring/asset contract nesta etapa (Opção C permanece fora de escopo).

### `IEntityAudioService`

Responsável por áudio local de ator/objeto e por resolução semântica de `purpose` para playback efetivo.

Ele deve ser capaz de:

- receber intenção semântica ou cue já resolvida, conforme contrato adotado,
- resolver playback local/global quando aplicável,
- integrar contexto espacial do ator,
- aplicar policy de cue/profile sem depender de `Presentation`.

### `IAudioPlaybackHandle`

Representa instância runtime reproduzida pelo sistema.

Contrato esperado:

- `IsValid`
- `IsPlaying`
- `Stop(float fadeOutSeconds = 0f)`

Regras canônicas:

- `IsPlaying` deve representar playback real de forma consistente, e não apenas "objeto ativo em hierarquia".
- se `Stop(fadeOutSeconds)` existir no contrato, a implementação deve respeitar a semântica prometida ou documentar explicitamente o limite do handle retornado.

### `AudioSfxVoice`

Voice dedicada e reutilizável de SFX.

Responsabilidades:

- ownar `AudioSource`,
- aplicar playback resolvido,
- acompanhar `followTarget` quando aplicável,
- devolver-se ao pool ou destruir-se ao fim do playback,
- responder a `Stop()` com ou sem fade, conforme o contrato do handle/voice.

## Ordem canônica de resolução de `VoiceProfile`

Para playback pooled, a resolução efetiva é:

1. `AudioSfxCueAsset.VoiceProfileOverride`
2. profile contextual do playback (`AudioPlaybackContext.VoiceProfile` ou binding estrutural mínimo equivalente)
3. profile explícito da camada chamadora/bridge/catálogo quando o fluxo exigir

Se nenhum profile for resolvido explicitamente, o runtime deve degradar para trilho não-pooled (quando permitido) em vez de depender de defaults de conteúdo.

## Anti-spam e concorrência

O módulo inclui proteção mínima via:

- cooldown curto por cue,
- limite simples de instâncias simultâneas por cue,
- drop simples quando o limite é atingido.

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

esses limites devem ser **enforcement real** do runtime, e não apenas campos declarativos sem efeito prático.

Voice stealing sofisticado fica fora do escopo atual.

## Política de BGM por contexto

### Regra canônica
BGM é reproduzido por cue explícita recebida por chamada de serviço.

O módulo `Audio` não escolhe música por conta própria a partir de defaults.

Mapeamento de contexto (`startup`, `frontend`, `gameplay`, `route`, `level`) pertence à camada chamadora/bridge/catálogo acima do core de `Modules/Audio/**`.

### Pause
Aplica ducking de BGM em vez de pausar completamente.

## Mixer e routing

O uso do `AudioMixer` da Unity é compatível com este ADR e faz parte do desenho do módulo, mas o contrato detalhado de mixer ainda não está completamente fechado neste momento.

Este ADR já assume:

- `mixer group` opcional em cue,
- routing base em `AudioDefaultsAsset`,
- possibilidade de ducking global por pause,
- integração futura entre `IAudioSettingsService` e parâmetros expostos do mixer.

Ainda assim, o detalhamento completo de:

- mixer asset canônico,
- grupos obrigatórios,
- parâmetros expostos obrigatórios,
- regra final de routing,
- política final de aplicação de volume do usuário,
- implementação final de ducking,

fica registrado como refinamento complementar do contrato deste ADR.

## Integrações opcionais

### Gameplay
Integrações com gameplay são permitidas e esperadas, mas devem consumir contratos canônicos do módulo.

### Navigation / LevelFlow / rotas
Uso de BGM por rota, cue por contexto de navegação ou catálogos por route/level é permitido como integração futura.

Esses catálogos e bridges pertencem ao módulo consumidor ou à camada de integração, não ao core de `Modules/Audio/**`.

### Skin audio
Integração com skin/config temática é opcional.

Ela pode existir como camada de resolução de conteúdo, mas não deve definir ownership do módulo nem contaminar o contrato central de `GlobalAudio` / `EntityAudio`.

## Editor UX e QA

O módulo pode manter:

- custom editors,
- preview/harness de QA,
- cues QA dedicadas,
- tooling de inspeção e diagnóstico.

Estado atual de QA de Audio (F3/F4/F5/F6):

- `AudioBgmQaSceneHarness` cobre o trilho F3 (BGM global).
- `AudioSfxDirectQaSceneHarness` cobre somente trilho direto de SFX (F4: 2D/3D + cooldown/limit/stop basico).
- `AudioSfxPooledQaSceneHarness` cobre somente trilho pooled de SFX (F5: restart_existing, budget, fallback e sequence reuse).
- `AudioEntitySemanticQaSceneHarness` cobre o trilho semantico standalone (F6: purpose -> cue/perfis).
- `AudioEntityEmitterQaSceneHarness` cobre a F7 validada (`purpose`/`cue` com emitter, trilho sem emitter e auto-stop do handle).
- probes forcados de diagnostico ficam restritos ao harness pooled.
- `AudioSfxQaSceneHarness` permanece apenas como shim legado de migracao, sem concentrar todos os testes.
- trilho manual oficial de cena: `MenuScene`, via rig canonico:
  - `Modules/Audio/QA/Prefabs/AudioQaMenuSceneRig.prefab`.

Esses artefatos são suporte operacional do módulo e não alteram o contrato arquitetural central.

## Fora de escopo

Ficam fora do escopo imediato:

- adaptive music,
- playlists complexas,
- stingers/camadas avançadas além do contrato básico,
- priority / voice stealing sofisticado,
- preview/tooling legado como contrato obrigatório,
- sistema completo de skin audio como eixo central do módulo,
- catálogos específicos de `Navigation`, `Gameplay`, `LevelFlow` ou `Skin` dentro do core do módulo.

## Consequências

### Positivas

- ownership de domínio fica explícito e auditável,
- reduz acoplamento com slices de `Presentation`,
- evita herança indevida de premissas de Player/Eater,
- preserva contratos operacionais úteis do módulo anterior,
- mantém pooling, playback context, defaults e settings em trilho canônico,
- o módulo pode nascer standalone e receber integrações depois,
- melhora rollout incremental por trilho (`GlobalAudio` e `EntityAudio`).

### Trade-offs

- exige corte de artefatos transicionais,
- exige migração de contratos/documentação,
- exige revalidação por trilho,
- exige hardening real de pontos antes implícitos no legado,
- exige disciplina para não recolocar semântica de entidade dentro de `Presentation` ou `Emitter`.

## Critério de aderência

Uma implementação é aderente a este ADR se:

- separa claramente `GlobalAudio` e `EntityAudio`,
- mantém bootstrap e ownership no `GlobalCompositionRoot`,
- garante que `Presentation` não é runtime owner de áudio,
- mantém o módulo `Audio` standalone,
- não cria dependência estrutural de módulos consumidores,
- mantém `cue` como contrato de comportamento,
- mantém `purpose` como intenção semântica de entidade,
- mantém `EntityAudioEmitter` mínimo ou o remove quando não agrega valor,
- consome pooling canônico do projeto,
- suporta `DirectOneShot` e `PooledOneShot`,
- preserva defaults do projeto separados do estado runtime do jogador,
- explicita contrato de BGM, handle e concorrência,
- evita compat layer/fallback estrutural como solução final,
- não depende de bootstrap legado, `Resources.Load` ou singleton paralelo.

## Estado do legado após esta decisão

O sistema legado permanece apenas como referência funcional temporária para auditoria comparativa.

Ele não é fonte de verdade do runtime atual e não deve ser promovido como base estrutural do módulo canônico.

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

## Addendum de estado validado (2026-03-21)

Este addendum consolida o baseline fechado antes de F7/F8:

- F3 (BGM runtime): fechado e validado.
- F4 (Global SFX direct): fechado e validado.
- F5 (Global SFX pooled one-shot): fechado e validado.
- Hardening final de F5 concluido:
  - pooled one-shot proibindo loop em runtime (`loopPolicy='forced_off_for_pooled_oneshot'`);
  - probes de QA pooled fechados para `restart_existing`, `block_budget`, `fallback_direct` e `sequence reuse`.

Shape canonico de SFX em uso real minimo:

- `Cue` (conteudo base);
- `EmissionProfile` (eixo de emissao);
- `ExecutionProfile` (eixo de execucao);
- dual-read mantido para compatibilidade gradual com campos legados.

Estado ativo a partir deste ponto:

- F6 (EntityAudio semantico standalone) passa a ser trilho ativo.
- F7 (EntityAudioEmitter minimo de cena) foi validada incrementalmente, ainda sem abrir integrações F8+.
- Pós-F7 imediato: saneamento pequeno para unificar precedência de profiles/spatial sem alterar contratos de runtime.



## Saneamento pós-F7 (P1 / Etapa 3)

Diretriz incremental aplicada apos a validacao da F7 e da unificacao canônica de precedence/spatial:

- `EntityAudioEmitter` permanece apenas como binding estrutural minimo;
- cleanup interno do emitter deve consolidar preparo de playback, sem alterar contrato publico;
- semantica (`purpose -> cue/perfis`) continua em `IEntityAudioService`;
- qualquer integracao com consumidores continua fora do core de `Modules/Audio/**` e nao entra nesta etapa.
