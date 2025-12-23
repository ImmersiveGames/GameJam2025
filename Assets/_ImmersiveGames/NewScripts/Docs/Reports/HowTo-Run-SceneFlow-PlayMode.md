# Como rodar o Smoke de Scene Flow (nativo + bridge) sem NUnit

O smoke `SceneFlowPlayModeSmokeBootstrap` valida o fluxo Started → ScenesReady → Completed do Scene Flow nativo e confirma que o bridge legado continua refletindo os eventos. Ele roda em PlayMode/CI sem NUnit e grava relatório em `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Smoke-Result.md`.

## Pré-requisitos
- Definir o símbolo `NEWSCRIPTS_SCENEFLOW_NATIVE` no Player Settings ou via linha de comando. Sem esse define, o resultado fica **INCONCLUSIVE**.
- Garantir que a cena carregada contenha (ou instancie) `SceneFlowPlayModeSmokeBootstrap`.

## Executando no Editor (PlayMode)
1. Abra o projeto no Unity.
2. Em uma cena de QA, adicione o componente `SceneFlowPlayModeSmokeBootstrap` (ou use uma cena que já o possua).
3. Entre em PlayMode. O bootstrap executará automaticamente, aguardará até 30s e escreverá o relatório Markdown.

## Executando em batchmode/CI
Exemplo de comando (ajuste paths conforme o agente):

```bash
/path/to/Unity -batchmode -nographics \
  -projectPath /workspace/GameJam2025 \
  -executeMethod _ImmersiveGames.NewScripts.Infrastructure.QA.SceneFlowPlayModeSmokeBootstrapCI.Run \
  -logFile ./SceneFlowSmoke.log \
  -defineSymbols "NEWSCRIPTS_SCENEFLOW_NATIVE"
```

Notas:
- Certifique-se de que a cena aberta no batch possui o `SceneFlowPlayModeSmokeBootstrap` na hierarquia (pode ser via cena dedicada de QA).
- O bootstrap define `Environment.ExitCode` (0=PASS, 2=FAIL, 3=INCONCLUSIVE) e chama `Application.Quit` em batchmode.
- O relatório contém defines detectados, resultado e até 30 logs marcados com `[SceneFlowTest]`.
