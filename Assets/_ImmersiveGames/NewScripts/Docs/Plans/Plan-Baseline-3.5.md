# Baseline 3.5: Estabilização Arquitetural para a Camada Superior

## Status

- Estado: **Draft**
- Data (proposta): **2026-03-26**
- Última atualização: **2026-03-26**
- Escopo: Consolidação da base arquitetural de `NewScripts` antes da próxima fase (`Audio`, `Actor Presentation`, `Content`, reorganizações incrementais)

## Objetivo

A Baseline 3.5 define o ponto em que a base do projeto deixa de ser apenas “funcional” e passa a ser considerada **arquiteturalmente estável o suficiente** para suportar uma nova camada de evolução acima dela.

Esta baseline **não** introduz uma grande feature nova.
Ela congela o estado da plataforma após as consolidações recentes para que as próximas frentes possam assumir contratos, ownerships e boundaries mais previsíveis.

Em termos práticos:

- **Baseline 3.0** = o trilho principal funciona
- **Baseline 3.5** = o trilho principal funciona, está mais organizado e tem contratos claros para crescer

---

## Fonte de verdade

Esta baseline se apoia nos ciclos já concluídos de estabilização e nos ADRs de intenção/ownership produzidos até aqui.

Fonte principal:

- plano de refoundation do `SceneFlow`
- plano de consolidação do stack macro
- plano de consolidação do stack de reset
- `ADR-0034: Actor Presentation Domain Intent and Boundaries`
- `ADR-0035: Ownership Canônico dos Clusters de Módulos em NewScripts`

Esta baseline assume que esses documentos já representam a leitura canônica do estado atual da plataforma.

---

## O que a Baseline 3.5 afirma

A Baseline 3.5 afirma que, no estado atual do projeto:

1. o trilho macro do jogo já tem ownership mais claro;
2. o stack de reset já tem contrato e boundaries mais explícitos;
3. a árvore lógica de módulos já tem uma leitura canônica de ownership suficiente para evitar novos refactors acidentais;
4. a próxima fase pode começar a trabalhar em uma camada acima da baseline sem precisar redefinir novamente o significado da base.

---

## Escopo da Baseline 3.5

Entram nesta baseline:

- consolidação de `SceneFlow`
- consolidação do stack macro (`SceneFlow`, `Navigation`, `LevelFlow`, `GameLoop`, `ResetInterop`)
- consolidação do stack de reset (`WorldReset`, `SceneReset`, `ResetInterop`, boundary com `SimulationGate` / `Gameplay`)
- ADR de intenção do domínio de `Actor Presentation`
- ADR de ownership canônico dos clusters de módulos em `NewScripts`

Não entram nesta baseline:

- nova arquitetura de `Audio`
- implementação do stack de `Actor Presentation`
- reorganização física ampla de pastas
- refactors cosméticos em módulos já estabilizados
- abertura de novos domínios sem plano específico

---

## Contratos preservados

A Baseline 3.5 considera preservados os seguintes contratos principais:

### 1. SceneFlow
- `SceneFlow` continua owner da timeline macro
- `SceneFlow` continua owner do handoff macro de transição
- `SceneFlow` não é owner de semântica de level, reset ou actor presentation

### 2. LevelFlow
- `LevelFlow` continua owner do lifecycle local de level
- `LevelFlow` continua owner de `prepare/clear`, seleção default e contexto semântico de level/start/restart

### 3. Navigation
- `Navigation` continua owner de intent/dispatch do usuário
- `Navigation` não disputa ownership da timeline macro de cena

### 4. GameLoop
- `GameLoop` continua owner da state machine da run
- decisões de `ready/start/resume` ficam semanticamente mais próximas do `GameLoop`

### 5. WorldReset / SceneReset / ResetInterop
- `WorldReset` continua owner do reset macro
- `SceneReset` continua owner do pipeline local de reset
- `ResetInterop` continua bridge fina
- `SimulationGate` continua owner da trava
- `Gameplay` e `Readiness` continuam consumidores de gate/readiness, não owners de reset

### 6. Organização lógica de módulos
- `Gameplay`, `WorldReset` e `LevelFlow` são lidos como domínios
- `SceneFlow`, `GameLoop`, `Navigation`, `SceneReset` e `PostGame` são lidos como orquestração
- `ResetInterop` é lido como bridge
- `Audio` é lido como capability compartilhada
- `Infrastructure/*` continua técnico
- `Docs/*` e `Analises/*` continuam governança/tooling

### 7. Actor Presentation
- `Actor Presentation` é um domínio de apresentação runtime do ator
- não pertence a `SceneFlow`, `LevelFlow`, `IntroStage`, `PostGame`, `Reset` ou `GameLoop`
- a próxima fase deve tratá-lo como domínio próprio, não como detalhe de lifecycle

---

## Consolidado nesta baseline

### A. Refoundation de SceneFlow
Consolidado:
- ownership da timeline macro
- delegação da composição técnica
- `Loading/Fade` como apresentação
- `GameLoop sync` fora do input bridge
- `LevelPrepare/Clear` fora do core macro
- lifecycle de orchestrators mais explícito

### B. Consolidação do stack macro
Consolidado:
- `GameLoop` mais claramente owner de decisões de completion sync
- `Navigation` mais claramente owner de dispatch macro
- `LevelFlow` mais claramente owner do contexto semântico de gameplay start/restart
- bridges/coordinators mais finos no trilho principal

### C. Consolidação do reset stack
Consolidado:
- contrato canônico de reset mais explícito
- `kind`, `targetScene`, `reason`, `contextSignature` e `origin` mais próximos de um contrato explícito
- `ResetInterop` mais próximo de bridge fina
- `WorldResetService` mais claramente fachada/owner
- pipeline local de `SceneReset` mais coeso
- `requiresWorldReset` consolidado do lado de `SceneFlow`
- boundary com `Gameplay` / `SimulationGate` mais claro

### D. Ownership lógico de módulos
Consolidado:
- leitura canônica da árvore de `NewScripts`
- separação lógica entre domínio, orquestração, interop, capability, infra e tooling

### E. Intenção do domínio de Actor Presentation
Consolidado:
- `Actor Presentation` passa a ter intenção formal registrada
- a próxima fase já não precisa descobrir novamente “o que esse domínio é”

---

## O que ficou conscientemente fora da Baseline 3.5

Os itens abaixo **não bloqueiam** a próxima fase e ficaram conscientemente fora:

- reorganização física ampla de `NewScripts`
- unificação total de nomes históricos de módulos
- mega-refactor de `Gameplay`
- refactor amplo de `Audio`
- implementação do stack completo de `Actor Presentation`
- reorganização estrutural de `Content`
- limpeza fina de resíduos pequenos de observabilidade/logs que não alteram ownership

---

## Resíduos aceitos conscientemente

| item | motivo para ficar fora | bloqueia próxima fase? |
|---|---|---|
| árvore física ainda híbrida | faltava primeiro congelar ownership lógico | não |
| resíduos pequenos de naming/observabilidade | baixo valor agora | não |
| `Modules/Gates` como resíduo histórico | tratar depois em reorganização incremental | não |
| `PostGame` ainda físico fora de `GameLoop` | não vale mover agora | não |
| `Audio` ainda sem plano próprio | próximo stack/capability, não base | não |
| `Actor Presentation` ainda sem implementação consolidada | será a camada acima da baseline | não |
| `Content` ainda sem reorganização própria | precisa decisão/plano específico | não |

---

## Critério de entrada da próxima fase

A próxima fase (`Audio`, `Actor Presentation`, `Content`, reorganização incremental de domínios) pode assumir que:

1. a base macro não mudará de semântica no curto prazo;
2. o reset macro/local já tem ownership suficientemente claro;
3. `Gameplay` não deve ser tratado como guarda-chuva universal;
4. `Actor Presentation` é domínio próprio do ator, não parte de lifecycle;
5. o trabalho acima da baseline não precisa redesenhar novamente `SceneFlow`, `Navigation`, `LevelFlow`, `WorldReset` ou `SceneReset` antes de começar.

---

## Checklist manual da Baseline 3.5

### Fluxos principais
- [ ] startup completo permanece íntegro
- [ ] `menu -> gameplay` permanece íntegro
- [ ] `restart` permanece íntegro
- [ ] `exit-to-menu` permanece íntegro

### Stack macro
- [ ] `SceneFlow` continua como owner da timeline macro
- [ ] `Navigation` continua como owner de dispatch
- [ ] `LevelFlow` continua como owner de `prepare/clear` e contexto semântico de gameplay start/restart
- [ ] `GameLoop` continua como owner da state machine

### Stack de reset
- [ ] frontend skip continua íntegro
- [ ] reset macro de gameplay continua íntegro
- [ ] `SceneReset` continua íntegro no pipeline local
- [ ] `SimulationGate` continua owner da trava
- [ ] `Gameplay` / `Readiness` continuam consumidores de gate/readiness

### Boundaries e ownership
- [ ] ADR-0034 está alinhado com a próxima fase de `Actor Presentation`
- [ ] ADR-0035 está alinhado com a leitura atual da árvore de módulos
- [ ] não há refactor estrutural pendente que mude o significado da base

---

## Exit Condition

A Baseline 3.5 pode ser considerada **fechada** quando:

1. este documento estiver aceito;
2. os planos concluídos estiverem marcados como concluídos;
3. os ADRs canônicos de ownership e de `Actor Presentation` estiverem aceitos;
4. a evidência manual dos fluxos principais confirmar que o comportamento observável permanece íntegro;
5. os resíduos restantes estiverem conscientemente aceitos como **não bloqueantes**.

Quando essas condições forem atendidas, a próxima fase pode começar assumindo esta baseline como **plataforma estável de evolução**.

---

## Decisão final desta baseline

A Baseline 3.5 é uma **baseline de estabilização arquitetural**, não de feature nova.

Ela existe para permitir que a próxima camada do projeto seja construída com menos ambiguidade de ownership, menos risco de retrabalho e menos chance de reabrir a base do sistema.
