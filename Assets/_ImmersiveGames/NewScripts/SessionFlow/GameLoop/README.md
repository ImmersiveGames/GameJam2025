# GameLoop

`GameLoop` aqui e apenas o executor tecnico de estado.

Separacao atual:
- `GameLoopCoreInstaller`: core do loop e contratos diretos.
- `RunPipelineBridgeInstaller`: integracao de servicos do Run Pipeline.
- `RunPipelineRuntimeBridgeComposer`: adapter temporario Unity-driven do `GameRunEndedEventBridge`.
- `SessionOperationalStartupRouteInstaller`: adapter temporario de startup route do SessionOperationalPipeline.

`GameLoopInstaller` e `GameLoopBootstrap` permanecem como agregadores temporarios durante a migracao Base 1.1.
Nao devem ser tratados como owners semanticos de rota, session, activity, run ou IntroStage.
O `GameLoopCore` nao e owner de `Pause`, `Run Pipeline` nem de `IntroStage`.

Notas de migracao:
- `GameLoopInputCommandBridge` foi removido e nao deve ser reintroduzido como bridge ativa legada.
- `Pause`/`Resume` canonicos pertencem ao `SessionActivityPipeline`.
- `Play` e `navigation` futuros devem entrar por um producer canonico de `Navigation`/`SessionOperational`.
- `Run outcome` pertence ao `Run Pipeline`.
- A UI de pause legada foi removida deste rail e sera recriada depois em formato canonico.
