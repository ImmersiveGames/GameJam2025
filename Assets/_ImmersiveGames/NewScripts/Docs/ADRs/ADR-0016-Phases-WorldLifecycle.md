# ADR-0016 — Sistema de Fases integrado ao WorldLifecycle

## Status

Proposto → Em implementação incremental (Marco 0 concluído, Marco 1 em andamento)

---

## Contexto

O projeto utiliza um **WorldLifecycle** que controla carregamento de cenas, reset do mundo, spawn de entidades e bloqueio/liberação da simulação.
Inicialmente, o foco foi garantir que essas operações ocorressem **somente em momentos seguros**, evitando bugs causados por ações fora de hora.

Com isso validado (Marco 0), surge a necessidade de introduzir o conceito de **Fase do jogo** (*Phase / Stage*), indo além de cenas ou estados visuais.

Em jogos com múltiplas fases, a cena de gameplay tende a ser um **template genérico**, enquanto o conteúdo real (inimigos, obstáculos, regras, objetivos) depende da fase ativa.
Portanto, **fase não pode ser tratada como sinônimo de cena**.

---

## Decisão

A arquitetura passa a distinguir explicitamente **dois conceitos diferentes de fase**:

### 1) PhasePlan — Fase solicitada (intenção)

Representa **qual fase o jogo pretende executar a seguir**.

* É definida por decisões de fluxo:

    * Play no menu
    * Restart
    * Avançar para próxima fase
* Existe **antes** do reset do mundo.
* Sobrevive a transições de cena.
* Não implica que a fase já esteja ativa.

Em termos simples:

> *“Qual fase queremos jogar agora?”*

---

### 2) Active Phase — Fase ativa (confirmada)

Representa a fase que **foi efetivamente aplicada** ao mundo.

* Só existe **depois** que:

    * a cena está pronta
    * o reset ocorreu
    * o conteúdo foi montado
* É usada para:

    * gameplay
    * regras
    * lógica de progresso

Em termos simples:

> *“Esta fase já começou de verdade.”*

---

## Regra fundamental

> **Uma fase só se torna ativa quando aplicada durante um reset do mundo.**

Isso garante que:

* nenhuma fase comece durante loading
* nenhuma fase mude no meio de uma transição
* o conteúdo do mundo seja sempre consistente com a fase ativa

---

## Separação de responsabilidades

### Phase (fase)

* Define **o que** o mundo deve conter
* Controla:

    * tipo de inimigos
    * obstáculos
    * regras
    * parâmetros
* É um **dado lógico**

### Cena (scene)

* Define **onde e como** o conteúdo é exibido
* É um **template estrutural**
* Não decide conteúdo de gameplay

### SceneFlow

* Decide **quando** trocar cenas
* Não decide qual fase está ativa

---

## Tipos de mudança de fase suportados

### 1) Mudança de fase “in-place” (sem trocar cena)

Exemplo:

* Fase 1 → Fase 2
* Mesma GameplayScene

Fluxo:

1. PhasePlan é atualizado
2. Reset do mundo ocorre
3. Conteúdo é recriado com base na nova fase

---

### 2) Mudança de fase com transição de cenas

Exemplo:

* Menu → Gameplay
* Gameplay → Gameplay (outra variação)

Fluxo:

1. PhasePlan é atualizado
2. SceneFlow troca cenas (fade/loading)
3. Reset do mundo ocorre
4. Conteúdo é criado com base na PhasePlan

Em ambos os casos:

> **A fase é definida antes do reset e consumida durante o reset.**

---

## Consequências

### Positivas

* Fases deixam de depender de cenas específicas
* GameplayScene se torna reutilizável
* Progressão de fases fica previsível e testável
* Evita acoplamento entre UI, cena e gameplay

### Custos

* Introdução de serviços explícitos de fase
* Necessidade de logs e evidências adicionais

---

## Estado atual

* Marco 0 validou **quando** é seguro iniciar uma fase
* Marco 1 introduz a existência explícita de PhasePlan e Active Phase
* Conteúdo específico por fase será tratado em marcos posteriores

---
