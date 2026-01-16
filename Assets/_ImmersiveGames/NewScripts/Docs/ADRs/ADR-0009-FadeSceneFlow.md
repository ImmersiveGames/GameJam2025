# ADR-0009 — Fade + SceneFlow (NewScripts)

## Status
- Estado: Implementado
- Data: 2025-12-24
- Escopo: SceneFlow + Fade + Loading HUD (NewScripts)

## Contexto

O pipeline NewScripts precisava:

- Aplicar **FadeIn/FadeOut** durante transições do SceneFlow.
- Evitar dependência do fade legado.
- Permitir configurar timings por **profile** (startup/frontend/gameplay).
- Coordenar Fade com **Loading HUD** para manter UI consistente (Loading só aparece após FadeIn e some antes do FadeOut).

## Decisão

1. Expor `INewScriptsFadeService` no **DI global**.
2. Integrar o fade ao `ISceneTransitionService` via adapter (`NewScriptsSceneFlowFadeAdapter`).
3. Resolver timings de fade por `NewScriptsSceneTransitionProfile`, carregado via `Resources` em:
   - `SceneFlow/Profiles/<profileName>`
4. Implementar o Fade como uma cena additive (`FadeScene`) com controlador (`NewScriptsFadeController`) que opera via `CanvasGroup`.
5. Integrar o pipeline de transição com `SceneFlowLoadingService` (LoadingHUD) para manter o ordering
   após o `FadeIn` e antes do `FadeOut`.

## Fora de escopo

- Compatibilidade com fade legado.

## Consequências

### Benefícios

- O SceneFlow se torna independente do fade legado.
- A duração do fade é declarativa por profile (sem strings espalhadas além do ponto de resolução).
- O Loading HUD pode ser sincronizado com fases explícitas do SceneFlow:
  - `AfterFadeIn` → `Show`
  - `BeforeFadeOut` → `Hide`
  - `Completed` → safety hide
- Fade e Loading HUD ficam integrados ao `SceneTransitionService` por adapters dedicados
  (`NewScriptsSceneFlowFadeAdapter` + `SceneFlowLoadingService`), mantendo responsabilidades separadas.

### Trade-offs / Riscos

- (não informado)

## Notas de implementação

Evidências observadas:

- O DI global registra `INewScriptsFadeService`.
- O resolver informa:
  - `Profile resolvido: name='<profile>', path='SceneFlow/Profiles/<profile>'`
- O adapter aplica timings:
  - `fadeIn=0,5` e `fadeOut=0,5` (exemplo do log)
- O fade carrega a `FadeScene` additive quando necessário e encontra `NewScriptsFadeController`.
- O canvas de fade opera com sorting alto (ex.: `sortingOrder=11000`) para sobrepor UI durante transição.

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot arquivado (2026-01-16): [`Baseline-2.1-ContractEvidence-2026-01-16.md`](../Reports/Evidence/2026-01-16/Baseline-2.1-ContractEvidence-2026-01-16.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [ADR-0010 — Loading HUD + SceneFlow (NewScripts)](ADR-0010-LoadingHud-SceneFlow.md)
- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
