# Plan — Reorganização de Ownership das Fases 5 e 6

## Objetivo
Clarificar ownership, boundary e ordem de execução das fases:

- `Reset Decision`
- `Reset Execution`

O plano deve separar com rigor:

- owner semântico
- executor operacional
- contratos de decisão
- contratos de execução

## Escopo
- Clarificar o que pertence à fase 5
- Clarificar o que pertence à fase 6
- Identificar a mistura atual entre decisão e execução
- Definir o owner semântico alvo de cada fase
- Definir o que permanece em `Orchestration`
- Definir o que pertence ao `Game Domain`
- Respeitar as fases 4, 7 e 8 já congeladas
- Preparar a próxima etapa sem implementação ainda

## Fora do escopo
- Reabrir fase 4
- Reabrir fase 7
- Reabrir fase 8
- Redesenhar o backbone macro inteiro
- Reorganização física ampla de pastas
- Criar adapter, bridge ou wrapper só para esconder boundary ruim
- Propor patch de implementação agora

## Base conceitual
- `Level` continua lido como `Contexto Local de Conteúdo`
- `Orchestration Domain` coordena e dispara o fluxo
- `Game Domain` não deve ser usado como executor operacional genérico
- `Reset Decision` decide se, quando e sob qual escopo o reset acontece
- `Reset Execution` executa o reset já decidido
- Hipótese de trabalho: a fase 5 tende a ficar semântica em `Orchestration/WorldReset`, e a fase 6 tende a ficar semântica em `Orchestration/SceneReset`; nesta fase, `SceneReset` é o owner semântico da execução operacional do reset, enquanto `Orchestration/WorldReset` permanece como owner semântico da decisão

## Dependências a respeitar
- Fase 4 já define o conteúdo local ativo; não reabrir isso
- Fase 7 já define a materialização; não reabrir isso
- Fase 8 já define rebind/inicialização válida; não reabrir isso
- Fase 5 e 6 devem consumir essas garantias, não recriá-las
- Qualquer dependência com 4/7/8 deve ser tratada como pré-condição, não como novo alvo arquitetural

# Bloco A — Reset Decision

## Pergunta que a fase responde
**“Deve haver reset agora, com qual escopo, sob qual política e por qual motivo?”**

## O que pertence à fase 5
- decisão de reset hard, soft ou parcial
- seleção do escopo do reset
- classificação da requisição de reset
- normalização de motivo, assinatura e contexto
- gate de strict/degraded mode
- validação de pré-condições para decidir
- escolha de política operacional para permitir ou negar a decisão

## O que não pertence à fase 5
- despawn concreto
- execução de fases de reset
- spawn de objetos
- restore/rebind detalhado
- composição de cena
- seleção do conteúdo local
- decisão macro de fluxo que já foi congelada em 4/7/8

## Owner semântico alvo
- `Orchestration/WorldReset`

## Executor operacional
- `SceneReset` apenas consome a decisão
- `SceneReset` não deve redefinir o significado da decisão

## Mistura atual a observar
- política, decisão e degradação ainda podem aparecer misturadas com o fluxo operacional
- há risco de o executor começar a decidir o que deveria apenas executar
- se houver contract shim entre decisão e execução, isso deve ser tratado como remendo e evitado

## Contratos de decisão
- `IWorldResetPolicy`
- contexto de reset
- requisição de reset
- metadados de strict/degraded mode
- feature ids e reason/signature de decisão

## O que deve permanecer em Orchestration
- policy e gate de decisão
- normalização da requisição
- classificação da severidade
- registro de decisão e telemetria operacional
- coordenação do momento de disparo

## O que deve pertencer ao Game Domain
- somente o que for estritamente gameplay-specific para interpretar impacto da decisão
- nenhum ownership da política de reset como trilho operacional
- nenhuma decisão de execução de pipeline

## Resultado esperado
- a fase 5 fica como contrato de decisão, não como execução
- `Orchestration` permanece como coordenador e dono do gate de decisão
- `Game` entra só como consumidor de contexto, quando necessário

# Bloco B — Reset Execution

## Pergunta que a fase responde
**“Como o reset decidido é executado com segurança e em ordem?”**

## O que pertence à fase 6
- aquisição e liberação de gate operacional
- execução sequencial do pipeline de reset
- despawn e limpeza operacional
- execução de hooks antes e depois das etapas
- coleta e uso de contexto operacional de cena
- fail-fast quando dependências obrigatórias estiverem ausentes
- aplicação do resultado operacional do reset

## O que não pertence à fase 6
- decidir se haverá reset
- reclassificar escopo do reset
- redefinir materialização de objetos
- redefinir rebind válido
- reabrir semântica da fase 7
- reabrir semântica da fase 8

## Owner semântico alvo
- `Orchestration/SceneReset` para a execução operacional
- `Orchestration/WorldReset` para a decisão já consolidada na fase 5

## Executor operacional
- `SceneResetController`
- `SceneResetPipeline`
- `SceneResetFacade`
- `SceneResetContext`

## Mistura atual a observar
- o pipeline pode carregar tanto execução quanto parte da semântica do reset
- há risco de hooks, policy e executor virarem um único bloco híbrido
- qualquer camada de compatibilização extra para manter a lógica “funcionando” no lugar errado deve ser evitada

## Contratos de execução
- `SceneResetContext`
- `SceneResetPipeline`
- `SceneResetHookCatalog`
- `SceneResetHookRunner`
- `IWorldSpawnServiceRegistry` como contrato operacional de serviços já resolvidos
- contratos de participantes de reset que já existirem no `Game`, sem redefinir sua semântica

## O que deve permanecer em Orchestration
- pipeline de execução
- contexto operacional local
- hooks e gate operacional
- ordenação do reset
- fail-fast de infraestrutura de execução

## O que deve pertencer ao Game Domain
- contratos gameplay-specific que participam do reset
- semântica de participantes de gameplay, quando o reset os chama
- qualquer lógica de gameplay que não deva ser absorvida pelo executor operacional

## Resultado esperado
- a fase 6 fica como execução operacional explícita
- `SceneReset` é owner semântico da execução operacional do reset
- `SceneReset` não é owner da decisão do reset
- `Game` mantém contratos de gameplay quando houver participação no reset
- a execução não decide o que já foi decidido na fase 5

## Ordem de execução recomendada
1. Fechar o mapa conceitual da fase 5, sem tocar execução
2. Identificar os pontos de mistura entre decisão e execução
3. Fixar a fronteira da fase 6 como execução operacional pura
4. Confirmar quais contratos ficam em `Orchestration` e quais ficam no `Game`
5. Só depois decidir cortes pontuais, se ainda fizer sentido

## Riscos
- Mover apenas o código de lugar e preservar ownership errado
- Criar adapter, bridge ou wrapper só para mascarar boundary ruim
- Deixar a fase 6 decidir o que deveria vir pronto da fase 5
- Reabrir indiretamente 4, 7 ou 8 por dependência de execução
- Misturar contrato de decisão com contrato de execução no mesmo tipo
- Tratar `SceneReset` como owner semântico em vez de executor operacional

## Critérios de pronto
- A fase 5 responde claramente se, quando e sob qual política um reset acontece
- A fase 6 responde claramente como o reset decidido é executado
- A fronteira entre decisão e execução está explícita e auditável
- `Orchestration` ficou com a coordenação operacional apropriada
- `Game` ficou apenas com o que for gameplay-specific e não operacional
- Nenhuma solução depende de bridge/wrapper/adapter para sustentar ownership errado
- As fases 4, 7 e 8 continuam congeladas e não são reabertas
## Status consolidado apos o corte minimo

### Fase 5 - Reset Decision
- Status: **congelada com ressalva pequena**
- Leitura: `Orchestration/WorldReset` permanece como owner semantico da decisao
- Observacao: `WorldResetOrchestrator` agora fica ancorado na decisao e nao mais na descoberta direta da execucao; a coordenacao de lifecycle ao redor da decisao continua aceitavel

### Fase 6 - Reset Execution
- Status: **congelada com ressalva pequena**
- Leitura: `Orchestration/SceneReset` permanece como owner semantico da execucao operacional
- Observacao: `WorldResetExecutor` e `WorldResetLocalExecutorLocator` estao explicitamente posicionados como helpers operacionais; `SceneReset` continua executando apenas o que ja veio decidido

### Pendencias abertas
- `R3` - hardening futuro do boundary residual entre decisao e coordenacao de lifecycle
- `R3` nao Ã© blocker arquitetural atual
- `R3` deve ser retomada apenas quando houver etapa de hardening de boundary entre decisao e execucao
- `R3` tambem deve ser retomada se `WorldResetOrchestrator` voltar a crescer a ponto de parecer owner da execucao concreta

### Regra para retomada futura
- nao mover so execucao
- corrigir ownership semantico de verdade
- nao aceitar adapter/bridge/wrapper apenas para mascarar boundary ruim

### Conclusao consolidada
- o pacote **5/6 esta fechado por agora**
- a pendencia aberta e apenas de hardening futuro, nao blocker arquitetural atual
