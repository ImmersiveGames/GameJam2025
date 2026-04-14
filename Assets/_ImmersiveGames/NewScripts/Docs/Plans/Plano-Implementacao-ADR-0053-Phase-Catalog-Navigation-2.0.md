# Plano de Implementação — ADR-0053 Phase Catalog Navigation 2.0

## Status
- Estado: Proposto
- Data: 2026-04-14
- Base: ADR-0053 — Phase Catalog Navigation 2.0
- Objetivo: transformar o ADR em uma sequência de implementação incremental, com baixo risco, reaproveitando o que já existe em `NewScripts`

---

## 1. Objetivo do plano

Implementar `Phase Catalog Navigation 2.0` sem refactor amplo e sem misturar:

- catálogo de phases
- phase runtime/conteúdo ativo
- continuidade macro
- restart/reset da phase atual
- tooling/QA

A implementação deve:

- reaproveitar a base já existente de ordem fixa, `next`, `previous`, `looping` e lookup por `phaseId`
- introduzir o estado explícito que ainda falta (`CurrentCommitted`, `PendingTarget`, `LoopCount`)
- transformar `GoToSpecificPhase` e `RestartCatalog` em capacidades reais do sistema
- manter `RestartCurrentPhase` fora do catálogo
- preservar o owner atual do conteúdo runtime da phase em `GameplayPhaseFlowService`

---

## 2. Princípios de implementação

### 2.1. Owners
- `Phase Catalog Navigation` é owner de:
  - ordem canônica das phases
  - navegação ordinal runtime
  - `CurrentCommitted`
  - `PendingTarget`
  - `Looping`
  - `LoopCount`
  - resolução de `next`, `previous`, `specific phase`
  - `RestartCatalog`
- `GameplayPhaseFlowService` continua owner de:
  - conteúdo/runtime ativo da phase
  - materialização phase-side
  - integração com `IntroStage` e pipeline local
- `SessionTransition` continua owner da continuidade macro
- `RestartCurrentPhase` continua fora do catálogo

### 2.2. Estratégia
- **reutilizar antes de criar**
- **adaptar antes de substituir**
- **não mover responsabilidade para módulos macro**
- **não usar QA/tooling como contrato canônico**
- **implementar por fases pequenas com build validando cada etapa**

### 2.3. Anti-objetivos
Não fazer neste plano:
- refactor amplo de pastas/namespaces
- acoplamento com `RunDecision`
- acoplamento com progressão persistida
- elegibilidade/bloqueio de phases
- branching por condição
- mutação dinâmica de catálogo runtime
- difficulty scaling por `LoopCount`

---

## 3. Inventário consolidado do que já existe

## 3.1. Reutilizar
- `PhaseDefinitionCatalogAsset`
  - ordem fixa das phases
  - traversal mode (`Finite` / `Looping`)
  - `TryGetNext`, `TryGetPrevious`, `ResolveNextOrFail`, `ResolvePreviousOrFail`
- `IPhaseDefinitionCatalog`
  - contrato base do catálogo atual
- `PhaseDefinitionResolver`
  - resolução por `phaseId`
- `PhaseNextPhaseService`
  - já tem trilho canônico atual para `next` e `previous`
- `PhaseCatalogTraversalMode.Looping`
  - base atual de looping

## 3.2. Adaptar
- `PhaseNextPhaseService`
  - hoje já navega, mas ainda sem shape explícito completo de catálogo runtime
- `PhaseDefinitionSelectedEvent`
  - útil como parte do handoff, mas não deve virar estado canônico do catálogo
- `GameplayStartSnapshot`
  - útil como snapshot phase-side, não como owner de catálogo
- `RestartContextService`
  - útil como continuidade/reentry, não como owner do estado do catálogo
- `GameplayPhaseFlowService`
  - consumidor correto do catálogo, não owner do catálogo
- `PhaseNavigationQaPanel`
  - deve continuar como tooling, mas usando capacidades reais

## 3.3. Criar
- estado explícito de catálogo runtime
  - `CurrentCommitted`
  - `PendingTarget`
  - `LoopCount`
- comando/capability explícita para:
  - `GoToSpecificPhase`
  - `RestartCatalog`
- eventos canônicos mínimos do catálogo

## 3.4. Manter fora
- `RestartCurrentPhase`
- `RunDecision`
- `RunContinuation`
- `GameNavigationCatalogAsset`
- `GameNavigationService`
- `GameplaySessionFlowContinuityService` como owner do catálogo

---

## 4. Shape alvo de implementação

## 4.1. Estado do catálogo runtime
O catálogo runtime deve ter shape explícito e pequeno.

### Estado mínimo
- `CurrentCommitted`
- `PendingTarget`
- `Looping`
- `LoopCount`
- `TraversalMode`
- assinatura/versionamento do estado, se necessário

### Regra principal
- `CurrentCommitted` muda quando a navegação foi **confirmada como destino canônico**
- `PendingTarget` representa alvo em transição ainda não aplicado
- `CurrentCommitted` **não** espera o fim completo do carregamento/conteúdo
- `CurrentCommitted` **não** muda no clique bruto antes de aceitação da navegação

## 4.2. Capacidades canônicas iniciais
- `ResolveNext`
- `ResolvePrevious`
- `ResolveSpecificPhase`
- `AdvancePhase`
- `PreviousPhase`
- `GoToSpecificPhase`
- `RestartCatalog`

## 4.3. Fora das capacidades do catálogo
- `RestartCurrentPhase`
- continuidade macro
- reset/restart phase-local
- apply de conteúdo da phase

---

## 5. Fases de implementação

## F1 — Introduzir o estado explícito do catálogo runtime

### Objetivo
Criar o estado canônico do catálogo sem mexer ainda em todo o resto.

### Entregas
- estrutura/serviço/runtime state para catálogo
- `CurrentCommitted`
- `PendingTarget`
- `LoopCount` inicial
- integração mínima com catálogo fixo existente

### Arquivos prováveis
- contratos novos de catálogo runtime
- serviço novo ou adaptação pequena em owner de phase navigation atual
- `PhaseNextPhaseService` ou novo owner fino do catálogo runtime
- tocar somente o necessário

### Reaproveitamento
- `PhaseDefinitionCatalogAsset`
- `IPhaseDefinitionCatalog`
- `PhaseDefinitionResolver`

### Critério de pronto
- existe um owner explícito do estado do catálogo
- `CurrentCommitted` e `PendingTarget` não estão mais implícitos e espalhados
- build passa

### Riscos
- duplicar estado com `GameplayPhaseFlowService`
- usar snapshot phase-side como estado de catálogo

---

## F2 — Adaptar `next` e `previous` para o estado explícito

### Objetivo
Fazer as navegações já existentes passarem pelo estado novo, sem mudar comportamento funcional.

### Entregas
- `AdvancePhase` continua funcionando
- `PreviousPhase` continua funcionando
- ambos usam `CurrentCommitted` / `PendingTarget`
- `looping` continua respeitado

### Arquivos prováveis
- `PhaseNextPhaseService`
- possíveis contratos de navegação de phase
- tooling QA só se necessário por compilação

### Reaproveitamento
- `TryGetNext`, `TryGetPrevious`, `ResolveNextOrFail`, `ResolvePreviousOrFail`

### Critério de pronto
- `next` e `previous` não dependem mais de current implícito espalhado
- build passa
- não há regressão no fluxo já existente

### Riscos
- tratar `PreviousPhase` como tooling-only acidentalmente
- atualizar current cedo ou tarde demais

---

## F3 — Promover `GoToSpecificPhase` a capacidade real

### Objetivo
Transformar lookup por `phaseId` em navegação real de catálogo.

### Entregas
- capability `GoToSpecificPhase`
- uso do resolver existente por `phaseId`
- integração com `PendingTarget` e `CurrentCommitted`
- contrato canônico, não só QA

### Arquivos prováveis
- `PhaseDefinitionResolver`
- contratos/capabilities de navigation
- owner do catálogo runtime
- QA tooling como consumidor, não owner

### Critério de pronto
- existe navegação canônica para phase específica
- QA pode usar a mesma capability real
- build passa

### Riscos
- implementar só como botão de QA
- deixar a capability sem owner claro

---

## F4 — Implementar `RestartCatalog`

### Objetivo
Criar a capability que reinicia o catálogo desde a primeira phase.

### Entregas
- `RestartCatalog`
- resolução do destino inicial do catálogo
- atualização correta de `PendingTarget` e `CurrentCommitted`
- sem tocar `RestartCurrentPhase`

### Regra importante
- `RestartCatalog` != `RestartCurrentPhase`
- `RestartCatalog` reinicia progressão ordinal
- `RestartCurrentPhase` reinicia a phase já ativa

### Arquivos prováveis
- owner do catálogo runtime
- serviço/capability de navigation
- possível integração com selection service inicial

### Critério de pronto
- capability existe como contrato real
- não foi misturada com restart local da phase
- build passa

### Riscos
- chamar reset local em vez de resetar a navegação ordinal
- acoplar `RestartCatalog` a `SessionTransition` cedo demais

---

## F5 — Fechar `LoopCount` end-to-end

### Objetivo
Fazer `LoopCount` deixar de ser só conceito do ADR e virar estado real do catálogo.

### Entregas
- incremento de `LoopCount` quando o catálogo completa ciclo
- comportamento correto em modo `Looping`
- nenhum incremento em modo `Finite`
- observability mínima

### Arquivos prováveis
- owner do catálogo runtime
- eventos mínimos do catálogo
- QA/tooling se necessário só como consumidor

### Critério de pronto
- `LoopCount` existe e é coerente com `Looping`
- não depende de progressão/dificuldade
- build passa

### Riscos
- contar loop no lugar errado
- misturar loop do catálogo com run macro

---

## F6 — Eventos canônicos mínimos do catálogo

### Objetivo
Formalizar os poucos eventos necessários para HUD, analytics e observability, sem inflar o sistema.

### Eventos mínimos iniciais
- phase confirmada no catálogo
- target pendente alterado
- loop concluído
- loop count atualizado

### Entregas
- contratos mínimos de evento
- publishers no owner correto do catálogo
- consumo futuro facilitado para HUD e outros sistemas

### Critério de pronto
- eventos existem e ficam claramente no domínio do catálogo
- não substituem reset/restart/continuity
- build passa

### Riscos
- criar eventos demais
- misturar evento de catálogo com evento de conteúdo runtime

---

## F7 — QA/tooling consumindo capacidades reais

### Objetivo
Fazer o tooling usar as mesmas capacidades canônicas, sem virar owner paralelo.

### Entregas
- `PhaseNavigationQaPanel` ou tooling equivalente usando:
  - `PreviousPhase`
  - `AdvancePhase`
  - `GoToSpecificPhase`
  - `RestartCatalog`
- sem trilho secreto separado

### Critério de pronto
- QA usa as capacidades reais
- tooling não define contrato
- build passa

### Riscos
- manter atalhos paralelos escondidos
- tooling continuar acoplado ao estado antigo

---

## 6. Dependências e ordem recomendada

### Ordem obrigatória
1. **F1 — estado explícito do catálogo runtime**
2. **F2 — next/previous no estado novo**
3. **F3 — specific phase**
4. **F4 — restart catalog**
5. **F5 — loop count**
6. **F6 — eventos mínimos**
7. **F7 — tooling usando capacidades reais**

### Motivo da ordem
- sem estado explícito, o resto continua implícito
- sem `CurrentCommitted` / `PendingTarget`, `specific` e `restart catalog` ficam mal posicionados
- `LoopCount` depende de looping já integrado ao estado
- eventos fazem mais sentido depois de haver estado real
- tooling deve vir por último para não virar contrato

---

## 7. Critérios gerais de qualidade

Cada fase deve respeitar:

- **owner correto**
- **sem duplicar estado**
- **sem mover responsabilidade para macro continuity**
- **sem usar QA como fonte de verdade**
- **sem invadir `GameplayPhaseFlowService` como owner do catálogo**
- **sem tocar `RestartCurrentPhase`**
- **com build local passando**

---

## 8. Riscos principais do plano

### R1. Duplicação de estado
Risco de manter current implícito em vários lugares:
- `GameplayPhaseFlowService`
- `RestartContextService`
- snapshots/eventos
- novo catálogo runtime

**Mitigação:** introduzir um único owner explícito do estado do catálogo já na F1.

### R2. Owner errado
Risco de usar:
- `GameNavigation*`
- `SessionTransition`
- `GameplaySessionFlowContinuityService`
como owner da navegação ordinal.

**Mitigação:** manter o catálogo no domínio de phase navigation, não macro navigation.

### R3. Confundir `RestartCatalog` com `RestartCurrentPhase`

**Mitigação:** tratar como capacidades diferentes desde o contrato.

### R4. Tooling virar contrato

**Mitigação:** trazer tooling só depois das capacidades reais estarem prontas.

### R5. LoopCount virar progressão/dificuldade cedo demais

**Mitigação:** manter `LoopCount` como dado do catálogo, não sistema de game design.

---

## 9. Resultado esperado ao final do plano

Ao final deste plano, o sistema deve ter:

- catálogo runtime explícito
- ordem fixa canônica
- `CurrentCommitted`
- `PendingTarget`
- `Looping`
- `LoopCount`
- `AdvancePhase`, `PreviousPhase`, `GoToSpecificPhase`, `RestartCatalog` como capacidades reais
- tooling usando o mesmo trilho real
- `GameplayPhaseFlowService` ainda como owner do conteúdo runtime ativo
- `RestartCurrentPhase` ainda fora do catálogo

---

## 10. Próximo passo imediato

Executar **F1 — Introduzir o estado explícito do catálogo runtime** antes de qualquer outra fase.

Esse é o corte mais importante porque prepara:
- `CurrentCommitted`
- `PendingTarget`
- `specific phase`
- `restart catalog`
- `loop count`

sem reabrir macro continuity nem phase runtime.
