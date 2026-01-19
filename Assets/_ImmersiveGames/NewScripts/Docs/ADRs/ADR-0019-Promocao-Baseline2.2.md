# ADR-0019 — Level Manager (progressão de níveis) e Promoção do Baseline 2.2

## Status
- Estado: Proposto
- Data: 2026-01-18
- Escopo: Level Manager + Promoção Baseline 2.2 (Docs/Reports/Evidence)

## Contexto

O Baseline 2.2 separa claramente **ContentSwap (Phase)** de **Level/Nível**. ContentSwap é troca de conteúdo; Level é progressão do jogo. Essa separação evita ambiguidade e permite evolução da arquitetura sem quebrar contratos atuais.

O **Level Manager** passa a ser o orquestrador da progressão de níveis, decidindo quando executar ContentSwap e **IntroStage**, com política explícita e evidência de QA.

## Decisão

### 1) Escopo do Level Manager
- **Orquestra ContentSwap + IntroStage**.
- **ContentSwap** segue o contrato do ADR-0018 (modos no ADR-0017).
- **IntroStage** é responsabilidade do Level Manager, não do ContentSwap.

### 2) Política padrão (Baseline 2.2)
- **Toda mudança de nível executa IntroStage** (policy default).
- Exceções devem ser declaradas explicitamente por configuração (não neste baseline).

### 3) Dependências explícitas (runtime atual)
O runtime já contém pontos de integração relevantes que o Level Manager deverá respeitar:
- `PhaseStartPipeline` e `PhaseStartPhaseCommitBridge` (IntroStage acionada em `PhaseCommitted` e coordenada com SceneTransition).
- `PhaseTransitionIntentWorldLifecycleBridge` (commit após reset).

Essas dependências **não mudam neste baseline**, mas passam a ser orquestradas pelo Level Manager.

### 4) Gates de promoção do Baseline 2.2
A promoção do Baseline 2.2 ocorre quando **todos os gates abaixo estiverem PASS**, com QA objetivo e evidência em snapshot datado.

#### G-01 — ContentSwap formalizado (contrato + observability)
**Critério verificável**
- ADR-0018 atualizado e sem conflito com ADR-0017.
- Logs canônicos de ContentSwap (PhaseChangeRequested / PhasePendingSet / PhaseCommitted) permanecem válidos.

**QA mínimo (ContextMenu)**
- `QA/ContentSwap/InPlace/Commit (NoVisuals)`
- `QA/ContentSwap/WithTransition/Commit (Gameplay Minimal)`

**Evidência**
- Snapshot datado com logs e verificação curada das duas rotas.

#### G-02 — Level Manager mínimo funcional
**Critério verificável**
- Mudança de nível aciona ContentSwap + IntroStage, de acordo com política default.
- IntroStage executa exatamente uma vez por mudança de nível.

**QA mínimo (ContextMenu)**
- `QA/Level/Advance/IntroStage (Default)`

**Evidência**
- Snapshot datado com logs mostrando ContentSwap + IntroStage no mesmo ciclo.

#### G-03 — Configuração centralizada (assets/definitions)
**Critério verificável**
- Configuração de níveis e conteúdo migra de scripts para assets/definitions.
- Nenhum script de runtime mantém “hardcode” de lista de níveis.

**QA mínimo (ContextMenu)**
- `QA/Level/Resolve/Definitions` (apenas logs de catálogo/resolução).

**Evidência**
- Logs mostrando resolução por catálogo + assinatura de conteúdo.

#### G-04 — QA + Evidências + Gate de promoção
**Critério verificável**
- Evidências consolidadas em snapshot datado (`Docs/Reports/Evidence/<YYYY-MM-DD>/`).
- `Docs/Reports/Evidence/LATEST.md` apontando para o snapshot.
- `Docs/CHANGELOG-docs.md` atualizado com os gates fechados.

## Consequências

### Benefícios
- Clarifica responsabilidades: Level Manager (progressão) vs ContentSwap (troca de conteúdo).
- Mantém compatibilidade com APIs atuais.
- Promoção com critérios objetivos e verificáveis.

### Trade-offs / Riscos
- Exige disciplina de QA e evidência para avançar o baseline.

## Evidências
- Metodologia: `Docs/Reports/Evidence/README.md`
- Ponte canônica: `Docs/Reports/Evidence/LATEST.md`

## Referências
- ADR-0018 — ContentSwap (Phase) — Contrato e Observability
- ADR-0017 — Tipos de troca de fase (In-Place vs SceneTransition)
- Plano 2.2 — Execução (plano2.2.md)
