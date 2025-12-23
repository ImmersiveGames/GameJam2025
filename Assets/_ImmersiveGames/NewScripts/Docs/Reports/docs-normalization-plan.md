# Plano de Normalização da Documentação

## 1. Inventário de documentos existentes

- **DECISIONS.md** — Documento normativo de limites e guardrails do projeto; principais seções: Política de Uso do Legado, Checklist para PR/Commit; ~26 linhas.
- **ARCHITECTURE.md** — Descritivo arquitetural (as-is/roadmap) com princípios, escopos, fluxo de vida atual e visão planejada; ~41 linhas.
- **Guides/UTILS-SYSTEMS-GUIDE.md** — Guia técnico operacional da pasta Úteis (sistemas de infraestrutura, DI/EventBus, pooling, etc.); ~207 linhas.
- **ADR/ADR-ciclo-de-vida-jogo.md** — ADR de decisão para fases de readiness, reset por escopo, passes de spawn e late bind; ~104 linhas.
- **ADR/ADR-0001-NewScripts-Migracao-Legado.md** — ADR de estratégia de migração legado → NewScripts com guardrails e plano incremental; ~56 linhas.
- **WorldLifecycle/WorldLifecycle.md** — Documento operacional de ciclo de vida/reset determinístico, fases, hooks e contrato de validação; ~262 linhas.
- **QA/WorldLifecycle-Baseline-Checklist.md** — Checklist normativo/QA para validar baseline do WorldLifecycle em logs (hard/soft reset, runner vs auto-init); ~262 linhas.
- **ADR/ADR.md** — Compilado de ADRs históricos e decisões relacionadas ao WorldLifecycle e papéis; ~227 linhas.
- **README.md** — Índice de documentação apontando para os artefatos acima; ~13 linhas.

## 2. Observações por documento (objetivo, seções, tamanho aproximado)

- **DECISIONS.md** — Normativo. Seções: Limites atuais; Política de Uso do Legado; Checklist para PR/Commit. ~26 linhas.
- **ARCHITECTURE.md** — Descritivo (as-is + roadmap). Seções: Princípios Fundamentais; Escopos; Fluxo de Vida Atual; World Lifecycle Reset & Hooks (As-Is); Planned (To-Be / Roadmap). ~41 linhas.
- **Guides/UTILS-SYSTEMS-GUIDE.md** — Guia técnico. Seções: Visão Geral; Política de Uso do Legado; Mapa de Sistemas; Guia por sistema (Event Bus, DI, Logging, Pooling, UniqueId, Predicados, Helpers); Dependências entre Sistemas; Relação com Reset/Spawn/Lifecycle; Pontos Fortes; Riscos; Glossário. ~207 linhas.
- **ADR/ADR-ciclo-de-vida-jogo.md** — ADR (decisão). Seções: Contexto; Objetivos; Decisões Arquiteturais; Definição de Fases; Reset Scopes; Spawn Passes; Late Bind; Uso do SimulationGateService; Linha do tempo oficial; Consequências; Não-objetivos; Plano de Implementação; Fase 1. ~104 linhas.
- **ADR/ADR-0001-NewScripts-Migracao-Legado.md** — ADR (decisão/roadmap de migração). Seções: Contexto; Decisão; Guardrails; Arquitetura atual (resumo); Plano incremental; Critérios de validação; Consequências; Validation/Exit Criteria. ~56 linhas.
- **WorldLifecycle/WorldLifecycle.md** — Contrato operacional. Seções: Visão geral do reset determinístico; Ciclo de Vida do Jogo (Scene Flow + WorldLifecycle); Escopos de Reset; Linha do tempo oficial; Fases de Readiness; Spawn determinístico e Late Bind; Resets por escopo; Soft Reset por Escopo; ResetScope as Gameplay Outcome; Registry e injeção; Hooks disponíveis; Otimização de cache; Scene Hooks; QA/Validação de Ordenação; Ordenação determinística; Do/Don't; Como registrar hooks; IOrderedLifecycleHook; Hooks em ator; QA: como reproduzir; Validation Contract (Hard/Soft/Driver); Troubleshooting; Boot order & DI timing; Migration Strategy; Baseline Validation Contract. ~262 linhas.
- **QA/WorldLifecycle-Baseline-Checklist.md** — Checklist operacional de QA. Seções: Objetivo; Pré-condições; Critérios globais (Hard/Soft); Fluxo A (sem runner); Fluxo B (com runner); Hard Reset; Soft Reset Players; Proteção contra reentrada; Warnings; Checklist rápido; Encerramento; Critérios de reprovação; Changelog. ~262 linhas.
- **ADR/ADR.md** — Compilado/guia de ADRs. Seções: Atualizações 02/2026; ADR – Ciclo de Vida do Jogo; Consolidação final; Divisão por Responsabilidade Única; Correções passo a passo; ADR: World Lifecycle Hooks Architecture (Context/Decision/Consequences, Lazy Injection, Separação de responsabilidades, Cache por ciclo); NewScripts ADRs (propriedade do registry, execução determinística). ~227 linhas.
- **README.md** — Índice/roteiro. Seções: Índice; Documentação principal (links para arquitetura, ciclo de vida, ADRs, decisões, guia de sistemas). ~13 linhas.

## 3. Duplicações conceituais/textuais detectadas

- **ADR-ciclo-de-vida-jogo.md ↔ WorldLifecycle.md**  
  - Duplicações: Linha do tempo oficial; fases de readiness; definição de escopos (Soft/Hard); passes de spawn; regras de late bind; uso do SimulationGate/gates.  
  - Propriedade sugerida: ADR mantém a decisão (motivos, objetivos, linha do tempo como contrato); WorldLifecycle mantém o detalhamento operacional (pipeline, hooks, exemplos, troubleshooting). ADR deve apontar para o doc operacional em vez de repetir instruções.

- **WorldLifecycle.md ↔ WorldLifecycle-Baseline-Checklist.md**  
  - Duplicações: pipeline de hard reset (gate acquire/release + ordem de fases); soft reset players com escopo; expectativas de logs e ordem determinística; referência ao runner vs auto-init.  
  - Propriedade sugerida: WorldLifecycle como fonte operacional (contrato e semântica); Checklist como guia de QA (passo a passo e critérios de aprovação) referenciando o contrato em vez de reexplicar pipeline.

- **ADR-0001-NewScripts-Migracao-Legado.md ↔ WorldLifecycle.md / ADR-ciclo-de-vida-jogo.md**  
  - Duplicações: guardrails de gate/ordem determinística; contrato de Soft Reset por escopo (Players) e baseline funcional; ownership do `WorldLifecycleHookRegistry`.  
  - Propriedade sugerida: ADR-0001 mantém decisões e guardrails de migração; WorldLifecycle detalha como os guardrails se manifestam no runtime; ADR-ciclo-de-vida-jogo continua como decisão de fases/escopos.

- **ARCHITECTURE.md ↔ WorldLifecycle.md**  
  - Duplicações: resumo do pipeline de reset/hook ordering e propriedade do registry.  
  - Propriedade sugerida: ARCHITECTURE mantém visão as-is/resumo; WorldLifecycle permanece com a descrição detalhada e exemplos.

## 4. Plano proposto de normalização (sem editar ainda)

1. **Centralizar contrato operacional no WorldLifecycle.md**  
   - Remover textuais duplicadas de pipeline, passes, fases e escopos em ADR-ciclo-de-vida-jogo e substituí-las por referências explícitas ao WorldLifecycle.md.  
   - Manter no ADR apenas a decisão, objetivos e linha do tempo resumida como contrato de alto nível.

2. **Reenquadrar ADRs para foco em decisão**  
   - Em ADR-ciclo-de-vida-jogo: reduzir detalhes operacionais de spawn/late bind/reset para bullet points de decisão e adicionar links para seções equivalentes de WorldLifecycle.md.  
   - Em ADR-0001: consolidar guardrails sem repetir o pipeline completo; referenciar a seção de Migration Strategy do WorldLifecycle.md para detalhes de execução.

3. **Checklist como consumidor do contrato**  
   - Em WorldLifecycle-Baseline-Checklist.md: substituir descrições longas do pipeline por referências ao contrato de validação em WorldLifecycle.md, mantendo apenas passos de QA, expectativas de log e critérios de aprovação/reprovação.  
   - Adicionar hiperlinks ou âncoras para as seções “Validation Contract” e “Troubleshooting” do WorldLifecycle.md.

4. **Alinhar resumos em ARCHITECTURE.md**  
   - Manter apenas o resumo do pipeline/ownership no capítulo “World Lifecycle Reset & Hooks (As-Is)” e linkar para o WorldLifecycle.md para detalhes operacionais.  
   - Garantir que DECISIONS.md continue como fonte de guardrails globais, sem repetir fluxo de reset.

5. **Índice e navegação**  
   - Atualizar README.md para refletir a divisão de responsabilidades (ADR = decisão, WorldLifecycle = operação, Checklist = QA, ARCHITECTURE = as-is, UTILS = infra).  
   - Opcional: criar mini-seção em `ADR/ADR.md` apontando para o ADR-ciclo-de-vida-jogo como fonte única das fases/escopos e para WorldLifecycle.md como contrato operacional.

6. **Governança futura**  
   - Adotar regra: qualquer nova seção operacional sobre lifecycle deve nascer em WorldLifecycle.md; ADRs podem referenciar, mas não duplicar.  
   - Para QA, novas checklists devem referenciar o contrato operacional e evitar copiar trechos de pipeline, limitando-se a steps e expected outputs.
