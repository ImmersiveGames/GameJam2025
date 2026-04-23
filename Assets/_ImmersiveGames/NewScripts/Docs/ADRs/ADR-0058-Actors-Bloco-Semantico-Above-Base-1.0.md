# ADR-0058 - ActorsSystem como owner canonico do conjunto de actors acima da Base 1.0

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

`ADR-0057` define a Base 1.0 como leitura sistemica principal:
- semantica acima
- seam explicito
- baseline tecnico/macro fino
- executores operacionais consumidores

`ADR-0056` congela o baseline como executor tecnico/macro.
`ADR-0055` congela o seam de traducao semantica para intencao operacional.
`ADR-0054` congela participacao semantica acima da execucao operacional.

No eixo de actors, a formulacao anterior ainda deixava ownership critico fora do modulo semantico, abrindo retorno da gravidade operacional.

## 2. Problema

O modelo anterior estava curto por tres motivos:

1. o owner do conjunto de actors nao estava explicito como sistema de conjunto;
2. recipe, presenca canonica e registry arquitetural ainda podiam ser lidos como detalhes operacionais;
3. `WorldDefinition` continuava tolerado como referencia forte do eixo.

Isso reabre leitura incorreta onde:
- quem executa materializacao passa a parecer owner de quem existe;
- `Spawn` vira pseudo-owner semantico;
- `ActorRegistry` vira detalhe local sem papel arquitetural do conjunto;
- bootstrap/composition volta a carregar ownership por conveniencia.

## 3. Decisao central

Adota-se **ActorsSystem** (plural) como decisao oficial do eixo de actors.

Regra central:
- o plural importa porque o owner e o **conjunto** de actors, nao um actor individual;
- `ActorsSystem` e owner da semantica e da definicao canonica do conjunto;
- `ActorsSystem` nao e executor concreto de spawn/despawn.

`ActorsSystem` passa a ser owner de:
- recipe canonica do conjunto de actors;
- identidade, role e relevancia do conjunto;
- presenca canonica do conjunto;
- registry como boundary arquitetural do eixo;
- definicao de quais actors existem/participam;
- spec canonica de materializacao em alto nivel;
- descricao canonica de como os objetos de actor sao representados no sistema.

`WorldDefinition` sai do modelo alvo.

## 4. Boundary arquitetural

### 4.1 Pertence a `ActorsSystem`

- definitions do conjunto de actors;
- recipes canonicas do conjunto;
- identity/role/relevance;
- presence canonica;
- registry do eixo como boundary arquitetural (contrato e ownership do eixo);
- materialization plan/spec canonica em alto nivel;
- descricao canonica dos objetos de actor.
- governanca do conjunto a partir de definitions do eixo, participacao semantica ja derivada e policies do proprio eixo.

### 4.2 Fica fora de `ActorsSystem`

- instantiate/despawn concretos;
- lifecycle fisico concreto de objetos;
- binding operacional de input;
- binding operacional de camera;
- reset operacional;
- bootstrap macro;
- infraestrutura neutra.

## 5. Papel correto de `Spawn`

`Spawn` e ferramenta operacional.

`Spawn` deve:
- instanciar/despawnar objetos concretos;
- aplicar recipe/spec recebida;
- executar lifecycle fisico de entrada/saida.

`Spawn` nao deve:
- definir quais actors existem;
- definir semantica do conjunto;
- ser owner de prefab/root como fonte canonica do sistema;
- ser owner do conjunto de actors.

## 6. Papel correto de `ActorRegistry`

`ActorRegistry` sobe para o eixo de `ActorsSystem` em termos arquiteturais.

Regra:
- o registry do eixo e boundary arquitetural, nao detalhe de runtime;
- participa da presenca canonica do conjunto;
- implementacoes/adapters operacionais de runtime podem existir abaixo desse boundary;
- runtime registry operacional, isoladamente, nao cumpre sozinho o papel arquitetural do registry do eixo;
- ownership arquitetural do conceito de registry deixa de ser detalhe externo.

## 7. Destino de `WorldDefinition`

`WorldDefinition` e incompativel com o modelo alvo como contrato canonico.

Regra normativa:
- `WorldDefinition` deve ser eliminado do shape final do eixo;
- pode existir apenas como artefato transitorio de migracao/legado;
- nao deve ser tratado como fonte principal de recipe, semantica ou ownership do conjunto.

## 8. Compatibilidade com a Base 1.0

Esta decisao e compativel com a Base 1.0:

- semantica permanece acima;
- seam (`Session Integration`) permanece explicito;
- baseline permanece tecnico/macro fino;
- `Spawn` e demais dominios continuam como consumidores/executores operacionais.
- participacao semantica continua no bloco semantico owner proprio; `ActorsSystem` nao absorve esse ownership.

Leitura obrigatoria:
- `ActorsSystem` acima da execucao operacional;
- `Spawn` abaixo como executor.

## 9. Shape conceitual recomendado

Shape conceitual minimo (nao prescritivo de pasta literal):

```text
ActorsSystem
|- Definitions
|- RosterEnsemble
|- IdentityRoles
|- Presence
|- Registry
|- MaterializationPlanSpec
`- Integration
```

Regras:
- manter modulo fino no sentido de nao executar lifecycle fisico;
- permitir adapters operacionais sem deslocar ownership do eixo.
- governar quem existe, quem participa (a partir da participacao semantica ja derivada), quem importa/relevancia e quem deve ser materializado;
- nao colapsar identidade semantica, presenca canonica e materializacao concreta no mesmo bloco operacional.

## 10. Relacao com a versao anterior do ADR

Esta versao supera explicitamente a formulacao anterior em que:
- `ActorRegistry` permanecia fora do eixo;
- `Spawn` ainda parecia owner forte do conjunto;
- `WorldDefinition` ainda sobrevivia como parte tolerada do modelo.

Esta versao redefine o contrato para ownership completo do conjunto em `ActorsSystem`.

## 11. Consequencias praticas

Positivas:
- ownership do eixo de actors fica completo e auditavel;
- reduz ambiguidade entre semantica e execucao;
- bloqueia retorno de ownership por gravidade operacional.

Trade-offs:
- exige migracao documental e contratual explicita;
- exige transicao controlada para remover `WorldDefinition` do modelo final.

## 12. Fechamento

`ActorsSystem` passa a ser a leitura canonica do eixo de actors.

`WorldDefinition` sai do modelo alvo.

Futuras decisoes de actors devem partir desta leitura:
- conjunto semantico owner em `ActorsSystem`;
- `Spawn` executor operacional;
- ownership nunca decidido por "quem executa hoje".

Decisoes baseadas em gravidade operacional sao desvio arquitetural.

## 13. Estado incremental consolidado (2026-04-22)

Sem alterar a decisao central deste ADR, fica congelado como shape implementado:

- refresh runtime separado em dois trilhos explicitos: participacao semantica e presenca runtime de spawn.
- read model de actors preservado como projecao, sem absorver policy de selecao.
- policy de ator relevante extraida para resolver dedicado (`primary/local/active/first`) separado do `ActorSystemReadModelService`.
- este shape validado e **transitorio** e fica abaixo da ambicao completa deste ADR.
- o `ActorSystem` atual deve ser lido como peca de projecao/consolidacao/read model, nao como owner final do conjunto.

Esses pontos reforcam a regra deste ADR: semantica/policy do conjunto de actors nao deve colapsar no executor operacional.

## 14. Leitura normativa do estado atual

O `ActorSystem` atualmente implementado nao representa o shape final normativo deste ADR.

Leitura correta do estado atual:
- leitura de participacao semantica (entrada);
- leitura de presenca runtime (entrada);
- resolucao de ator relevante (policy);
- manutencao de snapshot/read model (projecao).
- esse papel atual e de projection/read-model service com suporte de consistency/observation.

Leitura proibida:
- confundir essa peca com ownership completo do conjunto de actors.

## 15. Intencao de migracao arquitetural

Direcao normativa:
- nao expandir indefinidamente o `ActorSystem` atual por acumulo;
- estruturar um novo `ActorsSystem` como centro semantico do eixo;
- rebaixar o `ActorSystem` atual para papel auxiliar subordinado ao novo eixo.

Mudanca de centro arquitetural:
- sair de "validar o que existe no runtime";
- ir para "governar o conjunto de actors" acima da execucao operacional.

## 16. Reposicionamento do shape atual

A peca atual pode permanecer no target, subordinada ao owner do eixo, com escopo objetivo:
- projection/read-model service;
- consistency/observation support.

Ela nao deve ser tratada como centro semantico do eixo.

## 17. Anti-leitura proibida

E desvio arquitetural:
- tratar o modulo atual de projecao como cumprimento completo do ADR-0058;
- permitir que `Spawn` ou runtime registry retomem ownership do conjunto por gravidade operacional;
- colapsar identidade semantica, presenca canonica e materializacao concreta em um unico bloco operacional.
