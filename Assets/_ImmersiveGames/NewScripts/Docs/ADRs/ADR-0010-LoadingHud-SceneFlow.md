# ADR-0010 — Loading HUD + SceneFlow

- **Status:** Aceito (Baseline 2.2)
- **Data:** 2026-01-31
- **Owner:** NewScripts / Infrastructure
- **Relacionado:** ADR-0009 (Fade + SceneFlow), ADR-0013 (Ciclo de Vida)

## Contexto

O SceneFlow (transições de cena) já possui:
- envelope de **Fade** determinístico (ADR-0009)
- **completion gate** para aguardar `ResetCompleted` quando aplicável

Faltava um HUD simples de loading para:
- sinalizar transição de cena (principalmente em builds)
- manter o envelope observável e determinístico
- não “misturar UI” dentro da infra do SceneFlow

O risco identificado em auditorias: Loading HUD existia, mas estava **frágil** (fallback silencioso, logs pouco canônicos e responsabilidade espalhada).

## Decisão

Adotar um módulo mínimo e modular para Loading HUD:

1) **Infra (SceneFlow)** define apenas o contrato + orquestração de fases:
- `ILoadingHudService` (contrato mínimo)
- `SceneFlowLoadingService` (orquestra Started/FadeInCompleted/ScenesReady/BeforeFadeOut/Completed)

2) **Presentation (UI)** implementa a UI do HUD:
- `LoadingHudService` (concrete: carrega cena additiva + resolve controller + aplica Show/Hide)
- `LoadingHudController` (MonoBehaviour na cena `LoadingHudScene`)

3) Política de runtime:
- **Strict (Editor/Dev):** falhas são “gritantes” (log de erro + `Debug.Break()` + exception quando possível)
- **Release:** falhas degradam explicitamente com `DEGRADED_MODE` (feature=`loadinghud`) e o jogo continua **sem HUD**

## Escopo

Inclui:
- carregamento additivo da cena `LoadingHudScene`
- show/hide do HUD alinhado ao envelope do SceneFlow e ao Fade
- observabilidade com `signature` + `phase`

Não inclui:
- barras de progresso reais
- tracking de assets/Addressables
- “telas de loading” por feature (apenas HUD genérico)

## Contrato de Execução (ordem)

Para uma transição com Fade (`UseFade=True`):

1. `SceneTransitionStarted`  
   → `SceneFlowLoadingService` chama **Ensure** (garantir cena/controller, sem mostrar ainda)

2. `FadeInCompleted`  
   → `LoadingHUD.Show(signature, phase='AfterFadeIn')`

3. `ScenesReady`  
   → `LoadingHUD.Show(signature, phase='ScenesReady')` (mantém visível enquanto o gate conclui)

4. `BeforeFadeOut`  
   → `LoadingHUD.Hide(signature, phase='BeforeFadeOut')`

5. `TransitionCompleted`  
   → **Safety hide** `LoadingHUD.Hide(signature, phase='Completed')`

Observação: para transições sem Fade, o serviço ainda pode ser chamado; a fase “AfterFadeIn” continua sendo um marco lógico (mesmo que FadeIn seja 0).

## Política Strict/Release (fail-fast vs degraded)

### Strict (Editor/Dev)
- Se `LoadingHudScene` **não** estiver no Build Settings: falha imediata (antes de awaits) para permitir **fail-fast**.
- Se `LoadingHudController` não for encontrado após tentativas: log de erro + `Debug.Break()` (e exception quando aplicável).

### Release
- Se falhar carregar a cena, resolver controller ou aplicar UI:
  - emitir `DEGRADED_MODE feature='loadinghud' ...`
  - desabilitar o HUD para evitar spam
  - SceneFlow segue normalmente (sem bloquear transição)

## Observabilidade (âncoras)

Requisito: logs devem ser correlacionáveis por `signature` (SceneFlow) e `phase`.

Âncoras canônicas (mínimas):
- `[OBS][LoadingHUD] EnsureLoadScene signature='...' scene='LoadingHudScene' ...`
- `[OBS][LoadingHUD] EnsureReady signature='...' ...`
- `[OBS][LoadingHUD] ShowApplied signature='...' phase='...'`
- `[OBS][LoadingHUD] HideApplied signature='...' phase='...'`
- `DEGRADED_MODE feature='loadinghud' reason='...' signature='...'` (Release)

## Arquitetura / Separação

### Infra (não-UI)
- `NewScripts/Infrastructure/SceneFlow/Loading/ILoadingHudService.cs`
- `NewScripts/Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs`

### Presentation (UI)
- `NewScripts/Presentation/LoadingHud/LoadingHudService.cs`
- `NewScripts/Presentation/LoadingHud/LoadingHudController.cs`

### DI / Bootstrap
- `NewScripts/Infrastructure/GlobalBootstrap.cs` registra:
  - `ILoadingHudService` → `LoadingHudService(runtimePolicy + degradedReporter)`
  - `SceneFlowLoadingService` (listener de SceneFlow)

## Consequências

**Prós**
- SRP: SceneFlow não conhece UI concreta; UI não depende do loader do SceneFlow
- Observabilidade padronizada via `signature + phase`
- Fail-fast em dev/QA; Release degrada de forma explícita

**Contras**
- “LoadingHudScene” precisa existir em Build Settings
- A busca do controller usa `FindAnyObjectByType` (custo pequeno, mas é UI pouco frequente)

## Evidência (Baseline 2.2)

Fonte canônica diária:
- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

Evidência esperada:
- logs do envelope com `SceneTransitionStarted` + `FadeInCompleted` + `ScenesReady` + `BeforeFadeOut` + `TransitionCompleted`
- logs de Loading HUD com `signature` + `phase`
- `DEGRADED_MODE feature='loadinghud' ...` quando HUD não puder ser carregado em Release

