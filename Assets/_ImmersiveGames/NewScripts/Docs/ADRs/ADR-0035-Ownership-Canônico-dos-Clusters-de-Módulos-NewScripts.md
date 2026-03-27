# ADR-0035: Ownership Canônico dos Clusters de Módulos em NewScripts

## Status

- Estado: **Aceito**
- Data (decisão): **2026-03-26**
- Última atualização: **2026-03-26**
- Decisores: Time NewScripts
- Escopo: Organização lógica de módulos em `Assets/_ImmersiveGames/NewScripts` (**sem reorganização física nesta decisão**)

## Evidências canônicas (atualizado em 2026-03-26)

- Auditoria atual de organização de módulos de `NewScripts`
- `ADR-0034-Actor-Presentation-Domain-Intent-and-Boundaries`
- Planos já concluídos de consolidação macro e reset stack

---

## Contexto

A árvore `Assets/_ImmersiveGames/NewScripts` evoluiu de forma incremental e hoje mistura, no mesmo nível organizacional, categorias diferentes de responsabilidade:

- **domínios de negócio** (`Gameplay`, `WorldReset`, `LevelFlow`)
- **fluxos/orquestração** (`SceneFlow`, `GameLoop`, `Navigation`, `SceneReset`, `PostGame`)
- **interop/bridges** (`ResetInterop`)
- **capabilities compartilhadas** (`Audio`)
- **infraestrutura técnica** (`Core`, `Infrastructure/*`)
- **tooling / governança** (`Docs`, `Analises`)

A auditoria de organização concluiu que a árvore atual está mais alinhada à **evolução histórica do projeto** do que a uma taxonomia canônica por domínio. O principal problema não é falta de pasta, e sim ausência de uma decisão explícita sobre **quem é domínio**, **quem é orquestração**, **quem é bridge**, **quem é capability** e **quem é infraestrutura**.

Antes de qualquer reorganização física, é necessário congelar uma leitura única de ownership para evitar refactors cosméticos, movimentos prematuros de pasta e reforço acidental de módulos “guarda-chuva”.

---

## Decisão

1. **Nem todo módulo em `Modules/*` é um domínio de negócio.**
   A partir deste ADR, `Modules/*` deve ser lido como um agrupador de features/clusters, não como evidência de ownership semântico por si só.

2. **Os clusters de `NewScripts` passam a ser classificados canonicamente nas categorias abaixo:**
    - **Domain**
    - **Orchestration**
    - **Interop**
    - **Capability**
    - **Infrastructure**
    - **Tooling / Governance**

3. **Classificação canônica atual**

### 3.1 Domain
Owners de semântica de negócio do jogo:
- `Modules/Gameplay`
- `Modules/WorldReset`
- `Modules/LevelFlow`

### 3.2 Orchestration
Owners de fluxo, coordenação, handoff e lifecycle macro:
- `Modules/SceneFlow`
- `Modules/GameLoop`
- `Modules/Navigation`
- `Modules/SceneReset`
- `Modules/PostGame`

### 3.3 Interop
Ponte explícita entre módulos já canônicos, sem semântica própria de domínio:
- `Modules/ResetInterop`

### 3.4 Capability
Capacidade compartilhada do projeto, com runtime/config/QA próprios, mas não domínio de negócio:
- `Modules/Audio`

### 3.5 Infrastructure
Raiz técnica / shared runtime / composição / providers / executores:
- `Core/*`
- `Infrastructure/*`

### 3.6 Tooling / Governance
Documentação, auditoria, análise, QA e governança:
- `Docs/*`
- `Analises/*`

4. **`Modules/Gates` passa a ser tratado como resíduo técnico.**
   A capability canônica de gate pertence a `Infrastructure/SimulationGate`. Nenhum novo desenvolvimento deve reforçar `Modules/Gates` como owner.

5. **`Navigation` continua sendo camada de intenção e dispatch do usuário, não owner de roteamento macro de cena.**
   O roteamento e timeline macro permanecem semanticamente em `SceneFlow`.

6. **`PostGame` deve ser lido como comportamento terminal da run, semanticamente adjacente a `GameLoop`, mesmo permanecendo fisicamente separado por enquanto.**

7. **Esta decisão é lógica, não física.**
   Ela não reorganiza a árvore neste momento. Ela apenas congela a leitura correta de ownership para orientar documentação, planos e refactors futuros.

---

## Racional

- A árvore atual funciona, mas está parcialmente organizada por conveniência histórica.
- Sem uma decisão explícita, módulos de fluxo tendem a ser tratados como domínios por acidente.
- Isso aumenta risco de:
    - ownership ambíguo,
    - nomenclatura enganosa,
    - reorganização cosmética,
    - e reforço de acoplamento indevido.
- Congelar primeiro a taxonomia lógica reduz retrabalho e prepara reorganizações incrementais mais seguras.

---

## Consequências

### Positivas
- Ownership passa a ficar mais claro sem exigir refactor imediato.
- Módulos de fluxo/orquestração deixam de ser lidos como domínios por acidente.
- Futuros ADRs e planos podem usar uma base canônica comum.
- Reorganizações físicas futuras passam a ter critério arquitetural explícito.

### Negativas / Trade-offs
- A estrutura física continuará híbrida por algum tempo.
- Alguns nomes continuarão mais largos ou históricos do que o ideal.
- A leitura correta dependerá deste ADR até que uma reorganização física incremental aconteça.

---

## Alternativas consideradas

1. **Reorganizar toda a árvore imediatamente** (rejeitada)
   Risco alto de churn, regressão e movimentos cosméticos antes da estabilização completa dos boundaries.

2. **Manter a árvore como está sem ADR** (rejeitada)
   Mantém ambiguidade de ownership e dificulta decisões futuras de reorganização.

3. **Criar domínios top-level novos imediatamente para tudo** (rejeitada)
   Seria prematuro sem primeiro congelar quais clusters realmente são domínio, fluxo, interop ou capability.

---

## Evidência / Validação

**Esperado após este ADR:**
- documentação futura passa a classificar módulos por ownership semântico, não apenas por localização física;
- novos planos evitam reforçar `Gameplay`, `SceneFlow`, `GameLoop` ou outros clusters como “guarda-chuva” sem justificativa;
- reorganizações futuras começam por resíduos claros e de baixo risco, e não por mega-refactor estrutural.

**Critério de leitura correta após a decisão:**
- `Gameplay`, `WorldReset` e `LevelFlow` são lidos como domínios;
- `SceneFlow`, `GameLoop`, `Navigation`, `SceneReset` e `PostGame` são lidos como orquestração;
- `ResetInterop` é lido como bridge;
- `Audio` é lido como capability;
- `Infrastructure/*` e `Core/*` permanecem técnicos.

---

## Checklist de fechamento

- [x] Ownership lógico dos clusters de `NewScripts` explicitado
- [x] Distinção entre domínio, orquestração, interop, capability, infraestrutura e tooling formalizada
- [x] `Modules/Gates` marcado como resíduo técnico
- [x] Regra explícita de “decisão lógica antes de reorganização física” registrada
- [x] Base preparada para futuros ADRs e planos de reorganização incremental
