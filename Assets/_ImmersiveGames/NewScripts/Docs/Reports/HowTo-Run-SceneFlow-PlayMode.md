# Como rodar o PlayMode test de Scene Flow (nativo + bridge)

Este teste valida o fluxo Started → ScenesReady → Completed do Scene Flow nativo e confirma que o bridge legado continua refletindo os eventos. Ele é automatizado, não exige interação manual e grava um relatório em `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-PlayMode-Result.md`.

## Pré-requisitos
- Definir o símbolo `NEWSCRIPTS_SCENEFLOW_NATIVE` no Player Settings ou via linha de comando. Sem esse define, o teste marca o resultado como **Inconclusive**.
- Garantir que as dependências padrão do projeto estejam presentes (Scenes, EventBus, DI).

## Executando no Editor (UI)
1. Abrir o Unity e o projeto.
2. Menu **Window → General → Test Runner** (ou **Test Framework**).
3. Selecionar a aba **PlayMode**.
4. Localizar `SceneFlowNativePlayModeTests.SceneFlowNative_ShouldExecuteSmokeAndReport`.
5. Executar o teste. O relatório Markdown será atualizado ao fim da execução.

## Executando em batchmode/CI
Exemplo de comando (ajuste caminhos conforme o agente de build):

```bash
/path/to/Unity -batchmode -nographics \
  -projectPath /workspace/GameJam2025 \
  -runTests -testPlatform PlayMode \
  -testResults ./SceneFlowPlayMode-results.xml \
  -testFilter SceneFlowNative_ShouldExecuteSmokeAndReport \
  -logFile ./SceneFlowPlayMode.log \
  -defineSymbols "NEWSCRIPTS_SCENEFLOW_NATIVE"
```

Observações:
- `-defineSymbols` garante que o Scene Flow nativo esteja ativo durante o teste.
- O relatório Markdown é escrito dentro do projeto em `Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-PlayMode-Result.md` contendo defines detectados, testers executados, resultado e trechos dos logs marcados com `[SceneFlowTest]`.
- Use `-nographics` em ambientes headless para evitar dependência de display.
