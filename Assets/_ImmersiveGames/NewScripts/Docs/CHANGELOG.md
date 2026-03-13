# Changelog - Docs

## 2026-03-12 - loading docs closeout
- promoveu o loading de producao para a documentacao oficial atual, sem criar superficie paralela
- documentou `LoadingHudScene` como HUD canonica do macro flow e `ILoadingPresentationService` como owner da apresentacao
- alinhou guia principal, modulo de `SceneFlow`, canon, `LATEST` e HTML com barra, porcentagem, etapa, spinner e progresso hibrido
## 2026-03-12 - production guides full practical deepening
- aprofundou os guias canonicos de producao com receitas do zero para routes, styles, startup, levels, intro, post hook, ActorGroupRearm e chamadas reais de runtime
- ampliou a referencia de hooks com publisher atual, momento do fluxo, uso real, mini exemplos, quando usar e quando nao usar para os hooks operacionais principais
- transformou `Manual-Operacional.html` e `Hooks-Reference.html` em camada visual completa com indice, ancoras, cards, blocos de passo a passo, exemplos reais e checklist
- manteve o conteudo tecnico e a arquitetura documentada intactos, mudando apenas profundidade pratica e apresentacao
## 2026-03-12 - final docs closeout
- fechou a superficie documental principal como current-state-only em `README`, canon, modulos, guias, ADRs vigentes e `LATEST`
- alinhou a narrativa oficial com o runtime validado: bootstrap/startup, `RouteKind`, navigation/transition direct-ref, `IntroStage` level-owned opcional, `PostGame` global e `ActorGroupRearm`
- removeu ou despromoveu historicos e auditorias superseded da superficie principal para evitar duas verdades operacionais
- registrou o fechamento final em `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`

## 2026-03-12 - production guides public api deepening
- auditou a superficie publica real de producao em Navigation, SceneFlow, WorldLifecycle, LevelFlow, GameLoop, Gameplay e assets canonicos atuais
- aprofundou o manual operacional com servicos publicos, contratos de configuracao, IntroStage, ActorGroupRearm e exemplos curtos reais de codigo
- aprofundou a referencia de hooks com casos reais de uso, exemplos de assinatura via `EventBus<T>` e separacao mais clara entre hooks operacionais e hooks tecnicos
- refinou as versoes HTML para refletir melhor o uso de producao e registrou a auditoria em `Docs/Reports/Audits/2026-03-12/PRODUCTION-GUIDES-PUBLIC-API-DEEPENING.md`

## 2026-03-12 - visual guides layer
- refinou os dois guias em Markdown com exemplos minimos de codigo usando servicos e metodos reais do runtime atual
- criou `Docs/Guides/Manual-Operacional.html` e `Docs/Guides/Hooks-Reference.html` como camada visual estatica para leitura humana
- atualizou `Docs/README.md` para linkar os guias canonicos em Markdown e as versoes HTML
- manteve o Markdown como fonte canonica e o HTML apenas como camada de apresentacao

## 2026-03-12 - production guides editorial refinement
- simplificou a linguagem dos dois guias para leitura mais facil por pessoas com pouco conhecimento de programacao
- adicionou tabelas, blocos de consulta rapida, exemplos curtos e checklist mais visivel
- melhorou a navegacao no `Docs/README.md` sem mudar o conteudo tecnico dos guias
- registrou o refinamento editorial em `Docs/Reports/Audits/2026-03-12/PRODUCTION-GUIDES-EDITORIAL-REFINEMENT.md`

## 2026-03-12 - hooks docs refinement
- refinou `Docs/Guides/Production-How-To-Use-Core-Modules.md` com uma secao curta de fluxos mais comuns
- reorganizou `Docs/Guides/Event-Hooks-Reference.md` em hooks operacionais recomendados e hooks tecnicos do pipeline
- deixou explicito na docs que os nomes e a API dos eventos nao foram refatorados nesta rodada
- registrou a separacao operacional vs tecnica em `Docs/Reports/Audits/2026-03-12/HOOKS-DOC-REFINEMENT.md`

## 2026-03-12 - production usage guides
- criou `Docs/Guides/Production-How-To-Use-Core-Modules.md` com o trilho pratico atual de bootstrap, navigation, SceneFlow, LevelFlow, WorldLifecycle e ActorGroupRearm
- criou `Docs/Guides/Event-Hooks-Reference.md` com os hooks e eventos realmente publicados e uteis no runtime atual
- atualizou `Docs/README.md` e docs modulares com links curtos para os novos guias
- criou `Docs/Reports/Audits/2026-03-12/PRODUCTION-USAGE-GUIDES-CREATED.md` com fontes auditadas, criterios e exclusoes

## 2026-03-12 - docs cleanup / current-state-only
- reduziu a superficie documental para o estado operacional atual
- removeu planos, overviews, audits e evidence superseded da leitura principal
- promoveu uma unica cadeia oficial de docs centrada em bootstrap + RouteKind + direct-ref navigation/transition

## 2026-03-11
- rename de nomenclatura canonica: Gameplay RunRearm passa a se chamar Gameplay ActorGroupRearm em codigo e docs, sem mudanca de contrato ou comportamento
- coerencia de rename: tipos, arquivos, namespaces, composition root, participants e contratos associados foram renomeados de forma consistente para ActorGroupRearm
- docs sincronizados com o rename: ADR-0013, ADR-0014, ADR-0023, ADRs/README, Canon/Canon-Index, Plans/Plan-Continuous, Overview, Modules/Gameplay e relatorios correntes passaram a usar ActorGroupRearm como nomenclatura principal
- docs-only: higiene final remove residuos nominais correntes de `RunRearm` em docs ativos, preservando apenas referencias historicas rastreaveis

All notable documentation changes to NewScripts are documented in this file.

## 2026-02-25
- docs cleanup / retencao: removeu planos concluidos e snapshots antigos da navegacao principal, mantendo a trilha ativa em `Plan-Continuous`
- removeu versoes antigas de plano em `Docs/Overview/` e promoveu a versao mais recente para nome canonico
- atualizou indices e ponteiros para evitar referencias quebradas

## 2026-02-15
- F3 concluido: rota como fonte unica de `SceneData`
- plano Strings -> DirectRefs atualizado para politica strict fail-fast
- registrado como concluido o plano `SceneFlow-Navigation-LevelFlow-Refactor-Plan-v2.1.3`

## 2026-02-04
- ADR templates separados por tipo e ADRs alinhados ao template correspondente
- ADRs, overview e evidence latest atualizados com evidencia mais recente

## 2026-01-31
- consolidou `Overview/Architecture.md` + `Overview/WorldLifecycle.md` em `Overview/Overview.md`
- consolidou guias e checklists em `Guides.md`
- reorganizou a estrutura de `Docs/` e moveu auditorias para `Docs/Reports/Audits/<data>/`
- sincronizou ADRs e contrato de observabilidade com o snapshot canonico de 2026-01-31

## 2026-01-29
- Evidence/Baseline 2.2 atualizado com anchors e trechos canonicos do log
- ADR-0017 adicionado e `ADRs/README` atualizado

## 2026-01-28
- archived Baseline 2.2 evidence snapshot
- ADR-0012 e `Modules/WorldLifecycle` alinhados ao fluxo real de reset, skip e input mode

## 2026-01-27
- docs alinhadas ao baseline 2.0 e as fontes vigentes
- ADR-0012 atualizado para o fluxo canonico de post-gameplay

## 2026-01-21
- `ARCHITECTURE.md` e READMEs ajustados para terminologia consistente

## 2026-01-20
- plano 2.2 reordenado com QA separado para ContentSwap e Level
- contrato de observabilidade atualizado para ContentSwap + Level

## 2026-01-19
- indice de ADRs atualizado para refletir os novos escopos

## 2026-01-18
- novo snapshot de evidence 2026-01-18 com logs mesclados de Restart e ExitToMenu
- ADR-0012 e ADR-0015 atualizados para apontar para o novo snapshot

## 2026-01-16
- consolidou snapshot datado de evidencias em `Docs/Reports/Evidence/2026-01-16/`
- restaurou `Docs/Standards/Standards.md#observability-contract` como fonte de verdade

## 2026-01-15
- baseline 2.0 checklist ajustado para refletir a cobertura do log atual
- ADR-0016 refinado para explicitar o contrato operacional da IntroStage

## 2026-01-14
- ADR-0016 atualizado para consolidar IntroStage (PostReveal) como nomenclatura canonica

## 2026-01-13
- registro incremental de evidencias do Baseline 2.0 a partir do log usado como fonte de verdade

## 2026-01-05
- adicionou `IWorldResetRequestService` como gatilho de producao fora de transicao
- consolidou o conjunto canonico de documentacao para baseline 2.0

## 2026-01-03
- adicionou `Reports/Baseline-Audit-2026-01-03.md`
- atualizou `README.md`, `WORLD_LIFECYCLE.md`, ADR-0014 e docs de QA para alinhamento do reset por `ActorKind`

