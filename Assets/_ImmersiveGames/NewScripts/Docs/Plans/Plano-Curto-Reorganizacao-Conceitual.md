# Plano Curto de Reorganização Conceitual

## Objetivo

Reorganizar a leitura e o ownership do trecho mais confuso do pipeline, sem redesenhar o backbone macro e sem começar por reorganização física ampla.

Foco principal:
- `Local Content Context Activation`
- `Object Materialization`
- `Object Initialization / Rebind`

---

## Plano curto

| etapa | objetivo | resultado esperado |
|---|---|---|
| **1. Congelar vocabulário** | fixar os nomes canônicos das fases e owners | parar de usar `Level` como nome de fase macro |
| **2. Delimitar fase 4** | separar claramente `Scene Composition` de `Local Content Context Activation` | ficar claro onde termina “preparar o palco” e onde começa “ativar o conteúdo local” |
| **3. Delimitar fase 7** | explicitar o que é `Object Materialization` | ficar claro quem cria os objetos vivos |
| **4. Delimitar fase 8** | explicitar o que é `Object Initialization / Rebind` | ficar claro quem deixa os objetos válidos após spawn/reset |
| **5. Mapear owners atuais** | listar serviços/módulos que hoje participam dessas 3 fases | saber o que permanece, o que cruza fronteira e o que confunde |
| **6. Definir boundaries alvo** | decidir o owner esperado de cada responsabilidade | base para reorganização futura sem ambiguidade |
| **7. Só então planejar extrações** | decidir se basta reorganizar ou se precisa extrair módulos/coordenadores | evitar churn prematuro |
| **8. Adiar reorganização física ampla** | impedir mudança de pasta antes da consolidação conceitual | manter custo baixo e reduzir risco arquitetural |

---

## Etapa 1 — Congelar vocabulário

### Decisão
Adotar como leitura oficial:

1. `Run Flow`
2. `Route Transition`
3. `Scene Composition`
4. `Local Content Context Activation`
5. `Reset Decision`
6. `Reset Execution`
7. `Object Materialization`
8. `Object Initialization / Rebind`
9. `Gameplay Release`

### Regra
- `Level` = **Contexto Local de Conteúdo**
- `Level` **não** = nome da fase arquitetural geral

---

## Etapa 2 — Delimitar `Local Content Context Activation`

### Pergunta que essa fase responde
**“Qual conteúdo local passou a valer agora?”**

### Entra aqui
- seleção do conteúdo local
- contexto ativo corrente
- vínculo com rota macro
- snapshot/estado de entrada do conteúdo
- `EnterStage` do conteúdo local

### Não entra aqui
- spawn concreto de player/inimigos
- injectors técnicos
- restore/rebind de runtime
- gates macro

---

## Etapa 3 — Delimitar `Object Materialization`

### Pergunta que essa fase responde
**“Quais objetos vivos precisam existir agora?”**

### Entra aqui
- spawn de player
- spawn de inimigos
- spawn de interactables runtime
- registro de objetos criados
- validação de existência pós-reset

### Não entra aqui
- escolha do conteúdo local
- rota
- composição de cena
- rebind/restore detalhado

---

## Etapa 4 — Delimitar `Object Initialization / Rebind`

### Pergunta que essa fase responde
**“Os objetos já estão válidos para jogar?”**

### Entra aqui
- bind de dependências
- restore de estado
- rebind após reset/restart
- reconexão de sinais/controladores
- retorno ao estado jogável válido

### Não entra aqui
- load de cena
- escolha do conteúdo local
- decisão de reset macro

---

## Etapa 5 — Mapear owners atuais

### Mapa inicial
- `Local Content Context Activation` → `LevelMacroPrepareService`, `LevelFlowContentService`, `LevelStageOrchestrator`
- `Object Materialization` → `SceneResetPipeline`, `IWorldSpawnService`, `PlayerSpawnService`, `EaterSpawnService`
- `Object Initialization / Rebind` → `GameplayReset`, participants, injectors, adapters de runtime

### Resultado esperado
Uma tabela simples de:
- responsabilidade
- owner atual
- owner esperado
- conflito ou não

---

## Etapa 6 — Definir boundaries alvo

### Alvo conceitual
- **Orchestration Domain** coordena
- **Game Domain** é owner de:
  - `Local Content Context Activation`
  - `Object Materialization`
  - `Object Initialization / Rebind`

### Regra prática
Se algo responde:
- “qual conteúdo local está valendo?” → fase 4
- “quais objetos precisam existir?” → fase 7
- “esses objetos já estão válidos?” → fase 8

---

## Etapa 7 — Planejar extrações só depois

Só depois de fechar o mapa acima decidir:
- o que fica onde está
- o que só precisa mudar de nome/leitura
- o que precisa extração pontual
- o que precisa virar coordinator mais explícito

### Expectativa realista
- organização + extrações pontuais
- não redesenho amplo

---

## Etapa 8 — Adiar reorganização física ampla

### Regra
Não reorganizar pasta em larga escala por domínio agora.

### Motivo
O problema atual é mais de:
- boundary
- ownership
- leitura de pipeline

do que de pasta.

---

## Ordem recomendada

1. congelar vocabulário  
2. fechar fase 4  
3. fechar fase 7  
4. fechar fase 8  
5. mapear owners atuais  
6. definir owners alvo  
7. só então decidir extrações  
8. por último avaliar reorganização física

---

## Resultado esperado

Ao final, deve ficar claro:
- qual conteúdo local está ativo
- quem cria os objetos
- quem deixa esses objetos prontos
- onde termina orchestration e começa `Game Domain`
