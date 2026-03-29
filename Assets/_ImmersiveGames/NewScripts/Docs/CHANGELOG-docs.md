# Changelog Docs

# 2026-03-29
- Registrado o fechamento documental da frente central da `Phase 2 - Core Boundary Cleanup`.
- Consolidado o next step como `Audio / BGM context ownership cleanup`.
- Mantido o escopo fora de `Save`, `PostGame x GameLoop` e cleanup físico de projeto.

# 2026-03-29
- Clarificada documentalmente a fronteira `LevelFlow x Navigation` na entrada em gameplay.
- Registrado que `Navigation` apenas despacha a macro route e que `LevelFlow` é owner da seleção explicita/default no `LevelPrepare`.
- Alinhado o log de runtime para refletir essa separação sem ambiguidade.

# 2026-03-29
- Fechada documentalmente a frente `PostGame / Restart / ExitToMenu` dentro da `Phase 2 - Core Boundary Cleanup`.
- Registrado que `Restart`, `RestartFromFirstLevel` e `ExitToMenu` estão estabilizados no canon atual.
- Reapontado o próximo foco para `LevelFlow x Navigation boundary cleanup`.

# 2026-03-29
- Registrada a validação do contrato canônico de restart após a correção em runtime.
- `Restart` foi consolidado como reinício do level/contexto atual e `RestartFromFirstLevel` como força do primeiro level canônico do catálogo.
- Marcada como fechada a parte de restart da `Phase 2 - Core Boundary Cleanup`, mantendo `ExitToMenu` como próximo foco da fronteira.

# 2026-03-29
- Explicitada a stance estrutural da `Phase 2 - Core Boundary Cleanup` como canon-first.
- Registrado que rewrite/refactor canônico é permitido quando for a forma mais canônica de fechar ownership e fronteira.
- Priorizado ownership canônico sobre compatibilidade estrutural provisória e preservação por inércia de trilhos paralelos.

# 2026-03-29
- Fechado o ciclo atual de `Save` com o Slice 7 e o Slice 8 concluídos.
- Registrada a decisão de estacionar `Save/Checkpoint` no estado atual, sem novo aprofundamento imediato.
- Repriorizado o roadmap para os módulos centrais da baseline.
- Formalizado o próximo passo como `Phase 2 - Core Boundary Cleanup`, com foco em `GameLoop`, `PostGame`, `LevelFlow` e `Navigation`.

# 2026-03-29
- Fechada documentalmente a implementação mínima estrutural de `Checkpoint` dentro de `Save`.
- Criado o relatório de closure do Slice 8 em `Docs/Reports/Audits/2026-03-29/Slice-8-Checkpoint-Minimal-Implementation-Closure.md`.
- Mantido o Slice 8 como contrato canônico, sem reescrever o plano como histórico operacional.

# 2026-03-29
- Aberto o Slice 8 como freeze documental do rail canônico de `Checkpoint` dentro de `Save`.
- Registrado `Checkpoint` como domínio próprio, distinto de `Preferences` e `Progression`.
- Congelados `CheckpointSnapshot`, `ICheckpointService`, `ICheckpointBackend` e `checkpointId` como nomes canônicos mínimos.
- Mantidos backend final, save state completo e checkpoint operacional fora de escopo.

# 2026-03-29
- Fechada documentalmente a validação do Slice 7 apos a composicao explicita do backend de `Progression`.
- Confirmado `Save` como camada canonica de orquestracao e `Progression` como detalhe de infraestrutura via `IProgressionBackend`.
- Registrado `InMemoryProgressionBackend` como backend provisório explicitamente composto.
- Mantidos `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent` como hooks oficiais canônicos.
- Mantidos backend final, checkpoint, migração e cloud fora do corte.

# 2026-03-29
- Extratado contrato explicito de backend de `Progression`, separado do orquestrador canonico de `Save`.
- Adaptada a implementacao provisoria para `InMemoryProgressionBackend`, mantendo comportamento atual.
- Compensada a composicao do backend de forma explicita no installer do `Save`, sem fallback silencioso.
- Mantido `SaveOrchestrationService` como owner de policy, dedupe e dispatch para `Preferences` / `Progression`.
- Mantidos backend final e `Checkpoint` operacional fora do corte.

## 2026-03-29
- Endurecida a policy canônica de `Save` para impedir persistência redundante entre hooks equivalentes no mesmo trilho.
- `WorldResetCompletedEvent` passou a persistir apenas em `Level + Completed`; `Macro + Completed` e `Macro + SkippedByPolicy` ficam como no-op.
- `SceneTransitionCompletedEvent` passou a persistir apenas em `Gameplay` quando `RequiresWorldReset=false`; frontend e transições delegadas ao reset ficam como no-op.
- Adicionado dedupe same-frame no rail de `Save` e guard defensivo equivalente no `Progression`.
- Backend final e `Checkpoint` operacional seguem fora de escopo.

## 2026-03-29
- Fase 4 do Slice 6 fechada com validacao runtime do rail de `Frontend/UI`, sem ownership de run state, route, result, readiness ou dispatch primario.
- Observados no log os cenarios `Menu -> Play`, `Menu -> Quit`, `Pause -> Resume`, `PostRunMenu -> Restart` e `PostRunMenu -> ExitToMenu`, com `Quit` executado no Editor via `IFrontendQuitService`.
- Mantido como follow-up nao bloqueante a extracao futura de `FrontendQuitService` para arquivo tecnico proprio.
- Fase 2 do Slice 6 fechada com `Frontend/UI` consolidado como emissor de intents derivadas explicitas.
- Reforcada nos logs a separacao entre exibicao local, emissao de intent e delegacao downstream.
- Mantidos `Restart`, `ExitToMenu`, `Pause` e `Resume` como intents visuais, sem ownership de estado, rota, resultado, readiness ou dispatch primario downstream.
- Fase 1 do Slice 6 fechada com `PostRunMenu` e `PauseMenu` consolidados como contextos visuais locais.
- Mantidos os overlays como apresentacao local reagindo ao contexto canonico ja consolidado, sem ownership de estado, rota, resultado, readiness ou dispatch downstream.
- Nao foram abertas intents derivadas novas nem integracao downstream nesta fase.
- Fase 0 do Slice 6 fechada como freeze documental do rail `SceneFlow technical rail -> Frontend/UI local visual contexts -> derived intents`.
- Consolidado `Frontend/UI` como contexto visual local e emissor de intents, sem ownership de run state, route, result, readiness ou dispatch primario downstream.
- Mantidos como bridges/temporarios `GamePauseOverlayController`, `PostGameOverlayController`, `FrontendPanelsController`, `MenuPlayButtonBinder`, `MenuQuitButtonBinder`, `FrontendButtonBinderBase`, `PostLevelActionsService` e `GamePauseGateBridge`.
- Aberto o Slice 7 como corte oficial de `Save`, ancorado em `ADR-0041` e nos hooks oficiais de persistencia.
- Registrado `Preferences` como base reutilizavel do novo corte, com `Checkpoint` mantido apenas como contrato conceitual inicial.
- Fechada a Fase 1 do Slice 7 com o contrato canonico de `Save` formalizado por `SaveIdentity`, `ISaveOrchestrationService` e os contratos separados de `Progression`.
- Mantido como follow-up nao bloqueante o payload ainda opaco de `Progression`, sem backend final.
- Fechada a Fase 2 do Slice 7 com o rail oficial de hooks de `Save` conectado a `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent`.
- Restrito `SceneTransitionCompletedEvent` a transicoes de gameplay ou contextos com `RequiresWorldReset`, mantendo transicoes de frontend como no-op observavel.
- Mantidos backend final e `Checkpoint` fora da fatia operacional.
- Fechamento documental do Slice 4 do Baseline 4.0 com o backbone validado `Navigation primary dispatch -> Audio contextual reactions`.
- Marcadas como concluidas as fases do plano do Slice 4 e removidas as pendencias antigas ja provadas em runtime.
- Consolidado o owner documental: `Audio` como owner real das reacoes contextuais, `Navigation` como dispatch primario, `SceneFlow` como trilho tecnico e `GameLoop` como fonte upstream de pause / run state.
- Abertura do Slice 5 como corte curto de `SceneFlow`, focado em trilho tecnico, readiness, loading e fade.
- Fase 2 do Slice 5 fechada apos saneamento final de `LoadingProgressOrchestrator` e `GameReadinessService`, com a observabilidade de readiness alinhada e sem trilho paralelo novo.
- Fase 4 do Slice 5 fechada com validacao do rail tecnico, mantendo `SceneFlow`, `Navigation`, `GameLoop`, `Audio` e `Frontend/UI` nas fronteiras canonicas.
- Abertura do Slice 6 como corte de `Frontend/UI`, focado em contexto visual local e emissao de intents derivadas.
- O proximo passo documental permanece a Fase 3 do Slice 5.

## 2026-03-29
- Fechamento documental do Slice 3 do Baseline 4.0 com o rail validado `PostRunMenu -> Restart / ExitToMenu -> Navigation primary dispatch`.
- Marcadas as fases concluidas no plano do Slice 3 e mantidas as pendencias sem bloqueio como follow-up arquitetural.
- Consolidado o owner documental: `PostGame` para `PostRunMenu`, `Navigation` para dispatch primario, `GameLoop` sem ownership visual do menu/dispatch.

## 2026-03-29
- Fechamento documental do Slice 2 do Baseline 4.0 com o rail validado `Playing -> ExitStage -> RunResult -> PostRunMenu`.
- Marcadas as fases concluidas no plano do Slice 2 e mantidos os follow-ups sem bloqueio como ruido de naming.
- Consolidado o owner documental: `GameLoop` para `Playing` e fronteira de fim de run; `PostGame` para `ExitStage`, `RunResult` e `PostRunMenu`.

## 2026-03-28
- Registrado o fechamento documental de `Preferences baseline v1: Audio + Video concluidos`.
- Alinhados o report datado, o `LATEST` de auditoria e o overview ativo do slice.
- Mantido como proximo passo o smoke test em build desktop para validar visualmente resolucao/fullscreen.

## 2026-03-27
- Consolidado o ADR-0012 como fonte unica de verdade do `PostStage` implementado e validado.
- Atualizados os indices e sumarios canonicos para tratar `PostStage` como fluxo vigente de `Modules/PostGame`.
- Registrada a politica operacional: default sem presenter, presenter explicito roda `PostStage` real, skip automatico quando ausente.

## 2026-03-26
- Atualizada a documentacao para o modelo canonico atual de rotas.
- `SceneRouteDefinitionAsset` passou a ser a referencia efetiva da rota.
- `GameNavigationCatalogAsset` ficou explicito como catalogo fino de intent.
- `SceneFlow` passou a ser descrito como consumidor de rota ja resolvida.
- `SceneRouteCatalogAsset` foi removido da documentacao ativa e do runtime principal.
