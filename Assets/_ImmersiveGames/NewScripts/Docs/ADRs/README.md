# ADRs - Base 1.1

Este diretório mantém o acervo de ADRs e sua precedência normativa.

## Base 1.1 Viva - Fonte Normativa Atual

A partir da reorganização de Base 1.1, estes ADRs são a **única fonte normativa** para decisões arquiteturais:

1. **ADR-0001** - Base 1.1: Pipeline Convergence, Identidade Explícita e Isolamento contra Foreign Events
2. **ADR-0002** - Run Pipeline Canonical e Deactivation/Continuity
3. **ADR-0003** - Session Operational Pipeline e Session Transition Envelope
4. **ADR-0004** - Session Activity Pipeline
5. **ADR-0005** - Modules Produzem Facts/Commands, Adapters Executam Side-Effects
6. **ADR-0006** - Route, Scene Composition, Fade, Loading e Audio Adapters
7. **ADR-0007** - Gates, InputModes e Simulation Executors
8. **ADR-0008** - SaveSystem Canonical

## Precedência Normativa

Em decisões de arquitetura e ownership, prevalecem os ADRs acima em ordem de precedência.

### Regra Obrigatória de Leitura

- **ADRs de ADR-0001 a ADR-0008 são a fonte normativa exclusiva.**
- ADRs anteriores (históricos) devem ser lidos apenas como referência contextual.
- Em caso de conflito entre um ADR histórico e um ADR Base 1.1, a **Base 1.1 prevalece**.
- Ownership não é decidido por conveniência operacional, e sim pelo papel arquitetural definido na Base 1.1.
- Foreign/stale events não podem alterar o pipeline ativo.

## Classificação Normativa

### NORMATIVO_ATUAL (Base 1.1)

- ADR-0001
- ADR-0002
- ADR-0003
- ADR-0004
- ADR-0005
- ADR-0006
- ADR-0007
- ADR-0008

### HISTÓRICO (Referência Apenas)

Todos os ADRs anteriores a ADR-0001 foram movidos para `Docs/ADRs/Historico/` e devem ser lidos como referência histórica, **não como fonte normativa viva**.

Se um ADR histórico conflitar com a Base 1.1:

- Não abrir exceção local
- Não usar compatibilidade narrativa
- Aplicar a precedência normativa dos ADRs Base 1.1

## Conceitos Chave da Base 1.1

### Pipelines

- **Run Pipeline**: Orquestra run, deactivation e continuity
- **Session Operational Pipeline**: Orquestra transição, setup e handoff inicial de sessão
- **Session Activity Pipeline**: Orquestra ativação, engagement e ciclo interno de activity

### Separation of Concerns

- **Modules**: Produzem Pipeline Facts ou Pipeline Commands
- **Pipelines**: Decidem ordem, lifecycle, policy e handoffs
- **Adapters**: Executam side-effects comandados

### Identidade Explícita

- Todo ciclo relevante tem identidade canônica
- Foreign/stale events não podem alterar o pipeline ativo
- Identidade não é inferida por timing ou conveniência

### Componentes Executores

- **Gates**: Validam/transitam estado
- **InputModes**: Aplicam modo de input
- **Scene Composition**: Carrega/ativa cenas
- **Fade Adapter**: Executa fade visual
- **Loading Adapter**: Executa UI de loading
- **Audio Adapter**: Executa playback de áudio
- **SaveCoreService**: Executa save/load

## Checkpoint Congelado

Base11Sandbox Minimal Route + Session Activity Cycle foi aprovado e congelado com:

- Rail canônico: `RuntimeModeConfig` -> `SessionOperationalPipeline` -> Adapters -> `SessionActivityPipeline`
- Identidade explícita em todas as transições
- Foreign/stale events isolados pelo sistema
- Side-effects rastreáveis e corretos

Referências de materialização:

- `Docs/ADRs/Base-1.1-Consolidado-Atualizado-Base11Sandbox.md`
- Codebase atual em `Assets/_ImmersiveGames/NewScripts/`

## Para Desenvolvedores

### Como Decidir Ownership

1. Consulte **ADR-0001** para entender a arquitetura geral
2. Encontre o ADR que corresponde ao seu componente (ADR-0002 a ADR-0008)
3. Siga as regras e invariantes do ADR
4. Não crie fallback silencioso
5. Falhe cedo se precondições não forem atendidas

### Como Adicionar um Feature Novo

1. Verifique ADR-0005 (Modules/Adapters pattern)
2. Decida: seu feature é um módulo (fact/command produtor) ou um adapter (executor)?
3. Identifique qual pipeline decide o lifecycle
4. Documente a decisão em um ADR futuro se for mudança arquitetural

### Como Reportar Conflitos

Se encontrar um conflito entre um ADR histórico e um ADR Base 1.1:

1. Abra uma issue referenciando ambos
2. Cite o ADR Base 1.1 como fonte normativa
3. Não use compatibilidade narrativa; aplique o ADR

## Histórico de Reorganização

- **2026-05-01**: ADRs 0060-0070 freeze de Base 1.1 implementados
- **2026-05-12**: Reorganização para ADR-0001 a ADR-0008 com consolidação e renumeração
  - ADR-0060 consolidado em ADR-0001
  - ADR-0061 → ADR-0002
  - ADR-0062 + ADR-0069 → ADR-0003
  - ADR-0064 → ADR-0004
  - ADR-0063 → ADR-0005
  - ADR-0068 + ADR-0070 → ADR-0006
  - ADR-0066 → ADR-0007
  - ADR-0065 distribuído em ADR-0002 e ADR-0003
  - ADR-0008 (novo) → SaveSystem Canonical
