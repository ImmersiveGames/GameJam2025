# ADR-0041: Camada Canônica de Save com Backend Trocável e Backend Local Simples para Testes

## Status
- Estado: Aceito
- Implementação: Não iniciada
- Escopo: camada canônica de Save, contratos de aplicação, ports/adapters de backend e observabilidade mínima do módulo

## Contexto

O projeto não deve depender de uma tecnologia concreta de save como contrato principal do jogo.
O caminho atual precisa permitir evolução incremental, testes locais simples e troca futura do backend sem reescrever o jogo.

O risco a evitar é duplo:

- acoplar o jogo a uma solução concreta de persistência;
- transformar o Save em dono de fluxos que pertencem a outros módulos.

O módulo de Save precisa nascer como uma camada de aplicação e orquestração, com API estável e backend substituível.
Isso exige separar o que é contrato canônico do jogo do que é detalhe de infraestrutura.

## Decisão

### 1. O Save passa a ter uma camada canônica no projeto

O projeto passa a depender de uma camada canônica de Save, e não de uma tecnologia concreta.
Essa camada é o ponto de entrada estável para persistência e coordenação de estado salvo.

O Save canônico é responsável por orquestrar persistência, não por varrer o mundo nem por concentrar regra de domínio de outros módulos.

### 2. O backend do Save é trocável por ports/adapters

O contrato principal do Save deve ser exposto por abstrações estáveis, com backend intercambiável por meio de ports/adapters.

Regras:

- o jogo consome a API canônica do Save;
- o backend concreto não pode ser source-of-truth do contrato público;
- a troca do backend futuro não pode exigir reescrita do jogo;
- dependências obrigatórias de persistência devem falhar de forma explícita quando ausentes ou inválidas.

### 3. O backend simples aprovado é provisório e de infraestrutura

Para testes e evolução incremental, fica aprovado um backend simples:

- `PlayerPrefs` para preferências;
- `JSON` para progressão persistente.

Esse backend é provisório e de infraestrutura.
Ele não define o contrato principal do sistema e não deve ser tratado como o core definitivo de Save.

### 4. O contrato de Save separa Preferences, Progression e Checkpoint

O modelo canônico do Save deve separar conceitualmente três domínios de persistência:

- `Preferences`: configurações e preferências do jogador, de atualização leve e previsível;
- `Progression`: estado persistente de avanço, campanha, desbloqueios e progresso durável;
- `Checkpoint`: estado de retomada de sessão, previsto no contrato conceitual, mas fora da implementação inicial deste ciclo.

Essa separação é obrigatória no desenho do contrato.
O fato de `Checkpoint` estar fora da implementação inicial não autoriza misturar sua semântica com `Preferences` ou `Progression`.

### 5. O contrato precisa carregar `profileId` e `slotId`

A API canônica do Save deve considerar explicitamente `profileId` e `slotId`.

Regras:

- `profileId` identifica o perfil lógico do jogador;
- `slotId` identifica o slot ou fatia persistente usada no contrato;
- ambos devem existir como parte do contrato, mesmo que o backend local inicial os trate de forma simples;
- a ausência de `profileId` ou `slotId` em um fluxo que os exija é erro de configuração ou contrato.

### 6. Autosave só ocorre em marcos estáveis e explícitos

O Save só pode disparar autosave em marcos estáveis e explicitamente declarados pelo jogo.

Não é permitido:

- acoplamento oportunista com estado arbitrário de cena;
- inferência de persistência por condição local não canônica;
- autosave provocado por observação genérica e indiscriminada do mundo.

O Save responde a hooks e intents canônicos, e não a ruído de runtime.

### 7. O Save não é dono do pipeline macro do jogo

O módulo de Save não pode virar dono de:

- `SceneFlow`;
- `WorldLifecycle`;
- `GameLoop`;
- loading e fade;
- navegação;
- reset.

Esses fluxos pertencem aos seus respectivos owners.
O Save apenas observa hooks canônicos e persiste de forma desacoplada.

### 8. A composição do snapshot vem de contributors/provedores de domínio

A composição do snapshot persistente não deve vir de varredura genérica do mundo.

O modelo correto é:

- cada domínio relevante expõe contribuidores ou provedores explícitos de snapshot;
- o Save agrega apenas o que foi fornecido por contratos conhecidos;
- a ausência de contributor obrigatório deve falhar de forma explícita quando o cenário exigir persistência daquele domínio.

Esse desenho preserva ownership explícito e evita acoplamento a hierarquias arbitrárias de cena.

### 9. Observabilidade mínima do Save é obrigatória

O módulo deve expor observabilidade mínima e canônica.

No mínimo:

- início e fim de operações de save;
- perfil e slot usados;
- backend selecionado;
- tipo de persistência envolvida (`Preferences`, `Progression`, `Checkpoint`, quando aplicável);
- motivo de autosave ou de skip;
- falha de configuração ou de contrato;
- falha de backend;
- sucesso consolidado da operação.

A observabilidade deve ser suficiente para auditoria e suporte de regressão.
Ela não deve ser substituída por logs informais ou efeitos colaterais implícitos.

### 10. Fail-fast continua sendo a política padrão

Configuração obrigatória ausente, backend inválido, slot inválido ou contributor essencial ausente devem falhar de forma explícita.

O Save não deve inventar fallback silencioso para corrigir contrato quebrado.
Fallback silencioso só seria aceitável se futuramente for aprovado por ADR específico.

## Fora de escopo

Fica fora de escopo neste ciclo:

- definir o core definitivo de Save;
- implementar checkpoint/resume;
- acoplar o Save a `SceneFlow`, `WorldLifecycle`, `GameLoop`, loading/fade, navegação ou reset;
- fazer varredura genérica do mundo para montar snapshot;
- criar fallback silencioso para configuração ausente;
- migrar o jogo para um backend remoto ou cloud;
- redesenhar outros módulos para torná-los dependentes do Save.

## Consequências

- O jogo passa a ter um contrato canônico de Save independente da tecnologia concreta.
- O backend pode ser trocado depois sem reescrever o consumo do jogo.
- O backend simples `PlayerPrefs` + `JSON` viabiliza teste local e evolução incremental.
- `Preferences`, `Progression` e `Checkpoint` deixam de ser tratados como a mesma coisa.
- O risco de o Save assumir ownership indevido de fluxos macro fica reduzido.
- A persistência passa a depender de contributors explícitos, com ownership mais auditável.
- A observabilidade do Save melhora, o que facilita validação e diagnóstico.

## Notas de implementação

- A primeira implementação deve manter a camada de aplicação estável e isolar o backend simples atrás de abstrações.
- O backend simples deve ser tratado como infraestrutura substituível, não como contrato semântico final.
- A separação entre `Preferences` e `Progression` deve existir desde o início do desenho.
- `Checkpoint` deve aparecer como parte conceitual do contrato, mas sem comportamento operacional inicial.
- A composição de snapshot deve aceitar apenas contribuidores explícitos, com fail-fast para dependências obrigatórias.
- A orquestração de autosave deve reagir apenas a marcos estáveis, definidos por intent canônico.

## Evidências

- `ADR-0038` formaliza composição modular, fail-fast e fronteira entre owner do módulo e root global.
- `ADR-0031` define pipeline canônico de transição macro e reforça separação de responsabilidades.
- `ADR-0033` trata a resiliência de loading e fade como fronteira própria, não como responsabilidade transversal genérica.
- `ADR-0034` reforça boundaries entre domínio, apresentação e owners explícitos.
- `ADR-0035` consolida ownership canônico por cluster de módulos.
- `ADR-0040` estabelece leitura canônica e hook oficial por módulo, padrão compatível com Save observando intents e marcos explícitos.

## Referências

- `ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`
- `ADR-0033-Resiliencia-Canonica-de-Fade-e-Loading-no-Transito-Macro.md`
- `ADR-0034-Actor-Presentation-Domain-Intent-and-Boundaries.md`
- `ADR-0035-Ownership-Canônico-dos-Clusters-de-Módulos-NewScripts.md`
- `ADR-0038-Modular-DI-Registration-and-Module-Installers.md`
- `ADR-0040-InputModes-Estado-Canonico-e-Hook-Oficial.md`
