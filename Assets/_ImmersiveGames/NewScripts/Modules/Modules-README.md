# Organização de `Modules/` (NewScripts)

Convenção para organizar **features** em `Assets/_ImmersiveGames/NewScripts/Modules/`.

## Objetivo
- Cada feature deve ser **descobrível** por pasta e por nome.
- Evitar “caçar” partes de um mesmo sistema em múltiplas raízes.

## Regras (curtas e objetivas)
1) **Uma feature = um módulo**: tudo de uma feature vive em `Modules/<Feature>/`.
2) **Unity-facing separado**: `MonoBehaviour`/`ScriptableObject` ficam em `Bindings/` (ou `Dev/Bindings`).
3) **Runtime puro**: services/contratos/eventos/policies/validators ficam em `Runtime/`.
4) **Integração explícita**: bridges/adapters/drivers ficam em `Interop/` (quando houver volume) ou sufixados `*Bridge/*Adapter/*Driver`.
5) **Dev (não QA)**: ferramentas de teste/debug ficam em `Dev/` (Editor-only em `Dev/Editor`).
6) **Menos pastas possível**: módulos pequenos podem ficar “flat”, mas mantendo `Bindings/` e `Dev/` separados quando existirem.

## Estrutura padrão (referência)
```
Modules/<Feature>/
  Runtime/      # serviços, contratos, eventos (código puro)
  Bindings/     # MonoBehaviour / ScriptableObject / glue Unity
  Interop/      # integração com outros módulos (opcional)
  Dev/          # ContextMenu, hotkeys, seeders, debug
    Editor/     # tooling editor-only (quando necessário)
  Content/      # assets/prefabs/configs (quando necessário)
```

## Convenções de nomes
- Pastas: evitar termos genéricos e repetidos (`Core`, `Presentation`) e evitar palavras que causam duplicação no projeto (`Unity`, `Camera`, `Input`).
  - Preferir: `View`, `InputModes`, `Bindings`.
- Arquivos:
  - `I*.cs` contém **apenas a interface** (enums/structs vão para `*Contracts.cs` ou arquivo próprio).
  - Integração: `*Bridge`, `*Adapter`, `*Driver`, `*Coordinator`.
  - Dev tools: usar `Dev` no nome (evitar `Qa`).

## Migração segura (Unity)
- Mover/renomear via Unity (ou `git mv`) preservando `.meta`.
- Evitar mudar namespace de tipos serializados (MB/SO) durante reorganização.
- Evitar “recriar arquivo” (preservar encoding/line-endings).

## Índice de módulos (exemplos)
- `SceneFlow/`: transição de cena, fade, loading HUD, readiness.
- `WorldLifecycle/`: rearm/reset do mundo, integração com SceneFlow, spawn pipeline.
- `GameLoop/`: loop de jogo, IntroStage, Pause, comandos.
- `Gameplay/`: atores, ações, spawning, view, rearm de run.
- `Levels/`: catálogo/definições, sessão, aplicação via ContentSwap.
- `ContentSwap/`: troca de conteúdo (in-place) + contratos/eventos.
- `Navigation/`: navegação Menu↔Gameplay + binders de UI.
- `Gates/`: simulation gates e bridges.
- `InputModes/`: input/control modes + bridge com SceneFlow.
- `PostGame/`: overlay + ownership do pós-jogo.

## Checklist de fechamento do módulo
- [ ] Unity-facing em `Bindings/` (ou `Dev/Bindings`).
- [ ] Dev tools em `Dev/` (e editor-only em `Dev/Editor`).
- [ ] `I*.cs` sem tipos auxiliares “soltos”.
- [ ] Bridges/Adapters/Drivers explícitos (`Interop/` ou sufixos).
- [ ] Nomes refletem conteúdo (sem pastas “misteriosas”).
