# OLD BIRA'S FARM — FRAMEWORK INTEGRATION POLICY

## Objetivo

Este documento define a fronteira obrigatória entre o remake de Old Bira's Farm e o framework localizado em `Assets/_ImmersiveGames/NewScripts`.

## Regra Principal

O framework e seu baseline são dependências somente leitura para o trabalho de Old Bira's Farm.

Nenhuma implementação do remake deve alterar arquivos, contratos, assets, configurações ou documentação pertencentes ao framework. A manutenção e a evolução do framework são responsabilidade exclusiva de seu proprietário.

## Escopo de Trabalho

Implementações, integrações, adapters, configurações, assets e documentação específicos do remake devem permanecer sob:

```text
Assets/_ImmersiveGames/Old Biras Farm
```

O remake deve consumir os contratos públicos e respeitar os owners, pipelines, stages, adapters, endpoints e lifecycles definidos pelo framework.

Não é permitido contornar uma limitação do framework por meio de:

- cópia ou alteração de código interno do framework;
- dependência de detalhes internos não públicos;
- service locators, fallbacks silenciosos ou bridges paralelas;
- duplicação de ownership ou lifecycle;
- patches no baseline para atender somente ao remake.

## Necessidades ou Bloqueios do Framework

Quando uma funcionalidade do remake exigir mudança ou extensão no framework, nenhuma alteração deve ser feita diretamente.

A necessidade deve ser documentada para discussão com o proprietário do framework, contendo:

1. necessidade funcional do remake;
2. módulo, contrato ou boundary envolvido;
3. comportamento atual e motivo do bloqueio;
4. extensão ou contrato esperado no framework;
5. impacto e critérios de aceite;
6. alternativa temporária, quando existir, desde que não viole a arquitetura.

A implementação dependente deve permanecer bloqueada até que a decisão seja tomada e, quando aplicável, a mudança seja entregue pelo proprietário do framework.

## Regra para Novos Assets e Sistemas

Assets e sistemas trazidos gradualmente do Old Bira original devem ser tratados como fonte de intenção funcional. Eles devem ser adaptados à arquitetura atual, e não transplantados automaticamente com owners, managers, eventos ou dependências legadas.

Antes de cada integração:

1. identificar a intenção funcional;
2. localizar o owner e o contrato público corretos no framework;
3. implementar a adaptação exclusivamente no escopo de Old Bira's Farm;
4. documentar qualquer incompatibilidade ou dependência ausente.

## Autoridade

Esta política é obrigatória para todo trabalho realizado no remake. Em caso de dúvida, preservar o framework sem alterações e registrar a necessidade para decisão externa.
