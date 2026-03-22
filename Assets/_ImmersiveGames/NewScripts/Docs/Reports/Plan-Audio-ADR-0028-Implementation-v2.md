# Plan — Audio ADR-0028 Implementation

## 1. Objetivo

Implementar o ADR-0028 de áudio em `NewScripts` em fases pequenas, entregáveis e testáveis, evitando:

- regressão funcional silenciosa,
- migração big-bang,
- simplificação excessiva do módulo,
- novo acoplamento estrutural com módulos consumidores,
- perda de intenção arquitetural.

Objetivo principal:

**reconstruir o módulo de áudio como core standalone, modular e evolutivo, preservando capacidades úteis do legado sem importar sua dívida estrutural.**

---

## 2. Guardrails operacionais

### Guardrails de execução

- [ ] Não abrir a Unity durante a tarefa do Codex.
- [ ] Não executar player build / build da Unity.
- [ ] Pode usar `batch mode`, `MSBuild` e `dotnet build` para validações técnicas fora do build de player, quando explicitamente necessário.
- [ ] Não transformar validação automática em substituto da validação manual em Play Mode.
- [ ] Não fazer migração big-bang.
- [ ] Não migrar `GlobalAudio` e `EntityAudio` no mesmo passo se a fase não pedir isso explicitamente.
- [ ] Não remover comportamento antigo antes de existir substituto observável.

### Guardrails arquiteturais

- [ ] `Modules/Audio/**` deve permanecer standalone.
- [ ] O core do módulo não pode depender de `Navigation`, `Gameplay`, `Presentation`, `LevelFlow`, `Skin` ou outros consumidores.
- [ ] Integrações com outros módulos devem entrar depois, por `Interop/**`, helpers, facades, bridges ou adapters.
- [ ] `Presentation` nunca vira runtime owner de áudio.
- [ ] `cue` continua sendo contrato técnico de playback.
- [ ] `purpose` continua sendo intenção semântica de entidade.
- [ ] `EntityAudioEmitter` deve permanecer mínimo ou ser removido.
- [ ] Pooling continua sendo infraestrutura compartilhada.

### Regras de rastreamento

Status por fase:

- `NOT STARTED`
- `IN PROGRESS`
- `BLOCKED`
- `DONE`

Cada fase só avança quando:

- entregáveis estão completos,
- teste manual está definido,
- risco residual está conhecido,
- regressões abertas da própria fase foram tratadas.

---

## 3. Matriz de paridade

| Capacidade | Legado tinha? | ADR exige? | Fase | Teste manual principal | Status |
|---|---:|---:|---|---|---|
| Bootstrap único via owner canônico | Sim | Sim | F2 | verificar logs + owner único | NOT STARTED |
| BGM global | Sim | Sim | F3 | frontend/gameplay/pause | NOT STARTED |
| Fade/crossfade de BGM | Sim | Sim | F3 | trocar trilhas e observar transição | NOT STARTED |
| Ducking em pause | Sim | Sim | F3 | pausar e observar ducking | NOT STARTED |
| SFX global direto | Sim | Sim | F4 | UI/stinger/global one-shot | NOT STARTED |
| SFX pooled | Sim | Sim | F5 | burst de tiros/impactos | NOT STARTED |
| AudioPlaybackContext | Parcial | Sim | F4 | overrides de reason/volume/spatial | NOT STARTED |
| AudioDefaultsAsset | Parcial | Sim | F2 | defaults resolvidos e usados | NOT STARTED |
| AudioSfxVoiceProfileAsset | Parcial | Sim | F5 | resolução de voice profile | NOT STARTED |
| IAudioPlaybackHandle estável | Sim | Sim | F5 | IsPlaying/Stop/fadeOut | NOT STARTED |
| Anti-spam/cooldown por cue | Parcial | Sim | F5 | spam de evento repetitivo | NOT STARTED |
| Limite de instâncias por cue com enforcement real | Parcial | Sim | F5 | lotação de voices | NOT STARTED |
| EntityAudio semântico (`purpose`) | Parcial | Sim | F6 | attack/impact/death local | NOT STARTED |
| EntityAudioEmitter mínimo | Sim | Sim | F7 | entidade com/sem emitter + auto-stop do handle | DONE |
| Defaults do projeto separados do estado runtime do jogador | Parcial | Sim | F2 | UI/settings não alteram defaults | NOT STARTED |
| Mixer/routing base | Parcial | Sim | F2/F3/F4 | grupos/params básicos | NOT STARTED |
| Gameplay integration por bridge | Sim | Futuro | F8 | tiro/hit/death/FX | NOT STARTED |
| Navigation BGM por rota | Não canônico | Futuro | F8+ | BGM por route bridge | NOT STARTED |
| Skin audio opcional | Sim | Futuro | F8+ | resolução opcional de conteúdo | NOT STARTED |
| Tooling/editor/QA | Sim | Sim | F9 | preview/harness/diagnóstico | NOT STARTED |
| Cleanup final de acoplamentos | Não | Sim | F10 | smoke completo pós-limpeza | NOT STARTED |

---

## 4. Fases incrementais

## F0 — Baseline e matriz de paridade

- **Status:** `NOT STARTED`
- **Objetivo:** congelar a base de paridade e os riscos da segunda tentativa.

### Escopo

- auditoria final das capacidades exigidas;
- checklist do que não pode se perder;
- definição da ordem de rollout.

### Entregáveis

- [ ] este plano preenchido;
- [ ] matriz de paridade validada;
- [ ] lista de riscos conhecidos anexada ao plano.

### Critérios de pronto

- [ ] capacidades mínimas não-perdíveis estão listadas;
- [ ] cada capacidade está atribuída a uma fase;
- [ ] escopos fora do momento atual estão explícitos.

### Testes manuais

- revisão arquitetural do plano;
- validação humana da ordem de implementação.

### Riscos

- esquecer uma capacidade do legado e só perceber muito tarde;
- confundir core standalone com integração de consumidores.

### Não entra nesta fase

- implementação de código;
- alteração de runtime.

---

## F1 — Contratos e estrutura do módulo

- **Status:** `NOT STARTED`
- **Objetivo:** congelar a superfície pública e a organização do módulo antes de mexer em comportamento.

### Escopo

- contratos públicos;
- estrutura de pastas/namespaces;
- assets base;
- sem playback real ainda.

### Entregáveis

- [ ] estrutura canônica de pastas em `Modules/Audio/**`;
- [ ] contratos:
  - `IAudioBgmService`
  - `IGlobalAudioService`
  - `IEntityAudioService`
  - `IAudioSettingsService`
  - `IAudioPlaybackHandle`
  - `AudioPlaybackContext`
- [ ] assets base:
  - `AudioCueAsset`
  - `AudioBgmCueAsset`
  - `AudioSfxCueAsset`
  - `AudioDefaultsAsset`
  - `AudioSfxVoiceProfileAsset`

### Critérios de pronto

- [ ] contratos não dependem de módulos consumidores;
- [ ] `Presentation` não aparece como owner em nenhum contrato;
- [ ] `EntityAudioEmitter` ainda não é requisito estrutural do core.

### Testes manuais

- revisão de namespaces;
- revisão de dependências;
- leitura de contratos.

### Riscos

- assinar contratos cedo demais com shape ruim;
- deixar contratos já contaminados por consumer-specific logic.

### Não entra nesta fase

- playback real;
- integração com gameplay/navigation/presentation/skin.

---

## F2 — Bootstrap canônico + defaults + settings + mixer/routing base

- **Status:** `NOT STARTED`
- **Objetivo:** colocar o módulo sob owner único e fechar os blocos base de configuração do core standalone.

### Escopo

- bootstrap via `GlobalCompositionRoot`;
- `AudioDefaultsAsset`;
- `IAudioSettingsService`;
- resolução base de mixer/routing;
- observabilidade do boot.

### Entregáveis

- [ ] owner único do módulo no `GlobalCompositionRoot`;
- [ ] `AudioDefaultsAsset` funcional como defaults do projeto;
- [ ] `IAudioSettingsService` separado dos defaults;
- [ ] contrato base de mixer/routing inicial no core;
- [ ] logs canônicos de bootstrap do módulo.

### Critérios de pronto

- [ ] defaults e settings não estão misturados;
- [ ] o módulo não depende de bootstrap legado;
- [ ] existe base explícita para mixer/routing sem acoplar consumers;
- [ ] nenhum módulo consumidor foi integrado ainda.

### Testes manuais

- verificar logs de bootstrap;
- validar owner único;
- validar defaults/settings resolvidos;
- validar shape de mixer/routing base.

### Riscos

- misturar settings runtime do jogador com asset de defaults;
- fechar routing cedo demais com dependência de feature específica.

### Não entra nesta fase

- playback de BGM/SFX;
- pause ducking em produção;
- catálogos por navigation/gameplay.

---

## F3 — `GlobalAudio` trilha 1: BGM

- **Status:** `NOT STARTED`
- **Objetivo:** fechar a trilha global mais controlada primeiro.

### Escopo

- `IAudioBgmService`;
- play;
- stop;
- stop immediate;
- pause ducking;
- fade/crossfade;
- contexto macro: startup/frontend/gameplay.

### Entregáveis

- [ ] implementação canônica de `IAudioBgmService`;
- [ ] resolução de `AudioBgmCueAsset`;
- [ ] fade/crossfade previsível;
- [ ] ducking de pause definido no trilho BGM.

### Critérios de pronto

- [ ] BGM funciona independente de SFX;
- [ ] ducking não depende de gambiarra local;
- [ ] nenhuma dependência de `Navigation`/`Presentation` entrou no core.

### Testes manuais

- frontend toca BGM;
- gameplay toca BGM;
- troca de contexto troca trilha;
- pause aplica ducking;
- stop imediato e stop com fade funcionam.

### Riscos

- o contrato de mixer detalhado ainda pode exigir ajuste posterior;
- crossfade mal fechado pode contaminar o runtime de BGM.

### Não entra nesta fase

- SFX;
- pooled voices;
- BGM por rota via `Navigation`.

---

## F4 — `GlobalAudio` trilha 2: SFX direto (`DirectOneShot`)

- **Status:** `NOT STARTED`
- **Objetivo:** fechar o caminho simples de SFX global antes do pooling.

### Escopo

- `IGlobalAudioService`;
- `AudioSfxCueAsset`;
- playback global direto;
- playback spatial macro básico quando fizer sentido;
- `AudioPlaybackContext`;
- anti-spam simples por cue.

### Entregáveis

- [ ] `DirectOneShot` funcional para UI/stinger/macro SFX;
- [ ] `AudioPlaybackContext` aplicado de forma consistente;
- [ ] base simples de anti-spam por cue.

### Critérios de pronto

- [ ] UI e macro SFX tocam sem pooling;
- [ ] `cue` controla comportamento técnico;
- [ ] trilha direta existe antes da pooled.

### Testes manuais

- click/hover/UI;
- stinger global;
- SFX macro com `reason`;
- routing básico por cue/default.

### Riscos

- esconder no-op handles demais e perder capacidade de controle;
- tentar antecipar voice system sem necessidade.

### Não entra nesta fase

- pooling;
- entity audio;
- integrações por módulo consumidor.

---

## F5 — `GlobalAudio` trilha 3: pooled voices

- **Status:** `NOT STARTED`
- **Objetivo:** introduzir pooling/voices só depois que a trilha direta estiver estável.

### Escopo

- `AudioSfxVoice`;
- `AudioSfxVoiceProfileAsset`;
- integração com pooling canônico;
- `PooledOneShot`;
- enforcement real de concorrência;
- hardening de handle.

### Entregáveis

- [ ] `PooledOneShot` funcional;
- [ ] resolução canônica de voice profile;
- [ ] enforcement real de cooldown por cue;
- [ ] enforcement real de `MaxSimultaneousInstances`;
- [ ] correção semântica de `IsPlaying` e `Stop(fadeOutSeconds)`.

### Critérios de pronto

- [ ] pooling não depende do legado;
- [ ] `maxSoundInstances` deixa de ser decorativo;
- [ ] handle deixa de ser semanticamente fraco.

### Testes manuais

- burst de tiros/impactos repetitivos;
- lotação de voices;
- `Stop(fadeOut)` em voice pooled;
- `IsPlaying` acompanhando playback real.

### Riscos

- acoplar demais a policy de pooling ao core de SFX;
- trazer voice stealing sofisticado cedo demais.

### Não entra nesta fase

- entity audio semântico;
- integrações de gameplay específicas.

---

## F6 — `EntityAudio` trilha 1: contrato semântico

- **Status:** `NOT STARTED`
- **Objetivo:** separar definitivamente `EntityAudio` de `Presentation` e de slices concretos.

### Escopo

- `IEntityAudioService`;
- conceito `purpose -> cue`;
- contexto espacial do ator;
- chamada local por entidade;
- sem depender de bridge de presentation.

### Entregáveis

- [ ] `IEntityAudioService` funcional;
- [ ] contrato explícito de `purpose`;
- [ ] resolução `purpose -> cue` no domínio correto;
- [ ] playback local usando `AudioPlaybackContext`.

### Critérios de pronto

- [ ] Player/Eater deixam de ser contrato implícito;
- [ ] entidade toca áudio por semântica;
- [ ] `Presentation` não é owner do runtime.

### Testes manuais

- ator simples aciona `Movement`, `Impact`, `Attack`;
- spatialização acompanha contexto do ator;
- sem dependência estrutural de presentation.

### Riscos

- semântica de `purpose` mal definida virar novo acoplamento implícito;
- começar a depender de catálogos de gameplay cedo demais.

### Fechamento observado

- `Validate Setup` confirmou `serviceResolved=True`, `emitterResolved=True` e telemetria coerente de `effectiveOwner`;
- `PlayPurposeViaEmitterAndAutoStop` validou `AutoStopBeforeStop` + `StopLastHandle` + `completion='stop_immediate'`;
- `PlayCueViaEmitterAndAutoStop` validou o mesmo fluxo também para cue explícito pooled/direto.

### Saneamento pós-F7 (P1 / Etapa 2) — fechamento observado

- regra canônica de `EmissionProfile` / `ExecutionProfile` e de intenção espacial foi unificada em helper puro de runtime;
- `AudioEntitySemanticService` passou a consumir a mesma regra canônica sem assumir ownership de playback;
- `AudioGlobalSfxService` permaneceu owner da resolução final e do runtime observável.

### Próximo saneamento pós-F7 (P1 / Etapa 3)

- consolidar a implementação interna do `EntityAudioEmitter` em um único trilho de preparação;
- preservar o contrato público atual do emitter;
- manter o emitter estritamente como binding estrutural mínimo, sem semântica e sem catálogo.

### Não entra nesta fase

- bridges específicas de gameplay;
- skin audio;
- navegação por rota.

---

## F7 — `EntityAudio` trilha 2: `EntityAudioEmitter` mínimo

- **Status:** `DONE`
- **Objetivo:** reduzir `EntityAudioEmitter` ao mínimo estrutural ou provar sua remoção.

### Escopo

- `EntityAudioEmitter`;
- contexto mínimo:
  - `spatialAnchor`
  - `defaultVolumeScale`
  - `defaultVoiceProfile`
  - conveniências locais estritamente justificadas.

### Entregáveis

- [x] `EntityAudioEmitter` mínimo entregue;
- [x] nenhum fallback estrutural escondido;
- [x] nenhuma responsabilidade semântica no emitter;
- [x] harness dedicado `AudioEntityEmitterQaSceneHarness` criado para F7;
- [x] evidência final de stop/go rerrodada com auto-stop ativo.

### Critérios de pronto

- [x] emitter não é authoring central;
- [x] emitter não resolve `purpose`;
- [x] emitter não mascara lacuna de arquitetura;
- [x] evidência manual final de `StopLastHandle` em handle ativo foi reexecutada no Play Mode.

### Testes manuais

- `Validate Setup` resolve DI global e loga `configuredOwner`, `effectiveOwner` e `ownerSource`;
- `Play Purpose Via Emitter` continua tocando com `handleValid=True`;
- `Play Cue Via Emitter` continua tocando com `handleValid=True`;
- `Play Purpose Without Emitter` continua válido no trilho standalone;
- `Play Cue Via Emitter And Auto Stop` e `Play Purpose Via Emitter And Auto Stop` já foram rerrodados com `completion='stop_immediate'`.

### Riscos

- convenience API crescer e virar novo owner implícito;
- manter defaults demais no emitter;
- telemetria do harness ficar ambígua sobre owner efetivo (corrigido nesta etapa, requer rerun de evidência).

### Não entra nesta fase

- bridges por gameplay;
- catálogos por feature.

---

## F8 — Integrações opcionais por módulo consumidor

- **Status:** `NOT STARTED`
- **Objetivo:** integrar o módulo com consumidores sem perder standalone do core.

### Escopo

- `Presentation -> Audio` como bridge opcional;
- integrações de gameplay;
- integrações de navigation/route para BGM;
- skin audio opcional;
- helpers/facades/adapters por consumidor.

### Entregáveis

- [ ] bridges explícitas e pequenas;
- [ ] gameplay usa apenas contratos canônicos do módulo;
- [ ] navigation pode mapear rota -> cue/BGM fora do core;
- [ ] skin audio tratado como resolução opcional de conteúdo.

### Critérios de pronto

- [ ] consumidor depende do módulo, não o contrário;
- [ ] `Audio` continua sem depender de consumidores;
- [ ] não há catálogos consumer-specific dentro do core.

### Testes manuais

- tiro/hit/death/FX via bridge;
- BGM por route/contexto via integração;
- skin audio funciona quando presente e não quebra quando ausente.

### Riscos

- mover lógica de integração para dentro do core por conveniência;
- poluir `Interop/**` com ownership indevido.

### Não entra nesta fase

- novo contrato do core;
- refactor estrutural do módulo de áudio.

---

## F9 — Tooling, QA e hardening

- **Status:** `NOT STARTED`
- **Objetivo:** restaurar observabilidade e validação manual sem contaminar runtime de produção.

### Escopo

- tooling editor;
- preview de cues;
- harness de QA;
- diagnósticos;
- guards/observabilidade.

### Entregáveis

- [ ] tooling/editor separado do runtime de produção;
- [ ] harness de QA por trilha;
- [ ] logs canônicos por trilha;
- [ ] guards explícitos para dependências obrigatórias.

### Critérios de pronto

- [ ] existe forma de validar manualmente cada trilha;
- [ ] tooling não é parte do ownership do módulo;
- [ ] nada depende de `DevQA` legado.

### Testes manuais

- preview de cue;
- teste de burst;
- diagnóstico de voice/profile/context.

### Riscos

- tooling voltar a virar muleta estrutural;
- QA acoplar demais no runtime de produção.

### Não entra nesta fase

- novos contratos centrais;
- grandes integrações de domínio.

---

## F10 — Cleanup final e fechamento do rollout

- **Status:** `NOT STARTED`
- **Objetivo:** remover resíduos e fechar docs/ownership depois que as trilhas estiverem estáveis.

### Escopo

- limpeza de shims;
- remoção de dependências indevidas;
- atualização de docs/canon/index;
- smoke final completo.

### Entregáveis

- [ ] cleanup final do módulo;
- [ ] atualização de `Canon-Index`;
- [ ] atualização de docs do módulo;
- [ ] changelog do rollout.

### Critérios de pronto

- [ ] `GlobalAudio` e `EntityAudio` estão independentes;
- [ ] `Presentation` é só bridge;
- [ ] bootstrap só no `GlobalCompositionRoot`;
- [ ] nenhuma dependência estrutural do legado restante;
- [ ] o módulo fecha o ADR, não apenas “funciona”.

### Testes manuais

- smoke final completo:
  - boot
  - frontend BGM/UI
  - gameplay BGM
  - SFX direto
  - SFX pooled
  - entity audio
  - pause ducking
  - settings
  - integrações opcionais

### Riscos

- deixar limpeza para tarde demais e carregar resíduo entre fases;
- declarar done sem fechar docs/canon.

### Não entra nesta fase

- nova feature grande;
- redesign do ADR.

---

## 5. Ordem recomendada

Ordem recomendada de implementação:

1. `F0 — Baseline e matriz de paridade`
2. `F1 — Contratos e estrutura do módulo`
3. `F2 — Bootstrap + defaults + settings + mixer/routing base`
4. `F3 — BGM`
5. `F4 — Global SFX direto`
6. `F5 — Pooled voices`
7. `F6 — EntityAudio semântico`
8. `F7 — EntityAudioEmitter mínimo`
9. `F8 — Integrações opcionais por consumidor`
10. `F9 — Tooling/QA/hardening`
11. `F10 — Cleanup final`

Justificativa:

- primeiro congela contratos e owner;
- depois fecha `GlobalAudio` standalone;
- depois fecha `EntityAudio` standalone;
- só então entram integrações específicas;
- por último vem tooling e cleanup final.

---

## 6. Regras de stop/go

Só avança de fase se:

- [ ] entregáveis da fase anterior foram concluídos;
- [ ] existe teste manual objetivo para a fase;
- [ ] não há regressão aberta da própria fase sem plano explícito;
- [ ] o acoplamento removido já tem substituto observável.

Se falhar:

- [ ] não compensar na fase seguinte;
- [ ] corrigir na própria fase;
- [ ] registrar risco ou bloqueio no plano.

---

## 7. Primeira execução recomendada (pacote inicial)

### Pacote inicial sugerido

#### Pacote A — fundação segura
- `F0`
- `F1`
- `F2`

### Motivo

Esse pacote:

- cria base rastreável;
- congela contratos;
- fecha owner do módulo;
- separa defaults de settings;
- prepara o terreno para mixer/routing base;
- evita voltar a tocar playback real cedo demais.

### Critério para sair do Pacote A

- [ ] matriz de paridade revisada;
- [ ] contratos aprovados;
- [ ] owner/bootstrap aprovados;
- [ ] defaults/settings aprovados;
- [ ] nenhum consumer-specific coupling entrou no core.

### Próximo pacote após A

#### Pacote B — `GlobalAudio` standalone
- `F3`
- `F4`
- `F5`

#### Pacote C — `EntityAudio` standalone
- `F6`
- `F7`

#### Pacote D — integrações e fechamento
- `F8`
- `F9`
- `F10`

---

## 8. Anotações de acompanhamento

### Riscos conhecidos desde o início

- [ ] `MaxSimultaneousInstances` precisa de enforcement real.
- [ ] `Stop(fadeOut)` precisa semântica real ou contrato ajustado.
- [ ] `IsPlaying` não pode usar semântica fraca.
- [ ] mixer/routing ainda precisa refinamento adicional de contrato.
- [ ] integrações por `Navigation`, `Gameplay`, `Skin` não podem contaminar o core.

### Decisões já fechadas antes da implementação

- [x] `Audio` é standalone.
- [x] `Presentation` é bridge, não owner.
- [x] `GlobalAudio` e `EntityAudio` são trilhas separadas.
- [x] pooling é infraestrutura compartilhada.
- [x] catálogos e coleções específicas de domínio entram depois, no lado consumidor.
