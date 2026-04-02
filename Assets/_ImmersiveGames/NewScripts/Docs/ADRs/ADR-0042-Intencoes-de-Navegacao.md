# ADR-0042 — Intenções de Navegação

## Status atual do runtime

- Leitura prática: parcial / em formalização.
- O runtime atual já executa a distinção entre intenção, rota e dispatch; este ADR formaliza a leitura canônica, mas ainda está em Draft.

## Status
- Estado: Draft
- Data: 2026-03-28
- Tipo: Domínio / Navegação

## Dependência

Este ADR depende diretamente do **ADR-0001 — Glossário Fundamental de Contextos e Rotas**.

As definições de:
- Contexto Macro;
- Contexto Local;
- Rota;
- Rota Macro;
- Rota Local;
- Intenção de Navegação;
- Contexto Visual de Frontend

são consideradas pré-requisitos obrigatórios para este documento.

## Contexto

O projeto já possui conceitos de navegação, rotas, catálogo e dispatch, mas ainda havia ambiguidade entre:
- o pedido de mudança;
- a rota que executa essa mudança;
- o contexto resultante;
- e as validações necessárias para permitir essa transição.

Sem essa distinção, a navegação tende a misturar:
- regra de negócio;
- configuração de catálogo;
- pipeline técnico;
- e estado da aplicação.

## Decisão

O projeto passa a tratar **Intenção de Navegação** como o pedido semântico de mudança de contexto.

Ela não é:
- a rota em si;
- o pipeline técnico em si;
- a transição em si;
- nem a garantia de que a mudança pode ser executada.

Ela é apenas o pedido semântico que será avaliado e resolvido pelo sistema.

## Definição operacional

Uma Intenção de Navegação responde à pergunta:

**“O que eu quero que aconteça agora?”**

Exemplos:
- ir para gameplay;
- voltar para menu;
- abrir options;
- reiniciar;
- avançar para o próximo contexto local;
- sair para victory;
- sair para defeat;
- sair do jogo.

## O que a Intenção faz

A Intenção de Navegação:
- expressa um pedido do domínio;
- aciona a avaliação do sistema;
- permite que a navegação escolha a rota correta, quando existir;
- pode resultar em mudança de Contexto Macro, Contexto Local ou Contexto Visual de Frontend.

## O que a Intenção não faz

A Intenção de Navegação não deve:
- carregar toda a configuração técnica da mudança;
- virar dono do pipeline de transição;
- substituir a definição de rota;
- substituir o conceito de contexto;
- virar repositório de áudio, loading, reset ou composição de cena.

## Relação entre Intenção e Rota

A relação oficial do domínio passa a ser:

- **Intenção** = o que eu quero;
- **Rota** = como isso será executado.

Portanto:
- uma intenção pode ser resolvida em uma rota concreta;
- uma intenção pode resultar em mudança de contexto sem exigir rota explícita;
- a existência de uma intenção não implica, por si só, a existência obrigatória de uma rota.

## Relação entre Intenção e Catálogo de Navegação

O **Catálogo de Navegação** é o lugar onde a Intenção de Navegação encontra:
- a rota concreta, quando existir;
- o estilo/transição aplicável, quando existir;
- as regras mínimas de resolução necessárias para executar a mudança.

O catálogo não é o dono do desejo do domínio.
Ele é o lugar onde o pedido semântico é associado ao caminho concreto de execução.

## Regra de resolução

A resolução de uma Intenção de Navegação deve seguir este princípio:

1. receber o pedido semântico;
2. validar se o pedido é válido no estado atual;
3. resolver a rota aplicável, quando existir;
4. aplicar estilo, transição e pipeline necessários;
5. produzir a mudança de contexto resultante.

## Casos sem rota explícita

Nem toda Intenção de Navegação precisa necessariamente resultar em uma rota formalizada.

Exemplos típicos:
- mudanças de Contexto Macro sem troca estrutural completa de cenas;
- mudanças internas de frontend;
- mudanças de contexto cujo pipeline não precise ser modelado como rota dedicada.

Nesses casos, a Intenção continua válida como pedido semântico, mesmo sem rota explícita própria.

## Consequências

### Positivas
- separa melhor desejo, execução e estado resultante;
- evita usar rota como sinônimo de pedido do domínio;
- reduz confusão entre catálogo, rota e contexto;
- prepara melhor a discussão futura de Navigation, Audio e Frontend.

### Trade-offs
- alguns fluxos existentes podem revelar mistura antiga entre intenção e rota;
- o catálogo de navegação precisará ser lido com mais disciplina conceitual.

## Exclusões explícitas

A Intenção de Navegação não é:
- slot;
- asset de contexto;
- configuração de áudio;
- contrato de BGM;
- catálogo visual de frontend.

## Próximos passos
- esclarecer oficialmente o papel do Catálogo de Navegação;
- esclarecer a precedência por dimensão, especialmente para BGM e conteúdo/cenas;
- revisar os ADRs futuros de Navigation/Audio já usando esta definição.
