# Baseline 2.2 — Evidence Snapshot — 2026-01-31

## Context

- **Date:** 2026-01-31
- **Scope:** captura parcial (trecho colado no chat) para registrar que o boot do NewScripts e a infra global iniciam corretamente.
- **Nota:** este snapshot **não inclui o log completo**, então não dá para afirmar evidência forte para ADR-0017/0018/0019 (LevelManager/ContentSwap/Promoção). Ele existe para manter rastreabilidade do trecho recebido.

## Provided excerpt (raw)

```text
NEWSCRIPTS_MODE ativo: EventBusUtil.InitializeEditor ignorado.

NEWSCRIPTS_MODE ativo: DebugUtility.Initialize executando reset de estado.

[INFO] [DebugUtility] DebugUtility inicializado antes de todos os sistemas.

[INFO] [AnimationBootstrapper] NEWSCRIPTS_MODE ativo: AnimationBootstrapper ignorado.

NEWSCRIPTS_MODE ativo: DebugUtility.Initialize ignorado.

[INFO] [DependencyBootstrapper] NEWSCRIPTS_MODE ativo: ResetStatics ignorado.

[VERBOSE] [GlobalBootstrap] NewScripts logging configured.

[VERBOSE] [SceneServiceCleaner] SceneServiceCleaner inicializado.

[VERBOSE] [DependencyManager] DependencyManager inicializado (DontDestroyOnLoad).
```

## What this excerpt proves (soft)

- Boot do **NewScripts logging** executa
- **SceneServiceCleaner** inicializa
- **DependencyManager** inicializa em **DontDestroyOnLoad**

## Missing anchors (required for ADR-0017/0018/0019 acceptance)

Para fechar evidência hard (aceitação) para ADR-0017/0018/0019, o log completo precisa conter pelo menos:

- `[LevelManagerInstaller] Installing ...` (ou equivalente: registro do LevelManager + providers)
- evidência de **ContentSwap** (evento/bridge/uso) quando aplicável
- evidência do fluxo/caso da **Promoção Baseline 2.2** (se ADR-0019 depender disso)

---

### How to upgrade this snapshot

Quando o log completo estiver disponível:
1) colar o log integral neste arquivo (ou linkar um `.log` arquivado no mesmo snapshot),  
2) preencher a seção “Anchors found” com os trechos mínimos,  
3) promover este snapshot para “hard evidence” (ou criar um `ADR-00xx-Acceptance-YYYY-MM-DD.md` específico).
