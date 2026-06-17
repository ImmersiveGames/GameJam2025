# ATTR-HUD-4C — Actor Attribute HUD Binding Closure

Pacote documental de fechamento da frente `ATTR-HUD`.

## Conteúdo

- `ATTR-HUD-4C-Closure.md` — fechamento arquitetural e status dos cortes.
- `ActorAttribute-HUD-Setup-Guide.md` — receita prática para configurar a `UIScene`.
- `ATTR-HUD-Smoke-Criteria.md` — critérios de smoke/PASS e eventos esperados.
- `ATTR-HUD-Ownership-Matrix.md` — matriz de ownership e fronteiras.

## Status

`ATTR-HUD-4B` foi aceito como PASS funcional para o caso inicial:

```text
PrimaryPlayer / actor.attribute.health / ActorAttributeImageFillSink / Image.fillAmount
```

O fluxo final validado é:

```text
UIScene
-> SceneActorAttributeUiBindingRequestProvider
-> LoadedSceneActorAttributeUiBindingRequestProvider
-> ActivityEntryActorAttributeUiBindingStage
-> ActivityEntryActorAttributeUiTargetResolver
-> ActorAttributeUiBindingRuntime
-> ActorAttributeEventStream
-> ActorAttributeImageFillSink
```
