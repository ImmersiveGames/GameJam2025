# ADR-0057 - Base 1.0 como leitura sistemica composta entre Baseline 4.0, Session Integration e camadas semanticas

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## 1. Contexto

`ADR-0055` congela `Session Integration` como seam explicito acima do baseline.
`ADR-0056` congela o Baseline 4.0 como executor tecnico fino.
`ADR-0044` mantem o guarda-chuva do Baseline 4.0 ideal.
`ADR-0045`, `ADR-0046`, `ADR-0047` e `ADR-0052` definem as camadas semanticas e a transformacao acima do baseline.

Esses documentos nao sao concorrentes. Eles descrevem partes complementares do mesmo sistema.

Nome canonico recomendado para o sistema composto: **Base 1.0**.

`Baseline 4.0` continua sendo o nome da camada tecnica. `Base 1.0` e o nome da leitura sistemica que agrega baseline, integracao e semantica.

## 2. Problema

Sem uma leitura composta, o projeto volta a ser organizado por dominio isolado e nao por topologia arquitetural.
Isso reabre espacos para:

- bootstrap bloat
- bridges oportunistas
- costura semantica por conveniencia
- multiplas fontes de intencao
- buckets misturando contracts, runtime, compat e adapters
- regressao de ownership do gameplay para o baseline

Tratar `ADR-0055` e `ADR-0056` como decisoes independentes e insuficiente.
A arquitetura precisa ser lida como um sistema unico com fronteiras complementares.

## 3. Decisao

Adota-se `Base 1.0` como o nome canonico da leitura sistemica composta.

A leitura canonica passa a ser de tres camadas complementares:

1. baseline tecnico fino
2. `Session Integration`
3. camadas semanticas acima do baseline

A composicao dessas camadas e a forma oficial de interpretar o runtime daqui para frente.
`Base 1.0` nao substitui `ADR-0055` nem `ADR-0056`; ela congela como eles se encaixam.

## 4. Camadas do sistema

| Camada | Papel | Nao deve fazer |
|---|---|---|
| Baseline tecnico fino | executar boot, dependency root, `SceneFlow` macro, loading/fade, gates tecnicos, `WorldReset`/`SceneReset`/`ResetInterop`, dispatch macro, `InputModes` request/apply, materializacao e reset operacional | semantica da sessao, selecao de phase, ownership de participacao, politica de continuidade |
| `Session Integration` | consumir verdade semantica canonica e emitir intencao operacional canonica para dominios adjacentes | ownership semantico, execucao concreta de spawn/reset, virar bootstrap por conveniencia |
| Camadas semanticas acima | definir significado, politicas e ordem: `GameplayRuntimeComposition`, `GameplaySessionFlow`, `Session Transition`, phase composition, participation e futuros blocos semanticos | colapsar para o baseline ou para integracao operacional |

## 5. Relacao entre as camadas

- O baseline serve as camadas acima fornecendo infraestrutura tecnica e rails operacionais, nao significado.
- `Session Integration` consome a verdade semantica canonica e emite intencao operacional canonica para `InputModes`, spawn/reset e consumidores de `ActorRegistry`.
- `GameplayRuntimeComposition`, `GameplaySessionFlow`, `Session Transition` e os demais blocos semanticos permanecem acima do baseline como fonte de verdade e de politica.
- Nenhuma dessas camadas deve voltar para bootstraps por conveniencia historica.

## 6. Consequencias

- Futuras refatoracoes devem respeitar a leitura em tres camadas.
- Novos seams nao devem nascer em bootstraps por conveniencia.
- Bootstraps e installers devem permanecer finos e sem costura semantica.
- O baseline nao pode recuperar ownership do gameplay.
- Baseline, integracao e camadas semanticas devem evoluir de forma coordenada.
- Analises futuras de estado atual vs. ideal devem usar `Base 1.0` como referencia estrutural.

## 7. Relacao com `ADR-0055` e `ADR-0056`

- `ADR-0055` continua owner da area de integracao.
- `ADR-0056` continua owner do papel do baseline.
- Este ADR nao substitui nenhum dos dois. Ele apenas congela a leitura sistemica composta que os une.
- `ADR-0044`, `ADR-0045`, `ADR-0047` e `ADR-0052` continuam como antecedentes canonicos que alimentam essa composicao.
