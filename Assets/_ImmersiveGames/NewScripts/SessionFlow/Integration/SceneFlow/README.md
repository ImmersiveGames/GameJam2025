# SessionFlow / Integration / SceneFlow

`SessionOperationalStartupRouteAdapter` e os demais bridges deste diretório observam o fluxo legado de `Boot -> Menu` e `Menu -> Sandbox`.

O `GameLoop` conceitual fica restrito ao executor técnico de estado. Este adapter de startup route e temporario e sera rebaixado/removido quando o `SessionOperationalPipeline` assumir ownership real da rota.
