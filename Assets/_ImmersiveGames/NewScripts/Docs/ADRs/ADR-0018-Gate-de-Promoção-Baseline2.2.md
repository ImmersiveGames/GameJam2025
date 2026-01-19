# ADR-0018 — Gate de Promoção do Baseline 2.2 (WorldCycle Config-Driven)

## Status
- Estado: Aceito
- Data: 2026-01-18
- Escopo: Baseline 2.1 → 2.2 (Docs + Phases + Observability)

## Contexto

O Baseline 2.1 foi estabilizado e validado via snapshot datado (evidência canônica). A evolução planejada para o Baseline 2.2 introduz uma camada **config-driven** (WorldCycle) para controlar a evolução do jogo (fases/níveis) sem regressões no pipeline canônico já validado:

`SceneFlow → ScenesReady → WorldLifecycleResetCompleted → Completion Gate → FadeOut/Completed`

O risco principal do 2.2 não é “adicionar feature”, e sim **criar ambiguidade** (ex.: In-Place parecendo SceneFlow; reasons conflitantes; docs drift). Para evitar regressões silenciosas, definimos um conjunto de **gates objetivos** (checáveis por evidência) que precisam estar PASS antes de declarar “Baseline 2.2 promovido”.

## Decisão

Adotar os gates abaixo como critério de entrada para promoção do Baseline 2.2.

### G-01 — Phases (Nível B) “fechado”: contrato visual do In-Place

**Aceite**
- `PhaseChange/In-Place`:
    - não usa SceneFlow;
    - não exibe Loading HUD;
    - e o teste padrão de evidência ocorre **sem Fade/HUD** (sem ruído visual).
- QA de In-Place não pode habilitar opções visuais que violem este contrato.

**Evidência exigida (snapshot datado)**
- Log bruto (Console) + verificação curada com âncoras que provem:
    - ausência de logs de Fade/HUD entre “solicitação” e “PhaseCommitted”; e
    - ordem: `PhasePendingSet → Reset → PhaseCommitted`.
- Atualização do `Checklist-phase.md` marcando o item 2.1 como PASS com referência ao snapshot.

### G-02 — Observability: coerência de reasons e links do contrato

**Aceite**
1) **Link válido**: `Observability-Contract.md` não pode referenciar arquivos inexistentes.
- Opção A: criar `Docs/Reports/Reason-Map.md` (índice/glossário), ou
- Opção B: remover/ajustar a referência para apontar apenas para artefatos canônicos existentes.

2) **Rule-of-truth para reasons** (WorldLifecycle/Reset)
- Deve existir **regra oficial** (documentada) para:
    - `reason` do `WorldLifecycleResetCompletedEvent`, e
    - `reason` usado nos logs do driver/controller.
- Se driver e evento usam reasons diferentes, isso deve estar explicitamente documentado.

**Evidência exigida (snapshot datado)**
- Âncoras mostrando:
    - `WorldLifecycleResetCompletedEvent(signature, reason)` no formato considerado canônico; e
    - invariantes do pipeline preservados.

### G-03 — Docs sem drift (links corretos e IDs consistentes)

**Aceite**
- `Docs/ARCHITECTURE.md`:
    - referências corretas ao fechamento do Baseline 2.0 (`ADR-0015`);
    - nenhum link quebrado para relatórios removidos/obsoletos (restaurar ou ajustar).
- `Docs/README.md`:
    - descrições alinhadas com a integração runtime vigente (sem afirmar “componente principal” quando já foi substituído por driver/bridge atual).

**Evidência exigida**
- Commit/PR documental (sem mudança de runtime) com links corrigidos e referência para `Evidence/LATEST.md`.

### G-04 — Trigger de ResetWorld em produção (condicional)

**Só é obrigatório** se o Baseline 2.2 depender de resets fora do SceneFlow.

**Aceite**
- Existe caminho de produção para disparar reset direto com reason canônico `ProductionTrigger/<source>`.
- É best-effort: não trava o fluxo e sempre emite `WorldLifecycleResetCompletedEvent`.

**Evidência exigida (snapshot datado)**
- Âncora mostrando `ProductionTrigger/<source>` e o `ResetCompleted` correspondente.

## Fora de escopo
- Declarar a promoção em si (isso é coberto no ADR-0019).
- Implementação do WorldCycle (2.2). Este ADR define o *gate*, não o *feature set*.

## Consequências

### Benefícios
- Promoção baseada em critério verificável, reduzindo regressões por interpretação.
- Evita drift documental e divergência de reasons antes de adicionar a camada WorldCycle.

### Trade-offs / Riscos
- Exige disciplina de evidência (snapshot datado + verificação curada) antes de “avançar”.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`
- Snapshot vigente (2.1): `Docs/Reports/Evidence/2026-01-18/Baseline-2.1-Evidence-2026-01-18.md`

## Referências
- ADR-0015 — Baseline 2.0: Fechamento Operacional
- ADR-0016 — Phases + modos de avanço + IntroStage opcional
- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- Checklist-phase.md
