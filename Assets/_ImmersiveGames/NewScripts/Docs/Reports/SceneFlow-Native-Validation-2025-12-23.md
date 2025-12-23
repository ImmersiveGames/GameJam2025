# Scene Flow — Validação do fluxo nativo (NEWSCRIPTS_SCENEFLOW_NATIVE)

> Ambiente: execução de auditoria em ambiente headless (sem Editor/Player). Não foi possível executar PlayMode para capturar logs em tempo real.

## Configuração aplicada
- Target/plataforma: `Standalone` (configuração de PlayerSettings atual).
- Scripting Define Symbols (Standalone): `DOTWEEN;NEWSCRIPTS_MODE;NEWSCRIPTS_SCENEFLOW_NATIVE` (ativado neste passo).
- Fonte: `ProjectSettings/ProjectSettings.asset` — chave `scriptingDefineSymbols`.

## Execução solicitada
- Testers: `SceneTransitionServiceSmokeQATester`, `LegacySceneFlowBridgeSmokeQATester`.
- Via runner recomendado: `NewScriptsInfraSmokeRunner` (`QA/Infra/Run All`).

## Resultado
- **Status geral**: ⚠️ Não executado neste ambiente (falta de Unity Editor/Player para PlayMode).
- Observação: a flag nativa foi habilitada; os testes devem ser rodados no Editor/CI com PlayMode para validar eventos `Started → ScenesReady → Completed` e coexistência com o bridge.

## Instruções para reprodução (em ambiente com Editor)
1) Abrir o projeto no Unity (target `Standalone`).
2) Confirmar em **Project Settings → Player → Other Settings → Scripting Define Symbols** (Standalone): `DOTWEEN;NEWSCRIPTS_MODE;NEWSCRIPTS_SCENEFLOW_NATIVE`.
3) Na cena de QA/Infra, adicionar/comprovar `NewScriptsInfraSmokeRunner` com `runSceneTransitionServiceTester=true`.
4) Entrar em Play Mode e acionar `QA/Infra/Run All` (ContextMenu) ou `runOnStart=true`.
5) Registrar logs esperados:
   - `SceneTransitionServiceSmokeQATester`: eventos `Started`, `ScenesReady`, `Completed`; integração com gate/readiness; sem exceções.
   - `LegacySceneFlowBridgeSmokeQATester`: recebimento dos mesmos eventos refletidos; mapeamento de contexto `ScenesToLoad/Unload`, `TargetActiveScene`, `UseFade`.
6) Anexar resultados (PASS/FAIL) e trechos de log no próximo update do relatório.

## Próximos passos sugeridos
- Rodar os testers acima em ambiente com Unity e anexar logs reais.
- Se PASS: avaliar remoção progressiva do bridge conforme `Docs/ADR/ADR-0001-NewScripts-Migracao-Legado.md#bridges-temporários-legacysceneflowbridge`.
- Se FAIL: capturar stack/log e reabrir investigação no SceneTransitionService ou adapters.
