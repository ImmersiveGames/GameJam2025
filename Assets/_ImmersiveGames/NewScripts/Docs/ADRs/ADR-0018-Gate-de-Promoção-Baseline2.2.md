# Gate de Promoção — Baseline 2.2 (WorldCycle Config-Driven)

## Status

* Estado: **Rascunho**
* Data: **2026-01-17**
* Referências canônicas:

    * Contrato: `Docs/Reports/Observability-Contract.md`
    * Evidência vigente: `Docs/Reports/Evidence/LATEST.md`
    * ADRs: `Docs/ADRs/ADR-0015`, `ADR-0016`, `ADR-0017`
    * Checklist de fases: `Docs/ADRs/Checklist-phase.md`
    * Visão arquitetural: `Docs/ARCHITECTURE.md`

---

## Objetivo

Definir **critérios objetivos (gates)** para declarar o repositório “pronto para subir” do **Baseline 2.1** para o **Baseline 2.2**, onde o foco do 2.2 é iniciar a centralização da evolução do jogo via configuração (WorldCycle) **sem regressões** no pipeline canônico:

`SceneFlow → ScenesReady → WorldLifecycleResetCompleted → Gate → FadeOut/Completed`

---

## O que o Baseline 2.2 NÃO é (para reduzir escopo)

Para evitar “escopo invisível”, o 2.2 **não exige** (a menos que explicitado nos gates abaixo):

* Navegação 100% data-driven substituindo `GameNavigationCatalog`
* Scene recipes completos por fase (load/unload/active) como fonte única de verdade
* Refatoração grande de GameLoop/PostGame além do que já está estável em evidência 2.1

---

## Gates (obrigatórios)

### G-01 — Nível B de Phases “fechado” (contrato visual do In-Place)

**Motivação**
O `Checklist-phase.md` declara que o sistema funciona, mas há um **blocker** para fechar o Nível B corretamente: o **In-Place não pode virar “transição completa disfarçada”**.

**Critério de aceite**

* O caminho **PhaseChange/In-Place**:

    * **não usa SceneFlow**;
    * **não exibe Loading HUD** (conforme ADR-0017);
    * e, para o “contrato de produto” do checklist, o **teste padrão** de In-Place deve ocorrer **sem Fade/HUD** (sem ruído visual).
* O QA de In-Place não pode habilitar opções visuais que violem o contrato do checklist.

**Evidência exigida**

* Novo snapshot datado em `Docs/Reports/Evidence/<data>/` contendo:

    * log bruto (Console)
    * verificação curada com âncoras:

        * trecho do In-Place com **ausência** de logs `[Fade]` e `[LoadingHUD]` entre “solicitação” e “PhaseCommitted”
        * `PhasePendingSet → Reset → PhaseCommitted` na ordem correta
* Atualizar `Docs/ADRs/Checklist-phase.md`:

    * mover o item **2.1** para “PASS” com referência ao snapshot datado.

---

### G-02 — Coerência do contrato de observabilidade (reasons, links, fonte de verdade)

**Motivação**
`Observability-Contract.md` é a **fonte de verdade**. Qualquer ambiguidade aqui vira regressão “por interpretação”.

**Critério de aceite**

1. **Link válido para o índice de reasons**

    * `Observability-Contract.md` referencia `Reason-Map.md` (hoje inexistente no snapshot).
    * Para fechar o gate, escolher **uma** opção:

        * **(A)** criar `Docs/Reports/Reason-Map.md` (índice/glossário), ou
        * **(B)** remover/ajustar a referência no contrato para apontar somente para o que existe e é canônico.
2. **Reasons canônicos alinhados com a evidência**

    * O contrato de WorldLifecycle lista reasons como `ScenesReady/<scene>` e `ProductionTrigger/<source>`.
    * A evidência 2.1 mostra `ResetWorldAsync(reason='SceneFlow/ScenesReady')` (driver) e existe também `WorldLifecycleResetReason` definindo `ScenesReady/<scene>`.
    * Para fechar o gate, deve haver **uma regra oficial** (documentada e validada em evidência) para:

        * o `reason` do `WorldLifecycleResetCompletedEvent`, e
        * o `reason` usado nos logs do controller/driver (se forem diferentes, isso precisa estar explicitado).

**Evidência exigida**

* Atualização do snapshot datado (pode ser o mesmo do G-01) com âncoras que mostrem:

    * `WorldLifecycleResetCompletedEvent(signature, reason)` com reason no formato considerado canônico
    * invariantes do contrato preservados (Started → ScenesReady → ResetCompleted → Completed)

---

### G-03 — Docs de arquitetura e índice sem drift (sem links quebrados / IDs incorretos)

**Motivação**
O 2.2 vai introduzir “camada por cima” (WorldCycle). Se a documentação base já estiver inconsistente, o 2.2 vira uma sequência de correções reativas.

**Critério de aceite**

1. `Docs/ARCHITECTURE.md`

    * Corrigir referência incorreta ao ADR de fechamento do Baseline 2.0:

        * o fechamento está em `ADR-0015 — Baseline 2.0: Fechamento Operacional`
    * Remover ou corrigir links para relatórios inexistentes em `Docs/Reports/` (ex.: `Reports/GameLoop.md`, `Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md`, `Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md`), escolhendo:

        * **(A)** restaurar os arquivos, ou
        * **(B)** ajustar o documento para apontar apenas para `Reports/README.md`, `Reports/Observability-Contract.md` e `Reports/Evidence/*`.
2. `Docs/README.md`

    * Revisar menções a componentes “obsoletos”/não utilizados (ex.: texto citando `WorldLifecycleRuntimeCoordinator` como integração principal, quando há driver canônico em runtime).
    * O objetivo aqui é **alinhamento documental**, não refatoração.

**Evidência exigida**

* PR/commit documental (sem código) com:

    * links corrigidos
    * referências atualizadas para evidências vigentes (`Evidence/LATEST.md`)

---

## Gates condicionais (obrigatórios só se o 2.2 depender deles)

### G-04 — ResetWorld trigger de produção operacionalizado (ADR-0015)

**Motivação**
O ADR-0015 lista como “próximo passo” operacionalizar o trigger de produção. Isso só vira gate obrigatório se o 2.2 for depender de resets fora do SceneFlow (ex.: reconfiguração por fase com reset direto em produção).

**Critério de aceite (se aplicável)**

* Existe caminho de produção para disparar reset direto com reason canônico `ProductionTrigger/<source>`.
* É best-effort: **não trava fluxo** e sempre emite `WorldLifecycleResetCompletedEvent`.

**Evidência exigida**

* Âncora no snapshot datado mostrando `ProductionTrigger/<source>` no log e o `ResetCompleted` correspondente.

---

## Gate de “declaração” (o que precisa existir para dizer “Baseline 2.2 pronto”)

O Baseline 2.2 pode ser declarado promovido quando:

1. **G-01, G-02, G-03** estiverem PASS (e G-04 se aplicável).
2. Existe um snapshot datado em `Docs/Reports/Evidence/<data>/` contendo:

    * log bruto
    * verificação curada (âncoras/invariantes)
3. `Docs/Reports/Evidence/LATEST.md` aponta para o snapshot mais recente.
4. `Docs/CHANGELOG-docs.md` registra:

    * o fechamento do gate (o que foi corrigido e por quê)
    * o snapshot datado que fundamenta a promoção.

---

## Artefatos que devem ser atualizados quando o gate fechar

* `Docs/ADRs/Checklist-phase.md` (Nível B fechado)
* `Docs/Reports/Observability-Contract.md` (links e reasons coerentes)
* `Docs/ARCHITECTURE.md` e `Docs/README.md` (sem drift)
* `Docs/Reports/Evidence/LATEST.md` (ponte para o snapshot vigente)
* `Docs/CHANGELOG-docs.md` (registro do fechamento)

---

## Resultado esperado

Ao final deste gate, o repositório estará em um ponto onde:

* o **contrato de logs/reasons** é realmente canônico e verificável,
* o **PhaseChange/In-Place** não gera ruído visual nem cria ambiguidade com SceneFlow,
* a documentação aponta corretamente para a evidência vigente,
* e o caminho está livre para implementar o **WorldCycleDefinition** (2.2) sem retrabalho por inconsistência.

---

Se você quiser manter o padrão da pasta, eu sugiro salvar esse arquivo como:

* `Docs/Reports/Gate-Baseline-2.2.md`

(ou criar `Docs/Reports/Gates/` se você preferir separar por versão).
