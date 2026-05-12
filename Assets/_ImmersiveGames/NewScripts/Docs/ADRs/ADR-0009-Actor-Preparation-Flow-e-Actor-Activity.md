# ADR-0009 - Actor Preparation Flow e Actor Activity

## Status
- Estado: Proposed
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR, após aceite.

## Contexto

A Base 1.1 já define que o `SessionOperationalPipeline` é o owner do ciclo operacional de sessão e que o `Session Transition Envelope` possui uma fase explícita de `SessionOperationalSetup` antes de `BeforeFadeOut` e antes da entrada efetiva da `SessionActivityPipeline`.

Durante a evolução do rail de sessão, ficou claro que tratar `InputMode` como eixo arquitetural isolado é insuficiente. O estado real de gameplay depende de um conjunto maior de decisões envolvendo actors, players, controles, câmera, HUD, readiness, posicionamento, preservação e materialização.

Também ficou claro que a montagem principal dos actors necessários para iniciar uma activity não deve ser empurrada para a `SessionActivityPipeline`. A activity deve receber um palco já preparado, com actors obrigatórios materializados ou preservados, posicionados, inicializados, vinculados e validados. A activity passa então a comandar transformações in-game, e não a montagem inicial da sessão.

A Base 1.1 precisa formalizar esse corte sem criar um novo pipeline prematuramente e sem introduzir uma central onisciente de actors.

## Decisão

Adota-se o **Actor Preparation Flow** como parte do `SessionOperationalPipeline`.

Nesta etapa da Base 1.1, `Actor Preparation` **não é um pipeline próprio**. Ele é um `Pipeline Stage` operacional dentro do `SessionOperationalPipeline`, executado durante o `SessionOperationalSetup`, antes do `SessionActivityEntryHandoffPrepared`, antes de `BeforeFadeOut` e antes da abertura da cortina.

A `SessionActivityPipeline` recebe os actors preparados e passa a comandar a vida ativa dos actors durante a activity.

### 1. Corte principal

A Base 1.1 adota o seguinte corte:

```text
SessionOperationalPipeline
-> ActorPreparationStage
-> SessionActivityEntryHandoffPrepared
-> BeforeFadeOut
-> TransitionCompleted
-> SessionActivityPipeline
-> Actor Activity
```

A leitura canônica é:

```text
Operational prepara actors.
Activity transforma actors durante gameplay ativo.
Executores aplicam efeitos.
```

## 2. Actor Preparation Stage

`ActorPreparationStage` é a fase operacional responsável por preparar os actors necessários para a activity inicial da sessão.

Essa stage pertence ao `SessionOperationalPipeline` e não cria ownership próprio de lifecycle.

Responsabilidades:

- resolver a participação de actors da sessão;
- resolver o actor set necessário para a activity inicial;
- materializar, preservar ou rematerializar actors obrigatórios;
- posicionar actors na cena;
- entregar identidade/contexto aos actors;
- executar setup interno declarado pelos próprios actors;
- aplicar vínculos externos obrigatórios;
- validar readiness dos actors;
- produzir facts, commands, snapshots e handoff data para a activity.

Não responsabilidades:

- decidir lifecycle de run;
- decidir lifecycle global de session fora do `SessionOperationalPipeline`;
- decidir engagement da activity;
- comandar comportamento in-game contínuo;
- substituir a `SessionActivityPipeline`;
- virar uma central onisciente de player, input, HUD, camera, enemy, AI e interactables.

## 3. Fases conceituais do Actor Preparation Flow

O fluxo conceitual é dividido em fases internas. Essas fases podem virar classes, serviços ou etapas internas no futuro, mas não devem ser tratadas como pipelines próprios nesta etapa.

### 3.1 Actor Participation

Define quem participa da sessão.

Exemplos:

- existe player local;
- quantidade de players;
- slots de players ativos;
- player humano, bot ou remoto;
- sessão com gameplay participation;
- necessidade de controle;
- necessidade de actor jogável.

### 3.2 Actor Set Resolution

Resolve o elenco necessário para a activity inicial.

Exemplos:

- `PlayerActor` obrigatório;
- inimigos iniciais obrigatórios;
- objetos interativos obrigatórios;
- companion actors;
- actor-linked camera target;
- actor-linked HUD target;
- actor set opcional.

### 3.3 Actor Materialization

Cria, preserva ou rematerializa actors necessários.

Exemplos:

- instanciar actor ausente;
- preservar actor compatível já existente;
- rematerializar actor incompatível;
- remover runtime stale/orphan que bloqueia slot obrigatório;
- registrar actor materializado no runtime.

### 3.4 Actor Placement

Posiciona actors na cena após materialização ou preservação.

Exemplos:

- resolver spawn point;
- reposicionar player preservado;
- posicionar enemy inicial;
- posicionar objetos interativos obrigatórios;
- aplicar orientação inicial.

### 3.5 Actor Self Setup

Executa preparação interna declarada pelo próprio actor.

Exemplos:

- inicializar movement;
- inicializar health;
- inicializar interaction;
- inicializar AI;
- preparar estado bloqueado/dormant;
- restaurar estado local obrigatório;
- inicializar componentes internos que dependem de identidade/contexto operacional.

O lifecycle normal da Unity pode continuar sendo usado para inicialização local simples do objeto, desde que não assuma ownership de session, activity, pipeline ou lifecycle global.

Regra canônica:

```text
Unity lifecycle inicializa o objeto.
Actor Preparation integra o objeto à sessão.
```

### 3.6 External Actor Bindings

Aplica vínculos externos necessários aos actors.

Exemplos:

- input/control binding;
- HUD binding;
- camera binding;
- audio binding;
- save/state binding;
- target binding;
- interaction prompt binding.

Esses vínculos cruzam fronteiras de domínio e devem ser executados por adapters especializados quando necessário.

### 3.7 Actor Readiness

Valida se os actors obrigatórios estão prontos antes do handoff para activity.

Exemplos de readiness obrigatória:

- actor obrigatório existe;
- actor obrigatório possui identidade válida;
- actor obrigatório está posicionado;
- actor obrigatório executou setup interno mínimo;
- vínculos externos obrigatórios foram aplicados;
- input está em estado seguro;
- HUD/camera obrigatórios estão prontos quando exigidos;
- nenhum actor obrigatório falhou silenciosamente.

Se um requisito obrigatório falhar, o fluxo deve reportar failure explícito ou fail-fast. Não deve haver fallback silencioso.

### 3.8 Activity Handoff

Produz os dados necessários para a `SessionActivityPipeline` iniciar a activity sem remontar o palco.

O handoff deve carregar identidade suficiente para impedir que foreign/stale events alterem a activity ativa.

### 3.9 Actor Activity

Após o handoff, a `SessionActivityPipeline` comanda a vida ativa dos actors durante a activity.

Exemplos:

- liberar controle real;
- trocar input map por situação ativa;
- pausar/retomar actors;
- aplicar dano;
- mudar estado de actor;
- spawnar inimigos por gameplay;
- criar pickups por gameplay;
- destruir objetos durante gameplay;
- iniciar transformação in-game;
- bloquear/desbloquear controle por evento de activity.

Spawn ou materialização durante activity é permitido quando for consequência do gameplay ativo. O que for obrigatório para iniciar a activity pertence ao `ActorPreparationStage` operacional.

### 3.10 Actor Teardown / Preservation

Define o destino dos actors ao sair da activity ou trocar sessão.

Exemplos:

- preservar player entre activities;
- despawnar inimigos temporários;
- limpar objetos de gameplay;
- marcar actor como stale;
- preparar rematerialização futura;
- emitir facts de saída para save/continuidade quando comandado pelo pipeline correto.

## 4. Actors declaram necessidades de setup

Actors não decidem lifecycle. Actors declaram necessidades.

Um actor pode declarar:

- preciso de identidade;
- preciso de spawn point;
- preciso de player slot;
- preciso de controle;
- preciso de HUD;
- preciso de camera;
- preciso de target inicial;
- preciso iniciar bloqueado;
- preciso restaurar estado;
- estou pronto;
- falhei ao preparar.

Um actor não pode decidir:

- quando a sessão começa;
- quando a activity começa;
- quando a cortina abre;
- quando o pipeline avança;
- quando a rota muda;
- quando a gameplay é liberada globalmente.

Regra canônica:

```text
Actor declara necessidade.
Pipeline decide janela e ordem macro.
Fluxo interno executa preparação.
Adapters aplicam vínculos externos.
Readiness decide se o handoff pode prosseguir.
```

## 5. Nomenclatura

O termo `Adapter` deve ser reservado para fronteiras com outros domínios ou runtimes externos ao fluxo de actors.

### 5.1 Nomes internos ao fluxo de actors

Preferir nomes simples:

- `ActorPreparationStage`
- `ActorPreparationPlan`
- `ActorParticipation`
- `ActorSet`
- `ActorSetResolver`
- `ActorMaterialization`
- `ActorPlacement`
- `ActorSetup`
- `ActorSetupRequirement`
- `ActorSetupResult`
- `ActorReadiness`
- `ActorPreparationSnapshot`
- `ActorPreparationReport`

Evitar, por padrão:

- `ActorPreparationSystem`
- `ActorSetupSystem`
- `PlayerSystem`
- `ActorCentralSystem`
- `ActorMaterializationAdapter`
- `ActorPlacementAdapter`
- `ActorReadinessAdapter`

### 5.2 Nomes para fronteiras externas

Usar `Adapter` quando houver integração com outro domínio:

- `InputModeAdapter`
- `HudBindingAdapter`
- `CameraBindingAdapter`
- `AudioAdapter`
- `SaveAdapter`
- `SceneCompositionAdapter`
- `PlayerControlBindingAdapter`, se a execução cruzar fronteira de input/runtime externo

## 6. Relação com InputMode, HUD, Camera e Audio

`InputMode`, HUD, camera e audio não são owners do setup de actors.

Eles são vínculos externos aplicados quando o `ActorPreparationStage` ou a `SessionActivityPipeline` comandam.

Antes da activity:

- preparar binding de controle;
- preparar camera/HUD obrigatórios;
- manter input em estado seguro;
- não liberar gameplay antes da readiness.

Durante a activity:

- liberar controle real;
- trocar mapa de controle;
- alternar pause/overlay;
- atualizar HUD por estado ativo;
- alterar camera por evento de gameplay.

## 7. Identidade e proteção contra foreign/stale events

Todo ciclo relevante de Actor Preparation deve carregar identidade suficiente para validação.

Identidades mínimas esperadas:

- `Pipeline Identity`;
- `Session Identity`;
- `Route Identity`, quando aplicável;
- `Activity Identity`, quando aplicável;
- `Actor Identity`;
- `Actor Set Identity` ou `ActorPreparationBatchId`, quando necessário;
- `Setup Sequence` ou equivalente, quando houver múltiplas preparações dentro da mesma sessão.

Foreign/stale actor events não podem:

- materializar actor no pipeline ativo;
- reconfigurar binding ativo;
- liberar readiness;
- trocar actor atual;
- reabrir setup concluído;
- alterar activity ativa.

## 8. Invariantes

- `ActorPreparationStage` pertence ao `SessionOperationalPipeline`.
- `ActorPreparation` não é pipeline próprio nesta etapa da Base 1.1.
- `SessionOperationalPipeline` decide janela, ordem macro, readiness e handoff.
- Actors declaram necessidades de setup; não decidem lifecycle.
- O fluxo de actors não pode virar uma central onisciente.
- O termo `Adapter` deve ser reservado para fronteiras externas ao domínio de actors.
- Setup obrigatório de actors acontece antes da abertura da cortina.
- Activity não monta o palco inicial obrigatório.
- Activity transforma actors durante gameplay ativo.
- Spawn/materialização durante activity é permitido apenas como consequência de gameplay ativo.
- Readiness obrigatório falho não pode gerar fallback silencioso.
- Unity lifecycle pode inicializar objeto local, mas não decidir session/activity lifecycle.
- Foreign/stale actor events não podem alterar o pipeline ativo.

## 9. Consequências

- `InputMode` deixa de ser tratado como eixo arquitetural isolado e passa a ser lido como vínculo/execução técnica dentro de um contrato maior de actors/player.
- A preparação principal de actors passa a ocorrer no operacional, com a cortina fechada.
- A `SessionActivityPipeline` recebe um palco pronto e comanda apenas a vida ativa e transformações in-game.
- Actors ganham autonomia declarativa sem virar owners de lifecycle.
- O setup escala melhor porque cada actor informa suas necessidades.
- HUD, camera, input, audio e save permanecem como integrações por adapters quando cruzam fronteira de domínio.
- Evita-se um `ActorSetupManager` ou `ActorSystem` centralizador.
- Uma futura Base 2.0 pode extrair um pipeline próprio de actor preparation se a Base 1.1 provar necessidade real.

## 10. Relação com Base 1.0 e Base 2.0

- Base 1.0 espalhou materialização, readiness, input e preparação de actors entre serviços operacionais, readiness, input, scene-local code e lifecycle de objetos.
- Base 1.1 formaliza o corte: preparação obrigatória de actors pertence ao `SessionOperationalPipeline`; vida ativa pertence à `SessionActivityPipeline`; efeitos externos são executados por adapters.
- Base 2.0 futura pode transformar `ActorPreparationStage` em sub-pipeline ou pipeline próprio se a complexidade real justificar. Isso não deve ser feito agora.

## 11. Roadmap Futuro

- Mapear owners atuais de actors, readiness, input binding, camera binding, HUD binding e materialização.
- Criar matriz de migração específica de actors:

```text
owner antigo -> novo pipeline owner -> papel final -> ação necessária
```

- Definir contratos mínimos para `ActorSetupRequirement`, `ActorPreparationPlan`, `ActorReadiness` e `ActorPreparationSnapshot`.
- Definir quais bindings externos exigem adapters próprios.
- Implementar somente após fechar auditoria e matriz de migração.
