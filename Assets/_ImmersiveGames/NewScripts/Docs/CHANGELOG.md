# Changelog - Docs

## 2026-03-26 - modular composition closeout
- consolidou o ADR-0038 como decisão canônica final do pipeline modular em duas fases
- registrou a validação prática em `GameLoop`, `SceneFlow`, `Navigation`, `LevelFlow` e `WorldReset`
- adicionou o canon `Modular-Composition-Pipeline` e o guia `How-To-Add-A-New-Module-To-Composition`

## 2026-03-26 - baseline 3.5 closeout
- fechou a Baseline 3.5 como ponto de estabilizacao arquitetural de `NewScripts`
- consolidou o encerramento de `SceneFlow`, do stack macro e do stack de reset como base canonica para a proxima camada
- congelou os ADRs de `Actor Presentation` e de ownership global dos clusters de modulos como referencia de partida
- adicionou `Docs/Reports/Baseline-3.5.md` como documento central de encerramento desta etapa

## 2026-03-25 - reset stack consolidation closeout
- encerrou o ciclo `Plan-Reset-Stack-Consolidation.md` com status concluido
- registrou fechamento canonico do stack de reset: `WorldReset`, `SceneReset`, `ResetInterop`, `SceneFlow`, `Gameplay` e `SimulationGate`
- consolidou o vínculo entre a auditoria-base, os planos de macroflow/sceneflow e o outcome final do stack
- adicionou relatório curto de closure para rastreabilidade documental

## 2026-03-25 - macro flow stack consolidation closeout
- marcou `Plan-MacroFlow-Stack-Consolidation.md` como concluido com outcome final curto
- consolidou `SceneFlow` como owner da timeline macro e `GameLoop` com completion sync mais concentrado
- reforcou `ResetInterop` como ponte fina e `LevelFlow` como owner do contexto canonico de gameplay start/restart
- registrou `Navigation` como owner mais claro do macro dispatch e bridges residuais mais finos/consistentes

## 2026-03-25 - sceneflow refoundation closeout
- plano `Plan-SceneFlow-Refoundation.md` marcado como concluido
- `SceneFlow` mantido como owner da timeline macro; composicao tecnica removida do core macro
- policy strict/degraded de `Loading/Fade` alinhada ao baseline canonico
- sync de `GameLoop` removido do input bridge; `LevelPrepare/Clear` movido para `LevelFlow`
- lifecycle dos orchestrators de loading tornado explicito e previsivel

## 2026-03-13 - baseline v3 official freeze
- promoveu oficialmente o Baseline V3 para `PASS` com freeze canonico em `Docs/Reports/Evidence/LATEST.md`
- alinhou `README`, `Canon`, `LATEST`, `Plan-Continuous` e `CHANGELOG` para apontarem para o mesmo estado atual
- registrou que `Menu -> Gameplay` segue canonico, `Victory/Defeat` permanecem mockados de forma explicita e controlada, `PostGame` segue desacoplado de `IntroStage` e `Restart` continua fora do post
