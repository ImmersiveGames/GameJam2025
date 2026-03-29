# Plan - Baseline 4.0 Slice 7 - Save Canonical

Subordinado a `ADR-0043`, `ADR-0044`, `ADR-0041`, `ADR-0037` e ao [Blueprint-Baseline-4.0-Ideal-Architecture.md](./Blueprint-Baseline-4.0-Ideal-Architecture.md).

Escopo desta fase:
- `NewScripts` only
- `Docs/Plans` only
- sem tocar em `Scripts` legado
- sem implementar ainda
- sem reabrir `Slice 1`, `Slice 2`, `Slice 3`, `Slice 4`, `Slice 5` ou `Slice 6`
- `Save` entra agora como corte oficial de integracao canonica

## 1. Resumo executivo

O Slice 7 e o primeiro corte oficial de persistencia apos os slices de runtime, audio e UI.

Motivo do corte:
- `ADR-0041` promove `Save` a camada canonica separada, com backend trocavel e fail-fast.
- `Docs/Canon/Official-Baseline-Hooks.md` ja define os hooks oficiais que o `Save` deve observar.
- os slices 1-6 fecham a espinha runtime e a superficie visual; o que falta agora e a camada de persistencia desacoplada.

Objetivo do Slice 7: formalizar a camada canonica de `Save` como orquestrador de persistencia, sem assumir ownership de `GameLoop`, `SceneFlow`, `Navigation` ou `WorldReset`.
`Progression` passa a persistir via `IProgressionBackend`, mantendo a implementacao atual apenas como backend provisório explicitamente composto.

Foco operacional do slice:

`GameRunEndedEvent / WorldResetCompletedEvent / SceneTransitionCompletedEvent -> Save canonical layer -> Preferences / Progression -> backend trocavel`

Fase 0 concluida como freeze documental do rail canonico de `Save`.

Regra do slice:
- `Save` = camada canonica de orquestracao de persistencia
- `Preferences` = dominio leve de configuracao do jogador
- `Progression` = dominio persistente de avanco
- `Checkpoint` = contrato conceitual, nao implementacao inicial
- `profileId` e `slotId` = obrigatorios em qualquer fluxo que escreva ou leia persistencia canonica
- `Save` nao e dono de pipeline macro, scene flow, navigation, gameplay state ou UI

Fora de escopo:
- `Save` como dono de `GameLoop`
- `Save` como dono de `SceneFlow`
- `Save` como dono de `Navigation`
- `Save` como dono de `WorldReset`
- `Checkpoint` operacional nesta primeira fatia
- trocas massivas de nomes
- refatoracao ampla de pastas
- reabertura dos slices 1-6

## 2. Backbone do Slice 7

### Nomes canonicos congelados

- `Save`
- `Preferences`
- `Progression`
- `Checkpoint`
- `profileId`
- `slotId`
- `Backend`
- `Autosave`

### Nomes temporarios / bridges

- `PlayerPrefsPreferencesBackend`
- `PreferencesBootstrap`
- `PreferencesService`
- `IPreferencesBackend`
- `IPreferencesSaveService`
- `IPreferencesStateService`
- `BootstrapConfigAsset`

### Runtime rail canonico

1. `GameRunEndedEvent`, `WorldResetCompletedEvent` ou `SceneTransitionCompletedEvent` sinalizam um marco estavel.
2. `Save` valida que `profileId` e `slotId` estao presentes quando o contrato exigir persistencia.
3. `Save` agrega apenas contribuidores explicitos de `Preferences` e `Progression`.
4. `Save` delega o armazenamento a um backend trocavel.
5. `Save` registra observabilidade minima e falha de forma explicita em configuracao ou contrato invalido.
6. `Checkpoint` permanece apenas como parte conceitual do contrato, sem ciclo operacional inicial.

### Parallel rails to eliminate

- autosave por polling ou observacao generica do runtime
- fallback silencioso para config ausente
- varredura generica do mundo para montar snapshot
- backend concreto tratado como source-of-truth do contrato
- `Save` invadindo `GameLoop`, `SceneFlow`, `Navigation` ou `WorldReset`
- duplicacao de persistencia entre `Preferences` e uma futura camada de `Save`
- manual save path inventado sem ADR ou hook oficial

### Owners por modulo

| Modulo | Papel no slice |
|---|---|
| `Save` | owner da orquestracao canonica de persistencia |
| `Preferences` | source reutilizavel de preferencia leve e backend local inicial |
| `GameLoop` | fonte upstream de `GameRunEndedEvent` |
| `WorldReset` | fonte upstream de `WorldResetCompletedEvent` |
| `SceneFlow` | fonte upstream de `SceneTransitionCompletedEvent` |
| `Core` / `Infrastructure` | backend e bootstrap tecnico, sem ownership semantico de Save |
| `Frontend/UI` | consumidor ou emissor de intent quando existir interacao explicita, sem ownership de persistencia |

## 3. Reuse map

| Peca atual | Decisao | Observacao |
|---|---|---|
| `Modules/Preferences/Runtime/PreferencesService.cs` | Keep with reshape | base reutilizavel para o dominio de `Preferences`, mas nao o contrato final de `Save` |
| `Modules/Preferences/Runtime/PlayerPrefsPreferencesBackend.cs` | Keep with reshape | backend local simples provisorio, trocavel por contrato |
| `Modules/Save/Contracts/IProgressionBackend.cs` | Keep | contrato explicito de backend de `Progression` |
| `Modules/Save/Runtime/InMemoryProgressionBackend.cs` | Keep with reshape | backend provisório explicitamente composto, nao source-of-truth do contrato |
| `Modules/Preferences/Bootstrap/PreferencesBootstrap.cs` | Keep with reshape | bootstrap util para resolver config obrigatoria e aplicar snapshots |
| `Modules/Preferences/Contracts/IPreferencesBackend.cs` | Keep | port de backend do dominio leve |
| `Modules/Preferences/Contracts/IPreferencesSaveService.cs` | Keep | contrato de escrita/persistencia do dominio leve, candidato a encaixe no Save canonico |
| `Modules/Preferences/Contracts/IPreferencesStateService.cs` | Keep | leitura de estado atual do dominio leve |
| `Docs/Canon/Official-Baseline-Hooks.md` | Keep | fonte oficial de hooks que o Save deve observar |
| `Modules/Audio/Bootstrap/AudioRuntimeComposer.cs` | Keep as consumer boundary | usa Preferences, mas nao vira owner de Save |
| `Docs/ADRs/ADR-0041-Save-Camada-Canonica-com-Backend-Trocavel-e-Backend-Local-Simples-para-Testes.md` | Keep | contrato direto do slice |

## 4. Hooks/eventos minimos

Slice 7 precisa, no minimo, destes hooks canonicos:

| Hook/evento | Papel |
|---|---|
| `GameRunEndedEvent` | marco estavel principal para autosave terminal da run |
| `WorldResetCompletedEvent` | marco estavel para persistencia apos reset concluido |
| `SceneTransitionCompletedEvent` | marco estavel para persistencia apos transicao aplicada |
| `GameResetRequestedEvent` | observavel apenas se houver razao canonica de correlacao, nao como gatilho fraco |
| `ReadinessChangedEvent` | observavel tecnico apenas, nao contrato de autosave |

Regras:
- nao criar novo evento se os hooks oficiais acima ja cobrem o marco
- nao transformar eventos observaveis em API publica por conveniencia
- nao introduzir fallback silencioso para preencher ausencia de hook
- nao usar polling de runtime para compensar contrato fraco

## 5. Sequencia de implementacao em fases curtas

### Fase 0 - congelar o rail

- declarar `Save`, `Preferences`, `Progression`, `Checkpoint`, `profileId` e `slotId` como nomes canonicos
- fixar que `Checkpoint` permanece conceitual nesta fatia inicial
- deixar explicito que `Save` nao assume ownership de `GameLoop`, `SceneFlow`, `Navigation`, `WorldReset` ou UI
- registrar que `Preferences` continua reutilizavel, mas nao e o contrato final de persistencia
- fixar o rail canonico da fase como `GameRunEndedEvent / WorldResetCompletedEvent / SceneTransitionCompletedEvent -> Save canonical layer -> Preferences / Progression -> backend trocavel`
- registrar que o backend trocavel nao e source-of-truth do contrato
- registrar que os hooks oficiais sao seams preferidos, nao gatilhos cegos
- registrar que `SceneTransitionCompletedEvent` so pode disparar autosave quando o contexto/contrato de persistencia exigir
- registrar que a fase 0 esta fechada como freeze documental
- confirmar o caminho runtime alvo e os fora de escopo

### Fase 1 - contrato canonico

- formalizar o contrato de `Save` como camada de aplicacao e orquestracao
- separar explicitamente `Preferences` e `Progression`
- exigir `profileId` e `slotId` em fluxos que dependam de persistencia canonica
- manter `Checkpoint` apenas como conceito reservado

Fase 1 fechada nesta rodada:
- `SaveIdentity` formaliza o escopo canonico com `profileId` e `slotId`
- `ISaveOrchestrationService` explicita a camada de aplicacao/orquestracao e a separacao entre `Preferences` e `Progression`
- `IProgressionStateService` e `IProgressionSaveService` formalizam o contrato separado de `Progression`

Follow-up nao bloqueante:
- o payload efetivo de `Progression` permanece opaco nesta fatia inicial e pode ser refinado em corte futuro sem introduzir backend final

### Fase 2 - hooks oficiais

- alinhar `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent` como marcos estaveis de autosave
- manter os hooks como seams oficiais, nao como sinais tecnicos ad hoc
- evitar que UI ou polling substituam os hooks oficiais

Fase 2 fechada nesta rodada:
- `GameRunEndedEvent` e `WorldResetCompletedEvent` passaram a disparar o rail oficial de `Save`
- `WorldResetCompletedEvent` ficou canonicamente restrito a `Level + Completed`; `Macro + Completed` e `Macro + SkippedByPolicy` nao persistem
- `SceneTransitionCompletedEvent` ficou restrito a contexto canonico, acionando autosave apenas em transicoes de gameplay sem `RequiresWorldReset`
- `SceneTransitionCompletedEvent` em transicoes de frontend permanece como no-op observavel, para evitar persistencia redundante
- o bridge tecnico de `Save` passou a registrar hook recebido, decisao, dominio alvo, motivo e identidade
- o backend final continua fora da fatia
- `Checkpoint` continua fora da fatia operacional

### Fase 3 - hardening de policy e dedupe

- consolidar a matriz canonica de disparo para impedir persistencia redundante entre hooks equivalentes do mesmo trilho
- deduplicar saves por trilho canonicamente identificado no mesmo frame, evitando save repetido efetivo e warnings observaveis no fluxo de progressao
- manter `GameRunEndedEvent` como gatilho terminal sempre habilitado
- manter `WorldResetCompletedEvent` como gatilho canonico apenas para `Level + Completed`
- manter `SceneTransitionCompletedEvent` como gatilho canonico apenas para `Gameplay` quando `RequiresWorldReset=false`
- preservar `Preferences` e `Progression` separados, com backend trocavel e `Checkpoint` fora da fatia operacional

### Fase 4 - backend plugavel de Progression

- extrair um contrato explicito de backend de `Progression`, separado do orquestrador de `Save`
- manter a implementacao provisoria atual como backend in-memory adaptado
- compor o backend de forma explicita no installer, sem fallback silencioso de runtime
- falhar cedo se o backend de `Progression` nao puder ser resolvido durante a instalacao
- manter `SaveOrchestrationService` como owner apenas de policy, dedupe e dispatch para `Preferences` / `Progression`

Fase 4 fechada nesta rodada:
- `Progression` passou a persistir via `IProgressionBackend`
- `InMemoryProgressionBackend` ficou documentado como backend provisório explicitamente composto
- `Save` permaneceu como camada canonica de orquestracao; o backend continuou como detalhe de infraestrutura

### Fase 3 - backend trocavel

- manter `PlayerPrefsPreferencesBackend` como backend local simples provisorio
- preservar o backend como detalhe de infraestrutura
- falhar de forma explicita se a configuracao obrigatoria do backend ou do slot estiver ausente
- nao converter o backend local em source-of-truth do contrato

### Fase 4 - validacao documental

- conectar os contratos de `Save` ao changelog e aos hooks oficiais
- validar que `Save` continua desacoplado do pipeline macro
- registrar as pendencias que ficam para um corte futuro

## 6. Criterios de aceite

O Slice 7 so e aceito se:

- `Save` estiver descrito como camada canonica de orquestracao de persistencia
- `Preferences` e `Progression` estiverem separados conceitualmente
- `Checkpoint` continuar fora da implementacao inicial
- `profileId` e `slotId` forem obrigatorios onde a persistencia canonica exigir
- `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent` forem os marcos oficiais preferidos para autosave
- nenhum fallback silencioso novo for introduzido
- nenhum polling de runtime for usado para sustentar o contrato
- `Save` nao ganhar ownership de `GameLoop`, `SceneFlow`, `Navigation` ou `WorldReset`
- os slices 1-6 permanecerem fechados sem reabertura

## 7. Pendencias herdadas / nao bloqueantes

- `Checkpoint` existe como parte conceitual do contrato, mas nao entra na fatia operacional inicial
- `PlayerPrefsPreferencesBackend` continua como backend local provisorio ate um backend futuro ser escolhido
- qualquer UI manual de persistencia permanece fora deste corte, salvo ADR especifico
- o baseline de `Preferences` continua fechado e nao e reaberto aqui
- `Save` ainda depende do reaproveitamento da camada atual de `Preferences` ate a formalizacao completa do contrato canonico
- o uso de `SceneTransitionCompletedEvent` como gatilho de autosave deve sempre respeitar contexto/contrato para evitar persistencia redundante em transicoes de frontend
- `IProgressionBackend` e `InMemoryProgressionBackend` permanecem como infraestrutura do `Progression`, nao como owner do rail canônico de `Save`
