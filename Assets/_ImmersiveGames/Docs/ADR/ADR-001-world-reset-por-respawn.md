
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

**Status:** Proposto → Aceito
**Data:** 2025-XX-XX
**Decisores:** Core Architecture
**Relacionados:** ADR-001 (World Reset por Respawn)

---

## Contexto

Com o reset do mundo definido como **despawn + respawn**, surge a necessidade de responder a uma pergunta fundamental:

> **Quem cria o mundo, em que ordem, e sob qual contrato?**

Em arquiteturas anteriores, o spawn do mundo ocorria de forma implícita:

* via `Start()` espalhado,
* dependência de `yield return null`,
* ordem emergente baseada em execução de scripts,
* side-effects (ex.: planeta existir “porque alguém precisou dele”).

Isso resultou em:

* dependências ocultas entre sistemas,
* ordem frágil e difícil de auditar,
* bugs que desapareciam ou surgiam conforme o frame,
* impossibilidade de testar fases isoladas do mundo.

---

## Decisão

O projeto adota a seguinte decisão arquitetural:

> **A criação do mundo ocorre por meio de um pipeline explícito de spawn, orquestrado e auditável.**

Formalmente:

* Não existe spawn implícito de entidades relevantes.
* Não existe criação de mundo em `Start()` sem participação no pipeline.
* A ordem de spawn é **declarada**, não emergente.

O mundo é criado por um **World Spawn Pipeline**, composto por fases ordenadas.

---

## Estrutura Conceitual

### Spawn Service

Cada grupo lógico de entidades do mundo é controlado por um **Spawn Service**, que:

* conhece apenas **o que ele cria**,
* não conhece outros serviços,
* não decide ordem global.

Contrato conceitual mínimo:

* Estados: `NotSpawned → Spawning → Spawned`
* Operações:

    * `Spawn()`
    * `Despawn()`
* Eventos:

    * `SpawnStarted`
    * `SpawnCompleted`
    * `DespawnCompleted`

O Spawn Service **não é gameplay**, é infraestrutura de mundo.

---

### World Spawn Orchestrator

Existe um **orquestrador explícito**, responsável por:

* definir a ordem de spawn,
* invocar Spawn/Despawn,
* aguardar conclusão,
* emitir logs de fase,
* falhar de forma previsível.

Ele **não sabe o conteúdo interno** de cada spawn.

Exemplo conceitual de ordem:

1. Planetas (definem o espaço/fase)
2. Players
3. NPCs / Atores dinâmicos

Essa ordem é **documentada e única**.

---

## Consequências

### Positivas

* Ordem de criação do mundo é rastreável.
* Logs permitem auditoria completa de bootstrap.
* Testes podem validar fases isoladas.
* Reset reutiliza exatamente o mesmo pipeline.
* Elimina dependência de frame timing.

### Custos Assumidos

* Mais código estrutural inicial.
* Proibição de atalhos “rápidos”.
* Necessidade de disciplina arquitetural.

Esses custos são aceitos para garantir previsibilidade.

---

## Alternativas Rejeitadas

### Spawn implícito via Start()

Rejeitado por:

* ordem não garantida,
* dependência de engine,
* difícil de testar.

### Spawn descentralizado por sistemas

Rejeitado por:

* acoplamento indireto,
* dificuldade de reset global.

### Orquestração parcial

Rejeitado por:

* introduzir exceções,
* quebrar a uniformidade do pipeline.

---

## Diretrizes Derivadas

A partir deste ADR:

* Todo ator relevante nasce via Spawn Service.
* Nenhum sistema assume que “o mundo já está pronto”.
* Domínios escutam eventos de spawn, não criam entidades.
* UI só se liga após conclusão de spawn.
* Reset global reutiliza exatamente o mesmo pipeline.

---

## Observações Finais

Este ADR define **como o mundo passa a existir**.
Ele complementa o ADR-001 e juntos formam o núcleo da arquitetura.

Se algo precisa “existir antes” sem passar por spawn, isso **não é parte do mundo**, é infraestrutura.

---

## Próximo passo sugerido (ordem correta)

Agora existem duas bifurcações possíveis, ambas válidas:

1. **ADR 003 — Escopos de Serviços e Ciclo de Vida (Global / Scene / Actor)**
2. **Checklist de Setup do Projeto (Commit 0 / Commit 1 / Commit 2)**

Minha recomendação arquitetural: **ADR 003 primeiro**, para fechar o contrato de DI antes de qualquer checklist.

Confirme e seguimos.

# ADR 003 — Escopos de Serviço e Ciclo de Vida

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-001, ADR-002

---

## Contexto

Projetos anteriores apresentaram:

* serviços vivendo tempo demais,
* serviços sendo acessados fora do escopo correto,
* “singletons de gameplay” globais,
* vazamento de estado entre cenas e resets.

Esses problemas surgem quando **escopo e ciclo de vida não são decisões explícitas**.

---

## Decisão

O projeto adota **três escopos explícitos de serviço**, com regras rígidas.

### Escopos

| Escopo              | Responsabilidade                         |
| ------------------- | ---------------------------------------- |
| **Global**          | Infraestrutura pura, sem estado de mundo |
| **Scene**           | Estado do mundo atual                    |
| **Actor (ActorId)** | Estado individual de um ator             |

---

### Regras

* Nenhum serviço de gameplay pode existir no escopo global.
* Serviços de cena **são destruídos integralmente** no despawn do mundo.
* Serviços de ator **não sobrevivem ao despawn do ator**.
* Um serviço **não pode acessar escopos acima do seu** (ator → cena → global).

---

## Consequências

* Reset não vaza estado.
* Dependências ficam previsíveis.
* Serviços passam a ter ciclo de vida claro.
* Testes tornam-se determinísticos.

---

## Alternativas Rejeitadas

* Gameplay singletons globais
* Serviços “híbridos” (meio global, meio mundo)
* Acesso livre entre escopos

---

## Diretrizes Derivadas

* Infraestrutura no Global
* Mundo no Scene
* Ator no Object/ActorId

---

# ADR 005 — Gate de Simulação Não é Reset

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-001

---

## Contexto

Pause, freeze e reset foram historicamente confundidos, levando a:

* física pausada mas lógica ativa,
* sistemas parcialmente congelados,
* resets inconsistentes.

---

## Decisão

O **Gate de Simulação**:

* bloqueia execução de sistemas cooperativos,
* **não pausa física**,
* **não altera TimeScale**,
* **não limpa estado**.

Reset é sempre despawn + respawn (ADR-001).

---

## Consequências

* Gate vira ferramenta segura de orquestração.
* Pause, loading e transições ficam previsíveis.
* Reset deixa de ser implícito.

---

# ADR 006 — UI Reage ao Mundo, Nunca o Antecede

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** ADR-002

---

## Contexto

Problemas recorrentes ocorreram quando:

* UI assumia atores existentes,
* bindings quebravam no reset,
* HUD precisava “se proteger”.

---

## Decisão

A UI:

* **nunca assume que um ator existe**,
* reage a eventos de spawn/despawn,
* é reconstruída ou rebindada conforme necessário.

Ordem válida:

1. Spawn ator
2. Criar contexto de runtime
3. Bind UI

Nunca o inverso.

---

## Consequências

* UI sobrevive a reset.
* HUD deixa de ser frágil.
* Testes ficam mais simples.

---

# ADR 007 — Testes Validam Estados Finais, Não Transições Frágeis

**Status:** Aceito
**Data:** 2025-XX-XX
**Relacionados:** Todos

---

## Contexto

Testes anteriores falhavam por:

* depender de frames,
* depender de ordem implícita,
* validar passos internos instáveis.

---

## Decisão

Testes validam:

* estado final do mundo,
* presença/ausência de atores,
* consistência após reset.

Não validam:

* frames intermediários,
* ordens internas não contratuais.

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
