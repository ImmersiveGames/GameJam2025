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

### Objetivo de produção (sistema ideal)

Garantir que TODA transição de cena do SceneFlow tenha um envelope visual determinístico (fade-out → trabalho de transição → fade-in), com ordem consistente e sem flicker.

### Contrato de produção (mínimo)

- Fade-out inicia **antes** de descarregar/carregar cenas (ou qualquer mutação visual).
- Transição de cena só é considerada `Completed` após o fade-in (quando aplicável) ou após liberar o gate visual.
- Serviço de fade não cria UI 'em voo' em produção: depende de prefab/scene bootstrap (fail-fast).
- O contrato deve ser observável via logs/âncoras (ver Observability Contract).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- Criar automaticamente canvas/câmera de fade quando ausente (preferir erro explícito).
- Normalizar o visual do loading/HUD (isso é ADR-0010).

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

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- SceneFlow chama FadeService (ou equivalente) em todas as rotas (Boot→Menu, Menu→Gameplay, ExitToMenu, Restart, etc.).
- Gating/ordem: fade-out antes de mutações; fade-in somente após `ScenesReady`.
- Evidência: logs mostram o envelope de fade em transições críticas (ou ADR permanece 'Parcial').

## Notas de implementação

Evidências observadas:

- O DI global registra `INewScriptsFadeService`.
- O resolver informa:
  - `Profile resolvido: name='<profile>', path='SceneFlow/Profiles/<profile>'`
- O adapter aplica timings:
  - `fadeIn=0,5` e `fadeOut=0,5` (exemplo do log)
- O fade carrega a `FadeScene` additive quando necessário e encontra `NewScriptsFadeController`.
- O canvas de fade opera com sorting alto (ex.: `sortingOrder=11000`) para sobrepor UI durante transição.

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - TODO: definir âncoras de log para FadeOut/FadeIn (não aparecem na evidência canônica atual).
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Standards/Observability-Contract.md)

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot  (2026-01-17): [`Baseline-2.1-Evidence-2026-01-17.md`](../Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md)
- Contrato: [`Observability-Contract.md`](../Standards/Observability-Contract.md)

## Referências

- [ADR-0010 — Loading HUD + SceneFlow (NewScripts)](ADR-0010-LoadingHud-SceneFlow.md)
- [Overview/WorldLifecycle.md](../Overview/WorldLifecycle.md)
- [Observability-Contract.md](../Standards/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
- [`Observability-Contract.md`](../Standards/Observability-Contract.md)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
