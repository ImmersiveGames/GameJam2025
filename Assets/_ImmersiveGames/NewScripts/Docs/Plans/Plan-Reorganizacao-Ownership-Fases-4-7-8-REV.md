# Plan — Reorganização de Ownership das Fases 4, 7 e 8

## Status
Proposto — revisado após validação do plano

## Objetivo

Reorganizar o ownership arquitetural do trecho mais confuso do pipeline, sem redesenhar o backbone macro e sem iniciar por reorganização física ampla.

Foco deste plano:
- `Local Content Context Activation`
- `Object Materialization`
- `Object Initialization / Rebind`

---

## Por que este plano não inclui as fases 2 e 3 como foco principal

As fases abaixo **não são o foco principal deste plano**:

- `Route Transition` (fase 2)
- `Scene Composition` (fase 3)

### Motivo

As auditorias indicaram que o backbone macro já está relativamente estável nessas áreas, enquanto o maior gap real está em:

- ativação do conteúdo local
- materialização dos objetos
- reinicialização / rebind dos objetos

Em outras palavras:

- as fases 2 e 3 **entram como fronteira funcional**
- as fases 4, 7 e 8 **entram como alvo principal de reorganização**

### Regra prática

Este plano considera:
- fase 2 e fase 3 como **restrições e pré-condições**
- fase 4, fase 7 e fase 8 como **escopo principal**

### Observação importante

A fase 4 **depende funcionalmente** de 2/3, porque a ativação do conteúdo local só acontece depois que:
- a rota macro foi resolvida
- a composição de cena foi aplicada

Mesmo assim, isso **não transforma 2/3 em alvo principal de reorganização nesta etapa**.

### Consequência

Este plano **não propõe refatorar `Route Transition` nem `Scene Composition` agora**.  
Ele apenas exige que a fronteira delas com a fase 4 fique mais clara.

---

## Escopo

### Dentro do escopo
1. Clarificar `Local Content Context Activation`
2. Clarificar `Object Materialization`
3. Clarificar `Object Initialization / Rebind`
4. Reduzir mistura de ownership entre `Orchestration Domain` e `Game Domain`
5. Preservar o backbone macro existente sempre que possível

### Fora do escopo
1. Redesenho amplo de `GameLoop`
2. Redesenho amplo de `SceneFlow`
3. Reorganização física ampla por domínio
4. Refatoração global de pastas
5. Mudança extensa de nomenclatura sem necessidade arquitetural clara

---

## Base conceitual

### Leitura canônica
- `Level` deve ser lido como `Contexto Local de Conteúdo`
- `Level` não deve ser usado como nome de fase arquitetural macro
- `Orchestration Domain` coordena
- `Game Domain` deve ser owner semântico das fases 4, 7 e 8

### Fases do backbone relevantes para este plano
1. `Run Flow`
2. `Route Transition`
3. `Scene Composition`
4. `Local Content Context Activation`
5. `Reset Decision`
6. `Reset Execution`
7. `Object Materialization`
8. `Object Initialization / Rebind`
9. `Gameplay Release`

---

# Bloco A — Local Content Context Activation

## Objetivo
Separar claramente a ativação do conteúdo local da transição macro e da composição de cena.

## Pergunta que essa fase responde
**“Qual conteúdo local passou a valer agora?”**

## Deve entrar nesta fase
- seleção do conteúdo local
- fixação do conteúdo ativo
- vínculo do conteúdo local ao contexto macro já estabelecido
- snapshot/estado de entrada do conteúdo
- handoff para `EnterStage` local

## Não deve entrar nesta fase
- troca de rota macro
- composição de cena
- spawn concreto de atores
- rebind técnico de runtime
- gates macro

## Owner atual predominante
- `Orchestration/LevelLifecycle`
- `Orchestration/LevelFlow`

## Owner alvo
- `Game Domain`

## Problemas atuais
- `Orchestration` ainda decide e conduz demais a entrada do conteúdo local
- `Level` ainda aparece em nomes que sugerem owner de fluxo
- há mistura entre seleção do conteúdo local, snapshot, intro stage e coordenação operacional

## Dependência lateral relevante
- a fase 4 alimenta snapshots e contexto de restart
- qualquer corte aqui afeta `RestartContextService` e pontes relacionadas à retomada

## Resultado esperado
- `Orchestration` fica como dispatcher/gate
- a semântica de entrada do conteúdo local fica claramente no `Game`

---

# Bloco B — Object Materialization

## Objetivo
Separar a materialização dos objetos do trilho operacional que hoje a executa.

## Pergunta que essa fase responde
**“Quais objetos vivos precisam existir agora?”**

## Deve entrar nesta fase
- spawn de player
- spawn de inimigos
- spawn de interactables runtime
- criação/registro inicial dos objetos
- validação de existência mínima pós-reset

## Não deve entrar nesta fase
- seleção de conteúdo local
- composição de cena
- decisão de reset macro
- restore/rebind detalhado

## Owner atual predominante
- execução em `Orchestration/SceneReset`
- implementação concreta em `Game/Gameplay/Spawn`

## Owner alvo
- `Game Domain`

## Problemas atuais
- `SceneReset` parece dono da materialização porque executa o pipeline
- `WorldSpawnServiceFactory` mistura owner de criação com owner semântico
- o `Game` já sabe o que spawnar, mas não aparece como owner claro da fase

## Regra explícita
- `SceneReset` deve ser lido como **executor operacional/local**
- `SceneReset` não deve ser tratado como **owner semântico** da materialização

## Resultado esperado
- `SceneReset` fica como executor operacional/local
- o significado da materialização fica claramente no `Game`

---

# Bloco C — Object Initialization / Rebind

## Objetivo
Separar o contrato de reentrada válida dos objetos do pipeline operacional que o dispara.

## Pergunta que essa fase responde
**“Os objetos já estão válidos para operar e jogar?”**

## Deve entrar nesta fase
- bind de dependências
- restore de estado
- rebind após reset/restart
- reconexão de sinais/controladores
- retorno ao estado jogável válido

## Não deve entrar nesta fase
- load de cena
- escolha do conteúdo local
- decisão de reset macro
- spawn bruto sem restore/rebind

## Owner atual predominante
- semântica em `Game/Gameplay/GameplayReset`
- trigger/sequência ainda passam por `SceneReset`

## Owner alvo
- `Game Domain`

## Problemas atuais
- boundary misto entre pipeline de reset e contrato de rebind
- injectors e adapters ainda participam da fase sem boundary explícito
- parte do significado está no `Game`, mas a sequência ainda é puxada por `Orchestration`

## Regra explícita
- a fase 8 não deve ser reorganizada profundamente antes da fase 7 estar minimamente clara
- a fase 7 não deve ser reorganizada profundamente antes da fase 4 estar estabilizada

## Resultado esperado
- contratos de rebind ficam mais explícitos
- `Orchestration` permanece como disparador/ordenação
- o significado da reentrada válida permanece no `Game`

---

## Ordem de execução recomendada

### Etapa 1
Consolidar a leitura canônica da fase 4:
- o que entra
- o que não entra
- qual owner atual
- qual owner alvo

### Etapa 2
Consolidar a leitura canônica da fase 7:
- distinguir executor operacional de owner semântico
- isolar o papel de `SceneReset`

### Etapa 3
Consolidar a leitura canônica da fase 8:
- distinguir materialização de rebind
- explicitar o contrato de reentrada válida

### Etapa 4
Produzir mapa de:
- responsabilidade
- owner atual
- owner alvo
- conflito
- ação de reorganização

### Etapa 5
Somente depois decidir:
- extrações pontuais
- adapters necessários
- renomes pontuais
- reorganização física futura, se ainda fizer sentido

---

## Regra de ordem

### Regra central
**Não atacar 7 ou 8 antes de estabilizar 4.**

### Motivo
Se 7/8 forem reorganizadas cedo demais:
- o spawn pode ser congelado sobre uma semântica ainda ambígua de conteúdo local
- o rebind pode ser reorganizado antes de o ponto de entrada do contexto local estar claro
- o retrabalho tende a aumentar

---

## Riscos

1. Reorganizar cedo demais por pasta e esconder o problema real
2. Tratar `SceneReset` como owner semântico quando ele parece executor operacional
3. Continuar usando `Level` como nome de fase macro
4. Misturar spawn com rebind no mesmo owner sem clareza
5. Criar novas camadas antes de consolidar boundaries
6. Cortar a fase 4 sem considerar impacto em snapshots/restart

---

## Critérios de pronto

Este plano estará pronto para virar implementação incremental quando ficar claro:

1. onde termina `Scene Composition`
2. onde começa e termina `Local Content Context Activation`
3. quem é owner semântico de `Object Materialization`
4. quem é owner semântico de `Object Initialization / Rebind`
5. qual parte de `Orchestration` deve permanecer apenas como coordenação
6. quais conflitos exigem extração pontual
7. quais itens podem continuar onde estão sem gerar confusão arquitetural
8. quais dependências de restart foram preservadas de forma explícita

---

## Resultado esperado

Ao final desta reorganização conceitual, o projeto deve conseguir responder com clareza:

- qual conteúdo local está ativo
- quem cria os objetos
- quem deixa os objetos válidos para jogar
- onde `Orchestration` para
- onde `Game Domain` passa a ser owner semântico
