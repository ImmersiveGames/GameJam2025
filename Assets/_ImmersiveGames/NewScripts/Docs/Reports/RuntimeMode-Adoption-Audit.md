# RuntimeMode Adoption Audit (template)

## Objetivo
Documentar onde o projeto usa (ou deveria usar) `IRuntimeModeProvider` e `IDegradedModeReporter`
após a introdução do `RuntimeModeConfig` (ADR-0008).

## Módulos a revisar
- SceneFlow (Loading/Fade/Transition)
- WorldLifecycle (policies / completion gate)
- GameLoop (start/intro/pause)
- Gameplay (run rearm / spawning)
- Levels (catalog / apply / dev)
- ContentSwap
- Navigation
- Gates
- ControlModes
- PostGame

## Checklist por módulo
Para cada módulo:
- [ ] Existe fallback “best-effort” que deveria virar DEGRADED_MODE?
- [ ] Usa `DegradedKeys.Feature/Reason` (padronizado)?
- [ ] Em Strict, há necessidade de elevar severidade?
- [ ] O módulo funciona se provider/reporter não estiver disponível?

## TODOs encontrados
- (preencher)
