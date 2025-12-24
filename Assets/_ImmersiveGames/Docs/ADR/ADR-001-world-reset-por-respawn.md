
# ADR 001 — World Reset por Despawn + Respawn

**Status:** Aceito
**Data:** 2025-XX-XX
**Decisores:** Core Architecture
**Contexto:** Projeto base World-Driven, Actor-Centric

---

## Contexto

Em versões anteriores do projeto, o reset de jogo foi implementado através de múltiplas abordagens combinadas, incluindo:

* chamadas de `Reset()` em componentes,
* reconfiguração parcial de estado,
* congelamento de física,
* limpeza manual de listeners,
* tentativas de “estabilizar” o mundo após o reset.

Essas abordagens geraram problemas recorrentes:

* estado residual após reset (movimento, timers, lógica),
* ordem de execução implícita e frágil,
* dependência de correções tardias (esperar frame, yield),
* aumento progressivo de complexidade e acoplamento,
* sistemas precisando “se defender” contra reset.

Ficou claro que **reset por mutação incremental do mundo não é determinístico nem escalável**.

---

## Decisão

O projeto adota como decisão arquitetural central:

> **Reset de mundo é realizado exclusivamente por despawn completo seguido de respawn determinístico.**

Formalmente:

* Não existe reset global por reconfiguração de componentes.
* Não existe reset global por pausa ou congelamento de sistemas.
* Não existe reset global por limpeza manual de estado interno.

O reset global segue **sempre** o pipeline:

1. Aquisição do Gate de simulação
2. Despawn completo do mundo atual
3. Respawn determinístico do mundo
4. Liberação do Gate

Qualquer sistema que não funcione corretamente após um ciclo completo de despawn/respawn é considerado **arquiteturalmente inválido**.

---

## Consequências

### Consequências positivas

* Reset torna-se determinístico e previsível.
* Ordem de reconstrução do mundo é explícita.
* Sistemas deixam de precisar “corrigir” estado.
* Logs e QA passam a validar estados finais, não transições frágeis.
* Facilita testes, replay e reconstrução de mundo.

### Consequências negativas / custos assumidos

* Reset pode ser mais custoso computacionalmente do que mutação parcial.
* Sistemas precisam ser projetados para lifecycle completo (spawn/despawn).
* Não é possível “consertar” sistemas legados sem reescrevê-los.

Esses custos são **aceitos intencionalmente** em troca de previsibilidade e clareza arquitetural.

---

## Alternativas Consideradas (e Rejeitadas)

### Reset por `Reset()` em componentes

Rejeitado por:

* ordem implícita,
* dependência de estado prévio,
* alto risco de resíduos.

### Reset por pause/freeze (TimeScale, física)

Rejeitado por:

* não limpar estado lógico,
* mascarar bugs em vez de resolvê-los.

### Reset híbrido (parte despawn, parte reset)

Rejeitado por:

* ambiguidade,
* aumento exponencial de casos especiais,
* dificuldade de manutenção.

---

## Diretrizes Derivadas

A partir desta decisão:

* Spawn e despawn são conceitos de primeira classe.
* Nenhum sistema deve assumir existência permanente de atores.
* UI e domínios devem reagir a spawn/despawn, nunca antecipá-los.
* Gate de simulação **não é reset**.
* Reset local (por ator) segue a mesma lógica em escopo reduzido.

---

## Observações Finais

Este ADR é considerado **fundacional**.
Decisões futuras devem ser avaliadas contra ele.

Se uma solução parecer “mais simples” mas violar este ADR, a solução correta é **revisar o design**, não contornar a decisão.

# ADR 002 — Spawn como Pipeline Explícito e Orquestrado

**Status:** Aceito
**Data:** 2025-XX-XX
**Decisores:** Core Architecture
**Relacionados:** ADR-001 (World Reset por Despawn + Respawn)

---

## Contexto

Com o reset do mundo definido como **despawn + respawn**, surge a necessidade de responder:

> Quem cria o mundo, em que ordem, e sob qual contrato?

Em arquiteturas anteriores, o spawn do mundo ocorria de forma implícita (ex.: `Start()` espalhado, `yield return null`, ordem emergente), resultando em dependências ocultas, ordem frágil e baixa auditabilidade.

---

## Decisão

A criação do mundo ocorre por meio de um **pipeline explícito de spawn**, orquestrado e auditável.

Formalmente:

- Não existe spawn implícito de entidades relevantes.
- Não existe criação de mundo em `Start()` sem participação no pipeline.
- A ordem de spawn é **declarada**, não emergente.

---

## Estrutura Conceitual

### Spawn Service

Cada grupo lógico de entidades é controlado por um **Spawn Service** que:

- conhece apenas **o que ele cria**;
- não conhece outros serviços;
- não decide ordem global.

Contrato conceitual mínimo:

- Estados: `NotSpawned → Spawning → Spawned`
- Operações: `Spawn()`, `Despawn()`
- Sinais: `SpawnStarted`, `SpawnCompleted`, `DespawnCompleted`

> Spawn Service não é gameplay; é infraestrutura de mundo.

### World Spawn Orchestrator

Existe um orquestrador explícito que:

- define ordem de spawn;
- invoca `Spawn/Despawn`;
- aguarda conclusão;
- emite logs de fase;
- falha de forma previsível.

A ordem é **documentada e única** (ex.: Planetas → Players → NPCs).

---

## Consequências

### Positivas
- Ordem rastreável.
- Logs auditáveis.
- Testes podem validar fases isoladas.
- Reset reutiliza o mesmo pipeline.
- Elimina dependência de timing de frame.

### Custos assumidos
- Mais estrutura inicial.
- Proibição de atalhos.
- Disciplina arquitetural.

---

## Alternativas rejeitadas
- Spawn implícito via `Start()`
- Spawn descentralizado por sistemas
- Orquestração parcial (exceções)

---

## Diretrizes derivadas
- Todo ator relevante nasce via Spawn Service.
- Nenhum sistema assume que o mundo já está pronto.
- UI e domínios reagem a spawn/despawn.
- Reset global reutiliza exatamente o mesmo pipeline.

# ADR 003 — Escopos de Serviço e Ciclo de Vida

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-001, ADR-002

---

## Contexto

Problemas recorrentes em projetos anteriores:

- serviços vivendo tempo demais;
- acesso fora do escopo correto;
- “singletons de gameplay” globais;
- vazamento de estado entre cenas e resets.

Isso ocorre quando escopo e ciclo de vida não são decisões explícitas.

---

## Decisão

O projeto adota **três escopos explícitos de serviço**, com regras rígidas.

### Escopos

| Escopo | Responsabilidade |
| --- | --- |
| **Global** | Infraestrutura pura, sem estado de mundo |
| **Scene** | Estado do mundo atual |
| **Actor (ActorId)** | Estado individual de um ator |

### Regras

- Nenhum serviço de gameplay pode existir no escopo global.
- Serviços de cena são destruídos integralmente no despawn do mundo.
- Serviços de ator não sobrevivem ao despawn do ator.
- Um serviço não pode acessar escopos acima do seu (ator → cena → global).

---

## Consequências
- Reset não vaza estado.
- Dependências previsíveis.
- Ciclo de vida claro.
- Testes determinísticos.

---

## Diretrizes derivadas
- Infraestrutura no Global
- Mundo no Scene
- Ator no ActorId

# ADR 004 — Domínios Não Controlam Ciclo de Vida

**Status:** Rascunho (arquivo formalizado depois)
**Data:** 2025-XX-XX
**Relacionados:** ADR-001, ADR-002, ADR-003

---

## Intenção

Este ADR existe como princípio já adotado na arquitetura, porém o texto formal completo ainda será consolidado em arquivo próprio.

### Decisão (resumo)

- **Domínios não criam, não despawnam e não “mantêm vivos” atores.**
- Domínios reagem a eventos de spawn/despawn e operam sobre o estado disponível no escopo correto.
- O ciclo de vida é responsabilidade do **WorldLifecycle + Spawn Pipeline**.

### Motivo

Evitar acoplamento indireto, dependências ocultas e violações do reset determinístico (ADR-001).

---

## Próximo passo
Consolidar o texto completo quando a camada global (coordenação Scene Flow ↔ WorldLifecycle ↔ GameLoop) estiver estabilizada.

# ADR 005 — Gate de Simulação Não é Reset

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-001

---

## Contexto

Pause, freeze e reset foram historicamente confundidos, levando a:

- física pausada mas lógica ativa;
- sistemas parcialmente congelados;
- resets inconsistentes.

---

## Decisão

O **Gate de Simulação**:

- bloqueia execução de sistemas cooperativos;
- **não pausa física**;
- **não altera TimeScale**;
- **não limpa estado**.

Reset global é sempre **despawn + respawn** (ADR-001).

---

## Consequências
- Gate vira ferramenta segura de orquestração.
- Pause/loading/transições ficam previsíveis.
- Reset deixa de ser implícito.

---
# ADR 006 — UI Reage ao Mundo, Nunca o Antecede

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-002

---

## Contexto

Problemas recorrentes ocorreram quando:

- UI assumia atores existentes;
- bindings quebravam no reset;
- HUD precisava “se proteger”.

---

## Decisão

A UI:

- nunca assume que um ator existe;
- reage a eventos de spawn/despawn;
- é reconstruída ou rebindada quando necessário.

Ordem válida:

1. Spawn do ator
2. Criação de contexto de runtime
3. Bind de UI

Nunca o inverso.

---

## Consequências
- UI sobrevive a reset.
- HUD deixa de ser frágil.
- Testes ficam mais simples.

---

# ADR 007 — Testes Validam Estados Finais, Não Transições Frágeis

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** Todos

---

## Contexto

Testes anteriores falhavam por:

- depender de frames;
- depender de ordem implícita;
- validar passos internos instáveis.

---

## Decisão

Testes validam:

- estado final do mundo;
- presença/ausência de atores;
- consistência após reset.

Testes **não** validam:

- frames intermediários;
- ordens internas não contratuais.

---

## Consequências
- Menos flakiness.
- QA mais objetivo.
- Logs e checklists focam em invariantes finais.

---

## Conjunto de ADRs — Status Final

| ADR | Tema                                 | Status |
| --- | ------------------------------------ | ------ |
| 001 | World Reset por Respawn              | ✅      |
| 002 | Spawn como Pipeline                  | ✅      |
| 003 | Escopos de Serviço                   | ✅      |
| 004 | Domínios não controlam ciclo de vida | ✅      |
| 005 | Gate ≠ Reset                         | ✅      |
| 006 | UI Reativa                           | ✅      |
| 007 | Testes por estado final              | ✅      |

---

## Resultado

Neste ponto:

* A arquitetura está **formalmente fechada**.
* O projeto pode ser recriado do zero sem ambiguidade.
* Qualquer decisão futura pode ser avaliada contra os ADRs.

---

### Próximo passo natural (quando quiser)

* **Checklist de Setup do Projeto (Commit 0 → Commit 3)**
* ou **Mapa de Pastas + Naming Convention alinhado aos ADRs**

Quando quiser continuar, basta dizer qual deles.

---

## Addendum (2025-12-24) — Gatilho via SceneFlow

### Gatilho em produção (SceneFlow)
Em produção, o “hard reset por respawn” não é disparado pelo GameLoop diretamente.
O gatilho padrão é:

1. `SceneTransitionService` conclui load/unload e emite `SceneTransitionScenesReadyEvent`.
2. `WorldLifecycleRuntimeDriver` decide se executa o reset (perfís de gameplay) ou se faz skip (startup/Menu).
3. O `WorldLifecycleController` executa `ResetWorldAsync()` e o sistema emite `WorldLifecycleResetCompletedEvent` para coordenação.
