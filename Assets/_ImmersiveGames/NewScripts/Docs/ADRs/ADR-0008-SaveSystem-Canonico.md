# ADR-0008 - SaveSystem Canonical

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 estabeleceu save como um dos módulos da composição, mas não formalizou seu contrato canonical em relação aos pipelines. Base 1.1 exige que save seja claramente posicionado como um adapter comandado pelos pipelines operacionais.

## Decisão

Adota-se o `SaveSystem` como módulo produtor de facts e executor de save commands via adapters.

## Decisao Congelada Base 1.1 (Save)

- Preferences e Progression sao scopes diferentes.
- Preferences persistem escolhas do jogador/sistema (audio, video, input bindings, layout, idioma/acessibilidade quando existirem).
- Owner de decisao de Preferences: `PreferencesRuntimePipeline`.
- Progression persiste evolucao de jogo (activities, objetos, run, posicoes/status/checkpoints/inventario e equivalentes).
- Progression nao tem owner unico generico.
- O pipeline dono do ciclo decide quando coletar, carregar ou persistir progression.
- Objetos/dominios produzem conteudo por contrato de snapshot/registro.
- Objetos/dominios nao chamam backend diretamente.
- `SaveRuntime` fornece API estavel e executor comum para persistencia de progression comandada.
- Backend/core de `SaveRuntime` e mutavel/substituivel.
- Backend atual pode ser PlayerPrefs apenas como backend tecnico provisorio.
- PlayerPrefs nao deve permanecer como write path direto de Preferences quando a migracao ocorrer.
- `RouteActivitySave` permanece especifico de rota/activity no `SessionOperationalPipeline`.
- `SessionOperationalPipeline` nao vira owner generico de persistencia.
- `RunPipeline` sera owner de run save/continuity quando esse fluxo for materializado.
- Todo `Pipeline Command` de save deve carregar `Pipeline Identity`.
- Foreign/stale events devem ser rejeitados ou gerar skip explicito.
- Sem fallback silencioso.
- Config obrigatoria ausente e fail-fast.
- Nao manter dois write paths ativos.

### 1. Componentes Canônicos do SaveSystem

#### SaveConfigAsset
- Configuração declarativa de save behavior.
- Não define ownership de quando salvar; apenas parâmetros declarativos de execução.
- Propriedade: qual save backend usar.
- Propriedade: slots, serialization strategy, etc.

#### SaveBackendAsset
- Define implementação concreta de persistence (local file, cloud, etc.).
- Implementa `ISaveBackend` interface.

#### ISaveBackend Interface
- Contrato padrão para save backends.
- Métodos: `SaveAsync`, `LoadAsync`, `DeleteAsync`.
- Usado por `SaveCoreService` para executar operações.

#### SaveCoreService
- Core service que coordena save/load operations.
- Interage com `SaveBackendAsset` via `ISaveBackend`.
- Não decide quando salvar; executa o que foi comandado.
- Reporta completion/failure ao pipeline/adapter solicitante.

### 2. Save Pipeline Integration

#### SaveSystem as Adapter
- `SaveSystem` executa comandos de save/load vindos de pipelines.
- Os pipelines (`SessionOperationalPipeline`, `RunPipeline`, etc.) decidem quando e o quê salvar.
- Save não é owner de lifecycle; é executor de comandos.

#### SaveEligibility
- Decidida por `SceneRouteProfile` ou `RunPipeline` policy.
- Não inferida por `routeKind`.
- Explícita: rota pode ou não ser save-eligible.

#### Save Policies
- Decididas pelo pipeline, não pelo save module.
- Exemplo: "salvar ao mudar rota", "salvar ao completar run", etc.

### 2.1 Checkpoint SessionOperational (2026-05-14)

- `RouteActivitySavePlanReady` é plano/observabilidade, não execução.
- `loadActivitySaveOnEnter`:
  - executa no `SessionOperationalPipeline` após `SceneCompositionCompleted`;
  - ocorre antes de `InputCapability`, `PlayerPreparation` e handoff para `SessionActivityPipeline`;
  - ausência de snapshot salvo gera `RouteActivitySaveLoadSkipped skipReason='no_snapshot'`.
- `saveActivityOnExit` (Fase 2):
  - executa somente em troca de rota operacional;
  - decisão usa a rota anterior completa (não a rota atual);
  - executa antes de descarregar cena da rota anterior;
  - sem `Activity Snapshot Provider` canônico, gera `RouteActivitySaveSaveSkipped skipReason='no_snapshot_provider'`.
- `SessionOperationalActivitySaveAdapter` executa side-effect via `ISaveService`; não decide lifecycle.
- Ausência de adapter/config obrigatória permanece fail-fast.

### 3. Invariantes

- `SaveCoreService` não decide quando salvar.
- Backend é intercambiável; `ISaveBackend` define contrato.
- `SaveConfigAsset` é declarative, não imperative.
- Save failure deve ser reportado, não silenciado.
- Foreign/stale save commands são ignorados.
- Ausência de backend configurado é erro fail-fast.
- Sem fallback silencioso.
- Nao manter dois write paths ativos para o mesmo scope de dado.

### 3.1 Mapa Canonico Base 1.1

| Tipo de dado | Owner canonico | Pipeline/adapter responsavel | Executor/backend | Observacao/acao futura |
|---|---|---|---|---|
| Preferences (audio/video/input/layout/idioma/acessibilidade) | `PreferencesRuntimePipeline` | `PreferencesRuntimePipeline` (decide) + binders apenas como intencao UI | Backend tecnico atual de Preferences (PlayerPrefs provisorio) | Migrar write path de Preferences para trilho canonico de Save sem manter write path paralelo |
| Route Activity Save | `SessionOperationalPipeline` (scope de rota/activity) | `RouteActivitySave` + `SessionOperationalActivitySaveAdapter` | `SaveRuntime` (`ISaveService` + backend configurado) | Continua especifico de rota/activity; nao vira owner generico |
| Run Save / Continuity | `RunPipeline` (quando materializado) | `RunPipeline` + adapter de save do dominio run | `SaveRuntime` (backend substituivel) | Owner ainda futuro, mas ja congelado no trilho canonico |
| Progression de objetos/dominios | Pipeline dono do ciclo correspondente | Pipeline do ciclo + adapter de save do dominio | `SaveRuntime` (backend substituivel) | Dominios produzem snapshot/registro; dominio nao chama backend direto |

## Consequências

- Save deixa de ser espalhado em points of interest.
- Decisão fica no pipeline; execução em `SaveCoreService`.
- Backend é plugável e testável.
- Save eligibility é explícita e rastreável.
- Falhas de save são observáveis.

## Roadmap Futuro

- Implementar canonical `SaveAdapter` que encapsula `SaveCoreService`.
- Documentar quando Session Pipeline vs Run Pipeline vs Gate decide save.
- Adicionar checkpoint/versioning se necessário.
- Considerar save scope (global vs session vs run).

## Relação com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu save como módulo, mas deixou policy dispersa.
- Base 1.1 formaliza save como adapter executado por pipelines.
- Base 2.0 futura pode extrair save patterns se a Base 1.1 continuar provando estabilidade.

---

## Decisao Congelada de Ownership (Persistencia de Escolhas Runtime)

- Escolhas runtime de jogador pertencem ao produtor correto (Activity, selecao, profile/loadout, objeto de dominio ou sistema especifico).
- Inicializacao de sessao nao vira owner generico de persistencia dessas escolhas.
- Save continua executor comandado pelos owners corretos via `Pipeline Command` e `Pipeline Policy`.
- Preferences permanece fora de `RouteActivitySave`.
